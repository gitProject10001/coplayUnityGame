using UnityEngine;
using UnityEditor;

public class ForceRegenerate
{
    public static void Execute()
    {
        var procGen = GameObject.Find("ProceduralGenerator");
        if (procGen != null)
        {
            var gen = procGen.GetComponent<ProceduralGenerator>();
            if (gen != null)
            {
                gen.GenerateAll();
                EditorUtility.SetDirty(gen);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log("[ForceRegenerate] Trees, rocks, bushes regenerated with new code.");
            }
        }
    }
}
