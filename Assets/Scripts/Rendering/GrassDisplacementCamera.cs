using UnityEngine;

/// <summary>
/// Manages an orthographic camera that renders displacement objects to a RenderTexture.
/// Sets global shader properties so the geometry grass shader can sample displacement.
/// Attach to a GameObject with a Camera component, or one will be created.
/// </summary>
public class GrassDisplacementCamera : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float orthoSize = 15f;
    public int renderTextureSize = 256;
    public LayerMask displacementLayer;

    private Camera displacementCamera;
    private RenderTexture displacementRT;

    void Awake()
    {
        displacementCamera = GetComponent<Camera>();
        if (displacementCamera == null)
            displacementCamera = gameObject.AddComponent<Camera>();

        // Create RenderTexture
        displacementRT = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
        displacementRT.name = "GrassDisplacementRT";

        // Configure orthographic top-down camera
        displacementCamera.orthographic = true;
        displacementCamera.orthographicSize = orthoSize;
        displacementCamera.targetTexture = displacementRT;
        // Only render layer 31 (displacement objects)
        displacementCamera.cullingMask = 1 << 31;
        displacementCamera.clearFlags = CameraClearFlags.SolidColor;
        displacementCamera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0f); // neutral grey = no displacement
        displacementCamera.depth = -100; // render before main camera
        displacementCamera.enabled = true;

        // Look straight down
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Set global displacement texture
        Shader.SetGlobalTexture("_DisplacementRT", displacementRT);

        // Exclude displacement layer from the main camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
            mainCam.cullingMask &= ~(1 << 31);
    }

    void Update()
    {
        if (target != null)
        {
            transform.position = new Vector3(target.position.x, target.position.y + 20f, target.position.z);
        }

        // Update global shader properties
        Vector3 position = transform.position;
        position.x -= displacementCamera.orthographicSize;
        position.z -= displacementCamera.orthographicSize;

        Shader.SetGlobalVector("_DisplacementLocation", position);
        Shader.SetGlobalFloat("_DisplacementSize", displacementCamera.orthographicSize * 2f);
    }

    void OnDestroy()
    {
        if (displacementRT != null)
        {
            displacementRT.Release();
            Destroy(displacementRT);
        }
    }
}
