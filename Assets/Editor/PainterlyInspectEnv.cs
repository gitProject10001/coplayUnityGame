using UnityEditor;
using UnityEngine;

public static class PainterlyInspectEnv
{
    public static void Execute()
    {
        Debug.Log($"Skybox material: {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "<null>")}");
        if (RenderSettings.skybox != null)
        {
            Debug.Log($"  shader: {RenderSettings.skybox.shader.name}");
            if (RenderSettings.skybox.HasProperty("_Tint"))
                Debug.Log($"  _Tint: {RenderSettings.skybox.GetColor("_Tint")}");
            if (RenderSettings.skybox.HasProperty("_SkyTint"))
                Debug.Log($"  _SkyTint: {RenderSettings.skybox.GetColor("_SkyTint")}");
            if (RenderSettings.skybox.HasProperty("_GroundColor"))
                Debug.Log($"  _GroundColor: {RenderSettings.skybox.GetColor("_GroundColor")}");
            if (RenderSettings.skybox.HasProperty("_Exposure"))
                Debug.Log($"  _Exposure: {RenderSettings.skybox.GetFloat("_Exposure")}");
        }
        Debug.Log($"fog: {RenderSettings.fog} color: {RenderSettings.fogColor} mode: {RenderSettings.fogMode}");
        Debug.Log($"ambientMode: {RenderSettings.ambientMode}");
        Debug.Log($"ambientSky: {RenderSettings.ambientSkyColor}");
        Debug.Log($"ambientEquator: {RenderSettings.ambientEquatorColor}");
        Debug.Log($"ambientGround: {RenderSettings.ambientGroundColor}");
        Debug.Log($"ambientIntensity: {RenderSettings.ambientIntensity}");
    }
}
