using UnityEngine;

public class StopMotionEffect : MonoBehaviour
{
    [Header("Stop Motion Settings")]
    public int animationFPS = 8;

    private Animator animator;
    private float frameTimer;
    private float frameInterval;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        frameInterval = 1f / animationFPS;
        animator.speed = 0f;
    }

    void Update()
    {
        frameTimer += Time.deltaTime;

        if (frameTimer >= frameInterval)
        {
            // Advance the animator by the accumulated time, then freeze again
            animator.speed = 1f;
            animator.Update(frameTimer);
            animator.speed = 0f;
            frameTimer = 0f;
        }
    }
}
