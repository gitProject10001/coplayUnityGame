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

            // Simple Roberts Cross edge detection on raw depth — works for ortho and perspective
            float DepthEdge(float2 uv, float2 texelSize)
            {
                float d00 = SampleSceneDepth(uv + float2(-texelSize.x, -texelSize.y));
                float d11 = SampleSceneDepth(uv + float2( texelSize.x,  texelSize.y));
                float d01 = SampleSceneDepth(uv + float2(-texelSize.x,  texelSize.y));
                float d10 = SampleSceneDepth(uv + float2( texelSize.x, -texelSize.y));
                return abs(d00 - d11) + abs(d01 - d10);
            }

            float NormalEdge(float2 uv, float2 texelSize)
            {
                float3 n00 = SampleSceneNormals(uv + float2(-texelSize.x, -texelSize.y));
                float3 n11 = SampleSceneNormals(uv + float2( texelSize.x,  texelSize.y));
                float3 n01 = SampleSceneNormals(uv + float2(-texelSize.x,  texelSize.y));
                float3 n10 = SampleSceneNormals(uv + float2( texelSize.x, -texelSize.y));
                return distance(n00, n11) + distance(n01, n10);
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
                    // Each downscaled pixel covers _PixelScale full-res pixels
                    float2 texelSize = _BlitTexture_TexelSize.xy * _PixelScale;

                    float rawDepthEdge = DepthEdge(uv, texelSize);
                    float rawNormalEdge = NormalEdge(uv, texelSize);

                    // Normal edges are the primary outline driver (reliable)
                    // Depth edges need high thresholds to avoid noise
                    float depthEdge = smoothstep(_DepthThreshold * 0.0005, _DepthThreshold * 0.002, rawDepthEdge);
                    float normalEdge = smoothstep(_NormalThreshold * 0.05, _NormalThreshold * 0.3, rawNormalEdge);

                    // Convex-only: use center depth vs neighbors to detect convexity
                    if (_ConvexOnly > 0.5)
                    {
                        float centerDepth = SampleSceneDepth(uv);
                        float neighborAvg = (
                            SampleSceneDepth(uv + float2(-texelSize.x, 0)) +
                            SampleSceneDepth(uv + float2( texelSize.x, 0)) +
                            SampleSceneDepth(uv + float2(0, -texelSize.y)) +
                            SampleSceneDepth(uv + float2(0,  texelSize.y))
                        ) * 0.25;
                        // Reversed-Z: closer = higher depth value. Convex = center closer than neighbors.
                        #if UNITY_REVERSED_Z
                            bool isConvex = centerDepth > neighborAvg + 0.0001;
                        #else
                            bool isConvex = centerDepth < neighborAvg - 0.0001;
                        #endif
                        if (!isConvex)
                        {
                            depthEdge *= 0.0;
                            normalEdge *= 0.15;
                        }
                    }

                    // Depth outlines: darken (ink outline)
                    color.rgb *= (1.0 - _DepthOutlineStrength * depthEdge);

                    // Normal outlines: power blend (subtle edge highlight)
                    color.rgb = pow(abs(color.rgb), 1.0 + _NormalOutlineStrength * normalEdge);

                    // Hard outline for strong depth edges
                    if (depthEdge > 0.5)
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
