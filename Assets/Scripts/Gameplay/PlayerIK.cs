using UnityEngine;

public class PlayerIK : MonoBehaviour
{
    private Animator animator;
    public Vector3 lookAtTarget;
    public float lookAtWeight = 1.0f;

    // Arm IK for aiming
    public bool isShooting;
    public Vector3 aimTarget;
    public float aimIKWeight = 0f;
    private float aimIKSmoothed = 0f;

    [Header("Foot IK")]
    public bool enableFootIK = true;
    public LayerMask groundLayer = ~0;
    public float footRaycastHeight = 0.4f;
    public float footRaycastDistance = 0.6f;
    public float footIKWeight = 0.7f;
    public float footRotationWeight = 0.5f;
    public float footOffsetY = 0.02f;
    public float maxBodyOffset = 0.15f;
    public float bodyOffsetSpeed = 0.08f;

    private float lastBodyOffset;
    private PlayerController playerController;
    private Collider[] playerColliders;
    private Transform leftFootBone;
    private Transform rightFootBone;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();

        // Cache player colliders to ignore them during raycasts
        if (playerController != null)
            playerColliders = playerController.GetComponentsInChildren<Collider>();

        // Cache foot bone transforms
        if (animator != null)
        {
            leftFootBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFootBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    }

    void Update()
    {
        float targetWeight = isShooting ? 1f : 0f;
        aimIKSmoothed = Mathf.MoveTowards(aimIKSmoothed, targetWeight, Time.deltaTime * 8f);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Head/eyes look at cursor
        animator.SetLookAtWeight(lookAtWeight, 0f, 0.6f, 1.0f, 0.5f);
        animator.SetLookAtPosition(lookAtTarget);

        // Right hand aims toward target when shooting
        if (aimIKSmoothed > 0.01f)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, aimIKSmoothed);
            animator.SetIKPosition(AvatarIKGoal.RightHand, aimTarget);

            // Left hand also aims slightly for two-handed feel
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, aimIKSmoothed * 0.5f);
            Vector3 leftTarget = aimTarget + Vector3.left * 0.15f;
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftTarget);
        }

        // Foot IK — only during Idle/Moving states
        if (enableFootIK && IsGroundedState() && leftFootBone != null && rightFootBone != null)
        {
            // Temporarily disable player colliders so raycasts don't hit ourselves
            SetPlayerCollidersEnabled(false);

            // Left foot
            ApplyFootIK(AvatarIKGoal.LeftFoot, leftFootBone.position, out float leftOffset);

            // Right foot
            ApplyFootIK(AvatarIKGoal.RightFoot, rightFootBone.position, out float rightOffset);

            // Re-enable player colliders
            SetPlayerCollidersEnabled(true);

            // Adjust body/hip height — lower body by the largest downward offset
            // Only apply negative offsets (push body down), never up
            float targetBodyOffset = Mathf.Min(leftOffset, rightOffset);
            targetBodyOffset = Mathf.Clamp(targetBodyOffset, -maxBodyOffset, 0f);
            lastBodyOffset = Mathf.MoveTowards(lastBodyOffset, targetBodyOffset, bodyOffsetSpeed);

            Vector3 bodyPos = animator.bodyPosition;
            bodyPos.y += lastBodyOffset;
            animator.bodyPosition = bodyPos;
        }
        else
        {
            // Smoothly blend out body offset when not grounded
            lastBodyOffset = Mathf.MoveTowards(lastBodyOffset, 0f, bodyOffsetSpeed);

            // Reset foot IK weights
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        }
    }

    private bool IsGroundedState()
    {
        if (playerController == null) return true;
        var state = playerController.currentState;
        return state == PlayerState.Idle || state == PlayerState.Moving;
    }

    private void SetPlayerCollidersEnabled(bool enabled)
    {
        if (playerColliders == null) return;
        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] != null)
                playerColliders[i].enabled = enabled;
        }
    }

    private void ApplyFootIK(AvatarIKGoal foot, Vector3 footPos, out float heightOffset)
    {
        heightOffset = 0f;
        Vector3 rayOrigin = footPos + Vector3.up * footRaycastHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
            footRaycastHeight + footRaycastDistance, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 targetPos = hit.point + Vector3.up * footOffsetY;
            heightOffset = targetPos.y - footPos.y;

            // Only apply if the offset is reasonable (avoid extreme adjustments)
            if (Mathf.Abs(heightOffset) > 0.5f)
            {
                animator.SetIKPositionWeight(foot, 0f);
                animator.SetIKRotationWeight(foot, 0f);
                heightOffset = 0f;
                return;
            }

            animator.SetIKPositionWeight(foot, footIKWeight);
            animator.SetIKPosition(foot, targetPos);

            // Align foot rotation to surface normal (gentle blend)
            Quaternion footRotation = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, hit.normal), hit.normal);
            animator.SetIKRotationWeight(foot, footRotationWeight);
            animator.SetIKRotation(foot, footRotation);
        }
        else
        {
            animator.SetIKPositionWeight(foot, 0f);
            animator.SetIKRotationWeight(foot, 0f);
        }
    }
}
