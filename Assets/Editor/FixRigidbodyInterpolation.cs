using UnityEngine;
using UnityEditor;

public class FixRigidbodyInterpolation
{
    public static void Execute()
    {
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Debug.Log("Rigidbody interpolation set to Interpolate.");
            }
        }
    }
}
