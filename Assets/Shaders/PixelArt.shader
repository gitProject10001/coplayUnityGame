Shader "Hidden/PixelArt"
{
    Properties
    {
        _ColorSteps ("Color Steps", Float) = 16
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _DepthThreshold ("Depth Threshold", Float) = 1.5
        _NormalThreshold ("Normal Threshold", Float) = 0.5
        _PixelScale ("Pixel Scale", Float) = 4
        _EnableOutlines ("Enable Outlines", Float) = 1
        _EnableDithering ("Enable Dithering", Float) = 1
        _ConvexOnly ("Convex Only Outlines", Float) = 0
        _DepthOutlineStrength ("Depth Outline Strength", Float) = 1.0
        _NormalOutlineStrength ("Normal Outline Strength", Float) = 0.8
        _UsePalette ("Use Palette LUT", Float) = 0
        _PaletteTexture ("Palette LUT", 2D) = "white" {}
        _PaletteSize ("Palette Size", Float) = 16
        _EnableFog ("Enable Fog", Float) = 0
        _FogColor ("Fog Color", Color) = (0.12, 0.18, 0.15, 1)
        _FogDensity ("Fog Density", Float) = 2.0
        _FogStart ("Fog Start", Float) = 0.0
        _FogEnd ("Fog End", Float) = 1.0
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
            float _EnableFog;
            float4 _FogColor;
            float _FogDensity;
            float _FogStart;
            float _FogEnd;

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

            // Roberts Cross edge detection on depth buffer
            float DepthEdge(float2 uv, float2 texelSize)
            {
                float d00 = SampleSceneDepth(uv + float2(-texelSize.x, -texelSize.y));
                float d11 = SampleSceneDepth(uv + float2( texelSize.x,  texelSize.y));
                float d01 = SampleSceneDepth(uv + float2(-texelSize.x,  texelSize.y));
                float d10 = SampleSceneDepth(uv + float2( texelSize.x, -texelSize.y));
                return abs(d00 - d11) + abs(d01 - d10);
            }

            // Roberts Cross edge detection on normals buffer
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

                // --- Atmospheric Fog ---
                if (_EnableFog > 0.5)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    #if UNITY_REVERSED_Z
                        rawDepth = 1.0 - rawDepth;
                    #endif
                    float fogRange = max(_FogEnd - _FogStart, 0.001);
                    float fogFactor = saturate((rawDepth - _FogStart) / fogRange);
                    fogFactor = pow(fogFactor, max(_FogDensity, 0.01));
                    color.rgb = lerp(color.rgb, _FogColor.rgb, fogFactor);
                }

                // --- Outlines ---
                if (_EnableOutlines > 0.5)
                {
                    // The render target is already at downscaled resolution.
                    // Use 1 texel offset for edge detection — no extra multiplication needed.
                    float2 texelSize = _BlitTexture_TexelSize.xy;

                    float rawDepthEdge = DepthEdge(uv, texelSize);
                    float rawNormalEdge = NormalEdge(uv, texelSize);

                    // Threshold edges with smoothstep for clean outlines
                    // Tuned for orthographic camera where raw depth is 0-1 linear
                    float depthEdge = smoothstep(_DepthThreshold * 0.001, _DepthThreshold * 0.01, rawDepthEdge);
                    float normalEdge = smoothstep(_NormalThreshold * 0.3, _NormalThreshold * 0.8, rawNormalEdge);

                    // Optional convex-only filter (silhouettes only, no concave creases)
                    if (_ConvexOnly > 0.5)
                    {
                        float centerDepth = SampleSceneDepth(uv);
                        float neighborAvg = (
                            SampleSceneDepth(uv + float2(-texelSize.x, 0)) +
                            SampleSceneDepth(uv + float2( texelSize.x, 0)) +
                            SampleSceneDepth(uv + float2(0, -texelSize.y)) +
                            SampleSceneDepth(uv + float2(0,  texelSize.y))
                        ) * 0.25;
                        #if UNITY_REVERSED_Z
                            bool isConvex = centerDepth > neighborAvg + 0.0001;
                        #else
                            bool isConvex = centerDepth < neighborAvg - 0.0001;
                        #endif
                        if (!isConvex)
                        {
                            depthEdge *= 0.0;
                            normalEdge *= 0.3;
                        }
                    }

                    // Combine edges and apply as dark outlines (like Tunic)
                    float edge = saturate(max(depthEdge * _DepthOutlineStrength, normalEdge * _NormalOutlineStrength));
                    color.rgb = lerp(color.rgb, _OutlineColor.rgb, edge);
                }

                return color;
            }
            ENDHLSL
        }
    }
}
