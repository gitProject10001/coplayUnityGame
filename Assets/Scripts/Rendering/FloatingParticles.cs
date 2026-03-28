using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GPU-instanced floating dust motes / leaves that drift with sinusoidal
/// and Perlin noise movement. Particles billboard toward the camera.
/// </summary>
public class FloatingParticles : MonoBehaviour
{
    [Header("Settings")]
    public int particleCount = 80;
    public float spawnRadius = 12f;
    public float particleScaleMin = 0.05f;
    public float particleScaleMax = 0.15f;

    [Header("Movement")]
    public float driftSpeed = 0.3f;
    public float upwardSpeed = 0.1f;
    public float noiseStrength = 0.5f;
    public float sinAmplitude = 0.2f;
    public float sinFrequency = 1.5f;

    [Header("Rendering")]
    public Material particleMaterial;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float phase;
        public float lifetime;
        public float scale;
    }

    private Particle[] particles;
    private Matrix4x4[] matrices;
    private Mesh quadMesh;
    private MaterialPropertyBlock propBlock;

    // Batching
    private List<Matrix4x4[]> batches = new List<Matrix4x4[]>();
    private List<int> batchCounts = new List<int>();

    void Start()
    {
        CreateQuadMesh();
        InitParticles();

        propBlock = new MaterialPropertyBlock();
        propBlock.SetColor("_BaseColor", new Color(1.0f, 0.9f, 0.6f, 0.8f));
    }

    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        quadMesh.normals = new Vector3[]
        {
            -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward
        };
        quadMesh.RecalculateBounds();
    }

    void InitParticles()
    {
        particles = new Particle[particleCount];
        matrices = new Matrix4x4[particleCount];

        Camera cam = Camera.main;
        Vector3 center = cam != null ? cam.transform.position : transform.position;

        for (int i = 0; i < particleCount; i++)
        {
            particles[i] = SpawnParticle(center);
        }
    }

    Particle SpawnParticle(Vector3 center)
    {
        Particle p = new Particle();
        Vector3 offset = Random.insideUnitSphere * spawnRadius;
        offset.y = Mathf.Abs(offset.y) * 0.5f + 0.5f; // Keep above ground
        p.position = center + offset;
        p.velocity = new Vector3(
            Random.Range(-0.1f, 0.1f),
            Random.Range(0.02f, upwardSpeed),
            Random.Range(-0.1f, 0.1f)
        );
        p.phase = Random.Range(0f, Mathf.PI * 2f);
        p.lifetime = 0f;
        p.scale = Random.Range(particleScaleMin, particleScaleMax);
        return p;
    }

    void Update()
    {
        if (particleMaterial == null || quadMesh == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        Quaternion camRot = cam.transform.rotation;
        float dt = Time.deltaTime;

        for (int i = 0; i < particleCount; i++)
        {
            ref Particle p = ref particles[i];

            p.lifetime += dt;

            // Sinusoidal drift
            float sinOffset = Mathf.Sin(p.lifetime * sinFrequency + p.phase) * sinAmplitude;

            // Perlin noise wandering
            float noiseX = (Mathf.PerlinNoise(p.lifetime * 0.3f + p.phase, 0f) - 0.5f) * noiseStrength;
            float noiseZ = (Mathf.PerlinNoise(0f, p.lifetime * 0.3f + p.phase) - 0.5f) * noiseStrength;

            // Update position
            p.position += new Vector3(
                (p.velocity.x + noiseX) * dt + sinOffset * dt,
                (p.velocity.y + upwardSpeed) * dt,
                (p.velocity.z + noiseZ) * dt
            );

            // Respawn if too far from camera
            float dist = Vector3.Distance(p.position, camPos);
            if (dist > spawnRadius)
            {
                p = SpawnParticle(camPos);
            }

            // Billboard toward camera
            float s = p.scale;
            matrices[i] = Matrix4x4.TRS(p.position, camRot, new Vector3(s, s, s));
        }

        // Draw instanced (all fit in one batch since count=80 < 1023)
        Graphics.DrawMeshInstanced(quadMesh, 0, particleMaterial, matrices, particleCount, propBlock);
    }
}
