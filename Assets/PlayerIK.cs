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

    void Start()
    {
        animator = GetComponent<Animator>();
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
    }
}
