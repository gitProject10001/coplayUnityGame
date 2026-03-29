using UnityEngine;

/// <summary>
/// Attach to any GameObject (player, NPC, etc.) that should bend grass when moving.
/// Creates a flat disc on the displacement layer that the displacement camera renders.
/// </summary>
[ExecuteInEditMode]
public class GrassDisplacementObject : MonoBehaviour
{
    [SerializeField] private float displacementRadius = 1.5f;
    [SerializeField] private LayerMask grassLayer;
    [SerializeField] private float maxGroundDistance = 2f;

    private GameObject discObject;
    private MeshRenderer discRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int transparencyID;

    void Awake()
    {
        CreateDisplacementDisc();
        transparencyID = Shader.PropertyToID("_Transparency");
        propertyBlock = new MaterialPropertyBlock();
    }

    void CreateDisplacementDisc()
    {
        if (discObject != null) return;

        discObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        discObject.name = "GrassDisplacementDisc";
        discObject.transform.SetParent(transform);
        discObject.transform.localPosition = Vector3.zero;
        discObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        discObject.transform.localScale = Vector3.one * displacementRadius * 2f;

        // Remove collider from the primitive
        var collider = discObject.GetComponent<Collider>();
        if (collider != null)
            DestroyImmediate(collider);

        // Use layer 31 for displacement — only the displacement camera renders this
        discObject.layer = 31;

        discRenderer = discObject.GetComponent<MeshRenderer>();

        // Create a simple unlit white material for displacement rendering
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = Color.white;
        discRenderer.material = mat;
        discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        discRenderer.receiveShadows = false;
    }

    void Update()
    {
        if (discObject == null || discRenderer == null || propertyBlock == null) return;

#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif

        // Keep the disc flat and aligned to world horizontal
        discObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        discObject.transform.localScale = Vector3.one * displacementRadius * 2f;

        // Raycast down to find ground distance, fade displacement when airborne
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGroundDistance, grassLayer))
        {
            float fade = hit.distance / maxGroundDistance;
            propertyBlock.SetFloat(transparencyID, fade);
            discRenderer.SetPropertyBlock(propertyBlock);
            discObject.transform.position = new Vector3(transform.position.x, hit.point.y + 0.05f, transform.position.z);
        }
        else
        {
            // Too far from ground — hide displacement
            propertyBlock.SetFloat(transparencyID, 1f);
            discRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    void OnDestroy()
    {
        if (discObject != null)
        {
            if (Application.isPlaying)
                Destroy(discObject);
            else
                DestroyImmediate(discObject);
        }
    }
}
