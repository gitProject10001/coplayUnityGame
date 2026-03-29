Shader "Custom/GroundToon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}

        [Header(Ground Variation)]
        _DirtColor ("Dirt Color", Color) = (0.22, 0.17, 0.10, 1)
        _MossColor ("Moss Color", Color) = (0.15, 0.25, 0.10, 1)
        _PathColor ("Path Color", Color) = (0.28, 0.25, 0.18, 1)
        _StoneColor ("Stone Color", Color) = (0.22, 0.23, 0.28, 1)
        _NoiseScale1 ("Large Patch Scale", Float) = 0.15
        _NoiseScale2 ("Detail Scale", Float) = 1.5
        _NoiseScale3 ("Stone Patch Scale", Float) = 0.08
        _PatchBlend ("Patch Blend", Range(0, 1)) = 0.6

        [Header(Toon Shading)]
        _LightSteps ("Light Steps", Range(2, 8)) = 3
        _EdgeSmoothness ("Edge Smoothness", Range(0, 0.5)) = 0.05
        _ShadowColor ("Shadow Tint", Color) = (0.3, 0.25, 0.4, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "GroundToonForward"
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
                float4 _DirtColor;
                float4 _MossColor;
                float4 _PathColor;
                float4 _StoneColor;
                float _NoiseScale1;
                float _NoiseScale2;
                float _NoiseScale3;
                float _PatchBlend;
                float _LightSteps;
                float _EdgeSmoothness;
                float4 _ShadowColor;
                float _AmbientStrength;
            CBUFFER_END

            // ---- Hash-based 2D noise (no texture lookups) ----

            // Pseudo-random hash: returns a value in [0,1] for a given 2D integer cell
            float2 Hash2D(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Value noise with smooth interpolation
            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth hermite interpolation
                float2 u = f * f * (3.0 - 2.0 * f);

                // Four corner hashes
                float a = frac(sin(dot(i + float2(0.0, 0.0), float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1.0, 0.0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0.0, 1.0), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1.0, 1.0), float2(127.1, 311.7))) * 43758.5453);

                // Bilinear interpolation
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

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

                // ---- World-space noise-based ground variation ----
                float2 worldXZ = input.positionWS.xz;

                // Large patches: blend between dirt and moss
                float largePatch = Noise2D(worldXZ * _NoiseScale1);
                half3 groundColor = lerp(_DirtColor.rgb, _MossColor.rgb, largePatch);

                // Stone patches: large-scale rocky areas for variety
                float stonePatch = Noise2D(worldXZ * _NoiseScale3 + float2(73.7, 41.3));
                stonePatch = smoothstep(0.45, 0.65, stonePatch);
                groundColor = lerp(groundColor, _StoneColor.rgb, stonePatch);

                // Small detail: accent with path color
                float detail = Noise2D(worldXZ * _NoiseScale2);
                groundColor = lerp(groundColor, _PathColor.rgb, detail * (1.0 - _PatchBlend));

                // Combine: base texture * base color tinted by ground variation
                half3 baseColor = texColor.rgb * _BaseColor.rgb;
                half3 albedo = lerp(baseColor, groundColor, _PatchBlend);

                // ---- Toon lighting (same pattern as ToonLit) ----
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                float3 normal = normalize(input.normalWS);

                float NdotL = dot(normal, mainLight.direction);
                float toon = ToonDiffuse(NdotL * mainLight.shadowAttenuation, _LightSteps, _EdgeSmoothness);

                // Blend between shadow color and lit color
                half3 diffuse = lerp(_ShadowColor.rgb * albedo, albedo * mainLight.color, toon);

                // Ambient
                half3 ambient = albedo * _AmbientStrength;

                half3 finalColor = diffuse + ambient;

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

        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}
