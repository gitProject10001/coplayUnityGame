// One-shot setup for the painterly water:
//   1. Creates Assets/Materials/Painterly/Toon_Water.mat bound to Custom/PainterlyWater
//   2. Opens 08_ArtBible.unity and assigns Toon_Water.mat to ab_water_pool
//   3. Adds a WaterRippleEmitter to the GameplayBootstrap/Player so wakes appear on the water
//
// Idempotent — safe to run multiple times.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PainterlyWaterSetup
{
    const string MAT_PATH   = "Assets/Materials/Painterly/Toon_Water.mat";
    const string SHADER     = "Custom/PainterlyWater";
    const string SCENE_PATH = "Assets/08_ArtBible.unity";

    [MenuItem("Tools/Painterly/Setup Water")]
    public static void Execute()
    {
        // ---- Phase 1: create or refresh Toon_Water.mat ----
        var shader = Shader.Find(SHADER);
        if (shader == null)
        {
            Debug.LogError($"[PainterlyWaterSetup] Shader '{SHADER}' not found. Make sure PainterlyWater.shader compiled.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null)
        {
            mat = new Material(shader) { name = "Toon_Water" };
            AssetDatabase.CreateAsset(mat, MAT_PATH);
            Debug.Log($"[PainterlyWaterSetup] Created {MAT_PATH}");
        }
        else if (mat.shader != shader)
        {
            mat.shader = shader;
            Debug.Log($"[PainterlyWaterSetup] Re-bound {MAT_PATH} to {SHADER}");
        }

        // Painterly-tuned defaults — restrained, banded, cool tint
        mat.SetColor("_ShallowColor", new Color(0.48f, 0.78f, 0.85f, 0.85f));
        mat.SetColor("_MidColor",     new Color(0.18f, 0.45f, 0.62f, 0.92f));
        mat.SetColor("_DeepColor",    new Color(0.06f, 0.18f, 0.32f, 0.96f));
        mat.SetColor("_FoamColor",    new Color(0.96f, 0.98f, 1.00f, 1.00f));
        mat.SetColor("_ShadowColor",  new Color(0.29f, 0.25f, 0.38f, 1.00f));
        mat.SetFloat("_DepthFadeDistance", 1.5f);
        mat.SetFloat("_ShoreFoamWidth",    0.35f);
        mat.SetFloat("_MidBandStart",      0.18f);
        mat.SetFloat("_DeepBandStart",     0.55f);
        mat.SetFloat("_CausticScale",      1.4f);
        mat.SetFloat("_CausticSpeed",      0.18f);
        mat.SetFloat("_CausticThreshold",  0.35f);
        mat.SetFloat("_CausticStrength",   0.55f);
        mat.SetFloat("_WaveAmplitude",     0.04f);
        mat.SetFloat("_WaveFrequency",     3.0f);
        mat.SetFloat("_WaveSpeed",         0.6f);
        mat.SetFloat("_RippleAmp",         0.12f);
        mat.SetFloat("_RippleFreq",        7.0f);
        mat.SetFloat("_RippleSpeed",       1.6f);
        mat.SetFloat("_RippleLife",        2.5f);
        mat.SetFloat("_LightSteps",        3f);
        mat.SetFloat("_EdgeSmoothness",    0.04f);
        mat.SetFloat("_AmbientStrength",   0.45f);
        EditorUtility.SetDirty(mat);

        AssetDatabase.SaveAssets();

        // ---- Phase 2: assign to ab_water_pool in 08_ArtBible.unity ----
        var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        int assignedCount = 0;
        foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go.name.IndexOf("water", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) continue;
            mr.sharedMaterial = mat;
            // Reset the leftover scale from when the plane was 0.5m raw — the
            // FBX mesh is now intrinsically 4m, so localScale must be 1.
            var t = go.transform;
            t.localScale = Vector3.one;
            // Park it visibly above the terrain top, off to the side of the props
            t.position = new Vector3(-5f, 0.20f, -3.5f);
            EditorUtility.SetDirty(mr);
            EditorUtility.SetDirty(go);
            assignedCount++;
            Debug.Log($"[PainterlyWaterSetup] Assigned Toon_Water.mat → {go.name} at {t.position} scale={t.localScale}");
        }
        if (assignedCount == 0)
            Debug.LogWarning("[PainterlyWaterSetup] No water-named MeshRenderers found in 08_ArtBible to assign.");

        // ---- Phase 3: add WaterRippleEmitter to the player so wakes appear ----
        var bootstrap = GameObject.Find("GameplayBootstrap");
        if (bootstrap != null)
        {
            var player = bootstrap.transform.Find("Player");
            if (player != null)
            {
                var existing = player.GetComponent<WaterRippleEmitter>();
                if (existing == null)
                {
                    var emitter = player.gameObject.AddComponent<WaterRippleEmitter>();
                    emitter.waterHeight = 0.05f;   // matches the ab_water_pool top in the scene
                    emitter.waterBand   = 1.2f;    // generous so the player can wade in shallow water
                    Debug.Log("[PainterlyWaterSetup] Added WaterRippleEmitter to GameplayBootstrap/Player");
                }
                else
                {
                    Debug.Log("[PainterlyWaterSetup] WaterRippleEmitter already on Player");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PainterlyWaterSetup] Done.");
    }
}
