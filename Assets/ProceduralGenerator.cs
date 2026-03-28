using UnityEngine;

[ExecuteAlways]
public class ProceduralGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int cubeCount = 20;
    public int pyramidCount = 20;
    public float spawnAreaSize = 20f;

    [Header("Editor Controls")]
    [Tooltip("Check this box to regenerate cubes in the editor")]
    public bool forceRegenerateCubes = false;

    void OnEnable()
    {
        // Generate cubes automatically when the script is added or enabled in the editor
        if (!Application.isPlaying)
        {
            if (transform.Find("Cubes") == null)
            {
                GenerateCubes();
            }
        }
    }

    void Start()
    {
        // Generate pyramids only when the game starts (Play mode)
        if (Application.isPlaying)
        {
            GeneratePyramids();
        }
    }

    void Update()
    {
        // Allow regenerating cubes via the inspector checkbox
        if (forceRegenerateCubes && !Application.isPlaying)
        {
            forceRegenerateCubes = false;
            GenerateCubes();
        }
    }

    public void GenerateCubes()
    {
        Transform oldCubes = transform.Find("Cubes");
        if (oldCubes != null)
        {
            DestroyImmediate(oldCubes.gameObject);
        }

        GameObject cubesParent = new GameObject("Cubes");
        cubesParent.transform.SetParent(transform);

        for (int i = 0; i < cubeCount; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(cubesParent.transform);
            cube.transform.position = GetRandomPosition();
            
            // Random rotation and scale for variety
            cube.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            cube.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
        }
    }

    public void GeneratePyramids()
    {
        Transform oldPyramids = transform.Find("Pyramids");
        if (oldPyramids != null)
        {
            Destroy(oldPyramids.gameObject);
        }

        GameObject pyramidsParent = new GameObject("Pyramids");
        pyramidsParent.transform.SetParent(transform);

        Mesh pyramidMesh = CreatePyramidMesh();
        
        // Try to use URP Lit shader, fallback to Standard if not found
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        Material defaultMat = new Material(shader);
        defaultMat.color = Color.yellow; // Make pyramids distinct

        for (int i = 0; i < pyramidCount; i++)
        {
            GameObject pyramid = new GameObject("Pyramid");
            pyramid.transform.SetParent(pyramidsParent.transform);
            pyramid.transform.position = GetRandomPosition();
            
            MeshFilter mf = pyramid.AddComponent<MeshFilter>();
            mf.mesh = pyramidMesh;
            
            MeshRenderer mr = pyramid.AddComponent<MeshRenderer>();
            mr.material = defaultMat;

            MeshCollider mc = pyramid.AddComponent<MeshCollider>();
            mc.sharedMesh = pyramidMesh;
            mc.convex = true;

            pyramid.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            pyramid.transform.localScale = Vector3.one * Random.Range(0.5f, 1.5f);
        }
    }

    private Vector3 GetRandomPosition()
    {
        float x = Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f);
        float z = Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f);
        return new Vector3(x, 0.5f, z);
    }

    private Mesh CreatePyramidMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "PyramidMesh";

        Vector3[] vertices = new Vector3[]
        {
            // Base
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            // Top
            new Vector3(0, 1f, 0)
        };

        int[] triangles = new int[]
        {
            // Base
            0, 2, 1,
            0, 3, 2,
            // Sides
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
