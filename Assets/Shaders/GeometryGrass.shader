Shader "Custom/GeometryGrass"
{
    Properties
    {
        [Header(Grass Shape)]
        _BladeWidth ("Blade Width", Float) = 0.05
        _BladeWidthRandom ("Blade Width Random", Float) = 0.02
        _BladeHeight ("Blade Height", Float) = 0.5
        _BladeHeightRandom ("Blade Height Random", Float) = 0.3
        _BendRotationRandom ("Bend Rotation Random", Range(0, 1)) = 0.2
        _BladeForward ("Blade Forward Amount", Float) = 0.38
        _BladeCurve ("Blade Curvature", Range(1, 4)) = 2.0

        [Header(Tessellation)]
        _TessellationUniform ("Tessellation Density", Range(1, 64)) = 4

        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.08, 0.18, 0.05, 1)
        _TipColor ("Tip Color", Color) = (0.14, 0.28, 0.08, 1)

        [Header(Wind)]
        _WindDistortionMap ("Wind Distortion Map", 2D) = "white" {}
        _WindStrength ("Wind Strength", Float) = 0.15
        _WindFrequency ("Wind Frequency", Vector) = (0.05, 0.05, 0, 0)

        [Header(Masking)]
        _GrassMask ("Grass Mask", 2D) = "white" {}
        _GrassMaskThreshold ("Mask Threshold", Range(0, 1)) = 0.1

        [Header(Displacement)]
        _DisplacementFactor ("Displacement Factor", Float) = 2

        [Header(Ground)]
        _GroundTex ("Ground Texture", 2D) = "white" {}
    }

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    #define BLADE_SEGMENTS 4

    CBUFFER_START(UnityPerMaterial)
        float _BladeWidth;
        float _BladeWidthRandom;
        float _BladeHeight;
        float _BladeHeightRandom;
        float _BendRotationRandom;
        float _BladeForward;
        float _BladeCurve;
        float _TessellationUniform;
        float4 _BaseColor;
        float4 _TipColor;
        float4 _WindDistortionMap_ST;
        float _WindStrength;
        float2 _WindFrequency;
        float _GrassMaskThreshold;
        float _DisplacementFactor;
    CBUFFER_END

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float4 tangentOS : TANGENT;
        float2 uv : TEXCOORD0;
    };

    #include "CustomTessellation.hlsl"

    struct GeometryOutput
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 positionWS : TEXCOORD1;
        float3 normalWS : TEXCOORD2;
        float fogFactor : TEXCOORD3;
        float colorShift : TEXCOORD4;
    };

    TEXTURE2D(_WindDistortionMap);
    SAMPLER(sampler_WindDistortionMap);
    TEXTURE2D(_GrassMask);
    SAMPLER(sampler_GrassMask);
    TEXTURE2D(_GroundTex);
    SAMPLER(sampler_GroundTex);

    // Global displacement properties set by GrassDisplacementCamera
    float3 _DisplacementLocation;
    float _DisplacementSize;
    TEXTURE2D(_DisplacementRT);
    SAMPLER(sampler_DisplacementRT);

    // Pseudo-random hash
    float rand(float3 co)
    {
        return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
    }

    // Rotation matrix around an arbitrary axis
    float3x3 AngleAxis3x3(float angle, float3 axis)
    {
        float c, s;
        sincos(angle, s, c);
        float t = 1.0 - c;
        float x = axis.x;
        float y = axis.y;
        float z = axis.z;

        return float3x3(
            t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
            t * x * y + s * z,  t * y * y + c,       t * y * z - s * x,
            t * x * z - s * y,  t * y * z + s * x,   t * z * z + c
        );
    }

    GeometryOutput CreateGrassVertex(float3 positionWS, float width, float height, float forward, float2 uv, float3x3 transformMatrix, float colorShift)
    {
        GeometryOutput o;

        float3 tangentPoint = float3(width, forward, height);
        float3 localPosition = positionWS + mul(transformMatrix, tangentPoint);

        float3 tangentNormal = normalize(float3(0, -1, forward));
        float3 worldNormal = mul(transformMatrix, tangentNormal);

        o.positionCS = TransformWorldToHClip(localPosition);
        o.positionWS = localPosition;
        o.normalWS = worldNormal;
        o.uv = uv;
        o.fogFactor = ComputeFogFactor(o.positionCS.z);
        o.colorShift = colorShift;

        return o;
    }

    [maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
    void geo(triangle TessellationControlPoint IN[3], inout TriangleStream<GeometryOutput> triStream)
    {
        float3 pos = IN[0].positionWS;

        float3 vNormal = TransformObjectToWorldNormal(IN[0].normalOS);
        float3 vTangent = TransformObjectToWorldDir(IN[0].tangentOS.xyz);
        float3 vBinormal = cross(vNormal, vTangent) * IN[0].tangentOS.w;

        float3x3 tangentToWorld = float3x3(
            vTangent.x, vBinormal.x, vNormal.x,
            vTangent.y, vBinormal.y, vNormal.y,
            vTangent.z, vBinormal.z, vNormal.z
        );

        // Wind
        float2 windUV = pos.xz * _WindDistortionMap_ST.xy + _WindDistortionMap_ST.zw + _WindFrequency * _Time.y;
        float2 windSample = (SAMPLE_TEXTURE2D_LOD(_WindDistortionMap, sampler_WindDistortionMap, windUV, 0).xy * 2.0 - 1.0) * _WindStrength;
        float3 wind = normalize(float3(windSample.x, windSample.y, 0));
        float3x3 windRotation = AngleAxis3x3(PI * windSample.x, wind);

        // Displacement from interactive objects
        float3x3 dispRotation = float3x3(1,0,0, 0,1,0, 0,0,1); // identity
        if (_DisplacementSize > 0)
        {
            float2 dispUV = (pos.xz - _DisplacementLocation.xz) / _DisplacementSize;
            float2 dispMaskUV = max(saturate(dispUV), saturate(1.0 - dispUV));
            float dispMask = floor(max(dispMaskUV.x, dispMaskUV.y));
            float2 dispSample = lerp(
                SAMPLE_TEXTURE2D_LOD(_DisplacementRT, sampler_DisplacementRT, dispUV, 0).xz - 0.5,
                float2(0.001, 0.001),
                dispMask
            );
            float3 displacement = normalize(float3(dispSample.x, dispSample.y, 0));
            dispRotation = AngleAxis3x3(-_DisplacementFactor * abs(dispSample.x + dispSample.y), displacement);
        }

        // Random rotations per blade
        float3x3 facingRotation = AngleAxis3x3(rand(pos) * TWO_PI, float3(0, 0, 1));
        float3x3 bendRotation = AngleAxis3x3(rand(pos.zzx) * _BendRotationRandom * PI * 0.5, float3(-1, 0, 0));

        float3x3 transformMatrix = mul(mul(mul(tangentToWorld, mul(windRotation, dispRotation)), facingRotation), bendRotation);
        float3x3 transformMatrixFacing = mul(tangentToWorld, facingRotation);

        // Grass mask
        float mask = SAMPLE_TEXTURE2D_LOD(_GrassMask, sampler_GrassMask, IN[0].uv, 0).r;

        // Per-blade random height/width
        float height = ((rand(pos.zyx) * 2.0 - 1.0) * _BladeHeightRandom + _BladeHeight) * mask;
        float width = ((rand(pos.xzy) * 2.0 - 1.0) * _BladeWidthRandom + _BladeWidth) * mask;
        float forward = rand(pos.yyz) * _BladeForward;

        int segments = mask > _GrassMaskThreshold ? BLADE_SEGMENTS : 0;

        // Per-blade color variation
        float colorShift = rand(pos.xyz + float3(7.31, 0, 13.17)) * 0.3 - 0.15;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)BLADE_SEGMENTS;
            float segmentHeight = height * t;
            float segmentWidth = width * (1.0 - t);
            float segmentForward = pow(t, _BladeCurve) * forward;

            float3x3 mat = i == 0 ? transformMatrixFacing : transformMatrix;

            triStream.Append(CreateGrassVertex(pos,  segmentWidth, segmentHeight, segmentForward, float2(0, t), mat, colorShift));
            triStream.Append(CreateGrassVertex(pos, -segmentWidth, segmentHeight, segmentForward, float2(1, t), mat, colorShift));
        }

        // Tip vertex
        triStream.Append(CreateGrassVertex(pos, 0, height, forward, float2(0.5, 1), transformMatrix, colorShift));
    }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off

        // ---- Forward pass with toon lighting ----
        Pass
        {
            Name "GeometryGrassForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma require geometry tessellation
            #pragma target 4.6

            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            half4 frag(GeometryOutput input, half facing : VFACE) : SV_Target
            {
                float3 normal = facing > 0 ? input.normalWS : -input.normalWS;

                // Base-to-tip color gradient
                half3 grassColor = lerp(_BaseColor.rgb, _TipColor.rgb, input.uv.y);

                // Per-blade color variation
                grassColor.g += input.colorShift * 0.1;
                grassColor.r += input.colorShift * 0.05;

                // Fake AO: darken base of each blade
                float aoFactor = smoothstep(0.0, 0.4, input.uv.y);
                aoFactor = lerp(0.5, 1.0, aoFactor);
                grassColor *= aoFactor;

                // Shadow sampling
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Toon lighting matching project style
                float NdotL = 0.5 + 0.5 * dot(normal, mainLight.direction);
                float toon = floor(NdotL * 2.0 + 0.5) / 2.0;
                float shadow = lerp(0.25, 1.0, mainLight.shadowAttenuation);

                half3 shadowTint = half3(0.08, 0.10, 0.05);
                half3 lit = lerp(grassColor * shadowTint, grassColor * mainLight.color * 0.8, toon * shadow);
                half3 ambient = grassColor * 0.08;
                half3 finalColor = lit + ambient;

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        // ---- Shadow caster pass ----
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma require geometry tessellation
            #pragma target 4.6

            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragShadow

            #pragma multi_compile_shadowcaster

            float3 _LightDirection;

            half4 fragShadow(GeometryOutput input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // ---- Depth only pass ----
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma require geometry tessellation
            #pragma target 4.6

            #pragma vertex vert
            #pragma hull hull
            #pragma domain domain
            #pragma geometry geo
            #pragma fragment fragDepth

            half4 fragDepth(GeometryOutput input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // No DepthNormals pass — grass excluded from outlines
    }
}
