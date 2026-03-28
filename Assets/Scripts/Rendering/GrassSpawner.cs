using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns multi-type foliage (grass, bushes, small plants) on the ground plane
/// using Perlin noise density patches and GPU instancing.
/// Uses CROSSED QUADS (two quads at 90 degrees) for natural look from any angle.
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
    public float clearRadius = 2f;

    [Header("Density Noise")]
    public float noiseScale = 0.15f;
    public float noiseOffsetX = 100f;
    public float noiseOffsetZ = 200f;

    // Per-type instance matrices (each foliage instance = 2 crossed quads)
    private List<Matrix4x4> grassMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> bushMatrices = new List<Matrix4x4>();
    private List<Matrix4x4> plantMatrices = new List<Matrix4x4>();

    // Batched lists (max 1023 per draw call)
    private List<Matrix4x4[]> grassBatches = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> bushBatches = new List<Matrix4x4[]>();
    private List<Matrix4x4[]> plantBatches = new List<Matrix4x4[]>();

    private Mesh foliageQuad;

    void Start()
    {
        Random.InitState(123);
        CreateQuadMesh();
        SpawnAllFoliage();
        PrepareBatches();
    }

    void CreateQuadMesh()
    {
        // Star mesh: 2 crossed vertical quads + 1 horizontal quad on top.
        // This looks good from any angle, especially top-down orthographic.
        foliageQuad = new Mesh();

        float h = 1f;
        float halfW = 0.5f;
        float topY = h * 0.6f; // horizontal quad sits at 60% height

        Vector3[] verts = new Vector3[]
        {
            // Vertical quad 1 (along X)
            new Vector3(-halfW, 0, 0), new Vector3(halfW, 0, 0),
            new Vector3(halfW, h, 0),  new Vector3(-halfW, h, 0),
            // Vertical quad 2 (along Z, crossed 90 degrees)
            new Vector3(0, 0, -halfW), new Vector3(0, 0, halfW),
            new Vector3(0, h, halfW),  new Vector3(0, h, -halfW),
            // Horizontal quad (flat, at topY height)
            new Vector3(-halfW, topY, -halfW), new Vector3(halfW, topY, -halfW),
            new Vector3(halfW, topY, halfW),   new Vector3(-halfW, topY, halfW),
        };

        Vector2[] uvs = new Vector2[]
        {
            // Quad 1 UVs
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            // Quad 2 UVs
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            // Horizontal quad UVs
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
        };

        int[] tris = new int[]
        {
            // Quad 1 (both sides)
            0,2,1, 0,3,2,  1,2,0, 2,3,0,
            // Quad 2 (both sides)
            4,6,5, 4,7,6,  5,6,4, 6,7,4,
            // Horizontal quad (both sides — visible from above and below)
            8,10,9, 8,11,10,  9,10,8, 10,11,8,
        };

        Vector3[] normals = new Vector3[12];
        for (int i = 0; i < 4; i++) normals[i] = Vector3.forward;
        for (int i = 4; i < 8; i++) normals[i] = Vector3.right;
        for (int i = 8; i < 12; i++) normals[i] = Vector3.up;

        foliageQuad.vertices = verts;
        foliageQuad.uv = uvs;
        foliageQuad.triangles = tris;
        foliageQuad.normals = normals;
        foliageQuad.RecalculateBounds();
    }

    float GetDensitySpacing(float x, float z)
    {
        float noise = Mathf.PerlinNoise(
            (x + noiseOffsetX) * noiseScale,
            (z + noiseOffsetZ) * noiseScale
        );
        return Mathf.Lerp(1.0f, 0.3f, noise);
    }

    void SpawnAllFoliage()
    {
        grassMatrices.Clear();
        bushMatrices.Clear();
        plantMatrices.Clear();

        // Grass: thin blades, crossed quads
        SpawnCrossedFoliage(grassMatrices, grassCount, 0.15f, 0.35f, 0.3f, 0.6f);
        // Bushes: wider, shorter
        SpawnCrossedFoliage(bushMatrices, bushCount, 0.5f, 1.0f, 0.3f, 0.6f);
        // Small plants: tiny
        SpawnCrossedFoliage(plantMatrices, plantCount, 0.15f, 0.3f, 0.15f, 0.3f);
    }

    /// <summary>
    /// Spawns foliage using the star mesh (crossed quads + horizontal built-in).
    /// One instance per placement — the mesh itself has volume from all angles.
    /// </summary>
    void SpawnCrossedFoliage(List<Matrix4x4> matrices, int count,
        float widthMin, float widthMax, float heightMin, float heightMax)
    {
        List<Vector2> placedPositions = new List<Vector2>();
        int placed = 0;
        int maxAttempts = count * 30;

        for (int attempt = 0; attempt < maxAttempts && placed < count; attempt++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Mathf.Sqrt(Random.Range(0f, 1f)) * spawnRadius;
            float x = areaCenter.x + Mathf.Cos(angle) * dist;
            float z = areaCenter.y + Mathf.Sin(angle) * dist;

            Vector2 candidate = new Vector2(x, z);

            if (candidate.magnitude < clearRadius)
                continue;

            float spacing = GetDensitySpacing(x, z);

            bool tooClose = false;
            int checkStart = Mathf.Max(0, placedPositions.Count - 60);
            for (int i = placedPositions.Count - 1; i >= checkStart; i--)
            {
                if (Vector2.Distance(candidate, placedPositions[i]) < spacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            if (Physics.Raycast(new Vector3(x, raycastHeight, z), Vector3.down,
                out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                float w = Random.Range(widthMin, widthMax);
                float h = Random.Range(heightMin, heightMax);
                float yRot = Random.Range(0f, 360f);

                Vector3 pos = hit.point;
                Quaternion rot = Quaternion.Euler(0, yRot, 0);
                Vector3 scale = new Vector3(w, h, w); // uniform XZ for the star mesh

                matrices.Add(Matrix4x4.TRS(pos, rot, scale));
                placedPositions.Add(candidate);
                placed++;
            }
        }
    }

    void PrepareBatches()
    {
        BatchMatrices(grassMatrices, grassBatches);
        BatchMatrices(bushMatrices, bushBatches);
        BatchMatrices(plantMatrices, plantBatches);
    }

    void BatchMatrices(List<Matrix4x4> source, List<Matrix4x4[]> batches)
    {
        batches.Clear();
        for (int i = 0; i < source.Count; i += 1023)
        {
            int count = Mathf.Min(1023, source.Count - i);
            Matrix4x4[] batch = new Matrix4x4[count];
            source.CopyTo(i, batch, 0, count);
            batches.Add(batch);
        }
    }

    void Update()
    {
        if (foliageQuad == null) return;

        DrawBatches(grassMaterial, grassBatches);
        DrawBatches(bushMaterial, bushBatches);
        DrawBatches(smallPlantMaterial, plantBatches);
    }

    void DrawBatches(Material mat, List<Matrix4x4[]> batches)
    {
        if (mat == null) return;
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(foliageQuad, 0, mat, batch, batch.Length);
        }
    }
}
