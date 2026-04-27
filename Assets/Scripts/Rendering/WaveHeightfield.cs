using UnityEngine;

// Wave equation heightfield: u^(t+1) = u^t + (1-d)*(u^t - u^(t-1)) + c²(dt/h)²·∇²u^t
// Uses plain float[] — a 32×32 grid is 1024 iterations, well under 0.1ms without Burst.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaveHeightfield : MonoBehaviour
{
    [Header("Grid")]
    [Range(8, 128)] public int resolution = 32;
    public float worldSize = 4f;

    [Header("Wave")]
    [Range(0.1f, 5f)] public float waveSpeed = 1.5f;
    [Range(0f, 0.15f)] public float damping = 0.005f;

    [Header("Splash")]
    public float splashRadius = 0.5f;
    public float splashStrength = 0.3f;

    public static WaveHeightfield Instance { get; private set; }

    float[] _prev, _curr, _next, _impulse;
    Vector3[] _vertices;
    Mesh _mesh;
    float _nodeSpacing;
    int _w; // resolution locked at Awake — safe even if inspector field changes mid-play

    Texture2D _normalTex;
    Color[]   _normalPixels;

    static readonly int s_HfNormalMap = Shader.PropertyToID("_HeightfieldNormalMap");
    static readonly int s_HfOrigin    = Shader.PropertyToID("_HeightfieldOrigin");
    static readonly int s_HfWorldSize = Shader.PropertyToID("_HeightfieldWorldSize");

    void Awake()
    {
        Instance = this;
        _w = resolution;
        _nodeSpacing = worldSize / (_w - 1);

        int count = _w * _w;
        _prev    = new float[count];
        _curr    = new float[count];
        _next    = new float[count];
        _impulse = new float[count];

        _normalPixels = new Color[count];
        _normalTex    = new Texture2D(_w, _w, TextureFormat.RGBAHalf, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
        };

        BuildMesh();
    }

    void OnDisable()
    {
        // Clear global so edit-mode water doesn't show stale data after play stops
        Shader.SetGlobalFloat(s_HfWorldSize, 0f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_normalTex) Destroy(_normalTex);
    }

    void BuildMesh()
    {
        _mesh = new Mesh { name = "WaveHeightfield" };
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        int n = _w;
        _vertices = new Vector3[n * n];
        var uvs = new Vector2[n * n];

        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            int i = z * n + x;
            float lx = (x / (float)(n - 1) - 0.5f) * worldSize;
            float lz = (z / (float)(n - 1) - 0.5f) * worldSize;
            _vertices[i] = new Vector3(lx, 0f, lz);
            uvs[i]       = new Vector2(x / (float)(n - 1), z / (float)(n - 1));
        }

        int q = n - 1;
        var tris = new int[q * q * 6];
        int t = 0;
        for (int z = 0; z < q; z++)
        for (int x = 0; x < q; x++)
        {
            int bl = z * n + x;
            tris[t++] = bl;     tris[t++] = bl + n;     tris[t++] = bl + n + 1;
            tris[t++] = bl;     tris[t++] = bl + n + 1; tris[t++] = bl + 1;
        }

        _mesh.SetVertices(_vertices);
        _mesh.SetUVs(0, uvs);
        _mesh.SetTriangles(tris, 0);
        _mesh.RecalculateNormals();
        _mesh.MarkDynamic();
    }

    void Update()
    {
        if (_curr == null) return; // arrays not yet initialized (e.g. domain reload mid-play)

        float dt = Mathf.Min(Time.deltaTime, 0.033f);
        float h  = _nodeSpacing;
        float c  = Mathf.Min(waveSpeed, h / (dt * 1.415f)); // CFL clamp
        float k  = c * c * (dt * dt) / (h * h);
        float d  = damping;

        int w = _w;

        // Flush impulses into current frame
        for (int i = 0; i < _impulse.Length; i++)
        {
            if (_impulse[i] == 0f) continue;
            _curr[i]   += _impulse[i];
            _impulse[i] = 0f;
        }

        // Wave equation step
        for (int z = 0; z < w; z++)
        for (int x = 0; x < w; x++)
        {
            int idx = z * w + x;
            int xm = x > 0     ? idx - 1 : idx;
            int xp = x < w - 1 ? idx + 1 : idx;
            int zm = z > 0     ? idx - w : idx;
            int zp = z < w - 1 ? idx + w : idx;

            float laplacian = _curr[xp] + _curr[xm] + _curr[zp] + _curr[zm] - 4f * _curr[idx];
            float velocity  = _curr[idx] - _prev[idx];
            _next[idx] = _curr[idx] + velocity * (1f - d) + k * laplacian;
        }

        // 3-way buffer rotation via temp reference swap (float[] are reference types)
        float[] tmp = _prev;
        _prev = _curr;
        _curr = _next;
        _next = tmp;

        ApplyToMesh();
    }

    void ApplyToMesh()
    {
        int w = _w;
        float invSpacing2 = 1f / (2f * _nodeSpacing);

        for (int z = 0; z < w; z++)
        for (int x = 0; x < w; x++)
        {
            int idx = z * w + x;
            _vertices[idx].y = _curr[idx];

            // Central-difference normal for normal texture
            float dh_dx = (_curr[z * w + Mathf.Min(x + 1, w - 1)] - _curr[z * w + Mathf.Max(x - 1, 0)]) * invSpacing2;
            float dh_dz = (_curr[Mathf.Min(z + 1, w - 1) * w + x] - _curr[Mathf.Max(z - 1, 0) * w + x]) * invSpacing2;
            Vector3 n = Vector3.Normalize(new Vector3(-dh_dx, 1f, -dh_dz));
            _normalPixels[idx] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
        }

        _mesh.SetVertices(_vertices);
        _mesh.RecalculateNormals();

        _normalTex.SetPixels(_normalPixels);
        _normalTex.Apply(false);

        // lossyScale.x handles uniform scale on the parent — worldSize is in local units
        float worldExtent = worldSize * transform.lossyScale.x;
        Shader.SetGlobalTexture(s_HfNormalMap, _normalTex);
        Shader.SetGlobalVector(s_HfOrigin,     transform.position);
        Shader.SetGlobalFloat(s_HfWorldSize,   worldExtent);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SplashAt(Vector3 worldPos, float strength = 1f)
    {
        if (!WorldToGrid(worldPos, out float gx, out float gz)) return;

        float r  = splashRadius / _nodeSpacing;
        float r2 = r * r;
        int x0 = Mathf.Max(0,        Mathf.FloorToInt(gx - r));
        int x1 = Mathf.Min(_w - 1,   Mathf.CeilToInt(gx + r));
        int z0 = Mathf.Max(0,        Mathf.FloorToInt(gz - r));
        int z1 = Mathf.Min(_w - 1,   Mathf.CeilToInt(gz + r));

        for (int z = z0; z <= z1; z++)
        for (int x = x0; x <= x1; x++)
        {
            float dx = x - gx, dz = z - gz;
            float d2 = dx * dx + dz * dz;
            if (d2 > r2) continue;
            _impulse[z * _w + x] -= splashStrength * strength * (1f - d2 / r2);
        }
    }

    public float SampleHeight(Vector3 worldPos)
    {
        if (!WorldToGrid(worldPos, out float gx, out float gz))
            return transform.position.y;

        int x0 = Mathf.Clamp((int)gx, 0, _w - 2);
        int z0 = Mathf.Clamp((int)gz, 0, _w - 2);
        float tx = gx - x0, tz = gz - z0;

        float h00 = _curr[ z0      * _w + x0];
        float h10 = _curr[ z0      * _w + x0 + 1];
        float h01 = _curr[(z0 + 1) * _w + x0];
        float h11 = _curr[(z0 + 1) * _w + x0 + 1];

        return transform.position.y + Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
    }

    public Vector3 SampleNormal(Vector3 worldPos)
    {
        if (!WorldToGrid(worldPos, out float gx, out float gz))
            return Vector3.up;

        int x = Mathf.Clamp(Mathf.RoundToInt(gx), 1, _w - 2);
        int z = Mathf.Clamp(Mathf.RoundToInt(gz), 1, _w - 2);

        float dh_dx = (_curr[z * _w + x + 1] - _curr[z * _w + x - 1]) / (2f * _nodeSpacing);
        float dh_dz = (_curr[(z + 1) * _w + x] - _curr[(z - 1) * _w + x]) / (2f * _nodeSpacing);
        return Vector3.Normalize(new Vector3(-dh_dx, 1f, -dh_dz));
    }

    bool WorldToGrid(Vector3 worldPos, out float gx, out float gz)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        gx = (local.x / worldSize + 0.5f) * (_w - 1);
        gz = (local.z / worldSize + 0.5f) * (_w - 1);
        return gx >= 0f && gx <= _w - 1 && gz >= 0f && gz <= _w - 1;
    }

    [ContextMenu("Test: Splash Center")]
    void TestSplash() => SplashAt(transform.position, 1f);
}
