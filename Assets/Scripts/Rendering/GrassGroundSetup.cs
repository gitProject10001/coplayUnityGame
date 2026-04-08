using UnityEngine;

/// <summary>
/// Creates a subdivided ground plane mesh and applies the geometry grass material.
/// The geometry grass shader tessellates this mesh and emits grass blades from each triangle.
/// </summary>
[ExecuteAlways]
public class GrassGroundSetup : MonoBehaviour
{
    [Header("Ground Plane")]
    public float radius = 15f;
    [Tooltip("Grid subdivisions per unit of radius. Resolution auto-scales with radius to keep density constant.")]
    public float densityPerUnit = 2.5f;
    public float yOffset = 0.01f;

    [Header("Material")]
    public Material grassMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        Setup();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            Setup();
    }

    void Setup()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Auto-scale resolution to maintain consistent grass density at any radius
        int gridResolution = Mathf.Clamp(Mathf.RoundToInt(radius * densityPerUnit), 4, 250);
        meshFilter.sharedMesh = CreateSubdividedPlane(gridResolution, radius);

        if (grassMaterial != null)
            meshRenderer.sharedMaterial = grassMaterial;

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = true;

        transform.position = new Vector3(0, yOffset, 0);
    }

    Mesh CreateSubdividedPlane(int resolution, float size)
    {
        Mesh mesh = new Mesh();
        mesh.name = "GrassGroundPlane";

        int vertCount = (resolution + 1) * (resolution + 1);
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector4[] tangents = new Vector4[vertCount];

        float step = (size * 2f) / resolution;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int i = z * (resolution + 1) + x;
                vertices[i] = new Vector3(-size + x * step, 0, -size + z * step);
                normals[i] = Vector3.up;
                tangents[i] = new Vector4(1, 0, 0, -1);
                uvs[i] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int[] triangles = new int[resolution * resolution * 6];
        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bl = z * (resolution + 1) + x;
                int br = bl + 1;
                int tl = bl + (resolution + 1);
                int tr = tl + 1;

                triangles[t++] = bl;
                triangles[t++] = tl;
                triangles[t++] = br;

                triangles[t++] = br;
                triangles[t++] = tl;
                triangles[t++] = tr;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.tangents = tangents;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }
}
