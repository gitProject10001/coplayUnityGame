using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualUpgrade
{
    public static void Execute()
    {
        // === 1. Update Directional Light ===
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.7f;
                light.color = new Color(1f, 0.95f, 0.8f, 1f);
                // Angled light for visible god rays (35 deg pitch, 30 deg yaw)
                light.transform.rotation = Quaternion.Euler(35f, 30f, 0f);
                EditorUtility.SetDirty(light);
                Debug.Log($"Updated directional light: intensity={light.intensity}, rotation={light.transform.rotation.eulerAngles}");
            }
        }

        // === 2. Update Ambient Light ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.12f, 1f);
        Debug.Log("Updated ambient light to darker value");

        // === 3. Update Volume Profile (post-processing) ===
        var volumes = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (var volume in volumes)
        {
            if (volume.profile == null) continue;
            var profile = volume.profile;

            // Vignette
            if (profile.TryGet<Vignette>(out var vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.45f);
                vignette.color.Override(new Color(0.05f, 0.02f, 0.1f));
                Debug.Log("Updated vignette intensity to 0.45");
            }
            else
            {
                var v = profile.Add<Vignette>();
                v.active = true;
                v.intensity.Override(0.45f);
                v.color.Override(new Color(0.05f, 0.02f, 0.1f));
                Debug.Log("Added vignette with intensity 0.45");
            }

            // Bloom
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = true;
                bloom.intensity.Override(0.5f);
                bloom.threshold.Override(0.8f);
                bloom.scatter.Override(0.7f);
                Debug.Log("Updated bloom intensity to 0.5, threshold to 0.8");
            }

            // Color Adjustments
            if (profile.TryGet<ColorAdjustments>(out var colorAdj))
            {
                colorAdj.active = true;
                colorAdj.contrast.Override(18f);
                colorAdj.saturation.Override(12f);
                Debug.Log("Updated color adjustments");
            }
            else
            {
                var ca = profile.Add<ColorAdjustments>();
                ca.active = true;
                ca.contrast.Override(18f);
                ca.saturation.Override(12f);
                Debug.Log("Added color adjustments: contrast +18, saturation +12");
            }

            // Split Toning (warm highlights, cool shadows)
            if (profile.TryGet<SplitToning>(out var split))
            {
                split.active = true;
                split.shadows.Override(new Color(0.25f, 0.2f, 0.45f));
                split.highlights.Override(new Color(1f, 0.92f, 0.75f));
                split.balance.Override(-20f);
                Debug.Log("Updated split toning");
            }
            else
            {
                var st = profile.Add<SplitToning>();
                st.active = true;
                st.shadows.Override(new Color(0.25f, 0.2f, 0.45f));
                st.highlights.Override(new Color(1f, 0.92f, 0.75f));
                st.balance.Override(-20f);
                Debug.Log("Added split toning: purple shadows, warm highlights");
            }

            EditorUtility.SetDirty(profile);
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("=== Visual upgrade complete! ===");
    }

    public static void CheckFoliageMaterials()
    {
        // Find GrassSpawner and check material texture assignments
        var spawner = Object.FindFirstObjectByType<GrassSpawner>();
        if (spawner == null)
        {
            Debug.Log("No GrassSpawner found in scene");
            return;
        }

        CheckMat("Bush", spawner.bushMaterial);
        CheckMat("SmallPlant", spawner.smallPlantMaterial);
    }

    static void CheckMat(string name, Material mat)
    {
        if (mat == null) { Debug.Log($"{name}: material is NULL"); return; }
        var tex = mat.GetTexture("_BaseMap");
        Debug.Log($"{name}: shader={mat.shader.name}, texture={( tex != null ? tex.name : "NONE" )}, color={mat.GetColor("_BaseColor")}");
    }

    public static void FixFoliageColors()
    {
        var spawner = Object.FindFirstObjectByType<GrassSpawner>();
        if (spawner == null) return;

        // Darker foliage colors — forest floor should not be bright
        if (spawner.bushMaterial != null)
        {
            spawner.bushMaterial.SetColor("_BaseColor", new Color(0.08f, 0.18f, 0.05f, 1f));
            spawner.bushMaterial.SetColor("_TipColor", new Color(0.15f, 0.30f, 0.08f, 1f));
            EditorUtility.SetDirty(spawner.bushMaterial);
        }
        if (spawner.smallPlantMaterial != null)
        {
            spawner.smallPlantMaterial.SetColor("_BaseColor", new Color(0.10f, 0.20f, 0.05f, 1f));
            spawner.smallPlantMaterial.SetColor("_TipColor", new Color(0.16f, 0.32f, 0.08f, 1f));
            EditorUtility.SetDirty(spawner.smallPlantMaterial);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Foliage colors updated to darker, more natural tones");
    }
}
