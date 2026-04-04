using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor menu to create all 7 test scenes with proper script references.
/// Unity Menu: Tools > Test Scenes > Create [Scene Name]
/// </summary>
public class TestSceneCreator : Editor
{
    private const string ScenesPath = "Assets/TestScenes/Scenes";

    // ──────────────────────────────────────────
    // Individual scene creators
    // ──────────────────────────────────────────

    [MenuItem("Tools/Test Scenes/Create ALL Test Scenes")]
    static void CreateAllScenes()
    {
        CreateToonShadersScene();
        CreateGrassAndFoliageScene();
        CreatePostProcessingScene();
        CreateWaterScene();
        CreateProceduralWorldScene();
        CreateCombatScene();
        CreateCameraFeedbackScene();

        EditorUtility.DisplayDialog("Test Scenes",
            "All 7 test scenes created in:\n" + ScenesPath +
            "\n\nOpen any scene and press Play to start experimenting!",
            "OK");
    }

    [MenuItem("Tools/Test Scenes/01 - Toon Shaders")]
    static void CreateToonShadersScene()
    {
        var scene = CreateEmptyScene("01_ToonShaders");
        var root = CreateRoot("[ShaderDebug]");
        root.AddComponent<ShaderDebug>();
        SaveScene(scene, "01_ToonShaders");
    }

    [MenuItem("Tools/Test Scenes/02 - Grass and Foliage")]
    static void CreateGrassAndFoliageScene()
    {
        var scene = CreateEmptyScene("02_GrassAndFoliage");
        var root = CreateRoot("[GrassDebug]");
        root.AddComponent<GrassDebug>();

        // Add GrassSpawner with materials
        var spawnerGO = new GameObject("GrassSpawner");
        var spawner = spawnerGO.AddComponent<GrassSpawner>();

        // Try to assign existing materials
        var bushMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BushBillboard.mat");
        var plantMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SmallPlant.mat");
        if (bushMat != null) spawner.bushMaterial = bushMat;
        if (plantMat != null) spawner.smallPlantMaterial = plantMat;

        // Add floating particles
        var particleGO = new GameObject("FloatingParticles");
        var particles = particleGO.AddComponent<FloatingParticles>();
        var dustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DustMotes.mat");
        if (dustMat != null) particles.particleMaterial = dustMat;

        // Add displacement sphere (movable test object)
        var displacer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        displacer.name = "DisplacementTestObject";
        displacer.transform.position = new Vector3(0, 0.5f, 0);
        // GrassDisplacementObject will be added if the class is available
        var dispType = System.Type.GetType("GrassDisplacementObject");
        if (dispType != null) displacer.AddComponent(dispType);

        // Link spawner to debug
        root.GetComponent<GrassDebug>().grassSpawner = spawner;
        root.GetComponent<GrassDebug>().floatingParticles = particles;

        SaveScene(scene, "02_GrassAndFoliage");
    }

    [MenuItem("Tools/Test Scenes/03 - Post Processing")]
    static void CreatePostProcessingScene()
    {
        var scene = CreateEmptyScene("03_PostProcessing");
        var root = CreateRoot("[PostProcessDebug]");
        root.AddComponent<PostProcessDebug>();
        SaveScene(scene, "03_PostProcessing");
    }

    [MenuItem("Tools/Test Scenes/04 - Water")]
    static void CreateWaterScene()
    {
        var scene = CreateEmptyScene("04_Water");
        var root = CreateRoot("[WaterDebug]");
        root.AddComponent<WaterDebug>();
        SaveScene(scene, "04_Water");
    }

    [MenuItem("Tools/Test Scenes/05 - Procedural World")]
    static void CreateProceduralWorldScene()
    {
        var scene = CreateEmptyScene("05_ProceduralWorld");
        var root = CreateRoot("[ProceduralDebug]");

        // Add ProceduralGenerator on the same object
        root.AddComponent<ProceduralGenerator>();
        var debug = root.AddComponent<ProceduralDebug>();
        debug.generator = root.GetComponent<ProceduralGenerator>();

        SaveScene(scene, "05_ProceduralWorld");
    }

    [MenuItem("Tools/Test Scenes/06 - Combat")]
    static void CreateCombatScene()
    {
        var scene = CreateEmptyScene("06_Combat");

        // Combat debug root
        var root = CreateRoot("[CombatDebug]");
        root.AddComponent<CombatDebug>();

        // Note: Player setup requires the humanoid model + animator which can't
        // be created purely from code. The scene will have instructions.
        var instructions = new GameObject("--- READ ME ---");
        var text = instructions.AddComponent<UnityEngine.UI.Text>();
        // Just use the name to convey the message
        instructions.name = "!! Setup: Copy Player from SampleScene, or create Capsule with PlayerController !!";

        SaveScene(scene, "06_Combat");
    }

    [MenuItem("Tools/Test Scenes/07 - Camera and Feedback")]
    static void CreateCameraFeedbackScene()
    {
        var scene = CreateEmptyScene("07_CameraAndFeedback");
        var root = CreateRoot("[CameraFeedbackDebug]");
        root.AddComponent<CameraFeedbackDebug>();
        SaveScene(scene, "07_CameraAndFeedback");
    }

    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────

    static Scene CreateEmptyScene(string name)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = name;
        return scene;
    }

    static GameObject CreateRoot(string name)
    {
        var go = new GameObject(name);
        return go;
    }

    static void SaveScene(Scene scene, string filename)
    {
        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder(ScenesPath))
        {
            // Create nested folders
            if (!AssetDatabase.IsValidFolder("Assets/TestScenes"))
                AssetDatabase.CreateFolder("Assets", "TestScenes");
            if (!AssetDatabase.IsValidFolder("Assets/TestScenes/Scenes"))
                AssetDatabase.CreateFolder("Assets/TestScenes", "Scenes");
        }

        string path = $"{ScenesPath}/{filename}.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[TestSceneCreator] Created: {path}");
    }
}
