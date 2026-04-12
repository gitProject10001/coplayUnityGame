// Retunes PainterlyProfile + IsoARPGCamera so the game-camera render
// reads closer to the Unity Editor SceneView (which the user prefers).
// Reduces post-processing aggression: less vignette, less negative saturation,
// more exposure, less contrast, switches camera to skybox clear flags.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PainterlyTuneToEditorLook
{
    const string PROFILE_PATH = "Assets/Settings/PainterlyProfile.asset";
    const string CAMERA_PREFAB = "Assets/Prefabs/Cameras/IsoARPGCamera.prefab";

    [MenuItem("Painterly/Tune To Editor Look")]
    public static void Execute()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
        if (profile == null)
        {
            Debug.LogError("PainterlyProfile.asset not found.");
            return;
        }

        // -- Color adjustments: gentle lift, modest contrast, slight saturation --
        if (profile.TryGet<ColorAdjustments>(out var ca))
        {
            ca.postExposure.overrideState = true;
            ca.postExposure.value = 0.05f;     // was 0.45 (too bright)
            ca.contrast.overrideState = true;
            ca.contrast.value = 8f;            // was 4
            ca.saturation.overrideState = true;
            ca.saturation.value = 0f;          // was 2
            Debug.Log("ColorAdjustments tuned.");
        }

        // -- White balance: small warm bias is fine --
        if (profile.TryGet<WhiteBalance>(out var wb))
        {
            wb.temperature.overrideState = true;
            wb.temperature.value = 5f;
            wb.tint.overrideState = true;
            wb.tint.value = -1f;
        }

        // -- SMH: keep gentle warm/cool split, slightly less aggressive --
        if (profile.TryGet<ShadowsMidtonesHighlights>(out var smh))
        {
            smh.shadows.overrideState = true;
            smh.shadows.value = new Vector4(0.98f, 0.99f, 1.04f, 0f);
            smh.highlights.overrideState = true;
            smh.highlights.value = new Vector4(1.04f, 1.02f, 1.00f, 0f);
        }

        // -- Bloom: slightly softer --
        if (profile.TryGet<Bloom>(out var bl))
        {
            bl.intensity.overrideState = true;
            bl.intensity.value = 0.30f;
            bl.threshold.overrideState = true;
            bl.threshold.value = 1.05f;
            bl.scatter.overrideState = true;
            bl.scatter.value = 0.7f;
        }

        // -- Vignette: gentle, not heavy --
        if (profile.TryGet<Vignette>(out var vg))
        {
            vg.intensity.overrideState = true;
            vg.intensity.value = 0.20f;
            vg.smoothness.overrideState = true;
            vg.smoothness.value = 0.85f;
        }

        // -- Film grain: stays off --
        if (profile.TryGet<FilmGrain>(out var fg))
        {
            fg.intensity.overrideState = true;
            fg.intensity.value = 0f;
        }

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("PainterlyProfile retuned.");

        // -- Pastel blue-grey background matches the editor SceneView feel --
        var bg = new Color(0.55f, 0.62f, 0.72f, 1f); // soft cool sky
        var camPrefab = PrefabUtility.LoadPrefabContents(CAMERA_PREFAB);
        if (camPrefab != null)
        {
            var cam = camPrefab.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = bg;
                cam.orthographicSize = 5.5f;
                var ucd = cam.GetComponent<UniversalAdditionalCameraData>();
                if (ucd != null) ucd.renderPostProcessing = true;
            }
            PrefabUtility.SaveAsPrefabAsset(camPrefab, CAMERA_PREFAB);
            PrefabUtility.UnloadPrefabContents(camPrefab);
        }

        // -- Apply same change to any IsoARPGCamera in the active scene --
        var sceneCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var c in sceneCams)
        {
            if (c.gameObject.name.StartsWith("IsoARPGCamera"))
            {
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = bg;
                c.orthographicSize = 5.5f;
                var ucd = c.GetComponent<UniversalAdditionalCameraData>();
                if (ucd != null) ucd.renderPostProcessing = true;
                EditorUtility.SetDirty(c);
                Debug.Log($"Updated scene camera: {c.gameObject.name}");
            }
        }

        // Save the active scene if dirty
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        // -- Lower the key light intensity slightly so it's less blown out --
        foreach (var lt in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (lt.gameObject.name == "Key" && lt.type == LightType.Directional)
            {
                lt.intensity = 1.05f;
                lt.color = new Color(1f, 0.93f, 0.78f, 1f);
                EditorUtility.SetDirty(lt);
                Debug.Log($"Key light dialed to {lt.intensity}");
            }
        }

        // Lower trilight ambient intensity a touch
        RenderSettings.ambientIntensity = 0.95f;

        Debug.Log("Painterly tune-to-editor-look complete.");
    }
}
