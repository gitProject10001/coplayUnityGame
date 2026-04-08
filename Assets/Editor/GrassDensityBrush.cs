using UnityEditor;
using UnityEngine;
using System.IO;

public class GrassDensityBrush : EditorWindow
{
    [MenuItem("Tools/Grass Density Brush")]
    static void Open() => GetWindow<GrassDensityBrush>("Grass Brush");

    // References
    private GrassGroundSetup groundSetup;
    private Material grassMaterial;
    private Texture2D workingTexture;
    private Texture2D originalAssetTexture;

    // Brush settings
    private bool isPainting = false;
    private bool eraseMode = false;
    private float brushSize = 2f;
    private float brushOpacity = 0.5f;

    // State
    private bool isDirty = false;
    private const string MASK_PATH = "Assets/Textures/GrassMask.png";
    private const int TARGET_RESOLUTION = 512;

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindReferences();
        LoadWorkingTexture();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        if (isDirty)
        {
            if (EditorUtility.DisplayDialog("Grass Brush",
                "You have unsaved changes to the grass mask. Save now?",
                "Save", "Discard"))
            {
                SaveTextureToDisk();
            }
        }

        RestoreOriginalTexture();

        if (workingTexture != null)
            DestroyImmediate(workingTexture);
    }

    void FindReferences()
    {
        groundSetup = FindObjectOfType<GrassGroundSetup>();
        if (groundSetup == null) return;

        // Try the public field on the component first
        if (groundSetup.grassMaterial != null)
        {
            grassMaterial = groundSetup.grassMaterial;
            return;
        }

        // Try the MeshRenderer (may not exist in editor before Awake)
        var renderer = groundSetup.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            grassMaterial = renderer.sharedMaterial;
            return;
        }

        // Fallback: load the material asset directly
        grassMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GeometryGrass.mat");
    }

    void LoadWorkingTexture()
    {
        if (grassMaterial == null) return;

        // Ensure texture is readable
        var importer = AssetImporter.GetAtPath(MASK_PATH) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MASK_PATH);
        if (sourceTexture == null) return;

        originalAssetTexture = sourceTexture;

        // Determine resolution
        int res = Mathf.Max(sourceTexture.width, TARGET_RESOLUTION);

        workingTexture = new Texture2D(res, res, TextureFormat.RGBA32, true);
        workingTexture.wrapMode = TextureWrapMode.Clamp;
        workingTexture.filterMode = FilterMode.Bilinear;

        // Copy or upscale source pixels
        if (sourceTexture.width == res && sourceTexture.height == res)
        {
            workingTexture.SetPixels(sourceTexture.GetPixels());
        }
        else
        {
            // Upscale with bilinear sampling
            Color[] pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / (res - 1);
                    float v = (float)y / (res - 1);
                    pixels[y * res + x] = sourceTexture.GetPixelBilinear(u, v);
                }
            }
            workingTexture.SetPixels(pixels);
        }

        workingTexture.Apply();
        grassMaterial.SetTexture("_GrassMask", workingTexture);
    }

    void RestoreOriginalTexture()
    {
        if (grassMaterial != null && originalAssetTexture != null)
            grassMaterial.SetTexture("_GrassMask", originalAssetTexture);
    }

    // ─── Editor Window GUI ───────────────────────────────

    void OnGUI()
    {
        if (groundSetup == null)
        {
            EditorGUILayout.HelpBox("No GrassGroundSetup found in scene.", MessageType.Warning);
            if (GUILayout.Button("Refresh"))
            {
                FindReferences();
                LoadWorkingTexture();
            }
            return;
        }

        if (grassMaterial == null)
        {
            EditorGUILayout.HelpBox("Could not find GeometryGrass material. Check GrassGroundSetup.", MessageType.Error);
            if (GUILayout.Button("Refresh"))
            {
                FindReferences();
                LoadWorkingTexture();
            }
            return;
        }

        if (workingTexture == null)
        {
            EditorGUILayout.HelpBox("Could not load GrassMask texture at: " + MASK_PATH, MessageType.Error);
            if (GUILayout.Button("Refresh"))
                LoadWorkingTexture();
            return;
        }

        EditorGUILayout.Space(4);

        // Paint toggle
        EditorGUI.BeginChangeCheck();
        isPainting = GUILayout.Toggle(isPainting, isPainting ? "Brush ON" : "Brush OFF",
            "Button", GUILayout.Height(30));
        if (EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();

        EditorGUILayout.Space(4);

        // Mode
        eraseMode = GUILayout.Toolbar(eraseMode ? 1 : 0,
            new string[] { "Paint (Add Grass)", "Erase (Remove Grass)" }) == 1;

        EditorGUILayout.Space(4);

        // Brush settings
        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.5f, 10f);
        brushOpacity = EditorGUILayout.Slider("Brush Opacity", brushOpacity, 0.01f, 1f);

        EditorGUILayout.Space(8);

        // Fill buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill White (All Grass)"))
        {
            FillTexture(Color.white);
        }
        if (GUILayout.Button("Fill Black (No Grass)"))
        {
            FillTexture(Color.black);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Save
        EditorGUI.BeginDisabledGroup(!isDirty);
        if (GUILayout.Button("Save to Disk", GUILayout.Height(28)))
        {
            SaveTextureToDisk();
        }
        EditorGUI.EndDisabledGroup();

        if (isDirty)
            EditorGUILayout.HelpBox("Unsaved changes.", MessageType.Info);

        EditorGUILayout.Space(4);

        // Texture preview
        EditorGUILayout.LabelField("Mask Preview:");
        float previewSize = Mathf.Min(position.width - 20, 200);
        Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize);
        EditorGUI.DrawPreviewTexture(previewRect, workingTexture);
    }

    // ─── Scene View ──────────────────────────────────────

    void OnSceneGUI(SceneView sceneView)
    {
        if (!isPainting || groundSetup == null || workingTexture == null)
            return;

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, groundSetup.transform.position);

        if (!groundPlane.Raycast(ray, out float dist))
            return;

        Vector3 hitPoint = ray.GetPoint(dist);

        // Draw brush preview
        Handles.color = eraseMode
            ? new Color(1f, 0.2f, 0.2f, 0.8f)
            : new Color(0.2f, 1f, 0.2f, 0.8f);
        Handles.DrawWireDisc(hitPoint, Vector3.up, brushSize);

        // Handle painting
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            PaintAtWorldPosition(hitPoint);
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && !e.alt)
        {
            PaintAtWorldPosition(hitPoint);
            e.Use();
        }

        // Prevent default scene interaction while painting
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlId);
        }

        sceneView.Repaint();
        Repaint(); // update preview in window
    }

    // ─── Painting ────────────────────────────────────────

    void PaintAtWorldPosition(Vector3 worldPos)
    {
        float radius = groundSetup.radius;
        Vector3 planePos = groundSetup.transform.position;

        // World to UV
        float u = (worldPos.x - (planePos.x - radius)) / (2f * radius);
        float v = (worldPos.z - (planePos.z - radius)) / (2f * radius);
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        // Brush radius in UV space
        float brushRadiusUV = brushSize / (2f * radius);

        int texW = workingTexture.width;
        int texH = workingTexture.height;
        int centerX = Mathf.RoundToInt(u * (texW - 1));
        int centerY = Mathf.RoundToInt(v * (texH - 1));
        int pixelRadius = Mathf.CeilToInt(brushRadiusUV * texW);

        Color targetColor = eraseMode ? Color.black : Color.white;

        for (int py = -pixelRadius; py <= pixelRadius; py++)
        {
            for (int px = -pixelRadius; px <= pixelRadius; px++)
            {
                int x = centerX + px;
                int y = centerY + py;
                if (x < 0 || x >= texW || y < 0 || y >= texH) continue;

                float distNorm = Mathf.Sqrt(px * px + py * py) / pixelRadius;
                if (distNorm > 1f) continue;

                // Soft circular falloff
                float falloff = 1f - Mathf.SmoothStep(0.5f, 1f, distNorm);
                float strength = brushOpacity * falloff;

                Color existing = workingTexture.GetPixel(x, y);
                Color blended = Color.Lerp(existing, targetColor, strength);
                workingTexture.SetPixel(x, y, blended);
            }
        }

        workingTexture.Apply();
        isDirty = true;
    }

    void FillTexture(Color color)
    {
        Color[] pixels = new Color[workingTexture.width * workingTexture.height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        workingTexture.SetPixels(pixels);
        workingTexture.Apply();
        isDirty = true;
        SceneView.RepaintAll();
    }

    // ─── Save ────────────────────────────────────────────

    void SaveTextureToDisk()
    {
        byte[] png = workingTexture.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, "..", MASK_PATH);
        fullPath = Path.GetFullPath(fullPath);
        File.WriteAllBytes(fullPath, png);

        AssetDatabase.ImportAsset(MASK_PATH);

        // Fix import settings after reimport
        var importer = AssetImporter.GetAtPath(MASK_PATH) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.sRGBTexture = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        // Re-assign working texture since reimport may have broken the link
        if (grassMaterial != null)
            grassMaterial.SetTexture("_GrassMask", workingTexture);

        isDirty = false;
        Debug.Log("Grass mask saved to " + MASK_PATH);
    }
}
