using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Renders the active scene's IsoARPGCamera to a PNG so we can preview the
    /// actual game-look (volume + post-processing applied), not the editor view.
    /// Output: Assets/_TempScreenshots/ArtBibleShot.png
    /// </summary>
    public static class PainterlyRenderShot
    {
        const string OUT = "Assets/_TempScreenshots/ArtBibleShot.png";
        const int W = 1024;
        const int H = 1024;

        public static void Execute()
        {
            // Resolve a usable camera. Preference order:
            //   1. Active IsoARPGCamera (the painterly art-bible camera)
            //   2. Active PlayerCamera / Main Camera (gameplay framing)
            //   3. Inactive IsoARPGCamera, temporarily re-enabled for the shot
            Camera cam = null;
            bool tempEnabled = false;
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCams)
            {
                if (c.gameObject.name.StartsWith("IsoARPGCamera") && c.gameObject.activeInHierarchy)
                { cam = c; break; }
            }
            if (cam == null)
            {
                foreach (var c in allCams)
                {
                    if (!c.gameObject.activeInHierarchy) continue;
                    var n = c.gameObject.name;
                    if (n == "PlayerCamera" || n == "Main Camera") { cam = c; break; }
                }
            }
            if (cam == null)
            {
                foreach (var c in allCams)
                {
                    if (c.gameObject.name.StartsWith("IsoARPGCamera"))
                    {
                        c.gameObject.SetActive(true);
                        tempEnabled = true;
                        cam = c;
                        break;
                    }
                }
            }
            if (cam == null)
            {
                Debug.LogError("[PainterlyRenderShot] No usable camera found in active scene.");
                return;
            }
            Debug.Log($"[PainterlyRenderShot] Using camera: {cam.gameObject.name}");

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                useMipMap = false,
            };
            rt.Create();
            // Pre-clear the RT to camera background so the URP scriptable
            // pipeline doesn't leave garbage in the corners.
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, cam.backgroundColor);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
            var pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = 1f;
            tex.SetPixels(pixels);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            if (tempEnabled) cam.gameObject.SetActive(false);

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            Directory.CreateDirectory(Path.GetDirectoryName(OUT));
            File.WriteAllBytes(OUT, png);
            AssetDatabase.ImportAsset(OUT);
            Debug.Log($"[PainterlyRenderShot] wrote {OUT} ({png.Length} bytes)");
        }
    }
}
