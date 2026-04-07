using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ProceduralGenerator : MonoBehaviour
{
    [Header("Counts")]
    public int rockCount = 20;
    public int treeCount = 8;
    public int bushCount = 25;

    [Header("Layout")]
    public float spawnAreaSize = 30f;

    [Header("Editor Controls")]
    [Tooltip("Check this box to regenerate all environment in the editor")]
    public bool forceRegenerate = false;

    // Shared base material (ToonLit)
    private Material _toonMaterial;

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            if (transform.Find("Rocks") == null)
                GenerateAll();
        }
    }

    void Start()
    {
        if (Application.isPlaying)
            GenerateAll();
    }

    void Update()
    {
        if (forceRegenerate && !Application.isPlaying)
        {
            forceRegenerate = false;
            GenerateAll();
        }
    }

    // ─────────────────────────────────────────────
    // Main generation
    // ─────────────────────────────────────────────

    public void GenerateAll()
    {
        Random.InitState(42);

        EnsureToonMaterial();
        ClearChildren();

        // Generate base meshes once
        Mesh icoMesh = CreateIcosphere(2);

        // Placement data we accumulate for bush proximity
        List<Vector3> interestPoints = new List<Vector3>();

        GenerateRocks(icoMesh, interestPoints);
        GenerateTrees(icoMesh, interestPoints);
        GenerateBushes(icoMesh, interestPoints);
    }

    // ─────────────────────────────────────────────
    // Material
    // ─────────────────────────────────────────────

    private void EnsureToonMaterial()
    {
        if (_toonMaterial != null) return;
        Shader shader = Shader.Find("Custom/ToonLit");
        if (shader == null)
        {
            Debug.LogWarning("ProceduralGenerator: Custom/ToonLit shader not found, falling back to Standard.");
            shader = Shader.Find("Standard");
        }
        _toonMaterial = new Material(shader);
    }

    // ─────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────

    private void ClearChildren()
    {
        string[] groups = { "Rocks", "Trees", "Bushes", "Cubes", "Pyramids" };
        foreach (string name in groups)
        {
            Transform old = transform.Find(name);
            if (old != null)
                DestroyImmediate(old.gameObject);
        }
    }

    // ─────────────────────────────────────────────
    // Rocks
    // ─────────────────────────────────────────────

    private void GenerateRocks(Mesh baseIco, List<Vector3> interestPoints)
    {
        Transform parent = CreateGroup("Rocks");

        Color[] rockColors = {
            new Color(0.4f, 0.42f, 0.4f),
            new Color(0.3f, 0.4f, 0.25f),
            new Color(0.25f, 0.27f, 0.28f)
        };

        // Cluster centers
        int clusterCount = Random.Range(5, 8);
        int rocksPlaced = 0;

        for (int c = 0; c < clusterCount && rocksPlaced < rockCount; c++)
        {
            Vector3 clusterCenter = RandomPositionXZ();
            int rocksInCluster = Random.Range(3, 6);

            for (int r = 0; r < rocksInCluster && rocksPlaced < rockCount; r++)
            {
                float clusterRadius = Random.Range(2f, 3f);
                Vector2 offset = Random.insideUnitCircle * clusterRadius;
                Vector3 pos = clusterCenter + new Vector3(offset.x, 0f, offset.y);

                // Create unique noisy rock mesh
                float noiseStrength = Random.Range(0.15f, 0.3f);
                Mesh rockMesh = CloneMesh(baseIco);
                ApplyVertexNoise(rockMesh, noiseStrength, 2.0f);
                MakeFlatShaded(rockMesh);

                float scaleXZ = Random.Range(0.4f, 1.6f);
                float scaleY = scaleXZ * Random.Range(0.4f, 0.7f);
                Vector3 scale = new Vector3(scaleXZ, scaleY, scaleXZ);

                pos.y = -scaleY * 0.3f;

                GameObject rock = CreateMeshObject("Rock_" + rocksPlaced, rockMesh, parent);
                rock.transform.position = pos;
                rock.transform.localScale = scale;
                rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Color via MaterialPropertyBlock
                SetToonColor(rock, rockColors[Random.Range(0, rockColors.Length)]);

                // Simple sphere collider for gameplay (avoids convex mesh warnings)
                SphereCollider sc = rock.AddComponent<SphereCollider>();
                sc.radius = 0.5f;

                interestPoints.Add(pos);
                rocksPlaced++;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Trees
    // ─────────────────────────────────────────────

    private void GenerateTrees(Mesh baseIco, List<Vector3> interestPoints)
    {
        Transform parent = CreateGroup("Trees");

        Color trunkColor = new Color(0.35f, 0.25f, 0.15f);
        Color[] canopyColors = {
            new Color(0.18f, 0.38f, 0.12f),
            new Color(0.24f, 0.42f, 0.18f),
            new Color(0.15f, 0.32f, 0.10f),
            new Color(0.20f, 0.45f, 0.14f)
        };

        int treesPlaced = 0;
        int attempts = 0;

        while (treesPlaced < treeCount && attempts < 200)
        {
            attempts++;
            Vector3 pos = RandomPositionXZ();

            // Bias toward edges: reject if too close to center
            if (pos.magnitude < 5f && Random.value < 0.7f)
                continue;

            pos.y = 0f;

            GameObject treeRoot = new GameObject("Tree_" + treesPlaced);
            treeRoot.transform.SetParent(parent);
            treeRoot.transform.position = pos;

            // Trunk
            float trunkHeight = Random.Range(1f, 2f);
            float trunkRadius = Random.Range(0.1f, 0.2f);
            Mesh trunkMesh = CreateCylinder(8, trunkHeight, trunkRadius);
            MakeFlatShaded(trunkMesh);

            GameObject trunk = CreateMeshObject("Trunk", trunkMesh, treeRoot.transform);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            SetToonColor(trunk, trunkColor);

            // Canopy: 2-3 overlapping noisy spheres
            int canopyCount = Random.Range(2, 4);
            for (int i = 0; i < canopyCount; i++)
            {
                Mesh canopyMesh = CloneMesh(baseIco);
                ApplyVertexNoise(canopyMesh, Random.Range(0.1f, 0.2f), 2.0f);
                MakeFlatShaded(canopyMesh);

                float canopyScale = Random.Range(0.8f, 1.8f);
                Vector3 canopyOffset = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    trunkHeight + Random.Range(0f, 0.5f),
                    Random.Range(-0.3f, 0.3f)
                );

                GameObject canopy = CreateMeshObject("Canopy_" + i, canopyMesh, treeRoot.transform);
                canopy.transform.localPosition = canopyOffset;
                canopy.transform.localScale = Vector3.one * canopyScale;
                SetToonColor(canopy, canopyColors[Random.Range(0, canopyColors.Length)]);
            }

            interestPoints.Add(pos);
            treesPlaced++;
        }
    }

    // ─────────────────────────────────────────────
    // Bushes
    // ─────────────────────────────────────────────

    private void GenerateBushes(Mesh baseIco, List<Vector3> interestPoints)
    {
        Transform parent = CreateGroup("Bushes");

        int bushesPlaced = 0;
        int attempts = 0;

        while (bushesPlaced < bushCount && attempts < 500)
        {
            attempts++;

            // Pick a random interest point and offset from it
            if (interestPoints.Count == 0) break;

            Vector3 anchor = interestPoints[Random.Range(0, interestPoints.Count)];
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector3 pos = anchor + new Vector3(offset.x, 0f, offset.y);

            // Stay within bounds
            float half = spawnAreaSize * 0.5f;
            if (Mathf.Abs(pos.x) > half || Mathf.Abs(pos.z) > half)
                continue;

            float scale = Random.Range(0.3f, 0.7f);
            pos.y = -scale * 0.15f;

            Mesh bushMesh = CloneMesh(baseIco);
            ApplyVertexNoise(bushMesh, Random.Range(0.05f, 0.15f), 3.0f);
            MakeFlatShaded(bushMesh);

            GameObject bush = CreateMeshObject("Bush_" + bushesPlaced, bushMesh, parent);
            bush.transform.position = pos;
            bush.transform.localScale = Vector3.one * scale;
            bush.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Color bushColor = Color.Lerp(
                new Color(0.15f, 0.35f, 0.10f),
                new Color(0.25f, 0.45f, 0.15f),
                Random.value
            );
            SetToonColor(bush, bushColor);

            bushesPlaced++;
        }
    }

    // ─────────────────────────────────────────────
    // Mesh generation: Icosphere
    // ─────────────────────────────────────────────

    public Mesh CreateIcosphere(int subdivisions)
    {
        // Golden ratio
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        List<Vector3> verts = new List<Vector3>
        {
            new Vector3(-1,  t,  0).normalized,
            new Vector3( 1,  t,  0).normalized,
            new Vector3(-1, -t,  0).normalized,
            new Vector3( 1, -t,  0).normalized,
            new Vector3( 0, -1,  t).normalized,
            new Vector3( 0,  1,  t).normalized,
            new Vector3( 0, -1, -t).normalized,
            new Vector3( 0,  1, -t).normalized,
            new Vector3( t,  0, -1).normalized,
            new Vector3( t,  0,  1).normalized,
            new Vector3(-t,  0, -1).normalized,
            new Vector3(-t,  0,  1).normalized,
        };

        List<int> tris = new List<int>
        {
            0,11,5,  0,5,1,   0,1,7,   0,7,10,  0,10,11,
            1,5,9,   5,11,4,  11,10,2,  10,7,6,  7,1,8,
            3,9,4,   3,4,2,   3,2,6,   3,6,8,   3,8,9,
            4,9,5,   2,4,11,  6,2,10,  8,6,7,   9,8,1,
        };

        // Subdivide
        Dictionary<long, int> midpointCache = new Dictionary<long, int>();

        for (int s = 0; s < subdivisions; s++)
        {
            List<int> newTris = new List<int>();
            midpointCache.Clear();

            for (int i = 0; i < tris.Count; i += 3)
            {
                int v0 = tris[i];
                int v1 = tris[i + 1];
                int v2 = tris[i + 2];

                int a = GetMidpoint(v0, v1, verts, midpointCache);
                int b = GetMidpoint(v1, v2, verts, midpointCache);
                int c = GetMidpoint(v2, v0, verts, midpointCache);

                newTris.AddRange(new[] { v0, a, c });
                newTris.AddRange(new[] { v1, b, a });
                newTris.AddRange(new[] { v2, c, b });
                newTris.AddRange(new[] { a, b, c });
            }

            tris = newTris;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Icosphere";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private int GetMidpoint(int i0, int i1, List<Vector3> verts, Dictionary<long, int> cache)
    {
        long smallerIndex = Mathf.Min(i0, i1);
        long greaterIndex = Mathf.Max(i0, i1);
        long key = (smallerIndex << 16) + greaterIndex;

        if (cache.TryGetValue(key, out int ret))
            return ret;

        Vector3 mid = ((verts[i0] + verts[i1]) * 0.5f).normalized;
        verts.Add(mid);
        int index = verts.Count - 1;
        cache[key] = index;
        return index;
    }

    // ─────────────────────────────────────────────
    // Mesh generation: Flat shading
    // ─────────────────────────────────────────────

    public void MakeFlatShaded(Mesh mesh)
    {
        Vector3[] oldVerts = mesh.vertices;
        int[] oldTris = mesh.triangles;

        Vector3[] newVerts = new Vector3[oldTris.Length];
        int[] newTris = new int[oldTris.Length];

        for (int i = 0; i < oldTris.Length; i++)
        {
            newVerts[i] = oldVerts[oldTris[i]];
            newTris[i] = i;
        }

        mesh.vertices = newVerts;
        mesh.triangles = newTris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // ─────────────────────────────────────────────
    // Mesh generation: Vertex noise
    // ─────────────────────────────────────────────

    public void ApplyVertexNoise(Mesh mesh, float strength, float scale)
    {
        Vector3[] verts = mesh.vertices;
        Vector3[] normals = mesh.normals;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            float noise = Mathf.PerlinNoise(v.x * scale, v.z * scale) * strength;
            verts[i] = v + (normals.Length > i ? normals[i] : v.normalized) * noise;
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // ─────────────────────────────────────────────
    // Mesh generation: Cylinder
    // ─────────────────────────────────────────────

    public Mesh CreateCylinder(int sides, float height, float radius)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float halfH = height * 0.5f;

        for (int i = 0; i < sides; i++)
        {
            float a0 = (2f * Mathf.PI * i) / sides;
            float a1 = (2f * Mathf.PI * ((i + 1) % sides)) / sides;

            Vector3 b0 = new Vector3(Mathf.Cos(a0) * radius, -halfH, Mathf.Sin(a0) * radius);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * radius, -halfH, Mathf.Sin(a1) * radius);
            Vector3 t0 = new Vector3(Mathf.Cos(a0) * radius,  halfH, Mathf.Sin(a0) * radius);
            Vector3 t1 = new Vector3(Mathf.Cos(a1) * radius,  halfH, Mathf.Sin(a1) * radius);

            int idx = verts.Count;

            // Side quad (two triangles)
            verts.Add(b0); verts.Add(b1); verts.Add(t1); verts.Add(t0);
            tris.AddRange(new[] { idx, idx + 1, idx + 2 });
            tris.AddRange(new[] { idx, idx + 2, idx + 3 });

            // Bottom triangle
            idx = verts.Count;
            verts.Add(new Vector3(0, -halfH, 0)); verts.Add(b1); verts.Add(b0);
            tris.AddRange(new[] { idx, idx + 1, idx + 2 });

            // Top triangle
            idx = verts.Count;
            verts.Add(new Vector3(0, halfH, 0)); verts.Add(t0); verts.Add(t1);
            tris.AddRange(new[] { idx, idx + 1, idx + 2 });
        }

        Mesh mesh = new Mesh();
        mesh.name = "Cylinder";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private Mesh CloneMesh(Mesh source)
    {
        Mesh clone = new Mesh();
        clone.name = source.name + "_clone";
        clone.vertices = (Vector3[])source.vertices.Clone();
        clone.triangles = (int[])source.triangles.Clone();
        clone.normals = (Vector3[])source.normals.Clone();
        clone.bounds = source.bounds;
        return clone;
    }

    private Transform CreateGroup(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    private GameObject CreateMeshObject(string name, Mesh mesh, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = _toonMaterial;

        return go;
    }

    private void SetToonColor(GameObject go, Color baseColor)
    {
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", baseColor);
        block.SetColor("_ShadowColor", baseColor * 0.5f);
        block.SetFloat("_LightSteps", 3f);
        block.SetFloat("_EdgeSmoothness", 0.05f);
        block.SetFloat("_AmbientStrength", 0.15f);
        mr.SetPropertyBlock(block);
    }

    private Vector3 RandomPositionXZ(float minDistFromCenter = 3f)
    {
        float half = spawnAreaSize * 0.5f;
        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-half, half),
                0f,
                Random.Range(-half, half)
            );
            if (pos.magnitude > minDistFromCenter)
                return pos;
        }
        // fallback
        return new Vector3(
            Random.Range(-half, half),
            0f,
            Random.Range(-half, half)
        );
    }
}
