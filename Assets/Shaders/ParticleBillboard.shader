Shader "Custom/ParticleBillboard"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindAmount ("Wind Amount", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "ParticleBillboard"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float _WindSpeed;
                float _WindAmount;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Billboard: transform vertex position to world, then face camera
                float3 worldCenter = TransformObjectToWorld(float3(0, 0, 0));
                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;

                // Get object scale from the model matrix
                float3 scale;
                scale.x = length(UNITY_MATRIX_M[0].xyz);
                scale.y = length(UNITY_MATRIX_M[1].xyz);
                scale.z = length(UNITY_MATRIX_M[2].xyz);

                float3 worldPos = worldCenter
                    + camRight * input.positionOS.x * scale.x
                    + camUp * input.positionOS.y * scale.y;

                // Wind sway: only affects upper half of billboard
                float windPhase = worldCenter.x * 0.7 + worldCenter.z * 0.5 + _Time.y * _WindSpeed;
                float windOffset = sin(windPhase) * _WindAmount * saturate(input.positionOS.y + 0.5);
                worldPos.x += windOffset;

                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalColor = texColor * _BaseColor;

                // Alpha cutoff for pixel art crispness
                clip(finalColor.a - 0.5);

                // Simple toon lighting for foliage
                Light mainLight = GetMainLight();
                float fakeDiffuse = 0.5 + 0.5 * mainLight.direction.y;
                float toon = step(0.4, fakeDiffuse);
                float lightFactor = lerp(0.6, 1.0, toon);
                finalColor.rgb *= lightFactor * mainLight.color;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
