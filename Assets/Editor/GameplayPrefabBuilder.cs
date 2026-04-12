// Builds reusable gameplay prefabs from SampleScene so the player + camera
// rig can be dropped into any new scene without rebuilding the X Bot rig and
// every component by hand.
//
// Outputs (under Assets/Prefabs/Gameplay/):
//   Player.prefab            — root Player + X Bot rig + GrassDisplacementDisc + scripts
//   PlayerCamera.prefab      — Main Camera with CameraFollow (auto-finds Player by name)
//   GameplayBootstrap.prefab — convenience parent containing Player + PlayerCamera
//
// Then drops GameplayBootstrap into 08_ArtBible.unity to verify the round-trip.
// Existing IsoARPGCamera in that scene is disabled (not deleted) so the painterly
// camera config is preserved if the user wants to switch back.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayPrefabBuilder
{
    const string FOLDER = "Assets/Prefabs/Gameplay";
    const string PLAYER_PREFAB     = FOLDER + "/Player.prefab";
    const string CAMERA_PREFAB     = FOLDER + "/PlayerCamera.prefab";
    const string BOOTSTRAP_PREFAB  = FOLDER + "/GameplayBootstrap.prefab";

    public static void Execute()
    {
        EnsureFolders();

        // ---- Phase 1: open SampleScene and snapshot Player + Main Camera ----
        var sample = EditorSceneManager.OpenScene("Assets/SampleScene.unity", OpenSceneMode.Single);

        var playerGO = GameObject.Find("Player");
        var camGO    = GameObject.Find("Main Camera");
        if (playerGO == null || camGO == null)
        {
            Debug.LogError($"[GameplayPrefabBuilder] Missing Player or Main Camera in SampleScene. player={playerGO}, cam={camGO}");
            return;
        }

        // SaveAsPrefabAsset writes the asset without modifying the source scene,
        // so we don't risk dirtying SampleScene.
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerGO, PLAYER_PREFAB);
        var camPrefab    = PrefabUtility.SaveAsPrefabAsset(camGO,    CAMERA_PREFAB);
        Debug.Log($"[GameplayPrefabBuilder] Saved {PLAYER_PREFAB} and {CAMERA_PREFAB}");

        // ---- Phase 2: build the bootstrap parent in a temp scene-less object ----
        // Create a temporary GameObject in SampleScene, parent fresh prefab
        // instances under it, save the parent as a prefab, then destroy the temp
        // (and DON'T save SampleScene so SampleScene stays untouched).
        var bootstrap = new GameObject("GameplayBootstrap");
        SceneManager.MoveGameObjectToScene(bootstrap, sample);

        var playerInst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, sample);
        playerInst.transform.SetParent(bootstrap.transform, worldPositionStays: false);
        playerInst.transform.localPosition = new Vector3(0f, 1f, 0f);

        var camInst = (GameObject)PrefabUtility.InstantiatePrefab(camPrefab, sample);
        camInst.transform.SetParent(bootstrap.transform, worldPositionStays: false);
        camInst.transform.localPosition = new Vector3(0f, 15f, -10f);
        camInst.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

        var bootstrapPrefab = PrefabUtility.SaveAsPrefabAsset(bootstrap, BOOTSTRAP_PREFAB);
        Debug.Log($"[GameplayPrefabBuilder] Saved {BOOTSTRAP_PREFAB}");

        // Tear down the temp parent without saving SampleScene
        Object.DestroyImmediate(bootstrap);

        // ---- Phase 3: validate by dropping the bootstrap into 08_ArtBible ----
        var test = EditorSceneManager.OpenScene("Assets/08_ArtBible.unity", OpenSceneMode.Single);

        // If a previous run already added a bootstrap, remove it so we test cleanly
        var existingBoot = GameObject.Find("GameplayBootstrap");
        if (existingBoot != null) Object.DestroyImmediate(existingBoot);

        // Disable (don't delete) the painterly IsoARPGCamera so the player camera
        // doesn't conflict, but we keep the iso config for a future switch
        var iso = GameObject.Find("IsoARPGCamera");
        if (iso != null)
        {
            iso.SetActive(false);
            Debug.Log("[GameplayPrefabBuilder] Disabled IsoARPGCamera in 08_ArtBible.");
        }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(bootstrapPrefab);
        if (inst != null)
        {
            inst.name = "GameplayBootstrap";
            // Drop the player at the centre of the terrain
            inst.transform.position = new Vector3(0f, 0f, 0f);
            Debug.Log($"[GameplayPrefabBuilder] Instantiated bootstrap in 08_ArtBible at origin.");
        }
        else
        {
            Debug.LogError("[GameplayPrefabBuilder] Failed to instantiate bootstrap in 08_ArtBible.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(test);
        EditorSceneManager.SaveScene(test);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameplayPrefabBuilder] Done.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Gameplay"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Gameplay");
    }
}
