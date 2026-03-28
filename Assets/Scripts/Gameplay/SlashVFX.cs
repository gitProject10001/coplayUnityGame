using UnityEngine;

/// <summary>
/// Spawns a procedural slash arc that scales up and fades out.
/// </summary>
public class SlashVFX : MonoBehaviour
{
    public static void Spawn(Vector3 position, Quaternion rotation, int comboStep)
    {
        GameObject vfxObj = new GameObject("SlashVFX");
        SlashVFX vfx = vfxObj.AddComponent<SlashVFX>();
        vfx.Init(position, rotation, comboStep);
    }

    private MeshRenderer meshRenderer;
    private Material mat;
    private float lifetime;
    private float maxLifetime;
    private float startScale;
    private float endScale;
    private Color startColor;

    void Init(Vector3 position, Quaternion rotation, int comboStep)
    {
        transform.position = position;
        transform.rotation = rotation;

        // Create arc mesh
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = CreateArcMesh(comboStep);

        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        mat = new Material(shader);
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        // Color varies by combo step
        switch (comboStep)
        {
            case 0:
                startColor = new Color(1f, 1f, 1f, 0.9f);
                maxLifetime = 0.18f;
                startScale = 0.3f;
                endScale = 1.4f;
                break;
            case 1:
                startColor = new Color(0.6f, 0.85f, 1f, 0.95f);
                maxLifetime = 0.2f;
                startScale = 0.4f;
                endScale = 1.6f;
                break;
            case 2:
                startColor = new Color(1f, 0.7f, 0.3f, 1f);
                maxLifetime = 0.25f;
                startScale = 0.5f;
                endScale = 2.0f;
                break;
            default:
                startColor = Color.white;
                maxLifetime = 0.18f;
                startScale = 0.3f;
                endScale = 1.4f;
                break;
        }

        mat.color = startColor;
        meshRenderer.material = mat;
        lifetime = maxLifetime;
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float t = 1f - (lifetime / maxLifetime); // 0 → 1

        // Scale up with easing
        float easedT = 1f - (1f - t) * (1f - t); // ease out quad
        float scale = Mathf.Lerp(startScale, endScale, easedT);
        transform.localScale = Vector3.one * scale;

        // Fade out
        Color c = startColor;
        c.a = startColor.a * (1f - t * t); // Quadratic fade
        mat.color = c;
    }

    Mesh CreateArcMesh(int comboStep)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SlashArc";

        // Arc parameters vary by combo step
        float innerRadius = 0.5f;
        float outerRadius = 1.5f + comboStep * 0.3f;
        float arcAngle = 120f + comboStep * 15f; // Degrees
        int segments = 12;

        int vertCount = (segments + 1) * 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        int[] tris = new int[segments * 12]; // double-sided

        float startAngle = -arcAngle * 0.5f * Mathf.Deg2Rad;
        float angleStep = arcAngle * Mathf.Deg2Rad / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = startAngle + angleStep * i;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            verts[i * 2] = new Vector3(sin * innerRadius, 0f, cos * innerRadius);
            verts[i * 2 + 1] = new Vector3(sin * outerRadius, 0f, cos * outerRadius);
            uvs[i * 2] = new Vector2((float)i / segments, 0f);
            uvs[i * 2 + 1] = new Vector2((float)i / segments, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int bi = i * 12;
            int vi = i * 2;
            // Front face
            tris[bi + 0] = vi;
            tris[bi + 1] = vi + 2;
            tris[bi + 2] = vi + 1;
            tris[bi + 3] = vi + 1;
            tris[bi + 4] = vi + 2;
            tris[bi + 5] = vi + 3;
            // Back face (reversed winding)
            tris[bi + 6] = vi;
            tris[bi + 7] = vi + 1;
            tris[bi + 8] = vi + 2;
            tris[bi + 9] = vi + 1;
            tris[bi + 10] = vi + 3;
            tris[bi + 11] = vi + 2;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
