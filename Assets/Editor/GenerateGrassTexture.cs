using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateGrassTexture
{
    public static string Execute()
    {
        int width = 32;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Generate a grass blade shape: narrow at top, wider at bottom, with some variation
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1); // 0 at bottom, 1 at top
            // Blade width narrows toward the top
            float halfWidth = Mathf.Lerp(0.45f, 0.05f, t * t);
            float center = 0.5f + Mathf.Sin(t * 3.0f) * 0.05f; // Slight curve

            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float distFromCenter = Mathf.Abs(u - center);

                if (distFromCenter < halfWidth)
                {
                    // Grass color gradient: darker at base, lighter at tip
                    float colorT = t;
                    float r = Mathf.Lerp(0.2f, 0.5f, colorT);
                    float g = Mathf.Lerp(0.4f, 0.8f, colorT);
                    float b = Mathf.Lerp(0.1f, 0.2f, colorT);
                    // Soften edges
                    float edgeFade = 1.0f - Mathf.Clamp01((distFromCenter - halfWidth * 0.6f) / (halfWidth * 0.4f));
                    tex.SetPixel(x, y, new Color(r, g, b, edgeFade));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        // Also create a multi-blade variant (3 blades in one quad)
        int mw = 64, mh = 64;
        Texture2D multiTex = new Texture2D(mw, mh, TextureFormat.RGBA32, false);
        // Clear
        for (int y = 0; y < mh; y++)
            for (int x = 0; x < mw; x++)
                multiTex.SetPixel(x, y, Color.clear);

        float[] bladeOffsets = { 0.2f, 0.5f, 0.75f };
        float[] bladeHeights = { 0.7f, 1.0f, 0.8f };
        float[] bladeCurves = { -0.08f, 0.03f, 0.06f };

        foreach (int b in new int[]{0,1,2})
        {
            float cx = bladeOffsets[b];
            float maxH = bladeHeights[b];
            float curve = bladeCurves[b];

            for (int y = 0; y < mh; y++)
            {
                float t = y / (float)(mh - 1);
                if (t > maxH) continue;
                float nt = t / maxH; // normalized 0-1 within this blade
                float hw = Mathf.Lerp(0.08f, 0.015f, nt * nt);
                float bladeCenter = cx + Mathf.Sin(nt * 2.5f) * curve;

                for (int x = 0; x < mw; x++)
                {
                    float u = x / (float)(mw - 1);
                    float dist = Mathf.Abs(u - bladeCenter);
                    if (dist < hw)
                    {
                        float r = Mathf.Lerp(0.2f, 0.45f, nt);
                        float g = Mathf.Lerp(0.35f, 0.75f, nt);
                        float bl = Mathf.Lerp(0.08f, 0.15f, nt);
                        float fade = 1.0f - Mathf.Clamp01((dist - hw * 0.5f) / (hw * 0.5f));
                        Color existing = multiTex.GetPixel(x, y);
                        Color newCol = new Color(r, g, bl, fade);
                        // Blend over existing
                        if (newCol.a > existing.a)
                            multiTex.SetPixel(x, y, newCol);
                    }
                }
            }
        }
        multiTex.Apply();

        string dir = "Assets/Textures";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Textures");

        // Save textures
        string path1 = dir + "/GrassBlade.png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path1), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path1);

        string path2 = dir + "/GrassBladeMulti.png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, "..", path2), multiTex.EncodeToPNG());
        Object.DestroyImmediate(multiTex);
        AssetDatabase.ImportAsset(path2);

        // Set import settings
        foreach (string p in new string[]{ path1, path2 })
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

        // Assign multi-blade texture to grass material
        Material grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GrassBillboard.mat");
        Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path2);
        if (grassMat != null && grassTex != null)
        {
            grassMat.SetTexture("_BaseMap", grassTex);
            EditorUtility.SetDirty(grassMat);
        }

        AssetDatabase.SaveAssets();
        return "Generated grass textures and assigned to material.";
    }
}
