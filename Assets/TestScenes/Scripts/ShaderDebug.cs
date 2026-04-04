using UnityEngine;

/// <summary>
/// Test harness for ToonLit and GroundToon shaders.
/// Creates primitive meshes with materials and exposes all shader properties as live sliders.
/// Attach to an empty GameObject in 01_ToonShaders scene.
/// </summary>
[ExecuteAlways]
public class ShaderDebug : MonoBehaviour
{
    [Header("=== SCENE SETUP ===")]
    [Tooltip("Leave null to auto-find or create")]
    public Light directionalLight;

    [Header("=== TOON LIT SHADER ===")]
    public Color toonBaseColor = Color.white;
    public Color toonShadowColor = new Color(0.3f, 0.25f, 0.4f, 1f);
    [Range(2, 8)] public int lightSteps = 3;
    [Range(0f, 0.5f)] public float edgeSmoothness = 0.05f;
    [Range(0f, 1f)] public float ambientStrength = 0.15f;

    [Header("Specular")]
    public bool enableSpecular = false;
    [Range(0f, 1f)] public float specularIntensity = 0.5f;
    [Range(0.01f, 1f)] public float specularThreshold = 0.5f;

    [Header("Rim Light")]
    public bool enableRimLight = false;
    public Color rimColor = Color.white;
    [Range(0f, 1f)] public float rimPower = 0.5f;

    [Header("=== GROUND TOON SHADER ===")]
    public Color groundBaseColor = new Color(0.2f, 0.28f, 0.15f);
    public Color groundDirtColor = new Color(0.25f, 0.2f, 0.12f);
    public Color groundMossColor = new Color(0.2f, 0.32f, 0.12f);
    public Color groundPathColor = new Color(0.3f, 0.28f, 0.18f);
    public Color groundStoneColor = new Color(0.35f, 0.33f, 0.3f);
    [Range(0.01f, 2f)] public float groundNoiseScale = 0.3f;
    [Range(0.1f, 5f)] public float groundDetailNoise = 1.5f;
    [Range(0.01f, 0.5f)] public float groundStoneNoise = 0.08f;
    [Range(0f, 1f)] public float groundPatchBlend = 0.6f;

    [Header("=== DIRECTIONAL LIGHT ===")]
    [Range(0f, 360f)] public float lightRotationY = 50f;
    [Range(10f, 90f)] public float lightRotationX = 50f;
    [Range(0f, 3f)] public float lightIntensity = 1f;
    public Color lightColor = new Color(1f, 0.96f, 0.84f);

    // Internal references
    private Material toonMat;
    private Material groundMat;
    private GameObject[] toonObjects;
    private GameObject groundPlane;
    private MaterialPropertyBlock toonProps;
    private bool sceneBuilt;

    void OnEnable()
    {
        toonProps = new MaterialPropertyBlock();
        BuildScene();
    }

    void BuildScene()
    {
        if (sceneBuilt) return;

        // Find or create light
        if (directionalLight == null)
        {
            directionalLight = FindAnyObjectByType<Light>();
            if (directionalLight == null)
            {
                var lightGO = new GameObject("Debug Directional Light");
                lightGO.transform.SetParent(transform);
                directionalLight = lightGO.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                directionalLight.shadows = LightShadows.Soft;
            }
        }

        // Find or create camera
        if (Camera.main == null)
        {
            var camGO = new GameObject("Debug Camera");
            camGO.transform.SetParent(transform);
            var cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGO.transform.position = new Vector3(-6, 8, -6);
            camGO.transform.rotation = Quaternion.Euler(45, 45, 0);
        }

        // Find shaders
        Shader toonShader = Shader.Find("Custom/ToonLit");
        Shader groundShader = Shader.Find("Custom/GroundToon");

        if (toonShader != null)
            toonMat = new Material(toonShader);
        if (groundShader != null)
            groundMat = new Material(groundShader);

        // Create test objects with ToonLit
        if (toonMat != null)
        {
            toonObjects = new GameObject[4];
            toonObjects[0] = CreatePrimitive(PrimitiveType.Sphere, new Vector3(-3, 1, 0), "Test Sphere");
            toonObjects[1] = CreatePrimitive(PrimitiveType.Cube, new Vector3(-1, 1, 0), "Test Cube");
            toonObjects[2] = CreatePrimitive(PrimitiveType.Cylinder, new Vector3(1, 1, 0), "Test Cylinder");
            toonObjects[3] = CreatePrimitive(PrimitiveType.Capsule, new Vector3(3, 1, 0), "Test Capsule");

            foreach (var obj in toonObjects)
            {
                obj.transform.SetParent(transform);
                obj.GetComponent<Renderer>().material = toonMat;
            }
        }

        // Create ground plane
        if (groundMat != null)
        {
            groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "Test Ground";
            groundPlane.transform.SetParent(transform);
            groundPlane.transform.position = Vector3.zero;
            groundPlane.transform.localScale = new Vector3(3, 1, 3);
            groundPlane.GetComponent<Renderer>().material = groundMat;
        }

        sceneBuilt = true;
    }

