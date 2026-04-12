// Diagnostic — checks whether ab_rock_large in 08_ArtBible has a non-empty
// vertex color array on its imported mesh, and whether Toon_Stone.mat actually
// uses ToonLit.shader (the shader we patched to read vertex colors).
//
// If the colors array is empty: the FBX importer dropped them (need to enable
// "Vertex Colors → Import" on the FBX import settings).
// If the colors are present but uniform white: the bake didn't write or the
// export lost them.
// If the shader is wrong: we patched the wrong shader.

using UnityEditor;
using UnityEngine;

public static class PainterlyVertexColorDiag
{
    public static void Execute()
    {
        // ---- Check the FBX import settings ----
        const string FBX = "Assets/Models/ArtBible/ArtBibleProps.fbx";
        var importer = AssetImporter.GetAtPath(FBX) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"No ModelImporter at {FBX}");
            return;
        }
        Debug.Log($"[Diag] FBX importer: importVisibility={importer.importVisibility}, " +
                  $"keepQuads={importer.keepQuads}, optimizeMesh={importer.optimizeMeshPolygons}");
        // Unity 6 uses ModelImporter.importBlendShapes etc. — vertex colors are
        // imported by default unless the mesh has none. Print the property if it exists.

        // ---- Check the imported mesh's vertex colors ----
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
        if (prefab == null)
        {
            Debug.LogError("Could not load FBX as GameObject");
            return;
        }
        foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var m = mf.sharedMesh;
            if (m == null) continue;
            var c = m.colors;
            if (c == null || c.Length == 0)
            {
                Debug.LogWarning($"[Diag]  {mf.name}: NO vertex colors on imported mesh");
                continue;
            }
            // Check the spread (min/max per channel)
            float rMin = 1, rMax = 0, gMin = 1, gMax = 0, bMin = 1, bMax = 0;
            for (int i = 0; i < c.Length; i++)
            {
                rMin = Mathf.Min(rMin, c[i].r); rMax = Mathf.Max(rMax, c[i].r);
                gMin = Mathf.Min(gMin, c[i].g); gMax = Mathf.Max(gMax, c[i].g);
                bMin = Mathf.Min(bMin, c[i].b); bMax = Mathf.Max(bMax, c[i].b);
            }
            Debug.Log($"[Diag]  {mf.name}: {c.Length} vert colors  R[{rMin:F2}..{rMax:F2}] G[{gMin:F2}..{gMax:F2}] B[{bMin:F2}..{bMax:F2}]");
        }

        // ---- Check Toon_Stone.mat shader ----
        var stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Painterly/Toon_Stone.mat");
        if (stoneMat != null)
        {
            Debug.Log($"[Diag] Toon_Stone shader = {stoneMat.shader.name}");
            Debug.Log($"[Diag] Toon_Stone _VertexColorStrength = {stoneMat.GetFloat("_VertexColorStrength")}");
        }

        // ---- Check the actual scene mesh assigned ----
        var sceneRock = GameObject.Find("ArtBible_Content/Props/ab_rock_large");
        if (sceneRock != null)
        {
            var mf = sceneRock.GetComponent<MeshFilter>();
            var mr = sceneRock.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null)
            {
                Debug.Log($"[Diag] scene ab_rock_large mesh colors count = {mf.sharedMesh.colors.Length}");
            }
            if (mr != null && mr.sharedMaterial != null)
            {
                Debug.Log($"[Diag] scene ab_rock_large material = {mr.sharedMaterial.name} shader = {mr.sharedMaterial.shader.name}");
            }
        }
    }
}
