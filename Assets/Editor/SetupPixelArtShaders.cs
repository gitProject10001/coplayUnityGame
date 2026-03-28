using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.IO;

public class SetupPixelArtShaders
{
    public static string Execute()
    {
        string result = "";

        // 1. Create ToonLit material
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader == null)
            return "ERROR: Custom/ToonLit shader not found. Make sure it compiled.";

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // Create ToonLit material
        Material toonMat = new Material(toonShader);
        toonMat.SetColor("_BaseColor", Color.white);
        toonMat.SetFloat("_LightSteps", 3);
        toonMat.SetFloat("_EdgeSmoothness", 0.05f);
        toonMat.SetColor("_ShadowColor", new Color(0.3f, 0.3f, 0.5f, 1f));
        toonMat.SetFloat("_AmbientStrength", 0.15f);
        AssetDatabase.CreateAsset(toonMat, matDir + "/ToonLit.mat");
        result += "Created ToonLit.mat\n";

        // 2. Create palette textures
        string palDir = "Assets/Textures/Palettes";
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(palDir))
            AssetDatabase.CreateFolder("Assets/Textures", "Palettes");

        // GameBoy palette (4 colors)
        Color[] gameboyColors = {
            new Color(0.06f, 0.22f, 0.06f),  // darkest green
            new Color(0.19f, 0.38f, 0.19f),  // dark green
            new Color(0.55f, 0.67f, 0.06f),  // light green
            new Color(0.61f, 0.74f, 0.06f)   // lightest green
        };
        CreatePaletteTexture(palDir + "/Palette_GameBoy.png", gameboyColors);
        result += "Created Palette_GameBoy.png\n";

        // Retro 16-color palette
        Color[] retroColors = {
            Color.black,
            new Color(0.5f, 0f, 0f),        // dark red
            new Color(0f, 0.5f, 0f),         // dark green
            new Color(0.5f, 0.5f, 0f),       // dark yellow
            new Color(0f, 0f, 0.5f),         // dark blue
            new Color(0.5f, 0f, 0.5f),       // dark magenta
            new Color(0f, 0.5f, 0.5f),       // dark cyan
            new Color(0.75f, 0.75f, 0.75f),  // light gray
            new Color(0.5f, 0.5f, 0.5f),     // dark gray
            Color.red,
            Color.green,
            Color.yellow,
            Color.blue,
            Color.magenta,
            Color.cyan,
            Color.white
        };
        CreatePaletteTexture(palDir + "/Palette_Retro16.png", retroColors);
        result += "Created Palette_Retro16.png\n";

        // Earthy/warm palette (8 colors)
        Color[] earthyColors = {
            new Color(0.13f, 0.07f, 0.07f),  // near black
            new Color(0.35f, 0.15f, 0.10f),  // dark brown
            new Color(0.60f, 0.30f, 0.15f),  // brown
            new Color(0.80f, 0.55f, 0.25f),  // tan
            new Color(0.90f, 0.75f, 0.45f),  // sand
            new Color(0.45f, 0.55f, 0.30f),  // olive
            new Color(0.25f, 0.35f, 0.45f),  // steel blue
            new Color(0.85f, 0.85f, 0.80f)   // off-white
        };
        CreatePaletteTexture(palDir + "/Palette_Earthy8.png", earthyColors);
        result += "Created Palette_Earthy8.png\n";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Assign ToonLit material to scene objects with renderers
        // Ground
        AssignToonMaterial("Ground", toonMat, ref result);

        // Player mesh renderers
        var playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            var renderers = playerObj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material newMat = new Material(toonShader);
                    if (mats[i] != null)
                    {
                        newMat.SetColor("_BaseColor", mats[i].color);
                    }
                    newMat.SetFloat("_LightSteps", 3);
                    newMat.SetFloat("_EdgeSmoothness", 0.05f);
                    newMat.SetColor("_ShadowColor", new Color(0.3f, 0.3f, 0.5f, 1f));
                    newMat.SetFloat("_AmbientStrength", 0.15f);
                    string matName = "ToonLit_" + r.gameObject.name + "_" + i;
                    string matPath = matDir + "/" + matName + ".mat";
                    AssetDatabase.CreateAsset(newMat, matPath);
                    mats[i] = newMat;
                }
                r.sharedMaterials = mats;
                result += "Assigned ToonLit to " + r.gameObject.name + "\n";
            }
        }

        // Procedural cubes
        var procGen = GameObject.Find("ProceduralGenerator");
        if (procGen != null)
        {
            var renderers = procGen.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = toonMat;
            }
            result += "Assigned ToonLit to " + renderers.Length + " procedural cubes\n";
        }

        // 4. Configure PixelizeFeature on URP renderer
        // Find the renderer asset
        string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in rendererGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null) continue;

            // Check if PixelizeFeature already exists
            bool found = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is PixelizeFeature pf)
                {
                    pf.settings.convexOutlinesOnly = true;
                    pf.settings.depthOutlineStrength = 0.8f;
                    pf.settings.normalOutlineStrength = 0.6f;
                    pf.settings.usePalette = false; // off by default, user can toggle
                    result += "Updated PixelizeFeature settings on " + path + "\n";
                    found = true;
                    EditorUtility.SetDirty(rendererData);
                    break;
                }
            }
            if (!found)
            {
                result += "No PixelizeFeature found on " + path + " (add it manually in Inspector)\n";
            }
        }

        AssetDatabase.SaveAssets();

        // 5. Save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        result += "Scene saved.\n";

        return result;
    }

    static void AssignToonMaterial(string objName, Material mat, ref string result)
    {
        var obj = GameObject.Find(objName);
        if (obj != null)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
                result += "Assigned ToonLit to " + objName + "\n";
            }
        }
    }

    static void CreatePaletteTexture(string path, Color[] colors)
    {
        Texture2D tex = new Texture2D(colors.Length, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < colors.Length; i++)
        {
            tex.SetPixel(i, 0, colors[i]);
        }
        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        string fullPath = Path.Combine(Application.dataPath, "..", path);
        File.WriteAllBytes(fullPath, pngData);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);

        // Set texture import settings for palette
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
        }
    }
}
