using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public class SetupNewFeatures
{
    public static string Execute()
    {
        string result = "";

        string matDir = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // 1. Create GrassSpawner for bushes/plants (grass handled by GeometryGrass shader)
        {
            var spawnerObj = new GameObject("GrassSpawner");
            var spawner = spawnerObj.AddComponent<GrassSpawner>();
            spawner.spawnRadius = 15f;
            spawner.clearRadius = 3f;
            result += "Created GrassSpawner in scene\n";
        }

        // 2. Create Water material and plane
        Shader waterShader = Shader.Find("Custom/PixelWater");
        if (waterShader != null)
        {
            Material waterMat = new Material(waterShader);
            waterMat.SetColor("_ShallowColor", new Color(0.3f, 0.65f, 0.75f, 0.7f));
            waterMat.SetColor("_DeepColor", new Color(0.05f, 0.12f, 0.35f, 0.9f));
            waterMat.SetColor("_FoamColor", new Color(0.85f, 0.9f, 1f, 1f));
            waterMat.SetFloat("_WaveSpeed", 0.4f);
            waterMat.SetFloat("_WaveAmplitude", 0.03f);
            waterMat.SetFloat("_DepthFadeDistance", 2f);
            waterMat.SetFloat("_FoamWidth", 0.3f);
            waterMat.SetFloat("_RefractionStrength", 0.015f);
            AssetDatabase.CreateAsset(waterMat, matDir + "/PixelWater.mat");
            result += "Created PixelWater.mat\n";

            // Create water plane in scene (off to the side)
            var waterObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterObj.name = "WaterPlane";
            waterObj.transform.position = new Vector3(15, -0.2f, 0);
            waterObj.transform.localScale = new Vector3(2, 1, 2);
            waterObj.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            // Remove collider so player doesn't walk on it physically
            var col = waterObj.GetComponent<MeshCollider>();
            if (col != null) Object.DestroyImmediate(col);
            result += "Created WaterPlane in scene at (15, -0.2, 0)\n";
        }
        else
        {
            result += "ERROR: Custom/PixelWater shader not found\n";
        }

        // 3. Add VolumetricLightFeature to URP renderers
        string[] rendererGuids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in rendererGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue; // Skip package renderers

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData == null) continue;

            // Check if already has VolumetricLightFeature
            bool hasVolumetric = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature is VolumetricLightFeature)
                {
                    hasVolumetric = true;
                    break;
                }
            }

            if (!hasVolumetric)
            {
                var volumetricFeature = ScriptableObject.CreateInstance<VolumetricLightFeature>();
                volumetricFeature.name = "VolumetricLightFeature";
                volumetricFeature.settings.intensity = 0.12f;
                volumetricFeature.settings.steps = 16;
                volumetricFeature.settings.scattering = 0.4f;
                volumetricFeature.settings.density = 0.4f;

                AssetDatabase.AddObjectToAsset(volumetricFeature, rendererData);
                rendererData.rendererFeatures.Add(volumetricFeature);
                rendererData.SetDirty();
                EditorUtility.SetDirty(rendererData);
                result += "Added VolumetricLightFeature to " + path + "\n";
            }
            else
            {
                result += "VolumetricLightFeature already on " + path + "\n";
            }
        }

        AssetDatabase.SaveAssets();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        result += "All saved.\n";

        return result;
    }
}
