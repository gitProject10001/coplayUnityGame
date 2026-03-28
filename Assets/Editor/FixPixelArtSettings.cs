using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class FixPixelArtSettings
{
    [InitializeOnLoadMethod]
    static void FixSettings()
    {
        // Disable MSAA on URP assets
        string[] guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset != null)
            {
                asset.msaaSampleCount = 1; // Disabled
                EditorUtility.SetDirty(asset);
            }
        }

        // Disable Anti-aliasing on Main Camera
        if (Camera.main != null)
        {
            UniversalAdditionalCameraData cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.antialiasing = AntialiasingMode.None;
                EditorUtility.SetDirty(cameraData);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Pixel Art Settings Fixed: Disabled MSAA and Anti-aliasing.");
    }
}
