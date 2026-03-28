using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateGrassTexture
{
    public static string Execute()
    {
        string dir = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Textures");

        // 1. Multi-blade grass clump
        GenerateGrassClump(dir + "/GrassBladeMulti.png", 64, 64);

        // 2. Bush cluster texture
        GenerateBushTexture(dir + "/BushClump.png", 64, 64);

        // 3. Small plant
        GenerateSmallPlantTexture(dir + "/SmallPlant.png", 32, 32);

        // Set import settings
        foreach (string p in new string[]{
            dir + "/GrassBladeMulti.png",
            dir + "/BushClump.png",
            dir + "/SmallPlant.png"
        })
        {
            TextureImporter imp = AssetImporter.GetAtPath(p) as TextureImporter;
            if (imp != null)
            {
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.SaveAndReimport();
            }
        }

        // Assign textures to materials and fix settings
        SetupMaterial("Assets/Materials/GrassBillboard.mat", dir + "/GrassBladeMulti.png",
            new Color(0.35f, 0.6f, 0.2f), new Color(0.5f, 0.8f, 0.3f), 0.5f);
        SetupMaterial("Assets/Materials/BushBillboard.mat", dir + "/BushClump.png",
            new Color(0.2f, 0.45f, 0.12f), new Color(0.3f, 0.55f, 0.18f), 0.4f);
        SetupMaterial("Assets/Materials/SmallPlant.mat", dir + "/SmallPlant.png",
            new Color(0.25f, 0.5f, 0.15f), new Color(0.4f, 0.65f, 0.25f), 0.5f);

        AssetDatabase.SaveAssets();
        return "Generated all foliage textures with bright greens.";
    }

    static void SetupMaterial(string matPath, string texPath, Color baseCol, Color tipCol, float cutoff)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (mat != null)
        {
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", baseCol);
            mat.SetColor("_TipColor", tipCol);
            mat.SetFloat("_AlphaCutoff", cutoff);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }
    }

    static void SaveTexture(Texture2D tex, string path)
    {
        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path), png);
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
    }

    // ─── Grass: 5-7 thin blades, WHITE color (shader tints), sharp alpha ───
    static void GenerateGrassClump(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        ClearTexture(tex, w, h);

        int bladeCount = 6;
        for (int b = 0; b < bladeCount; b++)
        {
            float centerX = 0.15f + (b / (float)(bladeCount - 1)) * 0.7f;
            float maxHeight = 0.55f + (b % 3) * 0.15f; // vary heights
            float curve = (b % 2 == 0 ? 1f : -1f) * 0.06f * (b * 0.5f);
            float bladeWidth = 0.025f + (b % 2) * 0.015f;
            centerX += (b % 2 == 0 ? 0.02f : -0.02f);

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                if (t > maxHeight) continue;
                float nt = t / maxHeight;

                // Blade narrows to a sharp point
                float hw = bladeWidth * (1.0f - nt * nt * 0.9f);
                float cx = centerX + Mathf.Sin(nt * 3f) * curve + nt * nt * curve;

                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float dist = Mathf.Abs(u - cx);
                    if (dist < hw)
                    {
                        // Use WHITE — the shader's _BaseColor/_TipColor will tint
                        // Slight vertical gradient for depth
                        float brightness = Mathf.Lerp(0.7f, 1.0f, nt);
                        Color existing = tex.GetPixel(x, y);
                        if (1f > existing.a)
                            tex.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                    }
                }
            }
        }
        tex.Apply();
        SaveTexture(tex, path);
    }

    // ─── Bush: round leafy cluster, WHITE + alpha for shader tinting ───
    static void GenerateBushTexture(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        ClearTexture(tex, w, h);

        // Overlapping circular blobs
        int blobCount = 7;
        Vector2[] centers = new Vector2[blobCount];
        float[] radii = new float[blobCount];
        Random.InitState(77);

        for (int i = 0; i < blobCount; i++)
        {
            centers[i] = new Vector2(
                0.5f + Random.Range(-0.22f, 0.22f),
                0.5f + Random.Range(-0.2f, 0.2f)
            );
            radii[i] = Random.Range(0.15f, 0.3f);
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)(w - 1);
                float v = y / (float)(h - 1);

                bool inside = false;
                for (int i = 0; i < blobCount; i++)
                {
                    float d = Vector2.Distance(new Vector2(u, v), centers[i]);
                    float noiseR = radii[i] + Mathf.PerlinNoise(u * 10f + i * 3, v * 10f) * 0.06f - 0.03f;
                    if (d < noiseR)
                    {
                        inside = true;
                        break;
                    }
                }

                if (inside)
                {
                    float shade = Mathf.PerlinNoise(u * 5f + 10, v * 5f + 10);
                    float brightness = Mathf.Lerp(0.6f, 1.0f, shade);
                    tex.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                }
            }
        }
        tex.Apply();
        Random.InitState(123); // restore seed
        SaveTexture(tex, path);
    }

    // ─── Small plant: tiny leaf shapes ───
    static void GenerateSmallPlantTexture(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        ClearTexture(tex, w, h);

        int leafCount = 4;
        for (int l = 0; l < leafCount; l++)
        {
            float angle = -0.35f + (l / (float)(leafCount - 1)) * 0.7f;
            float leafLen = 0.5f + l * 0.1f;
            float leafWidth = 0.06f + (l % 2) * 0.02f;

            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                if (t > leafLen) continue;
                float nt = t / leafLen;

                float shape = Mathf.Sin(nt * Mathf.PI);
                float hw = leafWidth * shape;
                float cx = 0.5f + angle * nt;

                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float dist = Mathf.Abs(u - cx);
                    if (dist < hw)
                    {
                        float brightness = Mathf.Lerp(0.7f, 1.0f, nt);
                        Color existing = tex.GetPixel(x, y);
                        if (1f > existing.a)
                            tex.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
                    }
                }
            }
        }
        tex.Apply();
        SaveTexture(tex, path);
    }

    static void ClearTexture(Texture2D tex, int w, int h)
    {
        Color[] clear = new Color[w * h];
        tex.SetPixels(clear);
    }
}
