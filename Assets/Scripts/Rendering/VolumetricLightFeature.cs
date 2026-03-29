using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class VolumetricLightFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class VolumetricSettings
    {
        [Range(0f, 1f)]
        public float intensity = 0.6f;
        [Range(4, 64)]
        public int steps = 32;
        [Range(5f, 100f)]
        public float maxDistance = 40f;
        [Range(0f, 0.99f)]
        public float scattering = 0.7f;
        public Color lightColor = new Color(0.9f, 1f, 0.75f, 1f);
        [Range(0.01f, 2f)]
        public float density = 1.0f;
        [Range(0.5f, 20f)]
        public float noiseScale = 3f;
        [Range(0f, 1f)]
        public float noiseStrength = 0.15f;
    }

    public VolumetricSettings settings = new VolumetricSettings();
    private VolumetricPass volumetricPass;

    public override void Create()
    {
        volumetricPass = new VolumetricPass(settings);
        // Run after opaque + skybox, before transparent
        volumetricPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            volumetricPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(volumetricPass);
        }
    }

    class VolumetricPass : ScriptableRenderPass
    {
        private VolumetricSettings settings;
        private Material material;

        public VolumetricPass(VolumetricSettings settings)
        {
            this.settings = settings;
            Shader shader = Shader.Find("Hidden/VolumetricLight");
            if (shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(shader);
            }
        }

        private void SetMaterialProperties()
        {
            material.SetFloat("_Intensity", settings.intensity);
            material.SetFloat("_Steps", settings.steps);
            material.SetFloat("_MaxDistance", settings.maxDistance);
            material.SetFloat("_Scattering", settings.scattering);
            material.SetColor("_LightColor", settings.lightColor);
            material.SetFloat("_Density", settings.density);
            material.SetFloat("_NoiseScale", settings.noiseScale);
            material.SetFloat("_NoiseStrength", settings.noiseStrength);
        }

        private class PassData
        {
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid()) return;

            // Render volumetric light directly onto color buffer with additive blending
            // Only read depth texture, write to color as render attachment
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Volumetric Light", out var passData))
            {
                passData.material = material;

                if (resourceData.cameraDepthTexture.IsValid())
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                // Write additively to color buffer (shader uses Blend One One)
                builder.SetRenderAttachment(source, 0, AccessFlags.ReadWrite);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    SetMaterialProperties();
                    // Draw fullscreen quad with volumetric shader (additive blend)
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3);
                });
            }
        }

        // Compatibility mode fallback
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("VolumetricLight");
            RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            SetMaterialProperties();
            Blitter.BlitCameraTexture(cmd, cameraTarget, cameraTarget, material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            if (material != null) CoreUtils.Destroy(material);
        }
    }

    protected override void Dispose(bool disposing)
    {
        volumetricPass?.Dispose();
    }
}