    GameObject CreatePrimitive(PrimitiveType type, Vector3 position, string name)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        return go;
    }

    void Update()
    {
        UpdateLight();
        UpdateToonMaterial();
        UpdateGroundMaterial();
    }

    void UpdateLight()
    {
        if (directionalLight == null) return;
        directionalLight.transform.rotation = Quaternion.Euler(lightRotationX, lightRotationY, 0);
        directionalLight.intensity = lightIntensity;
        directionalLight.color = lightColor;
    }

    void UpdateToonMaterial()
    {
        if (toonMat == null) return;

        toonMat.SetColor("_BaseColor", toonBaseColor);
        toonMat.SetColor("_ShadowColor", toonShadowColor);
        toonMat.SetFloat("_LightSteps", lightSteps);
        toonMat.SetFloat("_EdgeSmoothness", edgeSmoothness);
        toonMat.SetFloat("_AmbientStrength", ambientStrength);

        toonMat.SetFloat("_EnableSpecular", enableSpecular ? 1f : 0f);
        toonMat.SetFloat("_SpecularIntensity", specularIntensity);
        toonMat.SetFloat("_SpecularThreshold", specularThreshold);

        toonMat.SetFloat("_EnableRim", enableRimLight ? 1f : 0f);
        toonMat.SetColor("_RimColor", rimColor);
        toonMat.SetFloat("_RimPower", rimPower);
    }

    void UpdateGroundMaterial()
    {
        if (groundMat == null) return;

        groundMat.SetColor("_BaseColor", groundBaseColor);
        groundMat.SetColor("_DirtColor", groundDirtColor);
        groundMat.SetColor("_MossColor", groundMossColor);
        groundMat.SetColor("_PathColor", groundPathColor);
        groundMat.SetColor("_StoneColor", groundStoneColor);
        groundMat.SetFloat("_NoiseScale", groundNoiseScale);
        groundMat.SetFloat("_DetailNoiseScale", groundDetailNoise);
        groundMat.SetFloat("_StoneNoiseScale", groundStoneNoise);
        groundMat.SetFloat("_PatchBlend", groundPatchBlend);
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUI.color = Color.white;

        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;

        GUILayout.Box($"=== SHADER DEBUG ===\n" +
            $"ToonLit: {lightSteps} steps, smooth={edgeSmoothness:F2}\n" +
            $"Specular: {(enableSpecular ? "ON" : "OFF")}, Rim: {(enableRimLight ? "ON" : "OFF")}\n" +
            $"Light: rot=({lightRotationX:F0},{lightRotationY:F0}) int={lightIntensity:F1}\n" +
            $"Ground: noise={groundNoiseScale:F2}, blend={groundPatchBlend:F2}",
            style);

        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        // Draw light direction
        if (directionalLight != null)
        {
            Gizmos.color = lightColor;
            Vector3 lightPos = new Vector3(0, 10, 0);
            Gizmos.DrawLine(lightPos, lightPos + directionalLight.transform.forward * 5f);
            Gizmos.DrawWireSphere(lightPos, 0.3f);
        }
    }

    void OnDisable()
    {
        if (toonMat != null) DestroyImmediate(toonMat);
        if (groundMat != null) DestroyImmediate(groundMat);
    }
}
