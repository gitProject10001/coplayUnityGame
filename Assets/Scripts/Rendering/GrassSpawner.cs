using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns bush and small plant foliage on the ground plane
/// using Perlin noise density patches and GPU instancing with per-instance color variation.
/// Grass is handled separately by the GeometryGrass shader.
/// </summary>
public class GrassSpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    public float spawnRadius = 15f;
    public Vector2 areaCenter = Vector2.zero;

    [Header("Foliage Counts")]
    public int bushCount = 1500;
    public int plantCount = 2500;

    [Header("Materials")]
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

    [Header("Color Variation")]
    [Range(0f, 0.5f)]
    public float colorVariation = 0.25f;

    private struct FoliageBatch
    {
        public Matrix4x4[] matrices;
        public MaterialPropertyBlock props;
    }

    private List<FoliageBatch> bushBatches = new List<FoliageBatch>();
    private List<FoliageBatch> plantBatches = new List<FoliageBatch>();

    private Mesh foliageQuad;

    void Start()
    {
        Random.InitState(123);
        CreateQuadMesh();
        SpawnAllFoliage();
    }

    void CreateQuadMesh()
    {
        foliageQuad = new Mesh();

        float h = 1f;
        float halfW = 0.5f;
        float topY = h * 0.6f;

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
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
        };

        int[] tris = new int[]
        {
            0,2,1, 0,3,2,  1,2,0, 2,3,0,
            4,6,5, 4,7,6,  5,6,4, 6,7,4,
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
        return Mathf.Lerp(0.5f, 0.12f, noise);
    }

    void SpawnAllFoliage()
    {
        var bushData = new List<(Matrix4x4 mat, Color baseCol, Color tipCol)>();
        var plantData = new List<(Matrix4x4 mat, Color baseCol, Color tipCol)>();

        SpawnFoliage(bushData, bushCount, 0.6f, 1.4f, 0.4f, 0.9f,
            new Color(0.06f, 0.14f, 0.04f), new Color(0.12f, 0.24f, 0.07f));
        SpawnFoliage(plantData, plantCount, 0.15f, 0.35f, 0.15f, 0.35f,
            new Color(0.07f, 0.16f, 0.04f), new Color(0.13f, 0.26f, 0.07f));

        PrepareBatchesWithColor(bushData, bushBatches);
        PrepareBatchesWithColor(plantData, plantBatches);
    }

    void SpawnFoliage(List<(Matrix4x4, Color, Color)> data, int count,
        float widthMin, float widthMax, float heightMin, float heightMax,
        Color baseCol, Color tipCol)
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
            int checkStart = Mathf.Max(0, placedPositions.Count - 40);
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
                Vector3 scale = new Vector3(w, h, w);

                Matrix4x4 mat = Matrix4x4.TRS(pos, rot, scale);

                float colorNoise = Mathf.PerlinNoise(x * 0.5f + 37.7f, z * 0.5f + 91.3f);
                float hueShift = (colorNoise - 0.5f) * colorVariation;
                float brightShift = (Random.Range(-1f, 1f)) * colorVariation * 0.5f;

                Color instanceBase = new Color(
                    Mathf.Clamp01(baseCol.r + hueShift * 0.3f + brightShift),
                    Mathf.Clamp01(baseCol.g + hueShift * 0.5f + brightShift),
                    Mathf.Clamp01(baseCol.b + hueShift * 0.2f + brightShift),
                    1f
                );
                Color instanceTip = new Color(
                    Mathf.Clamp01(tipCol.r + hueShift * 0.3f + brightShift),
                    Mathf.Clamp01(tipCol.g + hueShift * 0.5f + brightShift),
                    Mathf.Clamp01(tipCol.b + hueShift * 0.2f + brightShift),
                    1f
                );

                data.Add((mat, instanceBase, instanceTip));
                placedPositions.Add(candidate);
                placed++;
            }
        }
    }

    void PrepareBatchesWithColor(List<(Matrix4x4 mat, Color baseCol, Color tipCol)> data,
        List<FoliageBatch> batches)
    {
        batches.Clear();
        for (int i = 0; i < data.Count; i += 1023)
        {
            int count = Mathf.Min(1023, data.Count - i);
            Matrix4x4[] matrices = new Matrix4x4[count];
            Vector4[] baseColors = new Vector4[count];
            Vector4[] tipColors = new Vector4[count];

            for (int j = 0; j < count; j++)
            {
                var item = data[i + j];
                matrices[j] = item.mat;
                baseColors[j] = item.baseCol;
                tipColors[j] = item.tipCol;
            }

            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetVectorArray("_BaseColor", baseColors);
            props.SetVectorArray("_TipColor", tipColors);

            batches.Add(new FoliageBatch { matrices = matrices, props = props });
        }
    }

    void Update()
    {
        if (foliageQuad == null) return;

        DrawBatches(bushMaterial, bushBatches);
        DrawBatches(smallPlantMaterial, plantBatches);
    }

    void DrawBatches(Material mat, List<FoliageBatch> batches)
    {
        if (mat == null) return;
        foreach (var batch in batches)
        {
            Graphics.DrawMeshInstanced(foliageQuad, 0, mat, batch.matrices,
                batch.matrices.Length, batch.props);
        }
    }
}
