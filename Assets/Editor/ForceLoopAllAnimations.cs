using UnityEngine;
using UnityEditor;

public class ForceLoopAllAnimations
{
    public static void Execute()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Sword and Shield Pack" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.animationType == ModelImporterAnimationType.Human)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.clipAnimations;
                }

                bool changed = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    // Force Loop Time and Loop Pose for ALL animations
                    if (!clips[i].loopTime) { clips[i].loopTime = true; changed = true; }
                    if (!clips[i].loopPose) { clips[i].loopPose = true; changed = true; }
                }

                if (changed)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                }
            }
        }
        
        Debug.Log("Forced Loop Time on ALL animations!");
    }
}
