Shader "Custom/GrassBillboard"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.3, 0.6, 0.2, 1)
        _TipColor ("Tip Color", Color) = (0.5, 0.8, 0.3, 1)
        _BaseMap ("Grass Texture", 2D) = "white" {}
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.15
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.5
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 3.0
        _GrassHeight ("Grass Height", Range(0.1, 3)) = 0.6
        _GrassWidth ("Grass Width", Range(0.05, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        LOD 100
        Cull Off

        Pass
        {
            Name "GrassForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float verticalGradient : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _BaseMap_ST;
                float _AlphaCutoff;
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _GrassHeight;
                float _GrassWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Billboard: face the camera on the Y axis only (keep upright)
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = float3(0, 1, 0); // Keep grass upright

                // Vertex position relative to pivot (bottom center)
                float2 localOffset = input.uv - float2(0.5, 0);
                localOffset.x *= _GrassWidth;
                localOffset.y *= _GrassHeight;

                // Store vertical gradient for color blending
                output.verticalGradient = saturate(input.uv.y);

                // Wind displacement (more at the top)
                float windPhase = worldPos.x * _WindFrequency + worldPos.z * _WindFrequency * 0.7 + _Time.y * _WindSpeed;
                float wind = sin(windPhase) * _WindStrength * output.verticalGradient;

                // Apply billboard offset
                float3 billboardPos = worldPos + camRight * localOffset.x + camUp * localOffset.y;
                billboardPos.x += wind;
                billboardPos.z += wind * 0.5;

                output.positionWS = billboardPos;
                output.positionCS = TransformWorldToHClip(billboardPos);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // Alpha cutoff
                clip(texColor.a - _AlphaCutoff);

                // Blend base to tip color based on vertical gradient
                half3 grassColor = lerp(_BaseColor.rgb, _TipColor.rgb, input.verticalGradient);
                grassColor *= texColor.rgb;

                // Even lighting from base of mesh (prevents dark undersides)
                // Sample shadow at the base world position (y=0 plane)
                float3 basePos = input.positionWS;
                basePos.y = 0; // Use ground level for shadow sampling
                float4 shadowCoord = TransformWorldToShadowCoord(basePos);
                Light mainLight = GetMainLight(shadowCoord);

                // Simple toon lighting with very soft response
                float NdotL = 0.5 + 0.5 * dot(float3(0, 1, 0), mainLight.direction);
                float toon = floor(NdotL * 3.0) / 3.0;
                float shadow = mainLight.shadowAttenuation;

                half3 lit = grassColor * mainLight.color * toon * shadow;
                half3 ambient = grassColor * 0.3;
                half3 finalColor = lit + ambient;

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster for grass to cast shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float _AlphaCutoff;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(tex.a - _AlphaCutoff);
                return 0;
            }
            ENDHLSL
        }

        // DepthNormals pass for outlines
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull Off

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
