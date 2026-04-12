using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class CreateWaterSplash
{
    public static void Execute()
    {
        // ── 1. Build the WaterSplash prefab ─────────────────────────────────

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Sprites/Default");

        var root = new GameObject("WaterSplash");

        // -- Spray: upward burst of droplets --
        var ps  = root.AddComponent<ParticleSystem>();
        var psr = root.GetComponent<ParticleSystemRenderer>();

        var main = ps.main;
        main.duration        = 0.7f;
        main.loop            = false;
        main.playOnAwake     = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.35f, 0.80f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.07f, 0.22f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(0.90f, 0.97f, 1.00f, 0.95f),
                                   new Color(0.55f, 0.82f, 0.92f, 0.75f));
        main.gravityModifier = 2.2f;
        main.stopAction      = ParticleSystemStopAction.Destroy;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(18f, 28f)) });

        var sh = ps.shape;
        sh.enabled   = true;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle     = 38f;
        sh.radius    = 0.06f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space   = ParticleSystemSimulationSpace.Local;
        vol.x       = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        vol.z       = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size    = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.4f, 0.85f), new Keyframe(1f, 0f)));

        var colOL   = ps.colorOverLifetime;
        colOL.enabled = true;
        var sprayGrad = new Gradient();
        sprayGrad.SetKeys(
            new[] { new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.55f, 0.82f, 0.92f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.75f, 0.45f),
                    new GradientAlphaKey(0f,   1f) });
        colOL.color = new ParticleSystem.MinMaxGradient(sprayGrad);

        var sprayMat        = new Material(particleShader);
        sprayMat.color      = new Color(0.92f, 0.97f, 1f, 0.9f);
        psr.renderMode      = ParticleSystemRenderMode.Billboard;
        psr.material        = sprayMat;
        psr.sortingOrder    = 10;

        // -- Ring: flat foam disc that expands at water surface --
        var ringGO = new GameObject("SplashRing");
        ringGO.transform.SetParent(root.transform, false);
        var rps = ringGO.AddComponent<ParticleSystem>();
        var rr  = ringGO.GetComponent<ParticleSystemRenderer>();

        var rm        = rps.main;
        rm.duration        = 0.55f;
        rm.loop            = false;
        rm.playOnAwake     = true;
        rm.startLifetime   = 0.55f;
        rm.startSpeed      = 0f;
        rm.startSize       = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        rm.startRotation   = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        rm.startColor      = new Color(0.95f, 0.98f, 1f, 0.78f);
        rm.stopAction      = ParticleSystemStopAction.None;
        rm.simulationSpace = ParticleSystemSimulationSpace.World;

        var re = rps.emission;
        re.rateOverTime = 0f;
        re.SetBursts(new[] { new ParticleSystem.Burst(0f, new ParticleSystem.MinMaxCurve(12f, 18f)) });

        var rs        = rps.shape;
        rs.enabled    = true;
        rs.shapeType  = ParticleSystemShapeType.Circle;
        rs.radius     = 0.20f;

        var rsol   = rps.sizeOverLifetime;
        rsol.enabled = true;
        rsol.size    = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f), new Keyframe(1f, 1.5f)));

        var rcol    = rps.colorOverLifetime;
        rcol.enabled = true;
        var ringGrad = new Gradient();
        ringGrad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0.40f, 0.55f),
                    new GradientAlphaKey(0f,    1f) });
        rcol.color = new ParticleSystem.MinMaxGradient(ringGrad);

        var ringMat     = new Material(particleShader);
        ringMat.color   = new Color(0.92f, 0.97f, 1f, 0.82f);
        rr.renderMode   = ParticleSystemRenderMode.Billboard;
        rr.material     = ringMat;
        rr.sortingOrder = 10;

        // Save prefab
        string prefabPath = "Assets/Prefabs/WaterSplash.prefab";
        bool saved;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out saved);
        Object.DestroyImmediate(root);
        if (!saved)
        {
            Debug.LogError("[CreateWaterSplash] Failed to save WaterSplash prefab.");
            return;
        }
        Debug.Log("[CreateWaterSplash] Prefab saved: " + prefabPath);

        // ── 2. Wire trigger onto ab_water_pool ──────────────────────────────

        var waterPool = GameObject.Find("ab_water_pool");
        if (waterPool == null)
        {
            Debug.LogError("[CreateWaterSplash] ab_water_pool not found in active scene.");
            return;
        }

        // Remove any stale BoxColliders we may have added before
        foreach (var old in waterPool.GetComponents<BoxCollider>())
            Object.DestroyImmediate(old);
        foreach (var old in waterPool.GetComponents<WaterSplashTrigger>())
            Object.DestroyImmediate(old);

        // Thin trigger slab at the water surface.
        // Mesh is 4 verts (unit quad in object-space ≈ 4 units wide at this scale).
        var box    = waterPool.AddComponent<BoxCollider>();
        box.size   = new Vector3(4f, 0.06f, 4f);
        box.center = Vector3.zero;
        box.isTrigger = true;

        var trigger = waterPool.AddComponent<WaterSplashTrigger>();
        var so      = new SerializedObject(trigger);
        so.FindProperty("splashPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[CreateWaterSplash] WaterSplashTrigger wired on ab_water_pool.");
    }
}