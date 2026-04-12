using UnityEditor;
using UnityEngine;

namespace ArtStyle.EditorTools
{
    public static class PainterlyVerifyImport
    {
        public static void Execute()
        {
            const string fbx = "Assets/Models/ArtBible/ArtBibleProps.fbx";
            // Force a reimport so the post-processor runs against the present materials.
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);

            var objs = AssetDatabase.LoadAllAssetsAtPath(fbx);
            int matCount = 0;
            foreach (var o in objs)
            {
                if (o is Material m)
                {
                    matCount++;
                    string shaderName = m.shader != null ? m.shader.name : "<null>";
                    Debug.Log($"[Verify] FBX material slot \"{m.name}\" -> shader=\"{shaderName}\"");
                }
            }
            // Now check what each renderer in the imported model resolves to.
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (go != null)
            {
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string mp = mats[i] != null ? AssetDatabase.GetAssetPath(mats[i]) : "<null>";
                        string mn = mats[i] != null ? mats[i].name : "<null>";
                        Debug.Log($"[Verify] {r.gameObject.name} slot {i}: {mn}  ({mp})");
                    }
                }
            }
            Debug.Log($"[Verify] Total embedded materials in FBX: {matCount}");
        }
    }
}
