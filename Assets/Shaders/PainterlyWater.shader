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
        _RippleFoamColor ("Ripple Foam Color", Color) = (0.82, 0.91, 0.95, 1.0)
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
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.12
        _SpecularPow      ("Specular Power",    Range(8, 128)) = 48

        [Header(Surface Normals)]
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale    ("Normal Strength",    Range(0, 1))    = 0.15
        _NormalTiling   ("Normal Tiling",      Range(0.1, 10)) = 1.0
        _ScrollDir1     ("Scroll Direction 1", Vector)          = (0.03, 0.02, 0, 0)
        _ScrollDir2     ("Scroll Direction 2", Vector)          = (-0.02, 0.03, 0, 0)

        [Header(Heightfield)]
        _HeightfieldNormalStrength ("Heightfield Normal Strength", Range(0, 2)) = 1.0

        [Header(Reflections)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1))   = 0.65
        _ReflectionTint     ("Reflection Tint",     Color)          = (0.85, 0.92, 1.0, 1)

        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 0.1)) = 0.08
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "ToonLighting.hlsl"

            #define MAX_RIPPLES 16

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // Set globally each frame by WaveHeightfield.cs — world-space normal packed RGB
            TEXTURE2D(_HeightfieldNormalMap);
            SAMPLER(sampler_HeightfieldNormalMap);
            float4 _HeightfieldOrigin;
            float  _HeightfieldWorldSize;

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
                float3 normalWS   : TEXCOORD3; // Vertex normal in world space
                float  fogFactor  : TEXCOORD4; // TEXCOORD4 (was 5)
                float4 normalUV   : TEXCOORD5; // TEXCOORD5 (was 6)
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _MidColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float4 _RippleFoamColor; // New property
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

                float  _HeightfieldNormalStrength;
                float4 _ReflectionTint;
                float  _ReflectionStrength;
                float  _RefractionStrength;
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

                // Wider band for better visibility on low-poly meshes
                float band     = exp(-fromFront * fromFront * 1.5);
                float osc      = cos(fromFront * _RippleFreq);
                float lifeFade = 1.0 - saturate(age / _RippleLife);
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

                // Ambient rolling waves
                float2 wd1 = float2(0.7, 0.7);
                float2 wd2 = float2(-0.5, 0.85);
                float w1 = sin(dot(wp.xz, wd1) * _WaveFrequency + _Time.y * _WaveSpeed);
                float w2 = sin(dot(wp.xz, wd2) * _WaveFrequency * 0.7 + _Time.y * _WaveSpeed * 1.3);
                float ambient = (w1 + w2 * 0.6) * _WaveAmplitude;

                // Player wakes (displacement only visible if mesh is dense)
                float rippleHeight = TotalRippleHeight(wp.xz); // Calculate height here for vertex displacement
                wp.y += ambient + rippleHeight;

                float2 worldUV = wp.xz * _NormalTiling;
                o.normalUV.xy = worldUV + _ScrollDir1.xy * _Time.y;
                o.normalUV.zw = worldUV + _ScrollDir2.xy * _Time.y;

                o.positionWS = wp;
                o.positionCS = TransformWorldToHClip(wp);
                o.screenPos  = ComputeScreenPos(o.positionCS);
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = input.uv;
                o.fogFactor  = ComputeFogFactor(o.positionCS.z);
                return o;
            }

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

                half3 col;
                float midT  = smoothstep(_MidBandStart  - 0.04, _MidBandStart  + 0.04, depthN);
                float deepT = smoothstep(_DeepBandStart - 0.04, _DeepBandStart + 0.04, depthN);
                col = lerp(_ShallowColor.rgb, _MidColor.rgb,  midT);
                col = lerp(col,               _DeepColor.rgb, deepT);
                float baseAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthN);

                float hasFloor = step(0.05, depth);
                float openVar = sin(i.positionWS.x * 0.18 + _Time.y * 0.04)
                              * sin(i.positionWS.z * 0.22 - _Time.y * 0.03) * 0.08;
                half3 openColor = _MidColor.rgb + openVar;
                col = lerp(openColor, col, hasFloor);
                baseAlpha = lerp(_MidColor.a, baseAlpha, hasFloor);

                // Caustics
                float t = _Time.y * _CausticSpeed;
                float2 wp = i.positionWS.xz;
                float warpA = sin(wp.x * 0.4 + t * 0.5) + sin(wp.y * 0.3 + t * 0.4);
                float warpB = cos(wp.x * 0.3 - t * 0.3) + cos(wp.y * 0.5 + t * 0.6);
                float2 wPos = wp + float2(warpA, warpB) * 0.8;
                float l1 = sin(wPos.x * _CausticScale) * sin(wPos.y * _CausticScale);
                float2 rot2 = float2(wPos.x * 0.707 - wPos.y * 0.707, wPos.x * 0.707 + wPos.y * 0.707);
                float l2 = sin(rot2.x * _CausticScale * 1.43 + t) * sin(rot2.y * _CausticScale * 1.27 - t);
                float2 rot3 = float2(wPos.x * 0.5 + wPos.y * 0.866, -wPos.x * 0.866 + wPos.y * 0.5);
                float l3 = sin(rot3.x * _CausticScale * 2.11 - t * 0.8) * sin(rot3.y * _CausticScale * 1.91 + t * 0.5);
                float caustic = pow(saturate(max(l1, max(l2 * 0.85, l3 * 0.7)) * 1.2), 3.0);
                float foamMask = smoothstep(_CausticThreshold - 0.08, _CausticThreshold + 0.08, caustic);
                col = lerp(col, _FoamColor.rgb, foamMask * _CausticStrength * (1.0 - depthN * 0.55));

                // Shore foam
                float shoreT = 1.0 - saturate(depth / _ShoreFoamWidth);
                float realDepth = step(0.05, depth);
                float shore = step(0.55, shoreT) * realDepth;
                col = lerp(col, _FoamColor.rgb, shore);

                // Ripple height for wake foam (no fragment-side normal perturbation —
                // vertex displacement already gives correct mesh normals, and analytic
                // finite-difference derivatives here cause specular blowout)
                float h_center = TotalRippleHeight(i.positionWS.xz);

                // Normal from scrolling normal maps (gentle surface detail)
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.normalUV.xy), _NormalScale);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.normalUV.zw), _NormalScale);
                float3 blended = normalize(float3(n1.xy + n2.xy, 1.0));
                float3 N = normalize(float3(blended.x, blended.z, blended.y));

                // Heightfield normal map — world-space normal generated each frame from simulation
                // Packed as RGB (0-1): R=nx, G=ny, B=nz
                float2 hfUV = (_HeightfieldWorldSize > 0.0)
                    ? (i.positionWS.xz - _HeightfieldOrigin.xz) / _HeightfieldWorldSize + 0.5
                    : float2(0.5, 0.5);
                float inHF = step(0.001, hfUV.x) * step(hfUV.x, 0.999)
                           * step(0.001, hfUV.y) * step(hfUV.y, 0.999);
                float3 hfPacked = SAMPLE_TEXTURE2D(_HeightfieldNormalMap, sampler_HeightfieldNormalMap, saturate(hfUV)).rgb;
                float3 hfNWorld = hfPacked * 2.0 - 1.0; // world-space (x, y, z)
                N = normalize(N + hfNWorld * (_HeightfieldNormalStrength * inHF));

                // Wake foam — thin ring at wave crests only
                float wakeFoam = smoothstep(0.65, 0.9, abs(h_center) / max(_RippleAmp, 0.001));
                col = lerp(col, _RippleFoamColor.rgb, wakeFoam * 0.4);

                // Toon shading
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
                Light L  = GetMainLight(sc);
                float NdotL = saturate(dot(N, L.direction));
                float toon  = ToonDiffuse(NdotL, _LightSteps, _EdgeSmoothness);
                float shadowMix = (1.0 - toon) * (1.0 - L.shadowAttenuation * 0.6);
                half3 lit = col * (toon * L.color + _AmbientStrength);
                lit = lerp(lit, lit * _ShadowColor.rgb, shadowMix * 0.35);

                // Specular: Fresnel-gated so it only fires at glancing angles,
                // not on flat-facing isometric water (where halfDir ≈ N everywhere)
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float fresnel = pow(1.0 - saturate(dot(N, viewDir)), 4.0);
                float3 halfDir = normalize(L.direction + viewDir);
                float spec = pow(saturate(dot(N, halfDir)), _SpecularPow);
                float specToon = smoothstep(0.75, 0.9, spec) * fresnel;
                lit += L.color * specToon * _SpecularStrength;

                // Sky reflections — SH in reflect direction, visible at isometric angle.
                // No toon gate: sky reflects even on shadow/ambient areas.
                // High base (0.25) ensures visibility at 55° ortho camera, not just grazing.
                float3 reflDir = reflect(-viewDir, N);
                half3 skyRefl = SampleSH(reflDir) * _ReflectionTint.rgb;
                float fresnelRefl = saturate(pow(1.0 - saturate(dot(N, viewDir)), 1.5) + 0.25);
                lit += skyRefl * (_ReflectionStrength * fresnelRefl);

                // Refraction — show scene floor through water, offset by surface normal.
                // Blend kept non-zero even at depth so pool always feels transparent.
                float2 refrOffset = N.xz * _RefractionStrength;
                half3 refracted = SampleSceneColor(screenUV + refrOffset);
                float refrBlend = saturate(0.55 - depthN * 0.45);
                lit = lerp(lit, refracted, refrBlend);

                lit = MixFog(lit, i.fogFactor);
                float a = saturate(baseAlpha + foamMask * 0.15 + shore * 0.6 + wakeFoam * 0.15);
                return half4(lit, a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
