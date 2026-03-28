using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns multi-type foliage (grass, bushes, small plants) on the ground plane
/// using Perlin noise density patches and GPU instancing.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class GrassSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    public float spawnRadius = 15f;
    public Vector2 areaCenter = Vector2.zero;

    [Header("Foliage Counts")]
    public int grassCount = 4000;
    public int bushCount = 400;
    public int plantCount = 600;

    [Header("Materials")]
    public Material grassMaterial;
    public Material bushMaterial;
    public Material smallPlantMaterial;

    [Header("Placement")]
    public LayerMask groundLayer = ~0;
    public float raycastHeight = 20f;
    [Tooltip("Don't place foliage within this radius of the player spawn")]
    public float clearRadius = 3f;

    [Header("Density Noise")]
    [Tooltip("Scale of the Perlin noise used for density patches")]
    public float noiseScale = 0.15f;
    [Tooltip("Noise offset seed for variety")]
    public float noiseOffsetX = 100f;
    public float noiseOffsetZ = 200f;

    // Per-type instance matrices
    private List<Matrix4x4> grassMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> bushMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> plantMatrices = new List<Matrix4x4>();

    // Batched lists (max 1023 per draw call)
    private List<List<Matrix4x4>> grassBatches = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> bushBatches = new List<List<Matrix4x4>>();
    private List<List<Matrix4x4>> plantBatches = new List<List<Matrix4x4>>();

    private Mesh foliageQuad;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        Random.InitState(123);
        CreateQuadMesh();
        SpawnAllFoliage();
        PrepareBatches();
    }

    void CreateQuadMesh()
    {
        foliageQuad = new Mesh();
        foliageQuad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 1, 0),
            new Vector3(-0.5f, 1, 0)
        };
        foliageQuad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        foliageQuad.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        foliageQuad.normals = new Vector3[]
        {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward
        };
        foliageQuad.RecalculateBounds();
    }

    /// <summary>
    /// Returns a spacing value based on Perlin noise at the given world position.
    /// High noise = dense (0.3), low noise = sparse (1.0).
    /// </summary>
    float GetDensitySpacing(float x, float z)
    {
        float noise = Mathf.PerlinNoise(
            (x + noiseOffsetX) * noiseScale,
            (z + noiseOffsetZ) * noiseScale
        );
        // Lerp: noise 1 -> spacing 0.3 (dense), noise 0 -> spacing 1.0 (sparse)
        return Mathf.Lerp(1.0f, 0.3f, noise);
    }

    void SpawnAllFoliage()
    {
        grassMatrices.Clear();
        bushMatrices.Clear();
        plantMatrices.Clear();

        SpawnFoliageType(grassMatrices, grassCount, 0.2f, 0.4f, 0.4f, 0.8f);
        SpawnFoliageType(bushMatrices, bushCount, 0.8f, 1.5f, 0.5f, 1.0f);
        SpawnFoliageType(plantMatrices, plantCount, 0.2f, 0.4f, 0.2f, 0.4f);
    }

    void SpawnFoliageType(List<Matrix4x4> matrices, int count,
        float widthMin, float widthMax, float heightMin, float heightMax)
    {
        List<Vector2> placedPositions = new List<Vector2>();
        int placed = 0;
        int maxAttempts = count * 30;

        for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
        {
            // Random point within circular spawn radius
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(0f, spawnRadius);
            float x = areaCenter.x + Mathf.Cos(angle) * dist;
            float z = areaCenter.y + Mathf.Sin(angle) * dist;

            Vector2 candidate = new Vector2(x, z);

            // Skip if too close to center (player spawn)
            if (candidate.magnitude < clearRadius)
                continue;

            // Get density-based spacing from Perlin noise
            float spacing = GetDensitySpacing(x, z);

            // Check minimum spacing against recently placed instances
            bool tooClose = false;
            for (int i = placedPositions.Count - 1; i >= Mathf.Max(0, placedPositions.Count - 50); i--)
            {
                if (Vector2.Distance(candidate, placedPositions[i]) < spacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Raycast to find ground
            if (Physics.Raycast(new Vector3(x, raycastHeight, z), Vector3.down,
                out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                float w = Random.Range(widthMin, widthMax);
                float h = Random.Range(heightMin, heightMax);
                float yRot = Random.Range(0f, 360f);

                Vector3 pos = hit.point;
                Quaternion rot = Quaternion.Euler(0, yRot, 0);
                Vector3 scale = new Vector3(w, h, 1);

                matrices.Add(Matrix4x4.TRS(pos, rot, scale));
                placedPositions.Add(candidate);
                placed++;
            }
        }
    }

    void PrepareBatches()
    {
        propBlock = new MaterialPropertyBlock();
        BatchMatrices(grassMatrices, grassBatches);
        BatchMatrices(bushMatrices, bushBatches);
        BatchMatrices(plantMatrices, plantBatches);
    }

    void BatchMatrices(List<Matrix4x4> source, List<List<Matrix4x4>> batches)
    {
        batches.Clear();
        for (int i = 0; i < source.Count; i += 1023)
        {
            int count = Mathf.Min(1023, source.Count - i);
            batches.Add(source.GetRange(i, count));
        }
    }

    void Update()
    {
        if (foliageQuad == null) return;

        DrawBatches(grassMaterial, grassBatches);
        DrawBatches(bushMaterial, bushBatches);
        DrawBatches(smallPlantMaterial, plantBatches);
    }

    void DrawBatches(Material mat, List<List<Matrix4x4>> batches)
    {
        if (mat == null) return;

        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(foliageQuad, 0, mat, batch.ToArray(), batch.Count, propBlock);
        }
    }
}
