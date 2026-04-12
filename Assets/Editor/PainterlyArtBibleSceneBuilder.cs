using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Builds Assets/08_ArtBible.unity: instantiates the imported FBX,
    /// drops in the lighting rig prefab, the iso camera prefab, a Volume
    /// using PainterlyProfile, and overrides the water pool material with
    /// PixelWater. Idempotent — re-running rebuilds the scene from scratch.
    /// Entry point: Execute()
    /// </summary>
    public static class PainterlyArtBibleSceneBuilder
    {
        const string SCENE_PATH    = "Assets/08_ArtBible.unity";
        const string FBX_PATH      = "Assets/Models/ArtBible/ArtBibleProps.fbx";
        const string LIGHT_PREFAB  = "Assets/Prefabs/Lighting/PainterlyLightRig.prefab";
        const string CAM_PREFAB    = "Assets/Prefabs/Cameras/IsoARPGCamera.prefab";
        const string PROFILE_PATH  = "Assets/Settings/PainterlyProfile.asset";
        const string WATER_MAT     = "Assets/Materials/PixelWater.mat";

        public static void Execute()
        {
            // Create or open the scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "08_ArtBible";

            // Root container
            var root = new GameObject("ArtBible_Content");

            // Place the props FBX
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_PATH);
            if (fbx == null) { Debug.LogError("FBX not found: " + FBX_PATH); return; }
            var props = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            props.name = "Props";
            props.transform.SetParent(root.transform, false);

            // Override water pool material with PixelWater (binder leaves Toon_Stone otherwise)
            var water = root.transform.Find("Props/ab_water_pool");
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>(WATER_MAT);
            if (water != null && waterMat != null)
            {
                var rend = water.GetComponent<MeshRenderer>();
                if (rend != null) rend.sharedMaterial = waterMat;
            }

            // Lighting rig
            var lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LIGHT_PREFAB);
            var lightInst = (GameObject)PrefabUtility.InstantiatePrefab(lightPrefab);
            lightInst.name = "Lighting";
            lightInst.transform.SetParent(null, false);

            // Camera
            var camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CAM_PREFAB);
            var camInst = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab);
            camInst.tag = "MainCamera";
            // Frame the content: aim camera at world ~(0, 0.5, 0) from iso angle.
            // Iso rotation is (40,45,0); position back along that view at distance 14.
            var rot = Quaternion.Euler(40f, 45f, 0f);
            var target = new Vector3(0f, 0.5f, 0f);
            camInst.transform.position = target + rot * (Vector3.back * 14f);
            camInst.transform.rotation = rot;
            // Tighter ortho size to frame just the props
            var cam = camInst.GetComponent<Camera>();
            if (cam != null) cam.orthographicSize = 4.5f;

            // Volume with PainterlyProfile (scene-local, high priority)
            var volGo = new GameObject("PainterlyVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE_PATH);

            // Save scene
            Directory.CreateDirectory(Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SCENE_PATH);
            Debug.Log("[ArtBible] Scene built and saved: " + SCENE_PATH);
        }
    }
}
