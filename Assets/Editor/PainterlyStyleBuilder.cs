using System.Collections.Generic;
using System.IO;
using ArtStyle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// One-shot setup script for the painterly isometric ARPG style.
    /// Creates folders, the palette asset, all Toon_*.mat presets, the
    /// PainterlyProfile volume profile, the lighting rig prefab, and the
    /// iso camera prefab. Idempotent: safe to re-run, will overwrite existing
    /// assets created by this script.
    /// Entry point: Execute() — invoked via Coplay execute_script MCP tool.
    /// </summary>
    public static class PainterlyStyleBuilder
    {
        // ----- Paths -----
        const string ART_FOLDER          = "Assets/Art";
        const string PALETTE_FOLDER      = "Assets/Art/Palette";
        const string PALETTE_PATH        = "Assets/Art/Palette/PainterlyPalette.asset";
        const string MATERIALS_FOLDER    = "Assets/Materials/Painterly";
        const string PROFILE_PATH        = "Assets/Settings/PainterlyProfile.asset";
        const string LIGHTING_PREFAB_DIR = "Assets/Prefabs/Lighting";
        const string LIGHTING_PREFAB     = "Assets/Prefabs/Lighting/PainterlyLightRig.prefab";
        const string CAMERA_PREFAB_DIR   = "Assets/Prefabs/Cameras";
        const string CAMERA_PREFAB       = "Assets/Prefabs/Cameras/IsoARPGCamera.prefab";

        // ----- Shared toon-shader tuning constants -----
        static readonly Color SHADOW_TINT = Hex("#4A4060");
        static readonly Color RIM_COLOR   = new Color(0.78f, 0.84f, 1.00f, 1f);
        const float LIGHT_STEPS     = 2f;
        const float EDGE_SMOOTHNESS = 0.02f;
        const float AMBIENT         = 0.35f;
        const float RIM_POWER       = 3.5f;
        const float RIM_THRESHOLD   = 0.30f;

        // ===========================================================
        public static void Execute()
        {
            try
            {
                EnsureFolders();
                var palette = CreatePalette();
                CreateMaterials(palette);
                CreateVolumeProfile();
                CreateLightingRig();
                CreateIsoCamera();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[PainterlyStyleBuilder] Style framework built successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PainterlyStyleBuilder] FAILED: " + e);
                throw;
            }
        }

        // ----------------------------------------------------------
        static void EnsureFolders()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Palette");
            EnsureFolder("Assets/Materials/Painterly");
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Lighting");
            EnsureFolder("Assets/Prefabs/Cameras");
            EnsureFolder("Assets/Models");
            EnsureFolder("Assets/Models/ArtBible");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ----------------------------------------------------------
        static PainterlyPalette CreatePalette()
        {
            var p = ScriptableObject.CreateInstance<PainterlyPalette>();
            p.earth = new Color[] {
                Hex("#2E241A"), Hex("#4A382A"), Hex("#6B4F38"),
                Hex("#8C6A48"), Hex("#B0865A"), Hex("#D6B488"),
            };
            p.cool = new Color[] {
                Hex("#3B3F47"), Hex("#5A5F68"), Hex("#7A7F88"),
                Hex("#9CA1A8"), Hex("#C4C8CC"),
            };
            p.foliage = new Color[] {
                Hex("#2A3A1E"), Hex("#4A6230"), Hex("#6B8A3E"), Hex("#9CB860"),
            };
            p.sky = new Color[] {
                Hex("#1E3340"), Hex("#3E5A6E"), Hex("#6E8AA0"), Hex("#A8C0D0"),
            };
            p.accents = new Color[] {
                Hex("#B0322E"), Hex("#D8A030"), Hex("#A03A78"), Hex("#3098A8"), Hex("#D86A28"),
            };

            var existing = AssetDatabase.LoadAssetAtPath<PainterlyPalette>(PALETTE_PATH);
            if (existing != null)
            {
                EditorUtility.CopySerialized(p, existing);
                Object.DestroyImmediate(p);
                return existing;
            }
            AssetDatabase.CreateAsset(p, PALETTE_PATH);
            return p;
        }

        // ----------------------------------------------------------
        static void CreateMaterials(PainterlyPalette pal)
        {
            Shader toon = Shader.Find("Custom/ToonLit");
            Shader ground = Shader.Find("Custom/GroundToon");
            if (toon == null) { Debug.LogError("Custom/ToonLit shader not found."); return; }

            // Format: (name, base color, enableSpecular, enableRim)
            var defs = new (string name, Color color, bool spec, bool rim)[]
            {
                ("Toon_Stone",   pal.cool[2],   false, true ),
                ("Toon_Wood",    pal.earth[2],  false, true ),
                ("Toon_Metal",   pal.cool[3],   true,  true ),
                ("Toon_Foliage", pal.foliage[2],false, true ),
                ("Toon_Skin",    pal.earth[4],  false, true ),
                ("Toon_Cloth",   pal.earth[5],  false, true ),
                ("Toon_Banner",  pal.accents[0],false, true ),
            };

            foreach (var d in defs)
            {
                CreateToonMat(toon, d.name, d.color, d.spec, d.rim);
            }

            // Terrain variant uses GroundToon if present, otherwise ToonLit fallback.
            CreateToonMat(ground != null ? ground : toon, "Toon_Terrain", pal.earth[1], false, false);
        }

        static void CreateToonMat(Shader shader, string name, Color baseColor, bool enableSpec, bool enableRim)
        {
            string path = $"{MATERIALS_FOLDER}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor"))      mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_LightSteps"))     mat.SetFloat("_LightSteps", LIGHT_STEPS);
            if (mat.HasProperty("_EdgeSmoothness")) mat.SetFloat("_EdgeSmoothness", EDGE_SMOOTHNESS);
            if (mat.HasProperty("_ShadowColor"))    mat.SetColor("_ShadowColor", SHADOW_TINT);
            if (mat.HasProperty("_AmbientStrength"))mat.SetFloat("_AmbientStrength", AMBIENT);
            if (mat.HasProperty("_EnableSpecular")) mat.SetFloat("_EnableSpecular", enableSpec ? 1f : 0f);
            if (mat.HasProperty("_EnableRim"))      mat.SetFloat("_EnableRim", enableRim ? 1f : 0f);
            if (mat.HasProperty("_RimPower"))       mat.SetFloat("_RimPower", RIM_POWER);
            if (mat.HasProperty("_RimThreshold"))   mat.SetFloat("_RimThreshold", RIM_THRESHOLD);
            if (mat.HasProperty("_RimColor"))       mat.SetColor("_RimColor", RIM_COLOR * 0.4f);
            EditorUtility.SetDirty(mat);
        }

        // ----------------------------------------------------------
        static void CreateVolumeProfile()
        {
            const string source = "Assets/Settings/DefaultVolumeProfile.asset";
            // Always start fresh from source so re-runs are deterministic.
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH) != null)
                AssetDatabase.DeleteAsset(PROFILE_PATH);

            if (!AssetDatabase.CopyAsset(source, PROFILE_PATH))
            {
                // Source not found; create empty profile.
                var fresh = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(fresh, PROFILE_PATH);
            }
            AssetDatabase.ImportAsset(PROFILE_PATH);
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);
            if (profile == null) { Debug.LogError("Failed to load PainterlyProfile."); return; }

            var ca = GetOrAdd<ColorAdjustments>(profile);
            ca.postExposure.value = 0.2f;            ca.postExposure.overrideState = true;
            ca.contrast.value = 12f;                 ca.contrast.overrideState = true;
            ca.saturation.value = -4f;               ca.saturation.overrideState = true;

            var wb = GetOrAdd<WhiteBalance>(profile);
            wb.temperature.value = 8f;               wb.temperature.overrideState = true;
            wb.tint.value = -2f;                     wb.tint.overrideState = true;

            var smh = GetOrAdd<ShadowsMidtonesHighlights>(profile);
            smh.shadows.value     = new Vector4(0.96f, 0.98f, 1.06f, 0f); smh.shadows.overrideState = true;
            smh.midtones.value    = new Vector4(1.00f, 1.00f, 1.00f, 0f); smh.midtones.overrideState = true;
            smh.highlights.value  = new Vector4(1.05f, 1.02f, 1.00f, 0f); smh.highlights.overrideState = true;

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.value = 0.35f;           bloom.intensity.overrideState = true;
            bloom.threshold.value = 1.10f;           bloom.threshold.overrideState = true;
            bloom.scatter.value = 0.70f;             bloom.scatter.overrideState = true;

            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.value = 0.32f;             vig.intensity.overrideState = true;
            vig.smoothness.value = 0.85f;            vig.smoothness.overrideState = true;

            var grain = GetOrAdd<FilmGrain>(profile);
            grain.intensity.value = 0f;              grain.intensity.overrideState = true;

            EditorUtility.SetDirty(profile);
        }

        static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (p.TryGet<T>(out var c)) return c;
            return p.Add<T>(true);
        }

        // ----------------------------------------------------------
        static void CreateLightingRig()
        {
            var root = new GameObject("PainterlyLightRig");

            var key = new GameObject("Key");
            key.transform.SetParent(root.transform, false);
            key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var kl = key.AddComponent<Light>();
            kl.type = LightType.Directional;
            kl.color = Hex("#FFE0B0");
            kl.intensity = 1.3f;
            kl.shadows = LightShadows.Soft;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(root.transform, false);
            fill.transform.rotation = Quaternion.Euler(-30f, 150f, 0f);
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = Hex("#7088B0");
            fl.intensity = 0.4f;
            fl.shadows = LightShadows.None;

            var bounce = new GameObject("Bounce");
            bounce.transform.SetParent(root.transform, false);
            bounce.transform.rotation = Quaternion.Euler(-85f, 45f, 0f);
            var bl = bounce.AddComponent<Light>();
            bl.type = LightType.Directional;
            bl.color = Hex("#FFD8A0");
            bl.intensity = 0.15f;
            bl.shadows = LightShadows.None;

            PrefabUtility.SaveAsPrefabAsset(root, LIGHTING_PREFAB);
            Object.DestroyImmediate(root);
        }

        // ----------------------------------------------------------
        static void CreateIsoCamera()
        {
            var go = new GameObject("IsoARPGCamera");
            go.transform.position = new Vector3(0f, 14f, -14f);
            go.transform.rotation = Quaternion.Euler(40f, 45f, 0f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#2A2438");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Existing global-namespace component from Assets/Scripts/Rendering/CameraTexelSnap.cs
            var snapType = System.Type.GetType("CameraTexelSnap, Assembly-CSharp");
            if (snapType != null)
            {
                var snap = go.AddComponent(snapType);
                var fld = snapType.GetField("pixelScale");
                if (fld != null) fld.SetValue(snap, 1);
            }

            PrefabUtility.SaveAsPrefabAsset(go, CAMERA_PREFAB);
            Object.DestroyImmediate(go);
        }

        // ----------------------------------------------------------
        static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }
    }
}
