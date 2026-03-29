#ifndef CUSTOM_TESSELLATION_INCLUDED
#define CUSTOM_TESSELLATION_INCLUDED

struct TessellationControlPoint
{
    float4 positionOS : INTERNALTESSPOS;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
};

struct TessellationFactors
{
    float edge[3] : SV_TessFactor;
    float inside : SV_InsideTessFactor;
};

TessellationControlPoint vert(Attributes input)
{
    TessellationControlPoint o;
    o.positionOS = input.positionOS;
    o.normalOS = input.normalOS;
    o.tangentOS = input.tangentOS;
    o.uv = input.uv;
    o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    return o;
}

TessellationFactors patchConstantFunction(InputPatch<TessellationControlPoint, 3> patch)
{
    TessellationFactors f;
    f.edge[0] = _TessellationUniform;
    f.edge[1] = _TessellationUniform;
    f.edge[2] = _TessellationUniform;
    f.inside = _TessellationUniform;
    return f;
}

[domain("tri")]
[outputcontrolpoints(3)]
[outputtopology("triangle_cw")]
[partitioning("integer")]
[patchconstantfunc("patchConstantFunction")]
TessellationControlPoint hull(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OutputControlPointID)
{
    return patch[id];
}

[domain("tri")]
TessellationControlPoint domain(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 bary : SV_DomainLocation)
{
    TessellationControlPoint o;

    #define DOMAIN_INTERPOLATE(field) o.field = \
        patch[0].field * bary.x + \
        patch[1].field * bary.y + \
        patch[2].field * bary.z;

    DOMAIN_INTERPOLATE(positionOS)
    DOMAIN_INTERPOLATE(normalOS)
    DOMAIN_INTERPOLATE(tangentOS)
    DOMAIN_INTERPOLATE(uv)

    o.positionWS = TransformObjectToWorld(o.positionOS.xyz);

    return o;
}

#endif
