using UnityEngine;

/// <summary>
/// Static combat feedback system: screen shake (trauma-based) and hitstop.
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }

    [Header("Screen Shake")]
    public float maxShakeOffset = 0.3f;
    public float maxShakeAngle = 2f;
    public float traumaDecaySpeed = 2.5f;

    [Header("Hitstop")]
    public float hitstopDuration = 0.06f;
    public float hitstopTimeScale = 0.05f;

    private float trauma;
    private float hitstopTimer;
    private float originalTimeScale = 1f;
    private bool inHitstop;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Hitstop
        if (inHitstop)
        {
            hitstopTimer -= Time.unscaledDeltaTime;
            if (hitstopTimer <= 0f)
            {
                Time.timeScale = originalTimeScale;
                inHitstop = false;
            }
        }

        // Decay trauma
        trauma = Mathf.Max(0f, trauma - traumaDecaySpeed * Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Add trauma for screen shake (0-1 range, clamped).
    /// </summary>
    public static void AddTrauma(float amount)
    {
        if (Instance != null)
            Instance.trauma = Mathf.Clamp01(Instance.trauma + amount);
    }

    /// <summary>
    /// Get current shake offset for the camera.
    /// </summary>
    public static Vector3 GetShakeOffset()
    {
        if (Instance == null) return Vector3.zero;
        float shake = Instance.trauma * Instance.trauma; // Quadratic falloff
        float x = Instance.maxShakeOffset * shake * (Mathf.PerlinNoise(Time.unscaledTime * 25f, 0f) * 2f - 1f);
        float y = Instance.maxShakeOffset * shake * (Mathf.PerlinNoise(0f, Time.unscaledTime * 25f) * 2f - 1f);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Trigger a brief hitstop (time freeze).
    /// </summary>
    public static void TriggerHitstop(float duration = -1f)
    {
        if (Instance == null) return;
        if (Instance.inHitstop) return; // Don't stack
        Instance.originalTimeScale = 1f;
        Instance.hitstopTimer = duration > 0f ? duration : Instance.hitstopDuration;
        Time.timeScale = Instance.hitstopTimeScale;
        Instance.inHitstop = true;
    }
}
