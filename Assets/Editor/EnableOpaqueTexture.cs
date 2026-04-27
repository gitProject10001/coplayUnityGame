using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runs once on domain reload. Enables Camera Opaque Texture on the active URP
// pipeline asset so PainterlyWater.shader can sample the scene for refraction.
[InitializeOnLoad]
static class EnableOpaqueTexture
{
    static EnableOpaqueTexture()
    {
        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp
            && !urp.supportsCameraOpaqueTexture)
        {
            urp.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(urp);
            Debug.Log("[EnableOpaqueTexture] Enabled Camera Opaque Texture on URP asset.");
        }
    }
}
