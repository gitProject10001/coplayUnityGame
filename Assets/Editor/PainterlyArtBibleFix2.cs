using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Round 2 fix-up: brighten terrain so it reads against background, swap
    /// terrain material to Custom/ToonLit (more predictable for flat plane),
    /// nudge ambient warmer.
    /// </summary>
    public static class PainterlyArtBibleFix2
    {
        public static void Execute()
        {
            // Retune Toon_Terrain to use ToonLit (simpler, lighter)
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Painterly/Toon_Terrain.mat");
            var toon = Shader.Find("Custom/ToonLit");
            if (terrainMat != null && toon != null)
            {
                terrainMat.shader = toon;
                ColorUtility.TryParseHtmlString("#7A5E3C", out var c);
                terrainMat.SetColor("_BaseColor", c);
                terrainMat.SetFloat("_LightSteps", 2f);
                terrainMat.SetFloat("_EdgeSmoothness", 0.02f);
                ColorUtility.TryParseHtmlString("#3A2E48", out var sh);
                terrainMat.SetColor("_ShadowColor", sh);
                terrainMat.SetFloat("_AmbientStrength", 0.45f);
                terrainMat.SetFloat("_EnableRim", 0f);
                EditorUtility.SetDirty(terrainMat);
            }

            // Warmer ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.62f, 0.70f, 0.82f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.50f, 0.42f, 1f);
            RenderSettings.ambientGroundColor  = new Color(0.22f, 0.18f, 0.20f, 1f);
            RenderSettings.ambientIntensity    = 1.2f;

            // Tighten the camera framing
            Camera cam = null;
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.gameObject.name.StartsWith("IsoARPGCamera")) { cam = c; break; }
            if (cam != null)
            {
                var rot = Quaternion.Euler(35f, 45f, 0f); // slightly less steep
                var target = new Vector3(0.0f, 0.4f, 0.0f);
                cam.transform.position = target + rot * (Vector3.back * 18f);
                cam.transform.rotation = rot;
                cam.orthographicSize = 4.2f;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Fix2] retuned terrain + ambient + camera");
        }
    }
}
