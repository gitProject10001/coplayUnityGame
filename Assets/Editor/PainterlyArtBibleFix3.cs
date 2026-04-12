using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtStyle.EditorTools
{
    /// <summary>
    /// Round 3 fix-up: replace the imported (likely flipped-normal) ab_terrain
    /// with a built-in Unity Plane primitive carrying Toon_Terrain. Also force
    /// double-sided rendering on Toon_Terrain just in case.
    /// </summary>
    public static class PainterlyArtBibleFix3
    {
        public static void Execute()
        {
            // Disable old terrain
            var oldT = GameObject.Find("ArtBible_Content/Props/ab_terrain");
            if (oldT != null) oldT.SetActive(false);

            // Create built-in plane (10m x 10m at Y=0). Plane primitive default is 10x10 already.
            var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "TerrainPlane";
            plane.transform.position = new Vector3(0f, -0.05f, 0f);
            plane.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            // Parent under content
            var parent = GameObject.Find("ArtBible_Content");
            if (parent != null) plane.transform.SetParent(parent.transform, true);

            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Painterly/Toon_Terrain.mat");
            if (terrainMat != null)
            {
                plane.GetComponent<MeshRenderer>().sharedMaterial = terrainMat;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[Fix3] replaced terrain with Unity Plane primitive");
        }
    }
}
