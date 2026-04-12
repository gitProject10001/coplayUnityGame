using UnityEditor;
using UnityEngine;

public class TuneWaterMaterial
{
    public static void Execute()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Painterly/Toon_Water.mat");
        if (mat == null) { Debug.LogError("Toon_Water.mat not found"); return; }

        // Pull caustics way back — let normal map do the work
        mat.SetFloat("_CausticStrength", 0.10f);
        mat.SetFloat("_CausticThreshold", 0.70f);   // very few spots
        mat.SetFloat("_CausticScale", 2.5f);

        // Normal map is now the primary surface detail
        mat.SetFloat("_NormalScale", 0.45f);
        mat.SetFloat("_NormalTiling", 1.5f);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("Tuned water material: reduced caustics, boosted normals");
    }
}
