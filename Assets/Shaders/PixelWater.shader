Shader "Custom/PixelWater"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.3, 0.7, 0.8, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.15, 0.4, 0.95)
        _FoamColor ("Foam/Edge Color", Color) = (0.9, 0.95, 1.0, 1.0)

        [Header(Waves)]
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 0.5
        _WaveFrequency ("Wave Frequency", Range(1, 20)) = 8.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.5)) = 0.05
        _WaveDirection ("Wave Direction", Vector) = (1, 0, 0.5, 0)

        [Header(Depth)]
        _DepthFadeDistance ("Depth Fade Distance", Range(0.1, 10)) = 2.0
        _FoamWidth ("Foam Width", Range(0.01, 1)) = 0.2

        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.1)) = 0.02

        [Header(Surface Lines)]
        _LineFrequency ("Line Frequency", Range(1, 30)) = 12
        _LineThickness ("Line Thickness", Range(0, 0.5)) = 0.1
        _LineColor ("Line Color", Color) = (0.5, 0.8, 0.9, 0.3)

        [Header(Toon)]
        _LightSteps ("Light Steps", Range(2, 6)) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _WaveSpeed;
                float _WaveFrequency;
                float _WaveAmplitude;
                float4 _WaveDirection;
                float _DepthFadeDistance;
                float _FoamWidth;
                float _RefractionStrength;
                float _LineFrequency;
                float _LineThickness;
                float4 _LineColor;
                float _LightSteps;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Vertex wave displacement
                float2 waveDir = normalize(_WaveDirection.xz);
                float wave1 = sin(dot(worldPos.xz, waveDir) * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveAmplitude;
                float wave2 = sin(dot(worldPos.xz, float2(waveDir.y, -waveDir.x)) * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3) * _WaveAmplitude * 0.5;
                worldPos.y += wave1 + wave2;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // --- Depth fade ---
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth;

                #if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
                #endif

                if (unity_OrthoParams.w > 0.5)
                {
                    sceneDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                    float waterDepth = lerp(_ProjectionParams.y, _ProjectionParams.z,
                        1.0 - input.positionCS.z / input.positionCS.w);
                    sceneDepth = sceneDepth - waterDepth;
                }
                else
                {
                    sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams) - input.screenPos.w;
                }

                float depthFade = saturate(sceneDepth / _DepthFadeDistance);

                // --- Water color by depth ---
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFade);
                float waterAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthFade);

                // --- Foam at edges ---
                float foam = 1.0 - saturate(sceneDepth / _FoamWidth);
                waterColor = lerp(waterColor, _FoamColor.rgb, foam);
                waterAlpha = lerp(waterAlpha, 1.0, foam * 0.5);

                // --- Refraction (sample scene behind water with offset) ---
                float2 refractionOffset = float2(
                    sin(_Time.y * _WaveSpeed + input.positionWS.x * _WaveFrequency) * _RefractionStrength,
                    cos(_Time.y * _WaveSpeed * 0.8 + input.positionWS.z * _WaveFrequency) * _RefractionStrength
                );
                half3 sceneColor = SampleSceneColor(screenUV + refractionOffset);
                waterColor = lerp(sceneColor, waterColor, waterAlpha);

                // --- Surface lines (horizontal animated lines for pixel art feel) ---
                float linePattern = sin(input.positionWS.x * _LineFrequency + _Time.y * _WaveSpeed * 2.0)
                                  * sin(input.positionWS.z * _LineFrequency * 0.8 + _Time.y * _WaveSpeed * 1.5);
                float lines = step(1.0 - _LineThickness, linePattern);
                waterColor = lerp(waterColor, _LineColor.rgb, lines * _LineColor.a);

                // --- Toon lighting ---
                float3 normal = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float toon = floor(NdotL * _LightSteps) / _LightSteps;

                waterColor *= lerp(0.6, 1.0, toon) * mainLight.color;
                waterColor += waterColor * 0.2; // Ambient boost

                waterColor = MixFog(waterColor, input.fogFactor);
                return half4(waterColor, saturate(waterAlpha + foam * 0.3));
            }
            ENDHLSL
        }
    }
}
