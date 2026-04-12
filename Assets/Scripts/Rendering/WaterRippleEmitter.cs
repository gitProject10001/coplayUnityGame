using UnityEngine;

[DisallowMultipleComponent]
public class WaterRippleEmitter : MonoBehaviour
{
    [Header("Emission")]
    [Tooltip("Minimum world distance moved between ripple emissions.")]
    public float minSpawnDistance = 0.35f;
    [Tooltip("Minimum seconds between emissions even if the player keeps moving.")]
    public float minSpawnInterval = 0.12f;

    [Header("Water Surface Window")]
    [Tooltip("Approximate Y of the water surface in world space.")]
    public float waterHeight = 0f;
    [Tooltip("How far above/below the water surface counts as 'in water'.")]
    public float waterBand = 0.6f;

    // Shared circular buffer — last 16 ripples across ALL emitters in the scene.
    // 16 is the array size in PainterlyWater.shader (MAX_RIPPLES).
    const int MAX_RIPPLES = 16;
    static readonly Vector4[] s_Buffer = new Vector4[MAX_RIPPLES];
    static int s_NextIndex = 0;
    static int s_LiveCount = 0;
    static int s_PointsID  = -1;
    static int s_CountID   = -1;

    Vector3 _lastSpawnPos;
    float   _lastSpawnTime;

    static void EnsureIDs()
    {
        if (s_PointsID < 0) s_PointsID = Shader.PropertyToID("_WaterRipplePoints");
        if (s_CountID  < 0) s_CountID  = Shader.PropertyToID("_WaterRippleCount");
    }

    // Emit a one-shot ripple at a specific world position (for splashes, impacts, etc.)
    public static void EmitAt(Vector3 worldPos)
    {
        EnsureIDs();
        Vector4 ripple = new Vector4(worldPos.x, worldPos.y, worldPos.z, Time.timeSinceLevelLoad);
        s_Buffer[s_NextIndex] = ripple;
        s_NextIndex = (s_NextIndex + 1) % MAX_RIPPLES;
        s_LiveCount = Mathf.Min(s_LiveCount + 1, MAX_RIPPLES);
        Shader.SetGlobalVectorArray(s_PointsID, s_Buffer);
        Shader.SetGlobalInt(s_CountID, s_LiveCount);
    }

    void OnEnable()
    {
        EnsureIDs();
        _lastSpawnPos  = transform.position;
        _lastSpawnTime = -999f;
    }

    void Update()
    {
        Vector3 p = transform.position;

        // Only emit when within the water band
        if (Mathf.Abs(p.y - waterHeight) > waterBand) return;

        float dt = Time.time - _lastSpawnTime;
        float moved = Vector3.Distance(p, _lastSpawnPos);
        if (dt < minSpawnInterval) return;
        if (moved < minSpawnDistance && _lastSpawnTime > 0f) return;

        // Snap the ripple to the water surface so the wave front is on the plane
        Vector4 ripple = new Vector4(p.x, waterHeight, p.z, Time.timeSinceLevelLoad);
        s_Buffer[s_NextIndex] = ripple;
        s_NextIndex = (s_NextIndex + 1) % MAX_RIPPLES;
        s_LiveCount = Mathf.Min(s_LiveCount + 1, MAX_RIPPLES);

        Shader.SetGlobalVectorArray(s_PointsID, s_Buffer);
        Shader.SetGlobalInt(s_CountID, s_LiveCount);

        _lastSpawnPos  = p;
        _lastSpawnTime = Time.time;
    }
}
