using UnityEngine;
using UnityEditor;

public class SetupForestScene
{
    [MenuItem("Tools/Setup Forest Scene")]
    public static void Setup()
    {
        string result = Execute();
        Debug.Log(result);
    }

    public static string Execute()
    {
        string result = "=== Forest Scene Setup ===\n";

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // 1. Create Ground material with GroundToon shader
        Shader groundShader = Shader.Find("Custom/GroundToon");
        if (groundShader != null)
        {
            Material groundMat = new Material(groundShader);
            groundMat.SetColor("_BaseColor", new Color(0.2f, 0.28f, 0.15f, 1f));
            groundMat.SetColor("_DirtColor", new Color(0.25f, 0.2f, 0.12f, 1f));
            groundMat.SetColor("_MossColor", new Color(0.2f, 0.32f, 0.12f, 1f));
            groundMat.SetColor("_PathColor", new Color(0.3f, 0.28f, 0.18f, 1f));
            groundMat.SetColor("_ShadowColor", new Color(0.15f, 0.2f, 0.25f, 1f));
            groundMat.SetFloat("_LightSteps", 3f);
            groundMat.SetFloat("_EdgeSmoothness", 0.05f);
            groundMat.SetFloat("_AmbientStrength", 0.15f);
            groundMat.SetFloat("_NoiseScale1", 0.3f);
            groundMat.SetFloat("_NoiseScale2", 1.5f);
            groundMat.SetFloat("_PatchBlend", 0.6f);

            string groundMatPath = matDir + "/Ground.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(groundMatPath) != null)
                AssetDatabase.DeleteAsset(groundMatPath);
            AssetDatabase.CreateAsset(groundMat, groundMatPath);
            result += "Created Ground.mat\n";

            // Apply to Ground object in scene
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                var renderer = ground.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = groundMat;
                    result += "Applied Ground.mat to Ground object\n";
                }
            }
        }
        else
        {
            result += "WARNING: Custom/GroundToon shader not found\n";
        }

        // 2. Create Bush billboard material
        Shader grassShader = Shader.Find("Custom/GrassBillboard");
        if (grassShader != null)
        {
            // Bush material
            Material bushMat = new Material(grassShader);
            bushMat.SetColor("_BaseColor", new Color(0.12f, 0.3f, 0.08f, 1f));
            bushMat.SetColor("_TipColor", new Color(0.2f, 0.4f, 0.12f, 1f));
            bushMat.SetFloat("_GrassHeight", 1.0f);
            bushMat.SetFloat("_GrassWidth", 0.8f);
            bushMat.SetFloat("_WindStrength", 0.08f);
            bushMat.SetFloat("_AlphaCutoff", 0.1f);
            bushMat.enableInstancing = true;

            string bushMatPath = matDir + "/BushBillboard.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(bushMatPath) != null)
                AssetDatabase.DeleteAsset(bushMatPath);
            AssetDatabase.CreateAsset(bushMat, bushMatPath);
            result += "Created BushBillboard.mat\n";

            // Small plant material
            Material plantMat = new Material(grassShader);
            plantMat.SetColor("_BaseColor", new Color(0.18f, 0.35f, 0.12f, 1f));
            plantMat.SetColor("_TipColor", new Color(0.35f, 0.55f, 0.2f, 1f));
            plantMat.SetFloat("_GrassHeight", 0.3f);
            plantMat.SetFloat("_GrassWidth", 0.25f);
            plantMat.SetFloat("_WindStrength", 0.2f);
            plantMat.SetFloat("_AlphaCutoff", 0.1f);
            plantMat.enableInstancing = true;

            string plantMatPath = matDir + "/SmallPlant.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(plantMatPath) != null)
                AssetDatabase.DeleteAsset(plantMatPath);
            AssetDatabase.CreateAsset(plantMat, plantMatPath);
            result += "Created SmallPlant.mat\n";

            // Assign to GrassSpawner if it exists
            var spawnerObj = GameObject.FindObjectOfType<GrassSpawner>();
            if (spawnerObj != null)
            {
                spawnerObj.bushMaterial = bushMat;
                spawnerObj.smallPlantMaterial = plantMat;
                result += "Assigned materials to GrassSpawner\n";
            }
        }

        // 3. Create Dust Motes material
        Shader particleShader = Shader.Find("Custom/ParticleBillboard");
        if (particleShader != null)
        {
            Material dustMat = new Material(particleShader);
            dustMat.SetColor("_BaseColor", new Color(1.0f, 0.9f, 0.6f, 0.8f));
            dustMat.enableInstancing = true;

            string dustMatPath = matDir + "/DustMotes.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(dustMatPath) != null)
                AssetDatabase.DeleteAsset(dustMatPath);
            AssetDatabase.CreateAsset(dustMat, dustMatPath);
            result += "Created DustMotes.mat\n";

            // Create FloatingParticles if not present
            var particles = GameObject.FindObjectOfType<FloatingParticles>();
            if (particles == null)
            {
                var particleObj = new GameObject("FloatingParticles");
                particles = particleObj.AddComponent<FloatingParticles>();
                result += "Created FloatingParticles object\n";
            }
            particles.particleMaterial = dustMat;
            result += "Assigned DustMotes.mat to FloatingParticles\n";
        }

        // 4. Create 16-color forest palette
        CreateForestPalette(matDir);
        result += "Created Forest palette texture\n";

        // 5. Configure directional light
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1.0f, 0.92f, 0.75f, 1f);
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(45f, 30f, 0f);
                light.shadows = LightShadows.Soft;
                light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
                result += "Configured directional light: warm gold, 45deg, shadows\n";
                break;
            }
        }

        // 6. Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.12f, 0.08f, 1f);
        result += "Set ambient to dark forest green\n";

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        result += "=== Setup Complete ===";
        return result;
    }

    private static void CreateForestPalette(string matDir)
    {
        string texDir = "Assets/Textures/Palettes";
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder(texDir))
            AssetDatabase.CreateFolder("Assets/Textures", "Palettes");

        // 16-color forest palette inspired by Tunic
        Color[] colors = new Color[]
        {
            new Color(0.04f, 0.04f, 0.04f),  // near-black (outlines/deep shadow)
            new Color(0.10f, 0.16f, 0.10f),  // dark forest atmosphere
            new Color(0.10f, 0.23f, 0.10f),  // dark green shadow
            new Color(0.18f, 0.35f, 0.18f),  // mid green
            new Color(0.29f, 0.54f, 0.23f),  // bright green
            new Color(0.35f, 0.60f, 0.25f),  // highlight green
            new Color(0.17f, 0.31f, 0.12f),  // grass green
            new Color(0.10f, 0.25f, 0.25f),  // dark teal
            new Color(0.17f, 0.42f, 0.35f),  // mid teal
            new Color(0.23f, 0.16f, 0.10f),  // dark earth/bark
            new Color(0.35f, 0.29f, 0.16f),  // mid earth
            new Color(0.54f, 0.48f, 0.29f),  // light earth/sand
            new Color(0.75f, 0.63f, 0.31f),  // warm gold
            new Color(0.91f, 0.82f, 0.50f),  // bright gold/highlight
            new Color(0.23f, 0.23f, 0.23f),  // dark gray stone
            new Color(0.42f, 0.42f, 0.42f),  // mid gray stone
        };

        Texture2D palette = new Texture2D(16, 1, TextureFormat.RGBA32, false);
        palette.filterMode = FilterMode.Point;
        palette.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < 16; i++)
        {
            palette.SetPixel(i, 0, colors[i]);
        }
        palette.Apply();

        string path = texDir + "/Palette_Forest.png";
        byte[] pngData = palette.EncodeToPNG();
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(Application.dataPath, "../" + path),
            pngData
        );
        Object.DestroyImmediate(palette);

        AssetDatabase.Refresh();

        // Set import settings for palette
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
