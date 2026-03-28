using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class CheckURPSettings
{
    public static string Execute()
    {
        string result = "";

        // Check all URP assets
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset == null) continue;

            result += "URP Asset: " + path + "\n";
            result += "  supportsCameraDepthTexture: " + asset.supportsCameraDepthTexture + "\n";
            result += "  supportsCameraOpaqueTexture: " + asset.supportsCameraOpaqueTexture + "\n";

            // Enable depth and opaque textures
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(asset);
            result += "  -> Enabled depth + opaque textures\n";
        }

        // Also force the camera to require depth
        var cam = Camera.main;
        if (cam != null)
        {
            var camData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (camData != null)
            {
                camData.requiresDepthTexture = true;
                result += "Camera depth texture requirement: enabled\n";
            }
        }

        AssetDatabase.SaveAssets();
        return result;
    }
}
