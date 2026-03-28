using UnityEngine;
using UnityEditor;

public class RegenerateEnvironment
{
    public static string Execute()
    {
        var gen = Object.FindObjectOfType<ProceduralGenerator>();
        if (gen == null)
        {
            return "ERROR: No ProceduralGenerator found in scene";
        }

        gen.forceRegenerate = true;
        // Trigger the regeneration
        gen.SendMessage("Update", SendMessageOptions.DontRequireReceiver);

        // If that didn't work (Update checks forceRegenerate), call directly
        if (gen.forceRegenerate)
        {
            gen.forceRegenerate = false;
            gen.GenerateAll();
        }

        EditorUtility.SetDirty(gen);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        return "Environment regenerated with rocks, trees, and bushes";
    }
}
