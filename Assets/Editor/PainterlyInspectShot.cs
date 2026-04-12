using System.IO;
using UnityEditor;
using UnityEngine;

public static class PainterlyInspectShot
{
    public static void Execute()
    {
        var path = "Assets/_TempScreenshots/ArtBibleShot.png";
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        // Sample 4 corners + center
        Vector2Int[] pts = {
            new Vector2Int(5, 5),
            new Vector2Int(tex.width-6, 5),
            new Vector2Int(5, tex.height-6),
            new Vector2Int(tex.width-6, tex.height-6),
            new Vector2Int(tex.width/2, tex.height/2),
        };
        foreach (var p in pts)
        {
            var c = tex.GetPixel(p.x, p.y);
            Debug.Log($"px ({p.x},{p.y}) = R:{c.r:F3} G:{c.g:F3} B:{c.b:F3} A:{c.a:F3}");
        }
        Debug.Log($"Texture size: {tex.width}x{tex.height}");
        Object.DestroyImmediate(tex);
    }
}
