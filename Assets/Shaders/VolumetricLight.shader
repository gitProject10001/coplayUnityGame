Shader "Hidden/VolumetricLight"
{
    Properties
    {
        _Intensity ("Intensity", Float) = 0.15
        _Steps ("Ray Steps", Float) = 16
        _MaxDistance ("Max Distance", Float) = 30
        _Scattering ("Scattering", Float) = 0.3
        _LightColor ("Light Color", Color) = (1, 0.95, 0.8, 1)
        _Density ("Density", Float) = 0.5
        _NoiseScale ("Noise Scale", Float) = 5.0
        _NoiseStrength ("Noise Strength", Float) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "VolumetricLight"
            ZTest Always ZWrite Off Cull Off
            Blend One One // Additive blend

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _Steps;
            float _MaxDistance;
            float _Scattering;
            float4 _LightColor;
            float _Density;
            float _NoiseScale;
            float _NoiseStrength;

            // Simple hash noise for volume variation
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x),
                         lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                    lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                         lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y),
                    f.z
                );
            }

            // Mie scattering phase function approximation
            float MiePhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // Reconstruct world position from depth
                float rawDepth = SampleSceneDepth(uv);

                float3 worldPos;
                if (unity_OrthoParams.w > 0.5)
                {
                    // Orthographic reconstruction
                    float4 ndcPos = float4(uv * 2.0 - 1.0, rawDepth, 1.0);
                    #if UNITY_UV_STARTS_AT_TOP
                        ndcPos.y = -ndcPos.y;
                    #endif
                    float4 worldH = mul(UNITY_MATRIX_I_VP, ndcPos);
                    worldPos = worldH.xyz / worldH.w;
                }
                else
                {
                    float4 ndcPos = float4(uv * 2.0 - 1.0, rawDepth, 1.0);
                    float4 worldH = mul(UNITY_MATRIX_I_VP, ndcPos);
                    worldPos = worldH.xyz / worldH.w;
                }

                float3 camPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(worldPos - camPos);
                float totalDist = min(distance(camPos, worldPos), _MaxDistance);

                // Get main light direction
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                // Mie scattering phase
                float cosAngle = dot(rayDir, lightDir);
                float phase = MiePhase(cosAngle, _Scattering);

                // Raymarch through volume
                float stepSize = totalDist / _Steps;
                float accumLight = 0;

                for (float i = 0.5; i < _Steps; i += 1.0)
                {
                    float t = (i / _Steps) * totalDist;
                    float3 samplePos = camPos + rayDir * t;

                    // Sample shadow map at this position
                    float4 shadowCoord = TransformWorldToShadowCoord(samplePos);
                    float shadow = MainLightRealtimeShadow(shadowCoord);

                    // Add noise variation for natural look
                    float noise = noise3D(samplePos * _NoiseScale + _Time.y * 0.1);
                    float density = _Density * (1.0 + (noise - 0.5) * _NoiseStrength * 2.0);

                    // Accumulate light where not in shadow
                    accumLight += shadow * density * stepSize;
                }

                // Apply phase function and intensity
                accumLight *= phase * _Intensity;

                // Posterize the volumetric light for pixel art feel
                accumLight = floor(accumLight * 8.0) / 8.0;

                half3 volumetricColor = accumLight * _LightColor.rgb * mainLight.color;
                return half4(volumetricColor, 0);
            }
            ENDHLSL
        }
    }
}
