using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class AddPixelizeFeature
{
    [InitializeOnLoadMethod]
    static void AddFeature()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData != null)
            {
                bool hasFeature = false;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature != null && feature.GetType().Name == "PixelizeFeature")
                    {
                        hasFeature = true;
                        break;
                    }
                }

                if (!hasFeature)
                {
                    ScriptableRendererFeature newFeature = ScriptableObject.CreateInstance("PixelizeFeature") as ScriptableRendererFeature;
                    if (newFeature != null)
                    {
                        newFeature.name = "Pixelize Feature";
                        AssetDatabase.AddObjectToAsset(newFeature, rendererData);
                        rendererData.rendererFeatures.Add(newFeature);
                        EditorUtility.SetDirty(rendererData);
                        Debug.Log($"Added PixelizeFeature to {path}");
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
    }
}
