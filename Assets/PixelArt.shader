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

            static const float dither[16] = {
                0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
            };

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                // Sample the color
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                
                if (_EnableDithering > 0.5)
                {
                    // Calculate pixel coordinate in the downscaled texture
                    uint2 pixelCoord = uint2(uv * _BlitTexture_TexelSize.zw);
                    float ditherValue = dither[(pixelCoord.x % 4) + (pixelCoord.y % 4) * 4];
                    
                    // Shift color slightly based on dither matrix before posterizing
                    color.rgb += (ditherValue - 0.5) * (1.0 / _ColorSteps);
                }

                // Posterize
                color.rgb = floor(color.rgb * _ColorSteps) / _ColorSteps;

                if (_EnableOutlines > 0.5)
                {
                    float2 texelSize = _BlitTexture_TexelSize.xy;
                    
                    // Roberts Cross for edge detection
                    float2 uv00 = uv + float2(-texelSize.x, -texelSize.y);
                    float2 uv11 = uv + float2(texelSize.x, texelSize.y);
                    float2 uv01 = uv + float2(-texelSize.x, texelSize.y);
                    float2 uv10 = uv + float2(texelSize.x, -texelSize.y);

                    float d00 = LinearEyeDepth(SampleSceneDepth(uv00), _ZBufferParams);
                    float d11 = LinearEyeDepth(SampleSceneDepth(uv11), _ZBufferParams);
                    float d01 = LinearEyeDepth(SampleSceneDepth(uv01), _ZBufferParams);
                    float d10 = LinearEyeDepth(SampleSceneDepth(uv10), _ZBufferParams);

                    float depthDiff = abs(d00 - d11) + abs(d01 - d10);
                    
                    float centerDepth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                    
                    // Scale depth threshold by depth to avoid lines on flat ground far away
                    float adjustedDepthThreshold = _DepthThreshold * centerDepth * 0.05;

                    bool isOutline = false;

                    if (depthDiff > adjustedDepthThreshold)
                    {
                        // Only draw outline on the foreground object (1-pixel width)
                        float avgDepth = (d00 + d11 + d01 + d10) * 0.25;
                        if (centerDepth < avgDepth) 
                        {
                            isOutline = true;
                        }
                    }

                    if (!isOutline)
                    {
                        float3 n00 = SampleSceneNormals(uv00);
                        float3 n11 = SampleSceneNormals(uv11);
                        float3 n01 = SampleSceneNormals(uv01);
                        float3 n10 = SampleSceneNormals(uv10);

                        float normalDiff = distance(n00, n11) + distance(n01, n10);
                        if (normalDiff > _NormalThreshold)
                        {
                            isOutline = true;
                        }
                    }

                    if (isOutline)
                    {
                        return _OutlineColor;
                    }
                }
                
                return color;
            }
            ENDHLSL
        }
    }
}
