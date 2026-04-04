using UnityEngine;

/// <summary>
/// Test harness for the PixelWater shader.
/// Creates a water plane over submerged objects and exposes all shader properties live.
/// Attach to an empty GameObject in 04_Water scene.
/// </summary>
[ExecuteAlways]
public class WaterDebug : MonoBehaviour
{
    [Header("=== WATER COLORS ===")]
    public Color shallowColor = new Color(0.3f, 0.7f, 0.8f, 0.8f);
    public Color deepColor = new Color(0.05f, 0.15f, 0.4f, 0.95f);
    public Color foamColor = new Color(0.9f, 0.95f, 1.0f, 1.0f);

    [Header("=== WAVES ===")]
    [Range(0f, 2f)] public float waveSpeed = 0.5f;
    [Range(1f, 20f)] public float waveFrequency = 8.0f;
    [Range(0f, 0.5f)] public float waveAmplitude = 0.05f;
    public Vector2 waveDirection = new Vector2(1f, 0.5f);

    [Header("=== DEPTH ===")]
    [Range(0.1f, 10f)] public float depthFadeDistance = 2.0f;
    [Range(0.01f, 1f)] public float foamWidth = 0.2f;

    [Header("=== REFRACTION ===")]
    [Range(0f, 0.1f)] public float refractionStrength = 0.02f;

    [Header("=== SURFACE LINES ===")]
    [Range(1f, 30f)] public float lineFrequency = 12f;
    [Range(0f, 0.5f)] public float lineThickness = 0.1f;
    public Color lineColor = new Color(0.5f, 0.8f, 0.9f, 0.3f);

    [Header("=== TOON LIGHTING ===")]
    [Range(2, 6)] public int lightSteps = 3;

    [Header("=== WATER PLANE ===")]
    [Range(-2f, 2f)] public float waterHeight = 0.5f;
    [Range(1f, 10f)] public float waterSize = 4f;

    private Material waterMat;
    private GameObject waterPlane;
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
            camGO.transform.position = new Vector3(-5, 6, -5);
            camGO.transform.rotation = Quaternion.Euler(40, 45, 0);
        }

        // Ground (below water to show depth)
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Underwater Ground";
            ground.transform.SetParent(transform);
            ground.transform.position = new Vector3(0, -1.5f, 0);
            ground.transform.localScale = new Vector3(2, 1, 2);
            var mat = new Material(toonShader);
            mat.SetColor("_BaseColor", new Color(0.35f, 0.3f, 0.2f));
            ground.GetComponent<Renderer>().material = mat;

            // Submerged objects at different depths
            Color[] colors = { Color.red, Color.green, new Color(0.6f, 0.4f, 0.2f) };
            float[] depths = { -0.5f, -1f, -1.3f };
            for (int i = 0; i < 3; i++)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                obj.name = $"Submerged_{i} (depth={depths[i]:F1})";
                obj.transform.SetParent(transform);
                obj.transform.position = new Vector3(-2 + i * 2, depths[i], 0);
                var objMat = new Material(toonShader);
                objMat.SetColor("_BaseColor", colors[i]);
                obj.GetComponent<Renderer>().material = objMat;
            }

            // Objects above water for reference
            var aboveObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aboveObj.name = "Above Water Cube";
            aboveObj.transform.SetParent(transform);
            aboveObj.transform.position = new Vector3(0, 2, 3);
            var aboveMat = new Material(toonShader);
            aboveMat.SetColor("_BaseColor", Color.yellow);
            aboveObj.GetComponent<Renderer>().material = aboveMat;
        }

        // Water plane
        Shader waterShader = Shader.Find("Custom/PixelWater");
        if (waterShader != null)
        {
            waterMat = new Material(waterShader);
            waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "Water Surface";
            waterPlane.transform.SetParent(transform);
            waterPlane.transform.position = new Vector3(0, waterHeight, 0);
            waterPlane.transform.localScale = new Vector3(waterSize * 0.1f, 1, waterSize * 0.1f);
            waterPlane.GetComponent<Renderer>().material = waterMat;
        }

        sceneBuilt = true;
    }

    void Update()
    {
        if (waterMat == null) return;

        waterMat.SetColor("_ShallowColor", shallowColor);
        waterMat.SetColor("_DeepColor", deepColor);
        waterMat.SetColor("_FoamColor", foamColor);
        waterMat.SetFloat("_WaveSpeed", waveSpeed);
        waterMat.SetFloat("_WaveFrequency", waveFrequency);
        waterMat.SetFloat("_WaveAmplitude", waveAmplitude);
        waterMat.SetVector("_WaveDirection", new Vector4(waveDirection.x, 0, waveDirection.y, 0));
        waterMat.SetFloat("_DepthFadeDistance", depthFadeDistance);
        waterMat.SetFloat("_FoamWidth", foamWidth);
        waterMat.SetFloat("_RefractionStrength", refractionStrength);
        waterMat.SetFloat("_LineFrequency", lineFrequency);
        waterMat.SetFloat("_LineThickness", lineThickness);
        waterMat.SetColor("_LineColor", lineColor);
        waterMat.SetFloat("_LightSteps", lightSteps);

        // Update water plane transform
        if (waterPlane != null)
        {
            var pos = waterPlane.transform.position;
            pos.y = waterHeight;
            waterPlane.transform.position = pos;
            waterPlane.transform.localScale = new Vector3(waterSize * 0.1f, 1, waterSize * 0.1f);
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        GUILayout.Box(
            $"=== WATER DEBUG ===\n" +
            $"Waves: speed={waveSpeed:F1} freq={waveFrequency:F0} amp={waveAmplitude:F3}\n" +
            $"Depth fade: {depthFadeDistance:F1} | Foam: {foamWidth:F2}\n" +
            $"Refraction: {refractionStrength:F3}\n" +
            $"Lines: freq={lineFrequency:F0} thick={lineThickness:F2}\n" +
            $"Toon steps: {lightSteps} | Height: {waterHeight:F1}",
            style);

        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        // Draw water plane bounds
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.3f);
        Gizmos.DrawCube(new Vector3(0, waterHeight, 0), new Vector3(waterSize * 2, 0.01f, waterSize * 2));

        // Draw depth markers
        Gizmos.color = Color.cyan;
        for (float d = 0; d <= depthFadeDistance; d += depthFadeDistance * 0.25f)
        {
            float y = waterHeight - d;
            Gizmos.DrawWireCube(new Vector3(0, y, 0), new Vector3(1, 0, 1));
        }
    }

    void OnDisable()
    {
        if (waterMat != null) DestroyImmediate(waterMat);
    }
}
