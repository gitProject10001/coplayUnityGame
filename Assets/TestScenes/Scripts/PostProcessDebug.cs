using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Test harness for PixelizeFeature and VolumetricLightFeature.
/// Finds the renderer features on the active URP renderer and exposes their settings live.
/// Attach to an empty GameObject in 03_PostProcessing scene.
/// </summary>
[ExecuteAlways]
public class PostProcessDebug : MonoBehaviour
{
    [Header("=== PIXELIZE FEATURE ===")]
    public bool enablePixelize = true;
    [Range(1, 20)] public int pixelScale = 4;
    [Range(2, 256)] public int colorSteps = 16;
    public bool enableDithering = true;

    [Header("Outlines")]
    public bool enableOutlines = true;
    public Color outlineColor = Color.black;
    [Range(0.01f, 5f)] public float depthThreshold = 1.5f;
    [Range(0.1f, 3f)] public float normalThreshold = 0.5f;
    public bool convexOutlinesOnly = false;
    [Range(0f, 1f)] public float depthOutlineStrength = 1.0f;
    [Range(0f, 1f)] public float normalOutlineStrength = 0.8f;

    [Header("Fog")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.06f, 0.10f, 0.12f, 1f);
    [Range(0.1f, 10f)] public float fogDensity = 3.0f;
    [Range(0f, 1f)] public float fogStart = 0.0f;
    [Range(0f, 1f)] public float fogEnd = 1.0f;

    [Header("=== VOLUMETRIC LIGHT ===")]
    public bool enableVolumetric = true;
    [Range(0f, 1f)] public float volIntensity = 0.6f;
    [Range(4, 64)] public int volSteps = 32;
    [Range(5f, 100f)] public float volMaxDistance = 40f;
    [Range(0f, 0.99f)] public float volScattering = 0.7f;
    public Color volLightColor = new Color(0.9f, 1f, 0.75f, 1f);
    [Range(0.01f, 2f)] public float volDensity = 1.0f;
    [Range(0.5f, 20f)] public float volNoiseScale = 3f;
    [Range(0f, 1f)] public float volNoiseStrength = 0.15f;

    [Header("=== SCENE SETUP ===")]
    [Tooltip("Auto-creates test geometry if true")]
    public bool autoSetupScene = true;

    private PixelizeFeature pixelizeFeature;
    private VolumetricLightFeature volumetricFeature;
    private bool featuresFound;
    private bool sceneBuilt;

    void OnEnable()
    {
        FindFeatures();
        if (autoSetupScene) BuildScene();
    }

    void FindFeatures()
    {
        // Get the active URP renderer's features
        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset == null) return;

        // Use reflection to access renderer features since they're not publicly exposed
        var rendererDataList = urpAsset.GetType().GetField("m_RendererDataList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (rendererDataList == null) return;

        var renderers = rendererDataList.GetValue(urpAsset) as ScriptableRendererData[];
        if (renderers == null || renderers.Length == 0) return;

        foreach (var feature in renderers[0].rendererFeatures)
        {
            if (feature is PixelizeFeature pf) pixelizeFeature = pf;
            if (feature is VolumetricLightFeature vf) volumetricFeature = vf;
        }

        featuresFound = pixelizeFeature != null || volumetricFeature != null;

        // Read initial values from features
        if (pixelizeFeature != null)
        {
            var s = pixelizeFeature.settings;
            pixelScale = s.pixelScale;
            colorSteps = s.colorSteps;
            enableDithering = s.enableDithering;
            enableOutlines = s.enableOutlines;
            outlineColor = s.outlineColor;
            depthThreshold = s.depthThreshold;
            normalThreshold = s.normalThreshold;
            convexOutlinesOnly = s.convexOutlinesOnly;
            depthOutlineStrength = s.depthOutlineStrength;
            normalOutlineStrength = s.normalOutlineStrength;
            enableFog = s.enableFog;
            fogColor = s.fogColor;
            fogDensity = s.fogDensity;
            fogStart = s.fogStart;
            fogEnd = s.fogEnd;
        }

        if (volumetricFeature != null)
        {
            var s = volumetricFeature.settings;
            volIntensity = s.intensity;
            volSteps = s.steps;
            volMaxDistance = s.maxDistance;
            volScattering = s.scattering;
            volLightColor = s.lightColor;
            volDensity = s.density;
            volNoiseScale = s.noiseScale;
            volNoiseStrength = s.noiseStrength;
        }
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
            camGO.transform.position = new Vector3(-8, 10, -8);
            camGO.transform.rotation = Quaternion.Euler(45, 45, 0);
        }

