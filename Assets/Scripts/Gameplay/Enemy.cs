using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3f;
    public float health = 100f;
    private Rigidbody rb;
    private float flashTimer;
    private MeshRenderer mr;
    private Color originalColor = Color.red;
    private bool isFlashing;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mr = GetComponent<MeshRenderer>();
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }
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
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            
            rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
            
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.fixedDeltaTime));
            }
        }
    }

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
            // Death burst effect
            SpawnDeathBurst(transform.position + Vector3.up * 0.5f);
            Destroy(gameObject);
        }
    }

    // Backwards compat — old signature
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero, 0f);
    }

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
