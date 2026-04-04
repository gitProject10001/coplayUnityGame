using UnityEngine;

/// <summary>
/// Test harness for CameraFollow, CameraTexelSnap, CombatFeedback, and StopMotionEffect.
/// Creates a moving target and lets you trigger screen shake / hitstop with buttons.
/// Attach to an empty GameObject in 07_CameraAndFeedback scene.
/// </summary>
[ExecuteAlways]
public class CameraFeedbackDebug : MonoBehaviour
{
    [Header("=== CAMERA FOLLOW ===")]
    public CameraFollow cameraFollow;
    [Range(1f, 20f)] public float smoothSpeed = 8f;
    [Range(0f, 5f)] public float lookAheadDistance = 1.5f;
    [Range(0.5f, 10f)] public float lookAheadSmooth = 4f;
    public Vector3 cameraOffset = new Vector3(-10f, 10f, -10f);

    [Header("=== PIXEL SNAP ===")]
    public CameraTexelSnap texelSnap;
    public bool enableTexelSnap = true;
    [Range(1, 20)] public int pixelScale = 4;

    [Header("=== COMBAT FEEDBACK ===")]
    [Range(0.05f, 1f)] public float maxShakeOffset = 0.3f;
    [Range(0.5f, 2f)] public float maxShakeAngle = 2f;
    [Range(0.5f, 8f)] public float traumaDecaySpeed = 2.5f;
    [Range(0.02f, 0.3f)] public float hitstopDuration = 0.06f;

    [Header("Trigger Buttons (check in Inspector)")]
    [Tooltip("Triggers a small shake")] public bool triggerSmallShake = false;
    [Tooltip("Triggers a medium shake")] public bool triggerMediumShake = false;
    [Tooltip("Triggers a large shake")] public bool triggerLargeShake = false;
    [Tooltip("Triggers hitstop")] public bool triggerHitstop = false;

    [Header("=== MOVING TARGET ===")]
    public bool autoMoveTarget = true;
    [Range(1f, 10f)] public float moveRadius = 5f;
    [Range(0.1f, 3f)] public float moveSpeed = 1f;
    public bool manualControl = false;

    private GameObject targetObject;
    private bool sceneBuilt;
    private float moveAngle;

    void OnEnable()
    {
        BuildScene();
    }

    void BuildScene()
    {
        if (sceneBuilt) return;

        // Light
        if (FindAnyObjectByType<Light>() == null)
        {
            var lightGO = new GameObject("Debug Directional Light");
            lightGO.transform.SetParent(transform);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50, 50, 0);
        }

        // Ground with reference grid
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Test Ground";
            ground.transform.SetParent(transform);
            ground.transform.localScale = new Vector3(5, 1, 5);
            var mat = new Material(toonShader);
            mat.SetColor("_BaseColor", new Color(0.25f, 0.3f, 0.2f));
            ground.GetComponent<Renderer>().material = mat;

