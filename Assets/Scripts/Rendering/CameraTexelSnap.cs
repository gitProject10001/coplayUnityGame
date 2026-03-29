using UnityEngine;

/// <summary>
/// Snaps an orthographic camera to the pixel grid to prevent pixel swimming/crawling.
/// Attach to the same GameObject as the Camera. Runs after CameraFollow in LateUpdate.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)] // Run after CameraFollow
public class CameraTexelSnap : MonoBehaviour
{
    [Tooltip("Must match the pixelScale in PixelizeFeature settings")]
    public int pixelScale = 4;

    private Camera cam;
    private Vector3 subPixelOffset;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (Screen.height <= 0 || pixelScale <= 0) return;

        // Calculate the size of one macro-pixel in world units
        float macroPixelCount = Screen.height / (float)pixelScale;
        if (macroPixelCount < 1f) return;

        float pixelWorldSize;
        if (cam.orthographic)
        {
            pixelWorldSize = (cam.orthographicSize * 2f) / macroPixelCount;
        }
        else
        {
            // For perspective, approximate using the vertical extent at the ground plane distance
            float distToGround = Mathf.Max(transform.position.y, 1f);
            float verticalExtent = 2f * distToGround * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            pixelWorldSize = verticalExtent / macroPixelCount;
        }
        if (pixelWorldSize <= 0.0001f) return;

        Vector3 pos = transform.position;

        // Guard against NaN propagation — reset to origin if corrupted
        if (float.IsNaN(pos.x) || float.IsNaN(pos.z))
        {
            transform.position = new Vector3(0, pos.y, 0);
            return;
        }

        // Snap X and Z (top-down game) to pixel grid
        float snappedX = Mathf.Round(pos.x / pixelWorldSize) * pixelWorldSize;
        float snappedZ = Mathf.Round(pos.z / pixelWorldSize) * pixelWorldSize;

        // Final NaN check
        if (float.IsNaN(snappedX) || float.IsNaN(snappedZ)) return;

        // Store sub-pixel offset for potential re-projection (smooth feel)
        subPixelOffset.x = pos.x - snappedX;
        subPixelOffset.z = pos.z - snappedZ;

        transform.position = new Vector3(snappedX, pos.y, snappedZ);
    }

    public Vector3 GetSubPixelOffset()
    {
        return subPixelOffset;
    }
}
