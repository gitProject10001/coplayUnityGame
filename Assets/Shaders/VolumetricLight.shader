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
            Blend One One

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _Intensity;
            float _Steps;
            float _MaxDistance;
            float _Scattering;
            float4 _LightColor;
            float _Density;
            float _NoiseScale;
            float _NoiseStrength;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            // Procedural fullscreen triangle
            Varyings FullscreenVert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                // Generate a fullscreen triangle from 3 vertices
                output.texcoord = float2((vertexID << 1) & 2, vertexID & 2);
                output.positionCS = float4(output.texcoord * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    output.texcoord.y = 1.0 - output.texcoord.y;
                #endif
                return output;
            }

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

            float MiePhase(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * PI * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
            }

            // Procedural canopy occlusion — simulates light filtering through tree cover
            // Projects world position along light direction to create streak patterns
            float canopyOcclusion(float3 worldPos, float3 lightDir)
            {
                // Project position onto a plane perpendicular to the light
                float3 right = normalize(cross(lightDir, float3(0, 1, 0)));
                float3 up = normalize(cross(right, lightDir));
                float2 projected = float2(dot(worldPos, right), dot(worldPos, up));

                // Layer multiple noise octaves for organic canopy gaps
                float n1 = noise3D(float3(projected * 0.8, 0));
                float n2 = noise3D(float3(projected * 1.6, 10));
                float n3 = noise3D(float3(projected * 3.2, 20));
                float canopy = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;

                // Create distinct gaps (rays) vs blocked areas
                return smoothstep(0.35, 0.55, canopy);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                float rawDepth = SampleSceneDepth(uv);

                // Reconstruct world position
                float4 ndcPos = float4(uv * 2.0 - 1.0, rawDepth, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                    ndcPos.y = -ndcPos.y;
                #endif
                float4 worldH = mul(UNITY_MATRIX_I_VP, ndcPos);
                float3 worldPos = worldH.xyz / worldH.w;

                float3 camPos = _WorldSpaceCameraPos;
                float3 rayDir = normalize(worldPos - camPos);
                float totalDist = min(distance(camPos, worldPos), _MaxDistance);

                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                float cosAngle = dot(rayDir, lightDir);
                float phase = MiePhase(cosAngle, _Scattering);

                float stepSize = totalDist / _Steps;
                float accumLight = 0;

                for (float i = 0.5; i < _Steps; i += 1.0)
                {
                    float t = (i / _Steps) * totalDist;
                    float3 samplePos = camPos + rayDir * t;

                    // Real shadow from scene objects
                    float4 shadowCoord = TransformWorldToShadowCoord(samplePos);
                    float shadow = MainLightRealtimeShadow(shadowCoord);

                    // Procedural canopy for god ray streaks
                    float canopy = canopyOcclusion(samplePos, lightDir);

                    float noise = noise3D(samplePos * _NoiseScale + _Time.y * 0.1);
                    float density = _Density * (1.0 + (noise - 0.5) * _NoiseStrength * 2.0);

                    accumLight += shadow * canopy * density * stepSize;
                }

                accumLight *= phase * _Intensity;

                // Soft posterize for pixel art feel
                accumLight = floor(accumLight * 12.0 + 0.5) / 12.0;

                half3 volumetricColor = accumLight * _LightColor.rgb * mainLight.color;
                return half4(volumetricColor, 0);
            }
            ENDHLSL
        }
    }
}
