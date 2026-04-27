using System.Collections;
using UnityEngine;

public class AreaEnemy : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 1.8f;
    public float health = 180f;

    [Header("AI")]
    public float detectionRange = 14f;
    public float loseInterestRange = 20f;
    public float attackTriggerRange = 5.5f;
    public float slamRadius = 3.5f;
    public float shockwaveRadius = 6f;
    public float slamDamage = 40f;
    public float shockwaveDamage = 30f;
    public float attackCooldown = 2.5f;
    public float windupDuration = 1.1f;
    public float staggerDuration = 0.4f;
    public float patrolRadius = 4f;

    private Rigidbody rb;
    private MeshRenderer[] mrs;
    private Color[] originalColors;
    private Vector3 originalScale;

    static readonly Color BodyColor = new Color(0.28f, 0.13f, 0.48f);
    static readonly Color HeadColor = new Color(0.52f, 0.28f, 0.72f);
    static readonly Color ArmColor  = new Color(0.35f, 0.17f, 0.55f);

    private enum State { Idle, Patrol, Chase, Attack, Stagger }
    private enum AttackType { Slam, Shockwave }
    private State state;
    private AttackType currentAttack;
    private float stateTimer;
    private Vector3 spawnPos;
    private Vector3 patrolTarget;
    private PlayerController playerController;
    private bool attackFired;
    private float windupJitter;

    private GameObject aoeDisc;

    void Awake()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
        mrs = GetComponentsInChildren<MeshRenderer>();
        originalColors = new Color[mrs.Length];
    }

    void Start()
    {
        spawnPos = transform.position;

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
            playerController = target?.GetComponent<PlayerController>();
        }

        ApplyColors();
        CreateAoeDisc();
        EnterState(State.Idle);
    }

    void ApplyColors()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        for (int i = 0; i < mrs.Length; i++)
        {
            Color c = BodyColor;
            string n = mrs[i].gameObject.name;
            if (n.Contains("Head")) c = HeadColor;
            else if (n.Contains("Arm")) c = ArmColor;

            originalColors[i] = c;
            Material mat = new Material(shader);
            mat.color = c;
            mrs[i].material = mat;
        }
    }

    void CreateAoeDisc()
    {
        aoeDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(aoeDisc.GetComponent<Collider>());
        aoeDisc.name = "AreaEnemy_AoeDisc";
        aoeDisc.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
        aoeDisc.transform.localScale = new Vector3(0.1f, 0.03f, 0.1f);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = new Color(0.9f, 0.3f, 1f, 0.7f);
        aoeDisc.GetComponent<MeshRenderer>().material = mat;
        aoeDisc.SetActive(false);
    }

    void OnDestroy()
    {
        if (aoeDisc != null) Destroy(aoeDisc);
    }

    void Update()
    {
        if (target == null) return;
        switch (state)
        {
            case State.Idle:    UpdateIdle();    break;
            case State.Patrol:  UpdatePatrol();  break;
            case State.Chase:   UpdateChase();   break;
            case State.Attack:  UpdateAttack();  break;
            case State.Stagger: UpdateStagger(); break;
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;
        if (state == State.Chase)  MoveToward(target.position, moveSpeed);
        if (state == State.Patrol) MoveToward(patrolTarget, moveSpeed * 0.55f);
    }

    void UpdateIdle()
    {
        stateTimer -= Time.deltaTime;
        if (PlayerInRange(detectionRange)) { EnterState(State.Chase); return; }
        if (stateTimer <= 0f) EnterState(State.Patrol);
    }

    void UpdatePatrol()
    {
        if (PlayerInRange(detectionRange)) { EnterState(State.Chase); return; }
        stateTimer -= Time.deltaTime;
        if (Vector3.Distance(transform.position, patrolTarget) < 0.6f || stateTimer <= 0f)
            EnterState(State.Idle);
    }

    void UpdateChase()
    {
        float d = Dist();
        if (d > loseInterestRange) { EnterState(State.Idle); return; }
        if (d <= attackTriggerRange) { EnterState(State.Attack); return; }
    }

    void UpdateAttack()
    {
        stateTimer -= Time.deltaTime;

        Vector3 toPlayer = target.position - transform.position; toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toPlayer.normalized), 6f * Time.deltaTime);

        float totalWindup = windupDuration + windupJitter;
        float t = Mathf.Clamp01((totalWindup + attackCooldown - stateTimer) / totalWindup);

        if (t < 1f)
        {
            transform.localScale = originalScale * Mathf.Lerp(1f, 1.28f, t);

            if (aoeDisc != null)
            {
                float targetR = (currentAttack == AttackType.Slam ? slamRadius : shockwaveRadius) * 2f;
                float discS = Mathf.Lerp(0.1f, targetR, t);
                aoeDisc.transform.localScale = new Vector3(discS, 0.03f, discS);
                aoeDisc.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                aoeDisc.SetActive(true);
            }
        }

        if (!attackFired && t >= 1f)
        {
            attackFired = true;
            transform.localScale = originalScale;
            if (aoeDisc != null) aoeDisc.SetActive(false);

            if (currentAttack == AttackType.Slam)
                DoGroundSlam();
            else
                DoShockwave();

            CombatFeedback.AddTrauma(0.45f);
            CombatFeedback.TriggerHitstop(0.12f);
        }

        if (stateTimer <= 0f)
        {
            transform.localScale = originalScale;
            if (aoeDisc != null) aoeDisc.SetActive(false);
            EnterState(State.Chase);
        }
    }

    void DoGroundSlam()
    {
        SpawnImpactVFX(transform.position, slamRadius, new Color(0.6f, 0.25f, 1f));
        StartCoroutine(SlamSquish());
        DamageInRadius(transform.position, slamRadius, slamDamage);
    }

    void DoShockwave()
    {
        SpawnImpactVFX(transform.position, shockwaveRadius, new Color(0.85f, 0.4f, 1f));
        DamageInRadius(transform.position, shockwaveRadius, shockwaveDamage);
    }

    void DamageInRadius(Vector3 center, float radius, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var h in hits)
        {
            PlayerController pc = h.GetComponent<PlayerController>();
            if (pc != null) { pc.TakeDamage(damage); break; }
        }
    }

    IEnumerator SlamSquish()
    {
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            transform.localScale = new Vector3(
                originalScale.x * Mathf.Lerp(1.45f, 1f, t),
                originalScale.y * Mathf.Lerp(0.5f, 1f, t),
                originalScale.z * Mathf.Lerp(1.45f, 1f, t));
            yield return null;
        }
        transform.localScale = originalScale;
    }

    void SpawnImpactVFX(Vector3 center, float radius, Color color)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(radius * 3.5f), 10, 22);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * Mathf.PI * 2f;
            float r = Random.Range(0.4f, radius);
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * r, 0.1f, Mathf.Sin(angle) * r);

            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(chunk.GetComponent<Collider>());
            chunk.transform.position = pos;
            chunk.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
            chunk.transform.rotation = Random.rotation;

            Material mat = new Material(shader);
            mat.color = Color.Lerp(color, Color.white, Random.value * 0.3f);
            chunk.GetComponent<MeshRenderer>().material = mat;

            Rigidbody crb = chunk.AddComponent<Rigidbody>();
            crb.mass = 0.03f;
            Vector3 vel = (pos - center).normalized;
            vel.y = Random.Range(1f, 3.5f);
            crb.AddForce(vel * Random.Range(4f, 10f), ForceMode.Impulse);
            crb.AddTorque(Random.onUnitSphere * 22f);
            Destroy(chunk, Random.Range(0.35f, 0.7f));
        }
    }

    void UpdateStagger()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f) EnterState(PlayerInRange(detectionRange) ? State.Chase : State.Idle);
    }

    void EnterState(State s)
    {
        state = s;
        transform.localScale = originalScale;
        if (aoeDisc != null) aoeDisc.SetActive(false);

        switch (s)
        {
            case State.Idle:
                stateTimer = Random.Range(1f, 2.5f);
                break;
            case State.Patrol:
                Vector2 rr = Random.insideUnitCircle * patrolRadius;
                patrolTarget = spawnPos + new Vector3(rr.x, 0f, rr.y);
                stateTimer = 4f;
                break;
            case State.Attack:
                attackFired = false;
                windupJitter = Random.Range(0f, 0.25f);
                currentAttack = Dist() < slamRadius * 1.4f ? AttackType.Slam : AttackType.Shockwave;
                stateTimer = windupDuration + windupJitter + attackCooldown;
                break;
            case State.Stagger:
                stateTimer = staggerDuration;
                break;
        }
    }

    void MoveToward(Vector3 pos, float speed)
    {
        Vector3 dir = pos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();
        rb.MovePosition(transform.position + dir * speed * Time.fixedDeltaTime);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 4f * Time.fixedDeltaTime));
    }

    float Dist()
    {
        Vector3 d = target.position - transform.position; d.y = 0f;
        return d.magnitude;
    }

    bool PlayerInRange(float range) => Dist() <= range;

    public void TakeDamage(float amount, Vector3 knockDir, float knockForce)
    {
        health -= amount;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        foreach (var mr in mrs)
        {
            if (mr == null) continue;
            Material mat = new Material(shader);
            mat.color = Color.white;
            mr.material = mat;
        }
        CancelInvoke(nameof(ResetColors));
        Invoke(nameof(ResetColors), 0.12f);

        if (rb != null && knockDir.sqrMagnitude > 0.01f)
        {
            knockDir.y = 0f;
            rb.AddForce(knockDir.normalized * knockForce, ForceMode.Impulse);
        }

        SpawnHitSparks(transform.position + Vector3.up * 0.9f);

        if (health <= 0f)
        {
            SpawnDeathBurst(transform.position + Vector3.up * 0.9f);
            Destroy(gameObject);
            return;
        }

        EnterState(State.Stagger);
    }

    public void TakeDamage(float amount) => TakeDamage(amount, Vector3.zero, 0f);

    void ResetColors()
    {
        if (mrs == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        for (int i = 0; i < mrs.Length; i++)
        {
            if (mrs[i] == null) continue;
            Material mat = new Material(shader);
            mat.color = i < originalColors.Length ? originalColors[i] : BodyColor;
            mrs[i].material = mat;
        }
    }

    void SpawnHitSparks(Vector3 pos)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        for (int i = 0; i < Random.Range(4, 9); i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(spark.GetComponent<Collider>());
            spark.transform.position = pos;
            spark.transform.localScale = Vector3.one * Random.Range(0.05f, 0.13f);
            Material mat = new Material(shader);
            mat.color = new Color(0.8f, 0.5f, 1f);
            spark.GetComponent<MeshRenderer>().material = mat;
            Rigidbody srb = spark.AddComponent<Rigidbody>();
            srb.mass = 0.01f;
            Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 1.5f;
            srb.AddForce(dir * Random.Range(3f, 7f), ForceMode.Impulse);
            Destroy(spark, Random.Range(0.2f, 0.4f));
        }
    }

    void SpawnDeathBurst(Vector3 pos)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        for (int i = 0; i < Random.Range(14, 20); i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(piece.GetComponent<Collider>());
            piece.transform.position = pos + Random.insideUnitSphere * 0.45f;
            piece.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
            piece.transform.rotation = Random.rotation;
            Material mat = new Material(shader);
            mat.color = Color.Lerp(BodyColor, new Color(0.85f, 0.5f, 1f), Random.value);
            piece.GetComponent<MeshRenderer>().material = mat;
            Rigidbody prb = piece.AddComponent<Rigidbody>();
            prb.mass = 0.02f;
            Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y);
            prb.AddForce(dir * Random.Range(5f, 13f), ForceMode.Impulse);
            prb.AddTorque(Random.onUnitSphere * 22f);
            Destroy(piece, Random.Range(0.5f, 1.1f));
        }
    }
}
