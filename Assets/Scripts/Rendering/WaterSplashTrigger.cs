using System.Collections.Generic;
using UnityEngine;

// Place this on the water plane (or a thin trigger collider at the water surface).
// When any Rigidbody crosses the surface fast enough, it spawns a splash particle
// burst and injects a shader ripple via WaterRippleEmitter.EmitAt().
[RequireComponent(typeof(Collider))]
public class WaterSplashTrigger : MonoBehaviour
{
    [SerializeField] GameObject splashPrefab;
    [Tooltip("Incoming speed below this value is ignored (slow wading, not splashing).")]
    [SerializeField] float minSpeed = 1.2f;
    [Tooltip("Seconds before the same object can trigger another splash.")]
    [SerializeField] float perObjectCooldown = 0.6f;

    readonly Dictionary<int, float> _cooldowns = new();

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        int id = other.GetInstanceID();
        if (_cooldowns.TryGetValue(id, out float last) && Time.time - last < perObjectCooldown)
            return;
        _cooldowns[id] = Time.time;

        Rigidbody rb = other.attachedRigidbody;
        float speed = rb != null ? rb.linearVelocity.magnitude : 4f;
        if (speed < minSpeed) return;

        Vector3 hitPos = new Vector3(other.bounds.center.x,
                                     transform.position.y,
                                     other.bounds.center.z);

        WaterRippleEmitter.EmitAt(hitPos);
        WaveHeightfield.Instance?.SplashAt(hitPos, Mathf.Clamp01(speed / 5f));

        if (splashPrefab != null)
            Instantiate(splashPrefab, hitPos, Quaternion.identity);
    }
}