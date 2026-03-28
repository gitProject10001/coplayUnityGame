Shader "Hidden/PixelizePosterize"
{
    Properties
    {
        _ColorSteps ("Color Steps", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "PixelizePosterize"
            ZTest Always ZWrite Off Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorSteps;

            half4 Frag(Varyings input) : SV_Target
            {
                // Sample the texture
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                
                // Posterize (reduce color palette)
                color.rgb = floor(color.rgb * _ColorSteps) / _ColorSteps;
                
                return color;
            }
            ENDHLSL
        }
    }
}
