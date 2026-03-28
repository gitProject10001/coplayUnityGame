using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Combat")]
    public float attackRange = 2f;
    public float[] comboDamage = { 40f, 55f, 80f };
    public float[] comboKnockback = { 5f, 7f, 12f };
    public float comboWindowTime = 0.6f;
    public float attackDashForce = 4f;

    [Header("Fire Weapon")]
    public float fireRate = 0.08f;
    public float bulletSpeed = 25f;
    public float bulletDamage = 15f;
    public float spreadAngle = 8f;
    public float muzzleOffset = 1.2f;

    [Header("Turn Animation Settings")]
    public float turnThreshold90 = 60f;
    public float turnThreshold180 = 140f;
    public float turnCooldown = 0.5f;

    private Rigidbody rb;
    private Vector3 movement;
    private Animator animator;
    private PlayerIK playerIK;
    private float lastTurnTime;
    private float fireTimer;

    // Combo state
    private int comboStep = 0;           // 0 = not attacking, 1-3 = combo step
    private float comboTimer;             // Time left to chain next attack
    private bool canComboChain;           // Can we accept input for next combo?
    private bool attackQueued;            // Player clicked during combo window
    private float attackCooldownTimer;    // Prevent spam after combo ends

    // Slash arc rotation offsets for visual variety per combo step
    private float[] slashAngles = { -30f, 30f, 0f };

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        animator = GetComponentInChildren<Animator>();
        
        if (animator != null)
        {
            playerIK = animator.gameObject.GetComponent<PlayerIK>();
            if (playerIK == null) playerIK = animator.gameObject.AddComponent<PlayerIK>();
        }

        // Ensure CombatFeedback exists
        if (CombatFeedback.Instance == null)
        {
            GameObject fb = new GameObject("CombatFeedback");
            fb.AddComponent<CombatFeedback>();
        }
    }

    private Vector3 mouseWorldPoint;

    void Update()
    {
        // --- Input ---
        float moveHorizontal = 0f;
        float moveVertical = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveVertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveVertical -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveHorizontal += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveHorizontal -= 1f;
        }

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        movement = (cameraForward * moveVertical + cameraRight * moveHorizontal).normalized;
        bool isMoving = movement.sqrMagnitude > 0.01f;

        // --- Animator blend tree ---
        if (animator != null)
        {
            if (isMoving)
            {
                float localMoveX = Vector3.Dot(movement, transform.right);
                float localMoveY = Vector3.Dot(movement, transform.forward);
                animator.SetFloat("MoveX", localMoveX, 0.1f, Time.deltaTime);
                animator.SetFloat("MoveY", localMoveY, 0.1f, Time.deltaTime);
            }
            else
            {
                animator.SetFloat("MoveX", 0f, 0.1f, Time.deltaTime);
                animator.SetFloat("MoveY", 0f, 0.1f, Time.deltaTime);
            }

            HandleTurnAnimation(isMoving);
        }

        // --- Mouse Look ---
        if (Mouse.current != null && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float rayDistance))
            {
                mouseWorldPoint = ray.GetPoint(rayDistance);
                
                if (playerIK != null)
                {
                    Vector3 ikTarget = mouseWorldPoint;
                    ikTarget.y = transform.position.y + 1.2f;
                    Vector3 ikDir = ikTarget - (transform.position + Vector3.up * 1.2f);
                    if (ikDir.magnitude < 2f)
                        ikTarget = (transform.position + Vector3.up * 1.2f) + ikDir.normalized * 2f;
                    playerIK.lookAtTarget = ikTarget;
                }
            }
        }

        // --- Combo timer ---
        if (comboStep > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                // Combo expired
                comboStep = 0;
                canComboChain = false;
                attackQueued = false;
                attackCooldownTimer = 0.15f;
            }
        }

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // --- Attack Input ---
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (comboStep == 0 && attackCooldownTimer <= 0f)
            {
                // Start combo
                ExecuteAttack(0);
            }
            else if (comboStep > 0 && comboStep < 3)
            {
                // Queue next combo hit
                attackQueued = true;
            }
        }

        // Process queued combo
        if (attackQueued && canComboChain && comboStep > 0 && comboStep < 3)
        {
            attackQueued = false;
            ExecuteAttack(comboStep);
        }

        // --- Fire Weapon (Right Click — hold to shoot) ---
        fireTimer -= Time.deltaTime;
        bool isShooting = Mouse.current != null && Mouse.current.rightButton.isPressed;

        if (isShooting && fireTimer <= 0f)
        {
            fireTimer = fireRate;
            FireBullet();
        }

        // Update arm IK
        if (playerIK != null)
        {
            playerIK.isShooting = isShooting;
            Vector3 aimDir = mouseWorldPoint - transform.position;
            aimDir.y = 0f;
            playerIK.aimTarget = transform.position + aimDir.normalized * 3f + Vector3.up * 0.9f;
        }
    }

    // Combo chain delay per step — tuned to match animation lengths at their playback speeds
    private float[] comboChainDelay = { 0.25f, 0.28f, 0.35f };

    void ExecuteAttack(int step)
    {
        comboStep = step + 1;
        comboTimer = comboWindowTime;
        canComboChain = false;
        attackQueued = false;

        // Snap rotation toward mouse instantly so attack direction matches intent
        Vector3 attackDir = mouseWorldPoint - transform.position;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.1f)
        {
            Quaternion attackRotation = Quaternion.LookRotation(attackDir.normalized);
            rb.MoveRotation(attackRotation);
            transform.rotation = attackRotation;
        }

        // Trigger the appropriate slash animation
        if (animator != null)
        {
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("Slash");
        }

        // Attack dash — small lunge forward (uses snapped forward)
        float dashForce = attackDashForce * (1f + step * 0.3f);
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

        // Spawn slash VFX (uses snapped forward)
        Vector3 vfxPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        Quaternion vfxRot = transform.rotation * Quaternion.Euler(0f, slashAngles[step], 0f);
        SlashVFX.Spawn(vfxPos, vfxRot, step);

        // Screen shake — escalates with combo
        float shakeAmount = 0.15f + step * 0.1f;
        CombatFeedback.AddTrauma(shakeAmount);

        // Hit detection (uses snapped forward)
        float damage = step < comboDamage.Length ? comboDamage[step] : 50f;
        float knockback = step < comboKnockback.Length ? comboKnockback[step] : 5f;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, attackRange);
        bool hitSomething = false;
        foreach (var hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                enemy.TakeDamage(damage, knockDir, knockback);
                hitSomething = true;
            }
        }

        // Extra feedback on hit
        if (hitSomething)
        {
            CombatFeedback.AddTrauma(0.15f);
            CombatFeedback.TriggerHitstop(0.04f + step * 0.02f);
        }

        // Allow combo chaining after delay matched to animation timing
        float chainDelay = step < comboChainDelay.Length ? comboChainDelay[step] : 0.3f;
        Invoke(nameof(EnableComboChain), chainDelay);
    }

    void FireBullet()
    {
        Vector3 aimDir = mouseWorldPoint - transform.position;
        aimDir.y = 0f;
        aimDir.Normalize();

        // Random spread for bullet hell feel
        float spread = Random.Range(-spreadAngle, spreadAngle);
        Quaternion spreadRot = Quaternion.AngleAxis(spread, Vector3.up);
        Vector3 bulletDir = spreadRot * aimDir;

        // Spawn from muzzle position (in front of player, at hand height)
        Vector3 spawnPos = transform.position + aimDir * muzzleOffset + Vector3.up * 0.9f;

        GameObject bulletObj = new GameObject("Bullet");
        bulletObj.transform.position = spawnPos;
        bulletObj.tag = "Untagged";
        Bullet bullet = bulletObj.AddComponent<Bullet>();
        bullet.Init(bulletDir, bulletSpeed, bulletDamage);

        // Small screen shake per shot
        CombatFeedback.AddTrauma(0.03f);
    }

    void EnableComboChain()
    {
        canComboChain = true;
    }

    void HandleTurnAnimation(bool isMoving)
    {
        if (isMoving || Time.time - lastTurnTime < turnCooldown || comboStep > 0)
            return;

        Vector3 desiredDir = mouseWorldPoint - transform.position;
        desiredDir.y = 0f;

        if (desiredDir.sqrMagnitude < 0.5f)
            return;

        float signedAngle = Vector3.SignedAngle(transform.forward, desiredDir.normalized, Vector3.up);
        float absAngle = Mathf.Abs(signedAngle);

        if (absAngle >= turnThreshold180)
        {
            animator.SetTrigger("Turn180");
            lastTurnTime = Time.time;
        }
        else if (absAngle >= turnThreshold90)
        {
            if (signedAngle > 0f)
                animator.SetTrigger("TurnRight");
            else
                animator.SetTrigger("TurnLeft");
            lastTurnTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

        // Rotate to face mouse
        Vector3 lookDir = mouseWorldPoint - rb.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 15f * Time.fixedDeltaTime));
        }
    }
}
