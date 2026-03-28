Shader "Hidden/PixelArt"
{
    Properties
    {
        _ColorSteps ("Color Steps", Float) = 16
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _DepthThreshold ("Depth Threshold", Float) = 0.5
        _NormalThreshold ("Normal Threshold", Float) = 0.8
        _PixelScale ("Pixel Scale", Float) = 4
        _EnableOutlines ("Enable Outlines", Float) = 1
        _EnableDithering ("Enable Dithering", Float) = 1
        _ConvexOnly ("Convex Only Outlines", Float) = 1
        _DepthOutlineStrength ("Depth Outline Strength", Float) = 0.8
        _NormalOutlineStrength ("Normal Outline Strength", Float) = 0.6
        _UsePalette ("Use Palette LUT", Float) = 0
        _PaletteTexture ("Palette LUT", 2D) = "white" {}
        _PaletteSize ("Palette Size", Float) = 16
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "PixelArt"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorSteps;
            float4 _OutlineColor;
            float _DepthThreshold;
            float _NormalThreshold;
            float _PixelScale;
            float _EnableOutlines;
            float _EnableDithering;
            float _ConvexOnly;
            float _DepthOutlineStrength;
            float _NormalOutlineStrength;
            float _UsePalette;
            float _PaletteSize;

            TEXTURE2D(_PaletteTexture);
            SAMPLER(sampler_PaletteTexture);

            // Bayer 4x4 ordered dither matrix
            static const float dither[16] = {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            // Get linear depth that works for both ortho and perspective
            float GetLinearDepth(float rawDepth)
            {
                #if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
                #endif
                // For orthographic: depth is already linear in [0,1]
                // _ProjectionParams.x is 1 for ortho, check unity_OrthoParams.w
                if (unity_OrthoParams.w > 0.5)
                {
                    // Orthographic: map raw [0,1] to [near, far]
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }
                else
                {
                    return LinearEyeDepth(rawDepth, _ZBufferParams);
                }
            }

            half3 FindNearestPaletteColor(half3 color)
            {
                float bestDist = 1e10;
                half3 bestColor = color;
                for (int i = 0; i < (int)_PaletteSize; i++)
                {
                    float u = ((float)i + 0.5) / _PaletteSize;
                    half3 palColor = SAMPLE_TEXTURE2D_LOD(_PaletteTexture, sampler_PaletteTexture, float2(u, 0.5), 0).rgb;
                    float dist = distance(color, palColor);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestColor = palColor;
                    }
                }
                return bestColor;
            }

            void DetectEdges(float2 uv, float2 texelSize,
                out float depthEdge, out float normalEdge, out bool isConvex)
            {
                float rawCenter = SampleSceneDepth(uv);
                float3 normalCenter = SampleSceneNormals(uv);
                float linearCenter = GetLinearDepth(rawCenter);

                // 4-texel kernel: up, down, left, right
                float2 offsets[4] = {
                    float2(0, texelSize.y),
                    float2(0, -texelSize.y),
                    float2(-texelSize.x, 0),
                    float2(texelSize.x, 0)
                };

                float depths[4];
                float3 normals[4];
                float depthBiases[4];

                for (int i = 0; i < 4; i++)
                {
                    float2 sampleUV = uv + offsets[i];
                    depths[i] = GetLinearDepth(SampleSceneDepth(sampleUV));
                    normals[i] = SampleSceneNormals(sampleUV);
                    depthBiases[i] = depths[i] - linearCenter;
                }

                // --- Depth edge ---
                // Clamp biases to [0,1] for single-pixel foreground outlines
                float depthDiff = 0;
                for (int j = 0; j < 4; j++)
                {
                    depthDiff += saturate(depthBiases[j]);
                }

                // For orthographic, use a fixed threshold (depth doesn't scale with distance)
                float adjustedThreshold = _DepthThreshold;
                if (unity_OrthoParams.w < 0.5)
                {
                    adjustedThreshold = _DepthThreshold * linearCenter * 0.05;
                }
                depthEdge = smoothstep(adjustedThreshold * 0.3, adjustedThreshold, depthDiff);

                // --- Normal edge ---
                float normalDiff = 0;
                for (int k = 0; k < 4; k++)
                {
                    float sharpness = 1.0 - dot(normalCenter, normals[k]);
                    normalDiff += sharpness;
                }

                // Convex detection via cross product of opposing normals
                float avgDepthBias = (depthBiases[0] + depthBiases[1] + depthBiases[2] + depthBiases[3]) * 0.25;
                float3 crossH = cross(normals[2], normals[3]); // left x right
                float3 crossV = cross(normals[1], normals[0]); // down x up
                float convexity = dot(crossH, normalCenter) + dot(crossV, normalCenter);
                isConvex = (avgDepthBias > 0.001) || (convexity > 0.01);

                normalEdge = smoothstep(_NormalThreshold * 0.3, _NormalThreshold, normalDiff);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);

                // --- Dithering ---
                if (_EnableDithering > 0.5)
                {
                    uint2 pixelCoord = uint2(uv * _BlitTexture_TexelSize.zw);
                    float ditherValue = dither[(pixelCoord.x % 4) + (pixelCoord.y % 4) * 4];
                    color.rgb += (ditherValue - 0.5) * (1.0 / _ColorSteps);
                }

                // --- Posterize ---
                color.rgb = floor(color.rgb * _ColorSteps) / _ColorSteps;

                // --- Palette LUT ---
                if (_UsePalette > 0.5)
                {
                    color.rgb = FindNearestPaletteColor(color.rgb);
                }

                // --- Outlines ---
                if (_EnableOutlines > 0.5)
                {
                    float2 texelSize = _BlitTexture_TexelSize.xy;

                    float depthEdge, normalEdge;
                    bool isConvex;
                    DetectEdges(uv, texelSize, depthEdge, normalEdge, isConvex);

                    if (_ConvexOnly > 0.5 && !isConvex)
                    {
                        depthEdge = 0;
                        normalEdge *= 0.15;
                    }

                    // Depth outlines: darken (ink outline)
                    color.rgb *= (1.0 - _DepthOutlineStrength * depthEdge);

                    // Normal outlines: power blend (subtle edge highlight)
                    color.rgb = pow(abs(color.rgb), 1.0 + _NormalOutlineStrength * normalEdge);

                    // Hard outline for strong depth edges
                    if (depthEdge > 0.6)
                    {
                        color.rgb = lerp(color.rgb, _OutlineColor.rgb, saturate(depthEdge));
                    }
                }

                return color;
            }
            ENDHLSL
        }
    }
}