        // Some test geometry for visual reference
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader != null)
        {
            Material mat = new Material(toonShader);
            mat.SetFloat("_LightSteps", 3);

            // Create a grid of objects for post-process testing
            string[] names = { "Sphere", "Cube", "Cylinder" };
            PrimitiveType[] types = { PrimitiveType.Sphere, PrimitiveType.Cube, PrimitiveType.Cylinder };
            Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, new Color(0.8f, 0.4f, 0.1f) };

            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(types[i % 3]);
                go.name = $"Test_{names[i % 3]}_{i}";
                go.transform.SetParent(transform);
                go.transform.position = new Vector3((i - 2) * 3f, 1f, 0f);
                var objMat = new Material(mat);
                objMat.SetColor("_BaseColor", colors[i]);
                go.GetComponent<Renderer>().material = objMat;
            }

            // Tall objects for volumetric occlusion
            for (int i = 0; i < 3; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Occluder_{i}";
                go.transform.SetParent(transform);
                go.transform.position = new Vector3(-4 + i * 4, 3f, 5f);
                go.transform.localScale = new Vector3(1f, 6f, 1f);
                var objMat = new Material(mat);
                objMat.SetColor("_BaseColor", new Color(0.3f, 0.25f, 0.2f));
                go.GetComponent<Renderer>().material = objMat;
            }
        }

        // Ground
        Shader groundShader = Shader.Find("Custom/GroundToon");
        if (groundShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";
            ground.transform.SetParent(transform);
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.GetComponent<Renderer>().material = new Material(groundShader);
        }

        sceneBuilt = true;
    }

    void Update()
    {
        if (!featuresFound) FindFeatures();
        ApplyPixelizeSettings();
        ApplyVolumetricSettings();
    }

    void ApplyPixelizeSettings()
    {
        if (pixelizeFeature == null) return;

        pixelizeFeature.SetActive(enablePixelize);

        var s = pixelizeFeature.settings;
        s.pixelScale = pixelScale;
        s.colorSteps = colorSteps;
        s.enableDithering = enableDithering;
        s.enableOutlines = enableOutlines;
        s.outlineColor = outlineColor;
        s.depthThreshold = depthThreshold;
        s.normalThreshold = normalThreshold;
        s.convexOutlinesOnly = convexOutlinesOnly;
        s.depthOutlineStrength = depthOutlineStrength;
        s.normalOutlineStrength = normalOutlineStrength;
        s.enableFog = enableFog;
        s.fogColor = fogColor;
        s.fogDensity = fogDensity;
        s.fogStart = fogStart;
        s.fogEnd = fogEnd;

        pixelizeFeature.Create();
    }

    void ApplyVolumetricSettings()
    {
        if (volumetricFeature == null) return;

        volumetricFeature.SetActive(enableVolumetric);

        var s = volumetricFeature.settings;
        s.intensity = volIntensity;
        s.steps = volSteps;
        s.maxDistance = volMaxDistance;
        s.scattering = volScattering;
        s.lightColor = volLightColor;
        s.density = volDensity;
        s.noiseScale = volNoiseScale;
        s.noiseStrength = volNoiseStrength;

        volumetricFeature.Create();
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 280));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        string pixelInfo = pixelizeFeature != null
            ? $"  Scale: {pixelScale}x | Colors: {colorSteps} | Dither: {(enableDithering ? "ON" : "OFF")}\n" +
              $"  Outlines: {(enableOutlines ? "ON" : "OFF")} (depth={depthThreshold:F1} norm={normalThreshold:F1})\n" +
              $"  Fog: {(enableFog ? "ON" : "OFF")} (density={fogDensity:F1})"
            : "  NOT FOUND on renderer";

        string volInfo = volumetricFeature != null
            ? $"  Intensity: {volIntensity:F2} | Steps: {volSteps} | Scatter: {volScattering:F2}\n" +
              $"  Density: {volDensity:F2} | Noise: scale={volNoiseScale:F1} str={volNoiseStrength:F2}"
            : "  NOT FOUND on renderer";

        GUILayout.Box(
            $"=== POST-PROCESS DEBUG ===\n\n" +
            $"PIXELIZE [{(enablePixelize ? "ON" : "OFF")}]:\n{pixelInfo}\n\n" +
            $"VOLUMETRIC [{(enableVolumetric ? "ON" : "OFF")}]:\n{volInfo}",
            style);

        GUILayout.EndArea();
    }
}
