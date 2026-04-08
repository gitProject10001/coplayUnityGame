using UnityEngine;
using UnityEditor;

public class SetEnvironmentProperties
{
    public static void Execute()
    {
        // === Phase 1B: Grass Material Properties ===
        var grassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GeometryGrass.mat");
        if (grassMat != null)
        {
            grassMat.SetFloat("_TessellationUniform", 8f);
            grassMat.SetFloat("_BladeHeight", 0.75f);
            grassMat.SetFloat("_BladeHeightRandom", 0.4f);
            grassMat.SetFloat("_BladeWidth", 0.06f);
            grassMat.SetFloat("_BladeWidthRandom", 0.03f);
            grassMat.SetColor("_BaseColor", new Color(0.12f, 0.26f, 0.07f, 1f));
            grassMat.SetColor("_TipColor", new Color(0.22f, 0.42f, 0.12f, 1f));
            grassMat.SetFloat("_WindStrength", 0.25f);
            grassMat.SetVector("_WindFrequency", new Vector4(0.08f, 0.06f, 0f, 0f));
            grassMat.SetFloat("_BladeForward", 0.45f);
            grassMat.SetFloat("_BladeCurve", 2.5f);
            EditorUtility.SetDirty(grassMat);
            Debug.Log("[EnvUpgrade] GeometryGrass.mat updated");
        }

        // === Phase 4: Ground Material ===
        var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ground.mat");
        if (groundMat != null)
        {
            groundMat.SetColor("_MossColor", new Color(0.14f, 0.28f, 0.10f, 1f));
            groundMat.SetColor("_BaseColor", new Color(0.16f, 0.24f, 0.10f, 1f));
            EditorUtility.SetDirty(groundMat);
            Debug.Log("[EnvUpgrade] Ground.mat updated");
        }

        // === Phase 1C: GrassGround properties ===
        var grassGround = GameObject.Find("GrassGround");
        if (grassGround != null)
        {
            var setup = grassGround.GetComponent<GrassGroundSetup>();
            if (setup != null)
            {
                setup.densityPerUnit = 2.5f;
                setup.radius = 18f;
                EditorUtility.SetDirty(setup);
                Debug.Log("[EnvUpgrade] GrassGround updated");
            }
        }

        // === Phase 2A: ProceduralGenerator properties ===
        var procGen = GameObject.Find("ProceduralGenerator");
        if (procGen != null)
        {
            var gen = procGen.GetComponent<ProceduralGenerator>();
            if (gen != null)
            {
                gen.treeCount = 12;
                gen.rockCount = 18;
                gen.bushCount = 20;
                gen.spawnAreaSize = 28f;
                EditorUtility.SetDirty(gen);
                Debug.Log("[EnvUpgrade] ProceduralGenerator updated");
            }
        }

        // === Phase 3A: GrassSpawner properties ===
        var grassSpawner = GameObject.Find("GrassSpawner");
        if (grassSpawner != null)
        {
            var spawner = grassSpawner.GetComponent<GrassSpawner>();
            if (spawner != null)
            {
                spawner.bushCount = 1200;
                spawner.plantCount = 2000;
                spawner.spawnRadius = 18f;
                EditorUtility.SetDirty(spawner);
                Debug.Log("[EnvUpgrade] GrassSpawner updated");
            }
        }

        // === Phase 5: FloatingParticles ===
        var particles = GameObject.Find("FloatingParticles");
        if (particles != null)
        {
            var fp = particles.GetComponent<FloatingParticles>();
            if (fp != null)
            {
                fp.particleCount = 140;
                fp.spawnRadius = 16f;
                fp.particleScaleMax = 0.18f;
                EditorUtility.SetDirty(fp);
                Debug.Log("[EnvUpgrade] FloatingParticles updated");
            }
        }

        // === Phase 6: Directional Light ===
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj != null)
        {
            var light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.intensity = 1.6f;
                EditorUtility.SetDirty(light);
                Debug.Log("[EnvUpgrade] Directional Light updated");
            }
        }

        // === Phase 2C: Trigger ProceduralGenerator regeneration ===
        if (procGen != null)
        {
            var gen = procGen.GetComponent<ProceduralGenerator>();
            if (gen != null)
            {
                gen.forceRegenerate = true;
                EditorUtility.SetDirty(gen);
                Debug.Log("[EnvUpgrade] Triggered regeneration");
            }
        }

        // Mark scene dirty so it can be saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("[EnvUpgrade] All environment properties set successfully!");
    }
}
