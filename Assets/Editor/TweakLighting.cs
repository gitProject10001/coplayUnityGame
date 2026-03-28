using UnityEngine;
using UnityEditor;

public class TweakLighting
{
    public static string Execute()
    {
        string result = "";

        // Brighten ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.2f, 0.15f, 1f);
        result += "Ambient set to brighter forest green\n";

        // Increase directional light intensity
        var lights = Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1.0f, 0.95f, 0.8f, 1f);
                light.intensity = 1.5f;
                light.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
                result += "Light intensity=1.5, color warm, angle 50deg\n";
                break;
            }
        }

        // Make ground plane bigger to cover the spawn area
        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            ground.transform.localScale = new Vector3(5, 1, 5);
            result += "Ground scaled to 5x5 (50x50 units)\n";
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        return result;
    }
}
