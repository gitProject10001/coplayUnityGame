using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns billboard grass quads on the ground plane using Poisson-disk-like placement.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class GrassSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    public Vector2 areaSize = new Vector2(40, 40);
    public Vector2 areaCenter = Vector2.zero;

    [Header("Density")]
    [Tooltip("Minimum distance between grass blades")]
    public float minSpacing = 0.4f;
    [Tooltip("Maximum attempts to place each blade")]
    public int maxAttempts = 30;
    public int maxBlades = 2000;

    [Header("Appearance")]
    public Material grassMaterial;
    [Range(0.2f, 2f)]
    public float heightMin = 0.4f;
    [Range(0.2f, 2f)]
    public float heightMax = 0.8f;
    [Range(0.1f, 1f)]
    public float widthMin = 0.2f;
    [Range(0.1f, 1f)]
    public float widthMax = 0.4f;

    [Header("Placement")]
    public LayerMask groundLayer = ~0;
    public float raycastHeight = 20f;
    [Tooltip("Don't place grass within this radius of the player spawn")]
    public float clearRadius = 3f;

    private List<Matrix4x4> grassMatrices = new List<Matrix4x4>();
    private Mesh grassQuad;
    private MaterialPropertyBlock propBlock;
    private List<List<Matrix4x4>> batches = new List<List<Matrix4x4>>();

    void Start()
    {
        CreateGrassQuad();
        SpawnGrass();
        PrepareBatches();
    }

    void CreateGrassQuad()
    {
        // Simple quad mesh (2 triangles) - billboard shader handles orientation
        grassQuad = new Mesh();
        grassQuad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 1, 0),
            new Vector3(-0.5f, 1, 0)
        };
        grassQuad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        grassQuad.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        grassQuad.normals = new Vector3[]
        {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
        };
        grassQuad.RecalculateBounds();
    }

    void SpawnGrass()
    {
        grassMatrices.Clear();
        List<Vector2> placedPositions = new List<Vector2>();

        float halfW = areaSize.x * 0.5f;
        float halfH = areaSize.y * 0.5f;
        int placed = 0;

        // Simple Poisson-disk-like placement
        for (int attempt = 0; attempt < maxBlades * maxAttempts && placed < maxBlades; attempt++)
        {
            float x = areaCenter.x + Random.Range(-halfW, halfW);
            float z = areaCenter.y + Random.Range(-halfH, halfH);

            // Check minimum spacing
            bool tooClose = false;
            Vector2 candidate = new Vector2(x, z);

            // Skip if too close to center (player spawn)
            if (candidate.magnitude < clearRadius)
                continue;

            for (int i = placedPositions.Count - 1; i >= Mathf.Max(0, placedPositions.Count - 50); i--)
            {
                if (Vector2.Distance(candidate, placedPositions[i]) < minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Raycast to find ground
            if (Physics.Raycast(new Vector3(x, raycastHeight, z), Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                float h = Random.Range(heightMin, heightMax);
                float w = Random.Range(widthMin, widthMax);
                float yRot = Random.Range(0f, 360f);

                Vector3 pos = hit.point;
                Quaternion rot = Quaternion.Euler(0, yRot, 0);
                Vector3 scale = new Vector3(w, h, 1);

                grassMatrices.Add(Matrix4x4.TRS(pos, rot, scale));
                placedPositions.Add(candidate);
                placed++;
            }
        }
    }

    void PrepareBatches()
    {
        // GPU instancing supports max 1023 instances per batch
        batches.Clear();
        for (int i = 0; i < grassMatrices.Count; i += 1023)
        {
            int count = Mathf.Min(1023, grassMatrices.Count - i);
            batches.Add(grassMatrices.GetRange(i, count));
        }
        propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (grassMaterial == null || grassQuad == null) return;

        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(grassQuad, 0, grassMaterial, batch.ToArray(), batch.Count, propBlock);
        }
    }
}
