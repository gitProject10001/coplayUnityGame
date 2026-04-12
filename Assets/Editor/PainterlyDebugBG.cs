using UnityEditor;
using UnityEngine;

public static class PainterlyDebugBG
{
    public static void Execute()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name.StartsWith("IsoARPGCamera"))
            {
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = new Color(0.55f, 0.62f, 0.72f, 1f);
                EditorUtility.SetDirty(c);
                Debug.Log("Set BG red.");
            }
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
