using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState { Idle, Moving, Attacking, Dodging, Charging, Staggered }

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

    [Header("Attack Commitment")]
    public float[] attackActiveDuration = { 0.15f, 0.18f, 0.22f };
    public float[] attackRecoveryDuration = { 0.20f, 0.22f, 0.35f };
    public float whiffRecoveryMultiplier = 1.3f;

    [Header("Dodge Roll")]
    public float dodgeSpeed = 12f;
    public float dodgeDuration = 0.3f;
    public float dodgeCooldown = 0.15f;
    public float dodgeIFrameDuration = 0.25f;
    public float dodgeStaminaCost = 25f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 30f;
    public float staminaRegenDelay = 0.8f;
    public float attackStaminaCost = 20f;

    [Header("Charged Attack")]
    public float chargeHoldThreshold = 0.2f;
    public float chargeTimeRequired = 0.8f;
    public float chargedDamage = 150f;
    public float chargedKnockback = 20f;
    public float chargedRange = 2.5f;
    public float chargeStaminaCost = 40f;
    public float bloodburstHealAmount = 15f;
    public float chargedRecoveryDuration = 0.5f;

    [Header("Ranged Charges")]
    public int maxRangedCharges = 4;
    public int currentRangedCharges;
    public int chargesPerMeleeHit = 1;
    public float bulletSpeed = 25f;
    public float bulletDamage = 30f;
    public float muzzleOffset = 1.2f;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float invincibilityDuration = 0.5f;

    [Header("Turn Animation Settings")]
    public float turnThreshold90 = 60f;
    public float turnThreshold180 = 140f;
    public float turnCooldown = 0.5f;

    // State
    public PlayerState currentState { get; private set; } = PlayerState.Idle;

    private Rigidbody rb;
    private Vector3 movement;
    private Animator animator;
    private PlayerIK playerIK;
    private float lastTurnTime;
    private float invincibilityTimer;
    private bool isInvincible;
    private MeshRenderer playerMr;
    private Color playerOriginalColor;
    private bool isPlayerFlashing;
    private float playerFlashTimer;

    // Combo state
    private int comboStep = 0;
    private float comboTimer;
    private bool attackQueued;
    private float attackCooldownTimer;
    private float stateTimer;
    private bool attackHitSomething;
    private bool inRecovery;

    // Dodge state
    private Vector3 dodgeDirection;
    private float dodgeCooldownTimer;
    private bool isDodgeIFrame;
    private float dodgeGhostTimer;

    // Stamina
    private float staminaRegenDelayTimer;

    // Charge state
    private float chargeTimer;
    private bool leftButtonHeld;
    private GameObject chargeVFXObj;

    // Slash arc rotation offsets
    private float[] slashAngles = { -30f, 30f, 0f };
    private float[] comboChainDelay = { 0.25f, 0.28f, 0.35f };

    private Vector3 mouseWorldPoint;

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

        if (CombatFeedback.Instance == null)
        {
            GameObject fb = new GameObject("CombatFeedback");
            fb.AddComponent<CombatFeedback>();
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentRangedCharges = maxRangedCharges;
        playerMr = GetComponentInChildren<MeshRenderer>();
        if (playerMr != null) playerOriginalColor = playerMr.material.color;
    }

    void Update()
    {
        UpdateTimers();
        ReadMouseInput();

        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Moving:
                UpdateLocomotion();
                break;
            case PlayerState.Attacking:
                UpdateAttacking();
                break;
            case PlayerState.Dodging:
                UpdateDodging();
                break;
            case PlayerState.Charging:
                UpdateCharging();
                break;
            case PlayerState.Staggered:
                UpdateStaggered();
                break;
        }
    }

    void UpdateTimers()
    {
        // Invincibility
        if (isInvincible)
        {
            invincibilityTimer -= Time.unscaledDeltaTime;
            if (invincibilityTimer <= 0f) isInvincible = false;
        }

        // Damage flash
        if (isPlayerFlashing)
        {
            playerFlashTimer -= Time.unscaledDeltaTime;
            if (playerFlashTimer <= 0f)
            {
                isPlayerFlashing = false;
                if (playerMr != null) playerMr.material.color = playerOriginalColor;
            }
        }

        // Dodge cooldown
        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Time.deltaTime;

        // Attack cooldown
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        // Stamina regen
        if (staminaRegenDelayTimer > 0f)
            staminaRegenDelayTimer -= Time.deltaTime;
        else
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
    }

    void ReadMouseInput()
    {
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
    }

    // --- IDLE / MOVING ---

    void UpdateLocomotion()
    {
        // Movement input
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

        currentState = isMoving ? PlayerState.Moving : PlayerState.Idle;

        // Animator blend tree
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

        // --- Dodge input ---
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryDodge();
            return;
        }

        // --- Attack / Charge input ---
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                leftButtonHeld = true;
                chargeTimer = 0f;

                // Immediately start a normal attack
                if (attackCooldownTimer <= 0f && TryConsumeStamina(attackStaminaCost))
                {
                    ExecuteAttack(0);
                    return;
                }
            }

            // Check for charge hold (only if we haven't started a combo)
            if (leftButtonHeld && Mouse.current.leftButton.isPressed)
            {
                chargeTimer += Time.deltaTime;
                if (chargeTimer > chargeHoldThreshold && comboStep == 0 && currentState != PlayerState.Attacking)
                {
                    if (currentStamina >= chargeStaminaCost)
                    {
                        EnterCharging();
                        return;
                    }
                }
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
                leftButtonHeld = false;

            // --- Ranged input (single shot per click) ---
            if (Mouse.current.rightButton.wasPressedThisFrame && currentRangedCharges > 0)
            {
                currentRangedCharges--;
                FireBullet();
            }
        }

        // Update arm IK for shooting
        if (playerIK != null)
        {
            bool isShooting = Mouse.current != null && Mouse.current.rightButton.isPressed;
            playerIK.isShooting = isShooting;
            Vector3 aimDir = mouseWorldPoint - transform.position;
            aimDir.y = 0f;
            playerIK.aimTarget = transform.position + aimDir.normalized * 3f + Vector3.up * 0.9f;
        }
    }

    // --- ATTACKING ---

    void UpdateAttacking()
    {
        stateTimer -= Time.deltaTime;

        // Combo timer
        if (comboStep > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboStep = 0;
                attackQueued = false;
            }
        }

        // Queue attacks during active phase
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && comboStep > 0 && comboStep < 3)
            attackQueued = true;

        // Allow dodge-cancel during recovery phase
        if (inRecovery && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (TryDodge())
                return;
        }

        if (stateTimer <= 0f)
        {
            if (!inRecovery)
            {
                // Active phase ended, enter recovery
                inRecovery = true;
                int step = Mathf.Clamp(comboStep - 1, 0, attackRecoveryDuration.Length - 1);
                float recovery = attackRecoveryDuration[step];
                if (!attackHitSomething) recovery *= whiffRecoveryMultiplier;
                stateTimer = recovery;
            }
            else
            {
                // Recovery ended — check for queued combo
                if (attackQueued && comboStep > 0 && comboStep < 3 && TryConsumeStamina(attackStaminaCost))
                {
                    attackQueued = false;
                    ExecuteAttack(comboStep);
                }
                else
                {
                    // Return to idle
                    comboStep = 0;
                    attackQueued = false;
                    inRecovery = false;
                    attackCooldownTimer = 0.15f;
                    currentState = PlayerState.Idle;
                }
            }
        }

        // Read movement input but don't apply (keep the vector for potential dodge direction)
        ReadMovementInput();
    }

    // --- DODGING ---

    void UpdateDodging()
    {
        stateTimer -= Time.deltaTime;

        // I-frames end slightly before dodge movement ends
        if (stateTimer <= dodgeDuration - dodgeIFrameDuration)
            isDodgeIFrame = false;

        // Spawn ghost trail
        dodgeGhostTimer -= Time.deltaTime;
        if (dodgeGhostTimer <= 0f)
        {
            SpawnDodgeGhost();
            dodgeGhostTimer = 0.08f;
        }

        if (stateTimer <= 0f)
        {
            currentState = PlayerState.Idle;
            dodgeCooldownTimer = dodgeCooldown;
        }
    }

    // --- CHARGING ---

    void UpdateCharging()
    {
        chargeTimer += Time.deltaTime;
        float chargeProgress = Mathf.Clamp01(chargeTimer / chargeTimeRequired);

        // Slow movement while charging
        ReadMovementInput();

        // Animator
        if (animator != null)
            animator.SetFloat("ChargeLevel", chargeProgress);

        // VFX: growing glow at player position
        UpdateChargeVFX(chargeProgress);

        // Screen tension at full charge
        if (chargeProgress >= 1f)
            CombatFeedback.AddTrauma(0.02f);

        // Release charge
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            DestroyChargeVFX();
            leftButtonHeld = false;

            if (chargeProgress >= 1f && TryConsumeStamina(chargeStaminaCost))
            {
                ExecuteChargedAttack();
            }
            else
            {
                // Not fully charged — cancel back to idle
                currentState = PlayerState.Idle;
                if (animator != null)
                    animator.SetTrigger("ChargeCancelled");
            }
            return;
        }

        // Cancel on dodge
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            DestroyChargeVFX();
            leftButtonHeld = false;
            TryDodge();
        }
    }

    // --- STAGGERED ---

    void UpdateStaggered()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            currentState = PlayerState.Idle;
    }

    // --- ACTIONS ---

    void ExecuteAttack(int step)
    {
        comboStep = step + 1;
        comboTimer = comboWindowTime;
        attackQueued = false;
        inRecovery = false;
        attackHitSomething = false;

        currentState = PlayerState.Attacking;
        stateTimer = step < attackActiveDuration.Length ? attackActiveDuration[step] : 0.15f;

        // Snap rotation toward mouse
        Vector3 attackDir = mouseWorldPoint - transform.position;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.1f)
        {
            Quaternion attackRotation = Quaternion.LookRotation(attackDir.normalized);
            rb.MoveRotation(attackRotation);
            transform.rotation = attackRotation;
        }

        // Trigger animation
        if (animator != null)
        {
            animator.SetInteger("ComboStep", comboStep);
            animator.SetTrigger("Slash");
        }

        // Attack dash
        float dashForce = attackDashForce * (1f + step * 0.3f);
        rb.AddForce(transform.forward * dashForce, ForceMode.Impulse);

        // Slash VFX
        Vector3 vfxPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        Quaternion vfxRot = transform.rotation * Quaternion.Euler(0f, slashAngles[step], 0f);
        SlashVFX.Spawn(vfxPos, vfxRot, step);

        // Screen shake
        float shakeAmount = 0.15f + step * 0.1f;
        CombatFeedback.AddTrauma(shakeAmount);

        // Hit detection
        float damage = step < comboDamage.Length ? comboDamage[step] : 50f;
        float knockback = step < comboKnockback.Length ? comboKnockback[step] : 5f;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                enemy.TakeDamage(damage, knockDir, knockback);
                attackHitSomething = true;
            }
        }

        if (attackHitSomething)
        {
            CombatFeedback.AddTrauma(0.15f);
            CombatFeedback.TriggerHitstop(0.04f + step * 0.02f);

            // Refill ranged charges on melee hit (Death's Door style)
            currentRangedCharges = Mathf.Min(maxRangedCharges, currentRangedCharges + chargesPerMeleeHit);
        }
    }

    void ExecuteChargedAttack()
    {
        currentState = PlayerState.Attacking;
        comboStep = 0;
        attackQueued = false;
        inRecovery = false;
        attackHitSomething = false;
        stateTimer = 0.25f; // Active frames for charged attack

        // Snap rotation
        Vector3 attackDir = mouseWorldPoint - transform.position;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.1f)
        {
            Quaternion attackRotation = Quaternion.LookRotation(attackDir.normalized);
            rb.MoveRotation(attackRotation);
            transform.rotation = attackRotation;
        }

        // Animator
        if (animator != null)
            animator.SetTrigger("ChargeRelease");

        // Big dash forward
        rb.AddForce(transform.forward * attackDashForce * 2.5f, ForceMode.Impulse);

        // Charged slash VFX (step=3 for the golden variant)
        Vector3 vfxPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        SlashVFX.Spawn(vfxPos, transform.rotation, 3);

        // Heavy screen shake
        CombatFeedback.AddTrauma(0.5f);

        // Hit detection with larger range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position + transform.forward * 1.5f, chargedRange);
        foreach (var hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy != null)
            {
                Vector3 knockDir = (enemy.transform.position - transform.position).normalized;
                enemy.TakeDamage(chargedDamage, knockDir, chargedKnockback);
                attackHitSomething = true;
            }
        }

        if (attackHitSomething)
        {
            CombatFeedback.TriggerHitstop(0.12f);

            // Bloodburst: heal on charged hit (Eldest Souls)
            currentHealth = Mathf.Min(maxHealth, currentHealth + bloodburstHealAmount);

            // Refill all ranged charges
            currentRangedCharges = maxRangedCharges;
        }

        // Override recovery to use charged recovery duration (very punishing on whiff)
        // This is handled in UpdateAttacking when active frames end
    }

    void EnterCharging()
    {
        currentState = PlayerState.Charging;
        chargeTimer = 0f;
        comboStep = 0;
        attackQueued = false;
        inRecovery = false;

        if (animator != null)
            animator.SetTrigger("ChargeStart");
    }

    bool TryDodge()
    {
        if (dodgeCooldownTimer > 0f) return false;
        if (!TryConsumeStamina(dodgeStaminaCost)) return false;

        // Direction = movement input if moving, else forward
        ReadMovementInput();
        dodgeDirection = movement.sqrMagnitude > 0.01f ? movement.normalized : transform.forward;

        currentState = PlayerState.Dodging;
        stateTimer = dodgeDuration;
        isDodgeIFrame = true;
        dodgeGhostTimer = 0f;

        // Cancel any combo
        comboStep = 0;
        attackQueued = false;
        inRecovery = false;
        leftButtonHeld = false;
        DestroyChargeVFX();

        // Animator
        if (animator != null)
            animator.SetTrigger("Dodge");

        return true;
    }

    void FireBullet()
    {
        Vector3 aimDir = mouseWorldPoint - transform.position;
        aimDir.y = 0f;
        aimDir.Normalize();

        Vector3 spawnPos = transform.position + aimDir * muzzleOffset + Vector3.up * 0.9f;

        GameObject bulletObj = new GameObject("Bullet");
        bulletObj.transform.position = spawnPos;
        Bullet bullet = bulletObj.AddComponent<Bullet>();
        bullet.Init(aimDir, bulletSpeed, bulletDamage);

        CombatFeedback.AddTrauma(0.05f);
    }

    // --- DAMAGE ---

    public void TakeDamage(float damage)
    {
        if (isInvincible) return;
        if (currentState == PlayerState.Dodging && isDodgeIFrame) return;

        currentHealth -= damage;

        // Flash white
        if (playerMr != null)
        {
            playerMr.material.color = Color.white;
            isPlayerFlashing = true;
            playerFlashTimer = 0.15f;
        }

        // I-frames
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;

        // Screen shake
        CombatFeedback.AddTrauma(0.25f);

        // Enter stagger briefly
        currentState = PlayerState.Staggered;
        stateTimer = 0.2f;
        comboStep = 0;
        attackQueued = false;
        inRecovery = false;
        leftButtonHeld = false;
        DestroyChargeVFX();

        if (animator != null)
            animator.SetTrigger("Stagger");

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Debug.Log("Player died!");
            enabled = false;
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }

    // --- HELPERS ---

    bool TryConsumeStamina(float amount)
    {
        if (currentStamina < amount) return false;
        currentStamina -= amount;
        staminaRegenDelayTimer = staminaRegenDelay;
        return true;
    }

    void ReadMovementInput()
    {
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

    // --- VFX ---

    void SpawnDodgeGhost()
    {
        if (playerMr == null) return;

        GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(ghost.GetComponent<Collider>());
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        MeshRenderer ghostMr = ghost.GetComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material ghostMat = new Material(shader);
        ghostMat.SetFloat("_Surface", 1);
        ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMat.SetInt("_ZWrite", 0);
        ghostMat.renderQueue = 3000;
        ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        ghostMat.color = new Color(0.5f, 0.8f, 1f, 0.4f);
        ghostMr.material = ghostMat;

        // Fade and destroy
        DodgeGhostFade fader = ghost.AddComponent<DodgeGhostFade>();
        fader.Init(0.2f);
    }

    void UpdateChargeVFX(float progress)
    {
        if (chargeVFXObj == null)
        {
            chargeVFXObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(chargeVFXObj.GetComponent<Collider>());
            MeshRenderer vfxMr = chargeVFXObj.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            vfxMr.material = mat;
        }

        // Grow and pulse
        float scale = Mathf.Lerp(0.1f, 0.6f, progress);
        if (progress >= 1f)
            scale += Mathf.Sin(Time.time * 15f) * 0.05f; // Pulse at full charge

        chargeVFXObj.transform.position = transform.position + transform.forward * 0.8f + Vector3.up * 0.8f;
        chargeVFXObj.transform.localScale = Vector3.one * scale;

        // Color: white → gold as charge fills
        Color c = Color.Lerp(new Color(1f, 1f, 1f, 0.3f), new Color(1f, 0.85f, 0.2f, 0.8f), progress);
        chargeVFXObj.GetComponent<MeshRenderer>().material.color = c;
    }

    void DestroyChargeVFX()
    {
        if (chargeVFXObj != null)
        {
            Destroy(chargeVFXObj);
            chargeVFXObj = null;
        }
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Moving:
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
                RotateTowardMouse();
                break;
            case PlayerState.Charging:
                rb.MovePosition(rb.position + movement * moveSpeed * 0.3f * Time.fixedDeltaTime);
                RotateTowardMouse();
                break;
            case PlayerState.Dodging:
                rb.MovePosition(rb.position + dodgeDirection * dodgeSpeed * Time.fixedDeltaTime);
                break;
            // Attacking, Staggered: no player-driven movement
        }
    }

    void RotateTowardMouse()
    {
        Vector3 lookDir = mouseWorldPoint - rb.position;
        lookDir.y = 0f;

        if (lookDir.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 15f * Time.fixedDeltaTime));
        }
    }
}

/// <summary>
/// Simple fade-out component for dodge ghost meshes.
/// </summary>
public class DodgeGhostFade : MonoBehaviour
{
    private float lifetime;
    private float maxLifetime;
    private Material mat;

    public void Init(float duration)
    {
        lifetime = duration;
        maxLifetime = duration;
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Color c = mat.color;
        c.a = (lifetime / maxLifetime) * 0.4f;
        mat.color = c;
    }
}
