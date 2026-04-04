using UnityEngine;

/// <summary>
/// Test harness for GeometryGrass, GrassSpawner, GrassDisplacement, and FloatingParticles.
/// Exposes GPU instancing and grass shader properties for live tweaking.
/// Attach to an empty GameObject in 02_GrassAndFoliage scene.
/// </summary>
[ExecuteAlways]
public class GrassDebug : MonoBehaviour
{
    [Header("=== GEOMETRY GRASS ===")]
    [Range(0.01f, 0.2f)] public float bladeWidth = 0.05f;
    [Range(0.1f, 2f)] public float bladeHeight = 0.5f;
    public Color grassBaseColor = new Color(0.05f, 0.15f, 0.02f);
    public Color grassTipColor = new Color(0.1f, 0.35f, 0.05f);
    [Range(0f, 2f)] public float windSpeed = 0.5f;
    [Range(0f, 1f)] public float windStrength = 0.3f;

    [Header("=== GRASS SPAWNER (Foliage) ===")]
    [Tooltip("GrassSpawner in scene - will be found automatically")]
    public GrassSpawner grassSpawner;
    [Range(0, 3000)] public int bushCount = 1500;
    [Range(0, 5000)] public int plantCount = 2500;
    [Range(1f, 30f)] public float spawnRadius = 15f;
    [Range(0f, 0.5f)] public float colorVariation = 0.25f;
    [Range(0.01f, 0.5f)] public float noiseScale = 0.15f;
    public bool regenerateFoliage = false;

    [Header("=== FLOATING PARTICLES ===")]
    public FloatingParticles floatingParticles;
    [Range(10, 200)] public int particleCount = 80;
    [Range(0.01f, 0.3f)] public float particleScaleMin = 0.05f;
    [Range(0.05f, 0.5f)] public float particleScaleMax = 0.15f;
    [Range(0f, 1f)] public float driftSpeed = 0.3f;
    [Range(0f, 1f)] public float noiseStrength = 0.5f;

    [Header("=== DISPLACEMENT ===")]
    public bool showDisplacementGizmo = true;
    [Range(0.5f, 5f)] public float displacementRadius = 1.5f;

    [Header("=== DEBUG VISUALIZATION ===")]
    public bool showInstancePositions = false;
    public bool showSpawnArea = true;

    private Material grassMat;
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
            camGO.transform.position = new Vector3(-8, 12, -8);
            camGO.transform.rotation = Quaternion.Euler(50, 45, 0);
        }

        // Ground plane
        Shader groundShader = Shader.Find("Custom/GroundToon");
        if (groundShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";
            ground.transform.SetParent(transform);
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.GetComponent<Renderer>().material = new Material(groundShader);
        }

        // Find existing components or note they need manual setup
        if (grassSpawner == null) grassSpawner = FindAnyObjectByType<GrassSpawner>();
        if (floatingParticles == null) floatingParticles = FindAnyObjectByType<FloatingParticles>();

        // Try to find geometry grass material
        grassMat = Resources.Load<Material>("GeometryGrass");
        if (grassMat == null)
        {
            Shader grassShader = Shader.Find("Custom/GeometryGrass");
            if (grassShader != null)
            {
                var existingMats = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (var r in existingMats)
                {
                    if (r.sharedMaterial != null && r.sharedMaterial.shader == grassShader)
                    {
                        grassMat = r.sharedMaterial;
                        break;
                    }
                }
            }
        }

        sceneBuilt = true;
    }

    void Update()
    {
        UpdateGrassMaterial();
        UpdateSpawner();
        UpdateParticles();
    }

    void UpdateGrassMaterial()
    {
        if (grassMat == null) return;

        grassMat.SetFloat("_BladeWidth", bladeWidth);
        grassMat.SetFloat("_BladeHeight", bladeHeight);
        grassMat.SetColor("_BaseColor", grassBaseColor);
        grassMat.SetColor("_TipColor", grassTipColor);
        grassMat.SetFloat("_WindSpeed", windSpeed);
        grassMat.SetFloat("_WindStrength", windStrength);
    }

    void UpdateSpawner()
    {
        if (grassSpawner == null) return;

        grassSpawner.spawnRadius = spawnRadius;
        grassSpawner.colorVariation = colorVariation;
        grassSpawner.noiseScale = noiseScale;

        if (regenerateFoliage)
        {
            regenerateFoliage = false;
            grassSpawner.bushCount = bushCount;
            grassSpawner.plantCount = plantCount;
            // Re-trigger spawning by toggling the component
            grassSpawner.enabled = false;
            grassSpawner.enabled = true;
        }
    }

    void UpdateParticles()
    {
        if (floatingParticles == null) return;

        floatingParticles.particleScaleMin = particleScaleMin;
        floatingParticles.particleScaleMax = particleScaleMax;
        floatingParticles.driftSpeed = driftSpeed;
        floatingParticles.noiseStrength = noiseStrength;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 220));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        string spawnerInfo = grassSpawner != null
            ? $"Bushes: {bushCount} | Plants: {plantCount} | Radius: {spawnRadius:F0}"
            : "NOT FOUND (add GrassSpawner to scene)";

        string particleInfo = floatingParticles != null
            ? $"Count: {particleCount} | Drift: {driftSpeed:F1} | Noise: {noiseStrength:F1}"
            : "NOT FOUND (add FloatingParticles to scene)";

        GUILayout.Box(
            $"=== GRASS & FOLIAGE DEBUG ===\n\n" +
            $"Geometry Grass: w={bladeWidth:F3} h={bladeHeight:F1}\n" +
            $"  Wind: speed={windSpeed:F1} str={windStrength:F1}\n\n" +
            $"Spawner: {spawnerInfo}\n\n" +
            $"Particles: {particleInfo}",
            style);

        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (showSpawnArea)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(Vector3.zero, spawnRadius);
        }

        if (showDisplacementGizmo)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            // Show displacement radius around any GrassDisplacementObject
            var displacers = FindObjectsByType<GrassDisplacementObject>(FindObjectsSortMode.None);
            foreach (var d in displacers)
            {
                Gizmos.DrawWireSphere(d.transform.position, displacementRadius);
            }
        }
    }
}
