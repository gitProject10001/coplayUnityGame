// Disables every PixelizeFeature in PC_Renderer.asset via Unity's API so the
// change actually persists through scene reloads. Editing the YAML directly
// gets overwritten when Unity re-serializes the asset from memory.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class PainterlyDisablePixelize
{
    const string RENDERER_PATH = "Assets/Settings/PC_Renderer.asset";

    public static void Execute()
    {
        var allObjects = AssetDatabase.LoadAllAssetsAtPath(RENDERER_PATH);
        if (allObjects == null || allObjects.Length == 0)
        {
            Debug.LogError($"Could not load any assets at {RENDERER_PATH}");
            return;
        }

        int disabled = 0;
        foreach (var obj in allObjects)
        {
            if (obj == null) continue;
            var t = obj.GetType();
            var typeName = t.Name;
            // Match by type name so we don't need a hard reference to PixelizeFeature
            if (typeName == "PixelizeFeature" || typeName.Contains("Pixelize"))
            {
                if (obj is ScriptableRendererFeature srf)
                {
                    srf.SetActive(false);
                    EditorUtility.SetDirty(srf);
                    disabled++;
                    Debug.Log($"Disabled {typeName} ({obj.name})");
                }
                else
                {
                    // Fallback: poke the m_Active field via SerializedObject
                    var so = new SerializedObject(obj);
                    var prop = so.FindProperty("m_Active");
                    if (prop != null)
                    {
                        prop.boolValue = false;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(obj);
                        disabled++;
                        Debug.Log($"Disabled {typeName} via SerializedObject");
                    }
                }
            }
        }

        // Also iterate the renderer's feature list and call SetActive(false)
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RENDERER_PATH);
        if (renderer != null)
        {
            foreach (var f in renderer.rendererFeatures)
            {
                if (f != null && f.GetType().Name.Contains("Pixelize"))
                {
                    f.SetActive(false);
                    EditorUtility.SetDirty(f);
                    Debug.Log($"Renderer feature {f.GetType().Name} → SetActive(false)");
                }
            }
            EditorUtility.SetDirty(renderer);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PixelizeFeature disabled count: {disabled}");
    }
}
