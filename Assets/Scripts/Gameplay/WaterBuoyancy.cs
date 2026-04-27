using UnityEngine;

// Attach to any Rigidbody that should bob on a WaveHeightfield surface.
// Uses multi-point sampling so objects tip realistically on wave slopes.
[RequireComponent(typeof(Rigidbody))]
public class WaterBuoyancy : MonoBehaviour
{
    [Tooltip("Upward force per unit of submersion depth, per sample point")]
    public float buoyancyForce = 15f;
    [Tooltip("Number of sample points spread across the object bounds (more = better tipping)")]
    [Range(1, 8)] public int sampleCount = 4;
    [Tooltip("Extra angular drag while submerged to settle the rocking")]
    public float waterAngularDrag = 3f;
    [Tooltip("Linear drag while submerged")]
    public float waterLinearDrag = 2f;

    Rigidbody _rb;
    float _defaultAngularDrag;
    float _defaultLinearDrag;
    Vector3[] _sampleOffsets;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _defaultAngularDrag = _rb.angularDamping;
        _defaultLinearDrag  = _rb.linearDamping;
        BuildSampleOffsets();
    }

    void BuildSampleOffsets()
    {
        _sampleOffsets = new Vector3[sampleCount];
        if (sampleCount == 1) { _sampleOffsets[0] = Vector3.zero; return; }

        Bounds b = LocalBounds();
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            // Spread along both X and Z diagonally so we get roll AND pitch
            float lx = Mathf.Lerp(-b.extents.x, b.extents.x, t);
            float lz = Mathf.Lerp(-b.extents.z, b.extents.z, i % 2 == 0 ? t : 1f - t);
            _sampleOffsets[i] = new Vector3(lx, 0f, lz);
        }
    }

    Bounds LocalBounds()
    {
        var r = GetComponentInChildren<Renderer>();
        if (r != null) return r.localBounds;
        var col = GetComponentInChildren<Collider>();
        if (col != null) return new Bounds(col.bounds.center - transform.position, col.bounds.size);
        return new Bounds(Vector3.zero, Vector3.one);
    }

    void FixedUpdate()
    {
        var water = WaveHeightfield.Instance;
        if (water == null) return;

        bool anySubmerged = false;

        foreach (var offset in _sampleOffsets)
        {
            Vector3 sampleWorld = transform.TransformPoint(offset);
            float surfaceY = water.SampleHeight(sampleWorld);
            float depth    = surfaceY - sampleWorld.y;
            if (depth <= 0f) continue;

            anySubmerged = true;

            // Archimedes: force along surface normal, scaled by submersion depth
            Vector3 normal = water.SampleNormal(sampleWorld);
            _rb.AddForceAtPosition(normal * (buoyancyForce * depth), sampleWorld, ForceMode.Force);
        }

        // Dampen motion while in water so objects settle rather than oscillate
        _rb.angularDamping = anySubmerged ? waterAngularDrag : _defaultAngularDrag;
        _rb.linearDamping  = anySubmerged ? waterLinearDrag  : _defaultLinearDrag;
    }
}
