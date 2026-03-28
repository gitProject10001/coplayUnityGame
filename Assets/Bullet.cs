using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 15f;
    public float lifetime = 3f;
    private Vector3 direction;
    private TrailRenderer trail;
    private float timer;

    public void Init(Vector3 dir, float bulletSpeed, float bulletDamage)
    {
        direction = dir.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        transform.rotation = Quaternion.LookRotation(dir);
        timer = lifetime;
    }

    void Start()
    {
        // Visual: small sphere
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.2f;
        Destroy(visual.GetComponent<Collider>());

        // Glowing material
        Renderer rend = visual.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = new Color(1f, 0.6f, 0.1f, 1f);
        rend.material = mat;

        // Trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0f;
        trail.material = new Material(shader);
        trail.material.color = new Color(1f, 0.4f, 0f, 0.8f);
        trail.startColor = new Color(1f, 0.5f, 0.1f, 0.8f);
        trail.endColor = new Color(1f, 0.3f, 0f, 0f);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float moveDistance = speed * Time.deltaTime;

        // Raycast forward to detect hits — prevents tunneling through enemies
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, moveDistance + 0.2f))
        {
            Enemy enemy = hit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, direction, 2f);
                CombatFeedback.AddTrauma(0.08f);
                Destroy(gameObject);
                return;
            }
        }

        // Also do a small sphere cast for wider detection
        if (Physics.SphereCast(transform.position, 0.15f, direction, out RaycastHit sphereHit, moveDistance))
        {
            Enemy enemy = sphereHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, direction, 2f);
                CombatFeedback.AddTrauma(0.08f);
                Destroy(gameObject);
                return;
            }
        }

        transform.position += direction * moveDistance;
    }
}
