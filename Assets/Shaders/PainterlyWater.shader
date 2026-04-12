// Painterly toon water with player-wake ripple displacement.
//
// Look:
//   - Hard banded depth color (shallow → mid → deep)
//   - Painted caustic foam (animated stepped sin pattern, NOT noise textures)
//   - Edge foam against shore via depth-fade
//   - Wake foam where ripple displacement crests
//   - Cool shadow tint matching the painterly system
//
// Player interaction (the "physics"):
//   _WaterRipplePoints[16] — Vector4 array of (worldX, worldY, worldZ, startTime)
//   set every frame from C# WaterRippleEmitter on the player.
//   Each point spawns an expanding ring whose front travels at _RippleSpeed
//   and decays over _RippleLife seconds. Pure analytic — no render features,
//   no compute, no extra cameras.

Shader "Custom/PainterlyWater"
{
    Properties
    {
        [Header(Color Bands)]
        _ShallowColor ("Shallow Color", Color) = (0.48, 0.78, 0.85, 0.85)
        _MidColor     ("Mid Color",     Color) = (0.18, 0.45, 0.62, 0.92)
        _DeepColor    ("Deep Color",    Color) = (0.06, 0.18, 0.32, 0.96)
        _FoamColor    ("Foam Color",    Color) = (0.96, 0.98, 1.00, 1.0)
        _ShadowColor  ("Shadow Tint",   Color) = (0.29, 0.25, 0.38, 1.0)

        [Header(Depth)]
        _DepthFadeDistance ("Depth Fade Distance", Range(0.05, 8)) = 1.5
        _ShoreFoamWidth    ("Shore Foam Width",    Range(0.01, 2)) = 0.35
        _MidBandStart      ("Mid Band Start",      Range(0, 1))    = 0.18
        _DeepBandStart     ("Deep Band Start",     Range(0, 1))    = 0.55

        [Header(Painted Foam Pattern)]
        _CausticScale     ("Caustic Scale",     Range(0.1, 8))  = 1.4
        _CausticSpeed     ("Caustic Speed",     Range(0, 1))    = 0.18
        _CausticThreshold ("Caustic Threshold", Range(-1, 1))   = 0.35
        _CausticStrength  ("Caustic Strength",  Range(0, 1))    = 0.55

        [Header(Ambient Waves)]
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.3))  = 0.04
        _WaveFrequency ("Wave Frequency", Range(0.5, 12)) = 3.0
        _WaveSpeed     ("Wave Speed",     Range(0, 3))    = 0.6

        [Header(Ripple Defaults)]
        _RippleAmp     ("Ripple Amplitude (default)", Range(0, 0.6)) = 0.12
        _RippleFreq    ("Ripple Frequency (default)", Range(1, 30))  = 7.0
        _RippleSpeed   ("Ripple Front Speed",         Range(0.1, 8)) = 1.6
        _RippleLife    ("Ripple Life Seconds",        Range(0.5, 8)) = 2.5

        [Header(Toon)]
        _LightSteps      ("Light Steps",      Range(2, 6))   = 3
        _EdgeSmoothness  ("Edge Smoothness",  Range(0, 0.5)) = 0.04
        _AmbientStrength ("Ambient Strength", Range(0, 1))   = 0.45
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.28
        _SpecularPow      ("Specular Power",    Range(8, 128)) = 48

        [Header(Surface Normals)]
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale    ("Normal Strength",    Range(0, 1))    = 0.15
        _NormalTiling   ("Normal Tiling",      Range(0.1, 10)) = 1.0
        _ScrollDir1     ("Scroll Direction 1", Vector)          = (0.03, 0.02, 0, 0)
        _ScrollDir2     ("Scroll Direction 2", Vector)          = (-0.02, 0.03, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }
        LOD 100

        Pass
        {
            Name "PainterlyWaterForward"
            Tags { "LightMode" = "UniversalForward" }
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
            #include "ToonLighting.hlsl"

            #define MAX_RIPPLES 16

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos  : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float  rippleH    : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
                float4 normalUV   : TEXCOORD6;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float4 _ShadowColor;

                float _DepthFadeDistance;
                float _ShoreFoamWidth;
                float _MidBandStart;
                float _DeepBandStart;

                float _CausticScale;
                float _CausticSpeed;
                float _CausticThreshold;
                float _CausticStrength;

                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;

                float _RippleAmp;
                float _RippleFreq;
                float _RippleSpeed;
                float _RippleLife;

                float _LightSteps;
                float _EdgeSmoothness;
                float _AmbientStrength;
                float _SpecularStrength;
                float _SpecularPow;

                float  _NormalScale;
                float  _NormalTiling;
                float4 _ScrollDir1;
                float4 _ScrollDir2;
            CBUFFER_END

            // ---- Globals set by WaterRippleEmitter.cs ----
            float4 _WaterRipplePoints[MAX_RIPPLES]; // xyz=worldPos, w=startTime
            int    _WaterRippleCount;

            // Sample one ripple's contribution to vertical displacement at world XZ.
            float SampleRipple(float2 wpXZ, float4 ripple)
            {
                float age = _Time.y - ripple.w;
                if (age < 0.0 || age > _RippleLife) return 0.0;

                float dist     = length(wpXZ - ripple.xz);
                float front    = age * _RippleSpeed;
                float fromFront= dist - front;

                // Tight gaussian band at the wave front so we get an expanding ring,
                // not a uniform disc.
                float band     = exp(-fromFront * fromFront * 3.0);
                float osc      = cos(fromFront * _RippleFreq);
                float lifeFade = 1.0 - saturate(age / _RippleLife);
                // Squared life fade so the ripple holds energy then dies fast
                lifeFade = lifeFade * lifeFade;
                return band * osc * lifeFade * _RippleAmp;
            }

            float TotalRippleHeight(float2 wpXZ)
            {
                float h = 0.0;
                int count = min(_WaterRippleCount, MAX_RIPPLES);
                [loop]
                for (int i = 0; i < count; i++)
                {
                    h += SampleRipple(wpXZ, _WaterRipplePoints[i]);
                }
                return h;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(input.positionOS.xyz);

                // Ambient rolling waves — two crossed sine waves
                float2 wd1 = float2(0.7, 0.7);
                float2 wd2 = float2(-0.5, 0.85);
                float w1 = sin(dot(wp.xz, wd1) * _WaveFrequency + _Time.y * _WaveSpeed);
                float w2 = sin(dot(wp.xz, wd2) * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3);
                float ambient = (w1 + w2 * 0.6) * _WaveAmplitude;

                // Player wakes
                float ripple = TotalRippleHeight(wp.xz);

                wp.y += ambient + ripple;

                float2 worldUV = wp.xz * _NormalTiling;
                o.normalUV.xy = worldUV + _ScrollDir1.xy * _Time.y;
                o.normalUV.zw = worldUV + _ScrollDir2.xy * _Time.y;

                o.positionWS = wp;
                o.positionCS = TransformWorldToHClip(wp);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.rippleH    = ripple;
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            // Linear depth from raw depth, ortho-aware (matches PixelWater logic).
            float SceneDepthLinear(float2 screenUV, float4 positionCS, float screenW)
            {
                float rawDepth = SampleSceneDepth(screenUV);
                #if UNITY_REVERSED_Z
                    float r = 1.0 - rawDepth;
                #else
                    float r = rawDepth;
                #endif
                float sceneDepth;
                if (unity_OrthoParams.w > 0.5)
                {
                    sceneDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, r);
                    float waterDepth = lerp(_ProjectionParams.y, _ProjectionParams.z,
                                            1.0 - positionCS.z / positionCS.w);
                    sceneDepth = sceneDepth - waterDepth;
                }
                else
                {
                    sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams) - screenW;
                }
                return sceneDepth;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float depth = SceneDepthLinear(screenUV, i.positionCS, i.screenPos.w);
                float depthN = saturate(depth / _DepthFadeDistance);

                // Three-band depth color — hard transitions for painted feel
                half3 col;
                float midT  = smoothstep(_MidBandStart  - 0.04, _MidBandStart  + 0.04, depthN);
                float deepT = smoothstep(_DeepBandStart - 0.04, _DeepBandStart + 0.04, depthN);
                col = lerp(_ShallowColor.rgb, _MidColor.rgb,  midT);
                col = lerp(col,               _DeepColor.rgb, deepT);
                float baseAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthN);

                // Fallback for open water (no geometry below): use a position-varied
                // dark-to-mid gradient so the surface still reads as water.
                float hasFloor = step(0.05, depth);
                float openVar = sin(i.positionWS.x * 0.18 + _Time.y * 0.04)
                              * sin(i.positionWS.z * 0.22 - _Time.y * 0.03) * 0.08;
                half3 openColor = _MidColor.rgb + openVar;
                col = lerp(openColor, col, hasFloor);
                baseAlpha = lerp(_MidColor.a, baseAlpha, hasFloor);

                // Painted caustic — multiply two sin fields at different angles/scales
                // for organic cell-like highlights, then add a fine-detail third layer.
                float t = _Time.y * _CausticSpeed;
                float px = i.positionWS.x;
                float pz = i.positionWS.z;
                float fieldA = sin((px + t * 0.9)          * _CausticScale)
                             * sin((pz + t * 0.65)         * _CausticScale);
                float fieldB = sin((px * 0.83 - t * 0.55)  * _CausticScale * 1.31)
                             * sin((pz * 1.17 + t * 0.42)  * _CausticScale * 1.31);
                float detail = sin((px * 0.61 + pz * 1.09 + t * 0.38) * _CausticScale * 2.17)
                             * sin((px * 1.43 - pz * 0.77 - t * 0.29) * _CausticScale * 1.89);
                float caustic = fieldA * fieldB * 0.7 + detail * 0.3;
                float foamMask = smoothstep(_CausticThreshold - 0.08, _CausticThreshold + 0.08, caustic);
                col = lerp(col, _FoamColor.rgb, foamMask * _CausticStrength * (1.0 - depthN * 0.55));

                // Edge / shore foam — only fires near actual underwater geometry,
                // never on a free-floating plane with nothing beneath it.
                float shoreT = 1.0 - saturate(depth / _ShoreFoamWidth);
                float realDepth = step(0.05, depth);   // need >5cm of underwater depth somewhere
                float shore = step(0.55, shoreT) * realDepth;
                col = lerp(col, _FoamColor.rgb, shore);

                // Wake foam — soft crests where ripple displacement is high
                float wakeFoam = smoothstep(_RippleAmp * 0.3, _RippleAmp * 0.7, abs(i.rippleH));
                col = lerp(col, _FoamColor.rgb, wakeFoam * 0.72);

                // Scrolling normal maps — interference pattern
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.normalUV.xy), _NormalScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.normalUV.zw), _NormalScale);
                float3 blended = normalize(float3(n1.xy + n2.xy, 1.0));

                // Toon shading — keep it gentle so the foam pattern reads
                // Flat-plane TBN: tangent-space (x,y,z) → world (x,z,y)
                float3 N = normalize(float3(blended.x, blended.z, blended.y));
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light L  = GetMainLight(sc);
                float NdotL = saturate(dot(N, L.direction));
                float toon  = ToonDiffuse(NdotL, _LightSteps, _EdgeSmoothness);
                float shadowMix = (1.0 - toon) * (1.0 - L.shadowAttenuation * 0.6);
                half3 lit = col * (toon * L.color + _AmbientStrength);
                lit = lerp(lit, lit * _ShadowColor.rgb, shadowMix * 0.35);

                // Toon specular glints — posterized Blinn-Phong for sun sparkle
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float3 halfDir = normalize(L.direction + viewDir);
                float spec = pow(saturate(dot(N, halfDir)), _SpecularPow);
                float specToon = smoothstep(0.7, 0.85, spec);
                lit += _FoamColor.rgb * specToon * _SpecularStrength * toon;

                lit = MixFog(lit, i.fogFactor);
                float a = saturate(baseAlpha + foamMask * 0.15 + shore * 0.6 + wakeFoam * 0.5);
                return half4(lit, a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
