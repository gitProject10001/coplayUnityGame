using UnityEngine;

/// <summary>
/// Test harness for ProceduralGenerator.
/// Exposes generation parameters and provides a "Regenerate" button.
/// Attach to the same GameObject as ProceduralGenerator in 05_ProceduralWorld scene.
/// </summary>
[ExecuteAlways]
public class ProceduralDebug : MonoBehaviour
{
    [Header("=== GENERATION CONTROLS ===")]
    public ProceduralGenerator generator;

    [Header("Counts")]
    [Range(0, 50)] public int rockCount = 20;
    [Range(0, 20)] public int treeCount = 8;
    [Range(0, 60)] public int bushCount = 25;

    [Header("Layout")]
    [Range(10f, 60f)] public float spawnAreaSize = 30f;

    [Header("Seed")]
    public int randomSeed = 42;
    public bool useSeed = true;

    [Header("Toggles")]
    public bool showRocks = true;
    public bool showTrees = true;
    public bool showBushes = true;

    [Header("Actions")]
    [Tooltip("Check to regenerate environment")]
    public bool regenerate = false;

    [Header("=== DEBUG VISUALIZATION ===")]
    public bool showSpawnBounds = true;
    public bool showClusterCenters = true;

    private bool sceneBuilt;

    void OnEnable()
    {
        BuildScene();
    }

    void BuildScene()
    {
        if (sceneBuilt) return;

        // Light
        if (FindAnyObjectByType<Light>() == null)
        {
            var lightGO = new GameObject("Debug Directional Light");
            lightGO.transform.SetParent(transform);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50, 50, 0);
        }

        // Camera
        if (Camera.main == null)
        {
            var camGO = new GameObject("Debug Camera");
            camGO.transform.SetParent(transform);
            var cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGO.transform.position = new Vector3(-15, 18, -15);
            camGO.transform.rotation = Quaternion.Euler(45, 45, 0);
        }

        // Ground
        Shader groundShader = Shader.Find("Custom/GroundToon");
        if (groundShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";
            ground.transform.SetParent(transform);
            ground.transform.localScale = new Vector3(8, 1, 8);
            ground.GetComponent<Renderer>().material = new Material(groundShader);
        }

        // Find or add ProceduralGenerator
        if (generator == null) generator = GetComponent<ProceduralGenerator>();
        if (generator == null) generator = FindAnyObjectByType<ProceduralGenerator>();

        if (generator != null)
        {
            rockCount = generator.rockCount;
            treeCount = generator.treeCount;
            bushCount = generator.bushCount;
            spawnAreaSize = generator.spawnAreaSize;
        }

        sceneBuilt = true;
    }

    void Update()
    {
        if (generator == null) return;

        // Sync parameters
        generator.rockCount = showRocks ? rockCount : 0;
        generator.treeCount = showTrees ? treeCount : 0;
        generator.bushCount = showBushes ? bushCount : 0;
        generator.spawnAreaSize = spawnAreaSize;

        // Regenerate on request
        if (regenerate)
        {
            regenerate = false;
            if (useSeed) Random.InitState(randomSeed);
            generator.forceRegenerate = true;
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 220));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        string genInfo = generator != null
            ? $"Rocks: {rockCount} [{(showRocks ? "ON" : "OFF")}]\n" +
              $"Trees: {treeCount} [{(showTrees ? "ON" : "OFF")}]\n" +
              $"Bushes: {bushCount} [{(showBushes ? "ON" : "OFF")}]\n" +
              $"Area: {spawnAreaSize:F0}x{spawnAreaSize:F0}\n" +
              $"Seed: {(useSeed ? randomSeed.ToString() : "random")}"
            : "ProceduralGenerator NOT FOUND";

        GUILayout.Box(
            $"=== PROCEDURAL DEBUG ===\n\n{genInfo}\n\n" +
            $"Tip: Check 'regenerate' in Inspector\nor change seed & counts to experiment",
            style);

        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (showSpawnBounds)
        {
            float half = spawnAreaSize * 0.5f;
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(spawnAreaSize, 0.1f, spawnAreaSize));
        }

        if (showClusterCenters && generator != null)
        {
            // Visualize approximate cluster areas
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
            float half = spawnAreaSize * 0.5f;
            // Draw grid of potential cluster zones
            for (float x = -half * 0.6f; x <= half * 0.6f; x += half * 0.4f)
            {
                for (float z = -half * 0.6f; z <= half * 0.6f; z += half * 0.4f)
                {
                    Gizmos.DrawWireSphere(new Vector3(x, 0, z), 3f);
                }
            }
        }
    }
}
