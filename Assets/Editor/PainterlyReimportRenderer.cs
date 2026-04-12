using UnityEditor;
using UnityEngine;

public static class PainterlyReimportRenderer
{
    public static void Execute()
    {
        AssetDatabase.ImportAsset("Assets/Settings/PC_Renderer.asset", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log("Reimported PC_Renderer.asset");
    }
}
