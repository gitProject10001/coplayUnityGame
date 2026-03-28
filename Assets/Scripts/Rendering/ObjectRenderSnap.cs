using UnityEngine;

/// <summary>
/// Snaps this object's position to the pixel grid before rendering, then restores it.
/// Prevents sub-pixel jittering on pixelated objects.
/// Attach to any renderable GameObject that should be pixel-perfect.
/// </summary>
public class ObjectRenderSnap : MonoBehaviour
{
    [Tooltip("Must match the pixelScale in PixelizeFeature settings")]
    public int pixelScale = 4;

    [Tooltip("Optional: snap rotation to this angle increment (0 = no rotation snap)")]
    public float angleSnapDegrees = 0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isSnapped;

    void OnEnable()
    {
        Camera.onPreRender += OnCameraPreRender;
        Camera.onPostRender += OnCameraPostRender;
    }

    void OnDisable()
    {
        Camera.onPreRender -= OnCameraPreRender;
        Camera.onPostRender -= OnCameraPostRender;
    }

    void OnCameraPreRender(Camera cam)
    {
        if (cam.cameraType != CameraType.Game) return;
        if (!cam.orthographic) return;

        float pixelWorldSize = (cam.orthographicSize * 2f) / (Screen.height / (float)pixelScale);

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        isSnapped = true;

        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / pixelWorldSize) * pixelWorldSize;
        pos.z = Mathf.Round(pos.z / pixelWorldSize) * pixelWorldSize;
        transform.position = pos;

        if (angleSnapDegrees > 0f)
        {
            Vector3 euler = transform.eulerAngles;
            euler.y = Mathf.Round(euler.y / angleSnapDegrees) * angleSnapDegrees;
            transform.eulerAngles = euler;
        }
    }

    void OnCameraPostRender(Camera cam)
    {
        if (!isSnapped) return;
        if (cam.cameraType != CameraType.Game) return;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        isSnapped = false;
    }
}
