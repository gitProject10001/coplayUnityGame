using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int enemyCount = 5;
    public float spawnRadius = 15f;
    public float spawnInterval = 3f;
    
    private Material enemyMaterial;

    void Start()
    {
        // Create a red material for enemies
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        enemyMaterial = new Material(shader);
        enemyMaterial.color = Color.red;

        // Initial spawn
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();
        }

        // Continuous spawn
        InvokeRepeating("SpawnEnemy", spawnInterval, spawnInterval);
    }

    void SpawnEnemy()
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius; // Spawn at edge
        Vector3 spawnPos = new Vector3(randomCircle.x, 1f, randomCircle.y);
        
        GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObj.name = "Enemy";
        enemyObj.transform.position = spawnPos;
        
        enemyObj.GetComponent<MeshRenderer>().material = enemyMaterial;
        
        Rigidbody rb = enemyObj.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        enemyObj.AddComponent<Enemy>();
    }
}
