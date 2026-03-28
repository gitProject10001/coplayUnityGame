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
        if (!cam.orthographic) return;

        // Calculate the size of one pixel in world units
        // orthoSize = half the vertical world-space height visible
        // screenHeight / pixelScale = number of "macro pixels" vertically
        float pixelWorldSize = (cam.orthographicSize * 2f) / (Screen.height / (float)pixelScale);

        Vector3 pos = transform.position;

        // Snap X and Z (top-down game) to pixel grid
        float snappedX = Mathf.Round(pos.x / pixelWorldSize) * pixelWorldSize;
        float snappedZ = Mathf.Round(pos.z / pixelWorldSize) * pixelWorldSize;

        // Store sub-pixel offset for potential re-projection (smooth feel)
        subPixelOffset.x = pos.x - snappedX;
        subPixelOffset.z = pos.z - snappedZ;

        transform.position = new Vector3(snappedX, pos.y, snappedZ);
    }

    /// <summary>
    /// Returns the sub-pixel offset that was removed during snapping.
    /// Can be used for screen-space re-projection if needed.
    /// </summary>
    public Vector3 GetSubPixelOffset()
    {
        return subPixelOffset;
    }
}
