using UnityEngine;
using UnityEditor;

public class FixFBXImportSettings
{
    public static void Execute()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Sword and Shield Pack" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
            }
        }
        Debug.Log("All FBX models set to Humanoid!");
    }
}
