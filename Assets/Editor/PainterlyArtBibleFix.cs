using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Post-build tweaks for the 08_ArtBible scene:
    /// - Lift ambient (Skybox source -> trilight gradient warm/cool/sky)
    /// - Re-aim the iso camera + zoom out a touch so terrain is visible
    /// - Re-find the water pool by name and force PixelWater material
    /// - Move terrain slightly down so props sit on top of it cleanly
    /// </summary>
    public static class PainterlyArtBibleFix
    {
        public static void Execute()
        {
            // Lift ambient via gradient (sky/equator/ground)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.55f, 0.65f, 0.78f, 1f); // pale sky
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.46f, 0.40f, 1f); // warm midtone
            RenderSettings.ambientGroundColor  = new Color(0.18f, 0.16f, 0.20f, 1f); // cool dark
            RenderSettings.ambientIntensity    = 1.0f;

            // Reframe iso camera
            Camera cam = null;
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.gameObject.name.StartsWith("IsoARPGCamera")) { cam = c; break; }
            if (cam != null)
            {
                var rot = Quaternion.Euler(40f, 45f, 0f);
                var target = new Vector3(0.5f, 0.3f, 0.5f);
                cam.transform.position = target + rot * (Vector3.back * 18f);
                cam.transform.rotation = rot;
                cam.orthographicSize = 5.5f;
            }

            // Force water material on the water pool by deep search
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PixelWater.mat");
            foreach (var mr in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (mr.gameObject.name == "ab_water_pool" && waterMat != null)
                {
                    mr.sharedMaterial = waterMat;
                    Debug.Log("[Fix] water pool material set to PixelWater");
                }
                if (mr.gameObject.name == "ab_terrain")
                {
                    // Drop terrain a hair so props sit on top
                    var t = mr.transform;
                    t.localPosition = new Vector3(t.localPosition.x, -0.05f, t.localPosition.z);
                }
            }

            // Bump key light intensity a bit so the lit side reads stronger
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.gameObject.name == "Key")  l.intensity = 1.8f;
                if (l.gameObject.name == "Fill") l.intensity = 0.55f;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[Fix] Art bible scene tweaked + saved");
        }
    }
}
