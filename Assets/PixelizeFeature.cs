using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class PixelizeFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PixelizeSettings
    {
        [Range(1, 20)]
        public int pixelScale = 4;
        
        [Header("Color")]
        [Range(2, 256)]
        [Tooltip("Number of color steps per channel. Lower values mean fewer colors (more retro).")]
        public int colorSteps = 16;
        public bool enableDithering = true;

        [Header("Outlines")]
        public bool enableOutlines = true;
        public Color outlineColor = Color.black;
        [Range(0.01f, 5f)]
        public float depthThreshold = 0.5f;
        [Range(0.1f, 3f)]
        public float normalThreshold = 0.8f;
    }

    public PixelizeSettings settings = new PixelizeSettings();
    private PixelizePass pixelizePass;

    public override void Create()
    {
        pixelizePass = new PixelizePass(settings);
        // Run before post-processing to avoid blurring the pixels
        pixelizePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            pixelizePass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
            renderer.EnqueuePass(pixelizePass);
        }
    }

    class PixelizePass : ScriptableRenderPass
    {
        private PixelizeSettings settings;
        private Material blitMaterial;
        private Material pixelArtMaterial;

        public PixelizePass(PixelizeSettings settings)
        {
            this.settings = settings;
            
            Shader blitShader = Shader.Find("Hidden/Universal Render Pipeline/Blit");
            if (blitShader != null)
            {
                blitMaterial = CoreUtils.CreateEngineMaterial(blitShader);
            }

            Shader pixelArtShader = Shader.Find("Hidden/PixelArt");
            if (pixelArtShader != null)
            {
                pixelArtMaterial = CoreUtils.CreateEngineMaterial(pixelArtShader);
            }
        }

        private class PassData
        {
            public TextureHandle source;
            public Material material;
            public int colorSteps;
            public float pixelScale;
            public bool enableOutlines;
            public bool enableDithering;
            public Color outlineColor;
            public float depthThreshold;
            public float normalThreshold;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (blitMaterial == null || pixelArtMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid()) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            
            TextureDesc textureDesc = new TextureDesc(Mathf.Max(1, desc.width / settings.pixelScale), Mathf.Max(1, desc.height / settings.pixelScale));
            textureDesc.colorFormat = desc.graphicsFormat;
            textureDesc.depthBufferBits = 0;
            textureDesc.filterMode = FilterMode.Point;
            textureDesc.name = "_PixelizeTemp";
            
            TextureHandle tempTexture = renderGraph.CreateTexture(textureDesc);

            // Downscale and Posterize/Outline
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PixelArt Downscale", out var passData))
            {
                passData.source = source;
                passData.material = pixelArtMaterial;
                passData.colorSteps = settings.colorSteps;
                passData.pixelScale = settings.pixelScale;
                passData.enableOutlines = settings.enableOutlines;
                passData.enableDithering = settings.enableDithering;
                passData.outlineColor = settings.outlineColor;
                passData.depthThreshold = settings.depthThreshold;
                passData.normalThreshold = settings.normalThreshold;

                builder.UseTexture(source, AccessFlags.Read);
                
                if (resourceData.cameraDepthTexture.IsValid())
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                if (resourceData.cameraNormalsTexture.IsValid())
                    builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);

                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetFloat("_ColorSteps", data.colorSteps);
                    data.material.SetFloat("_PixelScale", data.pixelScale);
                    data.material.SetFloat("_EnableOutlines", data.enableOutlines ? 1.0f : 0.0f);
                    data.material.SetFloat("_EnableDithering", data.enableDithering ? 1.0f : 0.0f);
                    data.material.SetColor("_OutlineColor", data.outlineColor);
                    data.material.SetFloat("_DepthThreshold", data.depthThreshold);
                    data.material.SetFloat("_NormalThreshold", data.normalThreshold);
                    
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Upscale
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PixelArt Upscale", out var passData))
            {
                passData.source = tempTexture;
                passData.material = blitMaterial;

                builder.UseTexture(tempTexture, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
        }

        // Fallback for non-RenderGraph (Compatibility Mode)
        private RTHandle tempRT;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / settings.pixelScale);
            desc.height = Mathf.Max(1, desc.height / settings.pixelScale);
            desc.depthBufferBits = 0;
            
            RenderingUtils.ReAllocateIfNeeded(ref tempRT, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_PixelizeTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (tempRT == null || blitMaterial == null || pixelArtMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("PixelArt");
            
            RTHandle cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            pixelArtMaterial.SetFloat("_ColorSteps", settings.colorSteps);
            pixelArtMaterial.SetFloat("_PixelScale", settings.pixelScale);
            pixelArtMaterial.SetFloat("_EnableOutlines", settings.enableOutlines ? 1.0f : 0.0f);
            pixelArtMaterial.SetFloat("_EnableDithering", settings.enableDithering ? 1.0f : 0.0f);
            pixelArtMaterial.SetColor("_OutlineColor", settings.outlineColor);
            pixelArtMaterial.SetFloat("_DepthThreshold", settings.depthThreshold);
            pixelArtMaterial.SetFloat("_NormalThreshold", settings.normalThreshold);

            Blitter.BlitCameraTexture(cmd, cameraTarget, tempRT, pixelArtMaterial, 0);
            Blitter.BlitCameraTexture(cmd, tempRT, cameraTarget, blitMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempRT?.Release();
            if (blitMaterial != null) CoreUtils.Destroy(blitMaterial);
            if (pixelArtMaterial != null) CoreUtils.Destroy(pixelArtMaterial);
        }
    }

    protected override void Dispose(bool disposing)
    {
        pixelizePass?.Dispose();
    }
}
