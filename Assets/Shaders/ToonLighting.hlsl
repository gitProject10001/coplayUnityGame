#ifndef TOON_LIGHTING_INCLUDED
#define TOON_LIGHTING_INCLUDED

// Stepped diffuse lighting for cel/toon shading
// steps: number of discrete light bands (e.g. 3 = dark, mid, bright)
// smoothness: how smooth the transition between bands (0 = hard, 0.1 = slight smoothing)
float ToonDiffuse(float NdotL, float steps, float edgeSmoothness)
{
    float scaled = NdotL * steps;
    float quantized = floor(scaled);
    float fraction = scaled - quantized;

    // Smooth the edge between bands to reduce flickering
    float smoothed = quantized + smoothstep(0.5 - edgeSmoothness, 0.5 + edgeSmoothness, fraction);

    return saturate(smoothed / steps);
}

// Specular highlight with toon quantization
float ToonSpecular(float3 viewDir, float3 lightDir, float3 normal, float glossiness, float specularThreshold)
{
    float3 halfVec = normalize(lightDir + viewDir);
    float NdotH = saturate(dot(normal, halfVec));
    float spec = pow(NdotH, glossiness);
    return smoothstep(specularThreshold - 0.01, specularThreshold + 0.01, spec);
}

// Rim lighting effect (highlights edges facing away from camera)
float ToonRim(float3 viewDir, float3 normal, float rimPower, float rimThreshold)
{
    float rim = 1.0 - saturate(dot(viewDir, normal));
    rim = pow(rim, rimPower);
    return smoothstep(rimThreshold - 0.01, rimThreshold + 0.01, rim);
}

#endif