            // Reference objects (static, to see camera shake relative to them)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Reference_Pillar_{i}";
                pillar.transform.SetParent(transform);
                pillar.transform.position = new Vector3(Mathf.Cos(angle) * 8f, 1.5f, Mathf.Sin(angle) * 8f);
                pillar.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
                var pMat = new Material(toonShader);
                pMat.SetColor("_BaseColor", Color.HSVToRGB(i / 8f, 0.6f, 0.8f));
                pillar.GetComponent<Renderer>().material = pMat;
            }
        }

        // Moving target
        targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        targetObject.name = "Player";  // Named "Player" so CameraFollow auto-finds it
        targetObject.transform.SetParent(transform);
        targetObject.transform.position = Vector3.up;
        if (toonShader != null)
        {
            var mat = new Material(toonShader);
            mat.SetColor("_BaseColor", Color.white);
            targetObject.GetComponent<Renderer>().material = mat;
        }

        // Camera setup
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Debug Camera");
            camGO.transform.SetParent(transform);
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        // Add CameraFollow
        cameraFollow = cam.GetComponent<CameraFollow>();
        if (cameraFollow == null) cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
        cameraFollow.target = targetObject.transform;
        cameraFollow.offset = cameraOffset;

        // Add CameraTexelSnap
        texelSnap = cam.GetComponent<CameraTexelSnap>();
        if (texelSnap == null) texelSnap = cam.gameObject.AddComponent<CameraTexelSnap>();

        // Ensure CombatFeedback exists
        if (CombatFeedback.Instance == null)
        {
            var fbGO = new GameObject("CombatFeedback");
            fbGO.transform.SetParent(transform);
            fbGO.AddComponent<CombatFeedback>();
        }

        sceneBuilt = true;
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        UpdateTarget();
        UpdateCameraFollow();
        UpdateTexelSnap();
        UpdateFeedbackSettings();
        HandleTriggers();
    }

    void UpdateTarget()
    {
        if (targetObject == null) return;

        if (manualControl)
        {
            // WASD control
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = new Vector3(h, 0, v) * moveSpeed * 5f * Time.deltaTime;
            targetObject.transform.position += move;
        }
        else if (autoMoveTarget)
        {
            moveAngle += moveSpeed * Time.deltaTime;
            float x = Mathf.Cos(moveAngle) * moveRadius;
            float z = Mathf.Sin(moveAngle) * moveRadius;
            targetObject.transform.position = new Vector3(x, 1f, z);
        }
    }

    void UpdateCameraFollow()
    {
        if (cameraFollow == null) return;
        cameraFollow.smoothSpeed = smoothSpeed;
        cameraFollow.lookAheadDistance = lookAheadDistance;
        cameraFollow.lookAheadSmooth = lookAheadSmooth;
        cameraFollow.offset = cameraOffset;
    }

    void UpdateTexelSnap()
    {
        if (texelSnap == null) return;
        texelSnap.enabled = enableTexelSnap;
        texelSnap.pixelScale = pixelScale;
    }

    void UpdateFeedbackSettings()
    {
        if (CombatFeedback.Instance == null) return;
        CombatFeedback.Instance.maxShakeOffset = maxShakeOffset;
        CombatFeedback.Instance.maxShakeAngle = maxShakeAngle;
        CombatFeedback.Instance.traumaDecaySpeed = traumaDecaySpeed;
        CombatFeedback.Instance.hitstopDuration = hitstopDuration;
    }

    void HandleTriggers()
    {
        if (triggerSmallShake) { triggerSmallShake = false; CombatFeedback.AddTrauma(0.2f); }
        if (triggerMediumShake) { triggerMediumShake = false; CombatFeedback.AddTrauma(0.5f); }
        if (triggerLargeShake) { triggerLargeShake = false; CombatFeedback.AddTrauma(1.0f); }
        if (triggerHitstop) { triggerHitstop = false; CombatFeedback.TriggerHitstop(hitstopDuration); }

        // Keyboard shortcuts for quick testing
        if (Input.GetKeyDown(KeyCode.Alpha1)) CombatFeedback.AddTrauma(0.2f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) CombatFeedback.AddTrauma(0.5f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) CombatFeedback.AddTrauma(1.0f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) CombatFeedback.TriggerHitstop(hitstopDuration);
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 320, 260));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        float trauma = CombatFeedback.Instance != null ?
            (float)typeof(CombatFeedback).GetField("trauma",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(CombatFeedback.Instance) : 0f;

        Vector3 shake = CombatFeedback.GetShakeOffset();

        GUILayout.Box(
            $"=== CAMERA & FEEDBACK DEBUG ===\n\n" +
            $"Camera: smooth={smoothSpeed:F1} ahead={lookAheadDistance:F1}\n" +
            $"Texel Snap: {(enableTexelSnap ? "ON" : "OFF")} scale={pixelScale}\n\n" +
            $"Trauma: {trauma:F3} (squared: {trauma * trauma:F3})\n" +
            $"Shake offset: ({shake.x:F3}, {shake.y:F3})\n" +
            $"Time.timeScale: {Time.timeScale:F2}\n\n" +
            $"Keys: 1=small 2=med 3=large shake, 4=hitstop\n" +
            $"Target: {(manualControl ? "WASD control" : "auto-circle")}",
            style);

        GUILayout.EndArea();
    }

    void OnDrawGizmos()
    {
        if (targetObject != null)
        {
            // Draw movement circle
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            DrawGizmoCircle(Vector3.zero, moveRadius, 32);

            // Draw look-ahead
            if (cameraFollow != null && cameraFollow.target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(targetObject.transform.position,
                    targetObject.transform.position + targetObject.transform.forward * lookAheadDistance);
            }
        }
    }

    void DrawGizmoCircle(Vector3 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1) * radius, 0.1f, Mathf.Sin(a1) * radius),
                center + new Vector3(Mathf.Cos(a2) * radius, 0.1f, Mathf.Sin(a2) * radius));
        }
    }
}
