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
        [Tooltip("Number of color steps per channel. Lower = fewer colors (more retro).")]
        public int colorSteps = 16;
        public bool enableDithering = true;

        [Header("Palette LUT")]
        [Tooltip("Map colors to a specific retro palette texture (1D strip of colors).")]
        public bool usePalette = false;
        public Texture2D paletteTexture;
        [Range(2, 64)]
        public int paletteSize = 16;

        [Header("Outlines")]
        public bool enableOutlines = true;
        public Color outlineColor = Color.black;
        [Range(0.01f, 5f)]
        public float depthThreshold = 1.5f;
        [Range(0.1f, 3f)]
        public float normalThreshold = 0.5f;

        [Tooltip("Only show outlines on convex edges (silhouettes), not concave creases.")]
        public bool convexOutlinesOnly = false;
        [Range(0f, 1f)]
        public float depthOutlineStrength = 1.0f;
        [Range(0f, 1f)]
        public float normalOutlineStrength = 0.8f;

        [Header("Fog")]
        public bool enableFog = true;
        public Color fogColor = new Color(0.12f, 0.18f, 0.15f, 1f);
        [Range(0.1f, 10f)]
        public float fogDensity = 2.0f;
        [Range(0f, 1f)]
        public float fogStart = 0.0f;
        [Range(0f, 1f)]
        public float fogEnd = 1.0f;
    }

    public PixelizeSettings settings = new PixelizeSettings();
    private PixelizePass pixelizePass;

    public override void Create()
    {
        pixelizePass = new PixelizePass(settings);
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

        private void SetMaterialProperties(Material mat)
        {
            mat.SetFloat("_ColorSteps", settings.colorSteps);
            mat.SetFloat("_PixelScale", settings.pixelScale);
            mat.SetFloat("_EnableOutlines", settings.enableOutlines ? 1.0f : 0.0f);
            mat.SetFloat("_EnableDithering", settings.enableDithering ? 1.0f : 0.0f);
            mat.SetColor("_OutlineColor", settings.outlineColor);
            mat.SetFloat("_DepthThreshold", settings.depthThreshold);
            mat.SetFloat("_NormalThreshold", settings.normalThreshold);
            mat.SetFloat("_ConvexOnly", settings.convexOutlinesOnly ? 1.0f : 0.0f);
            mat.SetFloat("_DepthOutlineStrength", settings.depthOutlineStrength);
            mat.SetFloat("_NormalOutlineStrength", settings.normalOutlineStrength);
            mat.SetFloat("_UsePalette", settings.usePalette ? 1.0f : 0.0f);
            mat.SetFloat("_PaletteSize", settings.paletteSize);

            if (settings.paletteTexture != null)
            {
                mat.SetTexture("_PaletteTexture", settings.paletteTexture);
            }

            // Fog
            mat.SetFloat("_EnableFog", settings.enableFog ? 1.0f : 0.0f);
            mat.SetColor("_FogColor", settings.fogColor);
            mat.SetFloat("_FogDensity", settings.fogDensity);
            mat.SetFloat("_FogStart", settings.fogStart);
            mat.SetFloat("_FogEnd", settings.fogEnd);
        }

        private class PassData
        {
            public TextureHandle source;
            public Material material;
            public PixelizeSettings settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (blitMaterial == null || pixelArtMaterial == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle source = resourceData.activeColorTexture;
            if (!source.IsValid()) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;

            TextureDesc textureDesc = new TextureDesc(
                Mathf.Max(1, desc.width / settings.pixelScale),
                Mathf.Max(1, desc.height / settings.pixelScale));
            textureDesc.colorFormat = desc.graphicsFormat;
            textureDesc.depthBufferBits = 0;
            textureDesc.filterMode = FilterMode.Point;
            textureDesc.name = "_PixelizeTemp";

            TextureHandle tempTexture = renderGraph.CreateTexture(textureDesc);

            // Pass 1: Downscale + PixelArt effects (posterize, dither, fog, outlines, palette)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("PixelArt Downscale", out var passData))
            {
                passData.source = source;
                passData.material = pixelArtMaterial;
                passData.settings = settings;

                builder.UseTexture(source, AccessFlags.Read);

                if (resourceData.cameraDepthTexture.IsValid())
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                if (resourceData.cameraNormalsTexture.IsValid())
                    builder.UseTexture(resourceData.cameraNormalsTexture, AccessFlags.Read);

                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    SetMaterialProperties(data.material);
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // Pass 2: Upscale with point filtering
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

            SetMaterialProperties(pixelArtMaterial);

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
