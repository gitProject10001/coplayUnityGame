using UnityEngine;

public enum EnemyState { Idle, Patrol, Chase, Attack, Retreat, Stagger }

public class Enemy : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3f;
    public float health = 100f;

    [Header("AI")]
    public float detectionRange = 12f;
    public float loseInterestRange = 16f;
    public float attackRange = 2.2f;
    public float attackDamage = 20f;
    public float attackWindup = 0.5f;
    public float attackCooldown = 1.2f;
    public float patrolRadius = 5f;
    public float patrolSpeed = 1.5f;
    public float retreatHealthThreshold = 25f;
    public float retreatDuration = 1.5f;
    public float staggerDuration = 0.25f;

    [Header("Attack Lunge")]
    public float attackLungeForce = 8f;

    [Header("Extended Stagger")]
    public int hitsForExtendedStagger = 3;
    public float extendedStaggerDuration = 0.8f;

    private Rigidbody rb;
    private MeshRenderer mr;
    private Color originalColor = Color.red;
    private bool isFlashing;
    private float flashTimer;

    private EnemyState currentState = EnemyState.Idle;
    private float stateTimer;
    private Vector3 spawnPosition;
    private Vector3 patrolTarget;
    private PlayerController playerController;
    private bool hasRetreated;
    private bool attackHitDealt;
    private float attackWindupJitter;

    // Attack patterns
    private int attackPattern;
    private int attackPhase; // sub-phase within multi-hit patterns
    private float attackPhaseTimer;

    // Combo stagger tracking
    private int consecutiveHitsTaken;
    private float lastHitTime;
    private float comboHitTimeout = 2f;

    // Telegraph visual
    private Vector3 originalScale;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();
        spawnPosition = transform.position;
        originalScale = transform.localScale;

        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                target = player.transform;
                playerController = player.GetComponent<PlayerController>();
            }
        }
        else
        {
            playerController = target.GetComponent<PlayerController>();
        }

        EnterState(EnemyState.Idle);
    }

    void Update()
    {
        // Handle flash timer with unscaled time so it works during hitstop
        if (isFlashing)
        {
            flashTimer -= Time.unscaledDeltaTime;
            if (flashTimer <= 0f)
            {
                isFlashing = false;
                if (mr != null) mr.material.color = originalColor;
            }
        }

        // Reset consecutive hits if too much time passed
        if (consecutiveHitsTaken > 0 && Time.time - lastHitTime > comboHitTimeout)
            consecutiveHitsTaken = 0;

        if (target == null) return;

        switch (currentState)
        {
            case EnemyState.Idle: UpdateIdle(); break;
            case EnemyState.Patrol: UpdatePatrol(); break;
            case EnemyState.Chase: UpdateChase(); break;
            case EnemyState.Attack: UpdateAttack(); break;
            case EnemyState.Retreat: UpdateRetreat(); break;
            case EnemyState.Stagger: UpdateStagger(); break;
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                MoveToward(patrolTarget, patrolSpeed);
                break;
            case EnemyState.Chase:
                MoveToward(target.position, moveSpeed);
                break;
            case EnemyState.Retreat:
                MoveAwayFrom(target.position, moveSpeed * 0.8f);
                break;
        }
    }

    // --- State Updates ---

    void UpdateIdle()
    {
        if (PlayerInRange(detectionRange))
        {
            EnterState(EnemyState.Chase);
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterState(EnemyState.Patrol);
    }

    void UpdatePatrol()
    {
        if (PlayerInRange(detectionRange))
        {
            EnterState(EnemyState.Chase);
            return;
        }

        float distToTarget = Vector3.Distance(transform.position, patrolTarget);
        stateTimer -= Time.deltaTime;

        if (distToTarget < 0.5f || stateTimer <= 0f)
            EnterState(EnemyState.Idle);
    }

    void UpdateChase()
    {
        float dist = DistanceToPlayer();

        if (dist <= attackRange)
        {
            EnterState(EnemyState.Attack);
            return;
        }

        if (dist > loseInterestRange)
        {
            EnterState(EnemyState.Idle);
            return;
        }

        if (!hasRetreated && health <= retreatHealthThreshold)
        {
            EnterState(EnemyState.Retreat);
            return;
        }
    }

    void UpdateAttack()
    {
        stateTimer -= Time.deltaTime;

        // Face the player during windup
        Vector3 dir = (target.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }

        float totalWindup = attackWindup + attackWindupJitter;
        float timeInAttack = (totalWindup + attackCooldown) - stateTimer;

        // --- TELEGRAPH PHASE (windup) ---
        if (timeInAttack < totalWindup)
        {
            float windupProgress = timeInAttack / totalWindup;

            // Scale up "coiling" effect
            float scaleMultiplier = Mathf.Lerp(1f, 1.15f, windupProgress);
            transform.localScale = originalScale * scaleMultiplier;

            // Orange flash telegraph
            if (!isFlashing && mr != null)
                mr.material.color = Color.Lerp(originalColor, new Color(1f, 0.4f, 0f), windupProgress);

            // Brief pause at full windup (the "tell" moment)
            if (windupProgress > 0.85f && windupProgress < 0.95f)
                return; // Hold for a beat
        }

        // --- STRIKE PHASE ---
        if (!attackHitDealt && timeInAttack >= totalWindup)
        {
            attackHitDealt = true;

            // Reset scale and color
            transform.localScale = originalScale;
            if (!isFlashing && mr != null) mr.material.color = originalColor;

            // Lunge toward player
            Vector3 lungeDir = (target.position - transform.position).normalized;
            lungeDir.y = 0f;
            if (rb != null)
                rb.AddForce(lungeDir * attackLungeForce, ForceMode.Impulse);

            // Hit detection
            DealAttackDamage();

            // Reset consecutive hits on successful attack
            consecutiveHitsTaken = 0;

            // Handle multi-hit patterns
            if (attackPattern == 1 && attackPhase == 0)
            {
                // Two-hit combo: queue second hit
                attackPhase = 1;
                attackPhaseTimer = 0.4f;
            }
        }

        // --- SECOND HIT (Pattern 1: two-hit combo) ---
        if (attackPattern == 1 && attackPhase == 1)
        {
            attackPhaseTimer -= Time.deltaTime;
            if (attackPhaseTimer <= 0f)
            {
                attackPhase = 2; // Done
                // Second swipe
                if (rb != null)
                {
                    Vector3 lungeDir = (target.position - transform.position).normalized;
                    lungeDir.y = 0f;
                    rb.AddForce(lungeDir * attackLungeForce * 0.7f, ForceMode.Impulse);
                }
                DealAttackDamage();
            }
        }

        // Attack cycle complete
        if (stateTimer <= 0f)
            EnterState(EnemyState.Chase);
    }

    void DealAttackDamage()
    {
        Vector3 attackPos = transform.position + transform.forward * 1.0f;
        float hitRadius = attackPattern == 2 ? 1.5f : 1.0f; // Charge attack has wider range
        Collider[] hits = Physics.OverlapSphere(attackPos, hitRadius);
        foreach (var hit in hits)
        {
            PlayerController pc = hit.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(attackDamage);
                break;
            }
        }
    }

    void UpdateRetreat()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
            EnterState(EnemyState.Chase);
    }

    void UpdateStagger()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            if (PlayerInRange(detectionRange))
                EnterState(EnemyState.Chase);
            else
                EnterState(EnemyState.Idle);
        }
    }

    // --- State Entry ---

    void EnterState(EnemyState newState)
    {
        currentState = newState;

        // Reset scale when leaving attack
        transform.localScale = originalScale;

        switch (newState)
        {
            case EnemyState.Idle:
                stateTimer = Random.Range(1f, 2f);
                break;

            case EnemyState.Patrol:
                Vector2 rnd = Random.insideUnitCircle * patrolRadius;
                patrolTarget = spawnPosition + new Vector3(rnd.x, 0f, rnd.y);
                patrolTarget.y = transform.position.y;
                stateTimer = 3f;
                break;

            case EnemyState.Chase:
                break;

            case EnemyState.Attack:
                attackHitDealt = false;
                attackPhase = 0;
                attackWindupJitter = Random.Range(0f, 0.2f);

                // Randomly select attack pattern
                float roll = Random.value;
                if (roll < 0.5f)
                    attackPattern = 0; // Single swipe
                else if (roll < 0.8f)
                    attackPattern = 1; // Two-hit combo
                else
                    attackPattern = 2; // Charge/lunge (longer windup, wider range)

                float windup = attackPattern == 2 ? attackWindup * 1.5f : attackWindup;
                stateTimer = windup + attackWindupJitter + attackCooldown;
                break;

            case EnemyState.Retreat:
                hasRetreated = true;
                stateTimer = retreatDuration;
                break;

            case EnemyState.Stagger:
                stateTimer = staggerDuration;
                if (!isFlashing && mr != null) mr.material.color = originalColor;
                break;
        }
    }

    // --- Movement Helpers ---

    void MoveToward(Vector3 targetPos, float speed)
    {
        Vector3 direction = (targetPos - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        direction.Normalize();
        rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.fixedDeltaTime));
    }

    void MoveAwayFrom(Vector3 targetPos, float speed)
    {
        Vector3 direction = (transform.position - targetPos);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        direction.Normalize();
        rb.MovePosition(transform.position + direction * speed * Time.fixedDeltaTime);

        Quaternion lookRotation = Quaternion.LookRotation(-direction);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.fixedDeltaTime));
    }

    // --- Utility ---

    float DistanceToPlayer()
    {
        Vector3 diff = target.position - transform.position;
        diff.y = 0f;
        return diff.magnitude;
    }

    bool PlayerInRange(float range)
    {
        return DistanceToPlayer() <= range;
    }

    // --- Damage ---

    public void TakeDamage(float amount, Vector3 knockbackDir, float knockbackForce)
    {
        health -= amount;

        // Flash white
        if (mr != null)
        {
            mr.material.color = Color.white;
            isFlashing = true;
            flashTimer = 0.12f;
        }

        // Knockback
        if (rb != null)
        {
            knockbackDir.y = 0f;
            rb.AddForce(knockbackDir.normalized * knockbackForce, ForceMode.Impulse);
        }

        // Spawn hit sparks
        SpawnHitSparks(transform.position + Vector3.up * 0.5f);

        if (health <= 0)
        {
            SpawnDeathBurst(transform.position + Vector3.up * 0.5f);
            Destroy(gameObject);
            return;
        }

        // Track consecutive hits for extended stagger
        consecutiveHitsTaken++;
        lastHitTime = Time.time;

        if (consecutiveHitsTaken >= hitsForExtendedStagger)
        {
            consecutiveHitsTaken = 0;
            staggerDuration = extendedStaggerDuration;
            EnterState(EnemyState.Stagger);
            staggerDuration = 0.25f; // Reset for next time
        }
        else
        {
            EnterState(EnemyState.Stagger);
        }
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

    // --- VFX ---

    void SpawnHitSparks(Vector3 pos)
    {
        int sparkCount = Random.Range(4, 8);
        for (int i = 0; i < sparkCount; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(spark.GetComponent<Collider>());
            spark.transform.position = pos;
            spark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);

            MeshRenderer sparkMr = spark.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.color = Color.Lerp(Color.yellow, Color.white, Random.value * 0.5f);
            sparkMr.material = mat;

            Rigidbody sparkRb = spark.AddComponent<Rigidbody>();
            sparkRb.useGravity = true;
            sparkRb.mass = 0.01f;
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 1.5f;
            sparkRb.AddForce(dir * Random.Range(3f, 7f), ForceMode.Impulse);

            Destroy(spark, Random.Range(0.2f, 0.4f));
        }
    }

    void SpawnDeathBurst(Vector3 pos)
    {
        int count = Random.Range(10, 16);
        for (int i = 0; i < count; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.position = pos + Random.insideUnitSphere * 0.3f;
            piece.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);
            piece.transform.rotation = Random.rotation;

            MeshRenderer pMr = piece.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            mat.color = Color.Lerp(Color.red, new Color(1f, 0.3f, 0f), Random.value);
            pMr.material = mat;

            Rigidbody pRb = piece.AddComponent<Rigidbody>();
            pRb.mass = 0.02f;
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            pRb.AddForce(dir * Random.Range(4f, 10f), ForceMode.Impulse);
            pRb.AddTorque(Random.onUnitSphere * 20f);

            Destroy(piece, Random.Range(0.5f, 1f));
        }
    }
}
