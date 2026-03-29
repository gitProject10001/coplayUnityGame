using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(-10f, 10f, -10f);
    public float smoothSpeed = 8f;
    public float lookAheadDistance = 1.5f;
    public float lookAheadSmooth = 4f;

    private Vector3 currentLookAhead;
    private Vector3 lastTargetPos;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }
        if (target != null)
        {
            // If offset was never set (still zero), compute a sensible default
            if (offset.sqrMagnitude < 0.1f)
                offset = new Vector3(-10f, 10f, -10f);

            lastTargetPos = target.position;
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Compute velocity-based look-ahead
        Vector3 velocity = (target.position - lastTargetPos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastTargetPos = target.position;

        Vector3 targetLookAhead = Vector3.zero;
        if (velocity.sqrMagnitude > 0.5f)
        {
            targetLookAhead = velocity.normalized * lookAheadDistance;
            targetLookAhead.y = 0f;
        }
        currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSmooth * Time.deltaTime);

        // Desired position
        Vector3 desiredPosition = target.position + offset + currentLookAhead;

        // Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Apply screen shake
        transform.position += CombatFeedback.GetShakeOffset();
    }
}
