Shader "Custom/ToonLit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        [Header(Toon Shading)]
        _LightSteps ("Light Steps", Range(2, 8)) = 3
        _EdgeSmoothness ("Edge Smoothness", Range(0, 0.5)) = 0.05
        _ShadowColor ("Shadow Tint", Color) = (0.4, 0.4, 0.6, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.15
        [Header(Specular)]
        _EnableSpecular ("Enable Specular", Float) = 0
        _Glossiness ("Glossiness", Range(1, 256)) = 32
        _SpecularThreshold ("Specular Threshold", Range(0, 1)) = 0.5
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        [Header(Rim)]
        _EnableRim ("Enable Rim", Float) = 0
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimThreshold ("Rim Threshold", Range(0, 1)) = 0.4
        _RimColor ("Rim Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "ToonLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _LightSteps;
                float _EdgeSmoothness;
                float4 _ShadowColor;
                float _AmbientStrength;
                float _EnableSpecular;
                float _Glossiness;
                float _SpecularThreshold;
                float4 _SpecularColor;
                float _EnableRim;
                float _RimPower;
                float _RimThreshold;
                float4 _RimColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = texColor.rgb * _BaseColor.rgb;

                // Get main light
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));

                // Toon diffuse
                float NdotL = dot(normal, mainLight.direction);
                float toon = ToonDiffuse(NdotL * mainLight.shadowAttenuation, _LightSteps, _EdgeSmoothness);

                // Blend between shadow color and lit color
                half3 diffuse = lerp(_ShadowColor.rgb * baseColor, baseColor * mainLight.color, toon);

                // Ambient
                half3 ambient = baseColor * _AmbientStrength;

                half3 finalColor = diffuse + ambient;

                // Specular (optional)
                if (_EnableSpecular > 0.5)
                {
                    float spec = ToonSpecular(viewDir, mainLight.direction, normal, _Glossiness, _SpecularThreshold);
                    finalColor += spec * _SpecularColor.rgb * mainLight.color;
                }

                // Rim light (optional)
                if (_EnableRim > 0.5)
                {
                    float rim = ToonRim(viewDir, normal, _RimPower, _RimThreshold);
                    finalColor += rim * _RimColor.rgb;
                }

                // Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // Depth/Normal pass for outline detection
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
