using UnityEditor;
using UnityEngine;

public class SetupWaterNormal
{
    public static void Execute()
    {
        // Configure texture import as Normal Map
        string texPath = "Assets/Textures/Water/WaterNormal.png";
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
            Debug.Log("WaterNormal.png configured as Normal Map with Repeat wrap.");
        }
        else
        {
            Debug.LogError("Could not find texture at " + texPath);
            return;
        }

        // Assign to Toon_Water material
        string matPath = "Assets/Materials/Painterly/Toon_Water.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat != null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            mat.SetTexture("_NormalMap", tex);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned WaterNormal to Toon_Water material._NormalMap");
        }
        else
        {
            Debug.LogWarning("Toon_Water.mat not found at " + matPath);
        }
    }
}
