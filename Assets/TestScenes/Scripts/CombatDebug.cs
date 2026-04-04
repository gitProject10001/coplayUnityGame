using UnityEngine;

/// <summary>
/// Test harness for PlayerController, Enemy, Bullet, SlashVFX, and CombatHUD.
/// Spawns enemies, shows hitbox gizmos, displays state labels, and exposes combat stats.
/// Attach to an empty GameObject in 06_Combat scene.
/// </summary>
public class CombatDebug : MonoBehaviour
{
    [Header("=== PLAYER STATS ===")]
    public PlayerController player;
    [Range(10f, 200f)] public float playerMaxHealth = 100f;
    [Range(5f, 200f)] public float playerMoveSpeed = 5f;
    [Range(10f, 200f)] public float playerMaxStamina = 100f;
    [Range(5f, 60f)] public float staminaRegenRate = 30f;
    [Range(0.1f, 2f)] public float staminaRegenDelay = 0.8f;

    [Header("Attack Tuning")]
    [Range(0.5f, 5f)] public float attackRange = 2f;
    [Range(0.5f, 10f)] public float attackDashForce = 4f;
    [Range(0.5f, 2f)] public float whiffMultiplier = 1.3f;

    [Header("Dodge Tuning")]
    [Range(5f, 25f)] public float dodgeSpeed = 12f;
    [Range(0.1f, 0.5f)] public float dodgeDuration = 0.3f;
    [Range(0.1f, 0.5f)] public float dodgeIFrames = 0.25f;
    [Range(5f, 50f)] public float dodgeStaminaCost = 25f;

    [Header("=== ENEMY CONTROLS ===")]
    [Range(5f, 25f)] public float enemyDetectionRange = 12f;
    [Range(1f, 5f)] public float enemyAttackRange = 2.2f;
    [Range(5f, 50f)] public float enemyDamage = 20f;
    [Range(0.1f, 2f)] public float enemyAttackWindup = 0.5f;

    [Header("Spawn")]
    public bool spawnEnemy = false;
    public bool clearAllEnemies = false;
    [Range(1, 10)] public int spawnCount = 1;
    [Range(3f, 15f)] public float spawnRadius = 8f;

    [Header("=== DEBUG VISUALIZATION ===")]
    public bool showHitboxes = true;
    public bool showDetectionRanges = true;
    public bool showStateLabels = true;
    public bool showIFrameFlash = true;
    public bool godMode = false;

    [Header("=== FEATURE TOGGLES ===")]
    public bool enableSlashVFX = true;
    public bool enableScreenShake = true;
    public bool enableHitstop = true;

    private bool sceneBuilt;

    void Start()
    {
        BuildScene();
    }

    void BuildScene()
    {
        if (sceneBuilt) return;

        // Light
        if (FindAnyObjectByType<Light>() == null)
        {
            var lightGO = new GameObject("Debug Directional Light");
            lightGO.transform.SetParent(transform);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(50, 50, 0);
        }

        // Ground (needed for mouse raycast)
        Shader toonShader = Shader.Find("Custom/ToonLit");
        if (toonShader != null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Combat Ground";
            ground.transform.SetParent(transform);
            ground.transform.localScale = new Vector3(5, 1, 5);
            var mat = new Material(toonShader);
            mat.SetColor("_BaseColor", new Color(0.25f, 0.3f, 0.2f));
            ground.GetComponent<Renderer>().material = mat;
        }

        // Find player
        player = FindAnyObjectByType<PlayerController>();

        // Ensure CombatFeedback exists
        if (CombatFeedback.Instance == null)
        {
            var fbGO = new GameObject("CombatFeedback");
            fbGO.AddComponent<CombatFeedback>();
        }

        sceneBuilt = true;
    }

    void Update()
    {
        SyncPlayerStats();
        SyncEnemyStats();
        HandleSpawnCommands();

        if (godMode && player != null)
        {
            player.currentHealth = player.maxHealth;
            player.currentStamina = player.maxStamina;
        }
    }

    void SyncPlayerStats()
    {
        if (player == null) return;

        player.maxHealth = playerMaxHealth;
        player.moveSpeed = playerMoveSpeed;
        player.maxStamina = playerMaxStamina;
        player.staminaRegenRate = staminaRegenRate;
        player.staminaRegenDelay = staminaRegenDelay;
        player.attackRange = attackRange;
        player.attackDashForce = attackDashForce;
        player.whiffRecoveryMultiplier = whiffMultiplier;
        player.dodgeSpeed = dodgeSpeed;
        player.dodgeDuration = dodgeDuration;
        player.dodgeIFrameDuration = dodgeIFrames;
        player.dodgeStaminaCost = dodgeStaminaCost;
    }

    void SyncEnemyStats()
    {
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.detectionRange = enemyDetectionRange;
            enemy.attackRange = enemyAttackRange;
            enemy.attackDamage = enemyDamage;
            enemy.attackWindup = enemyAttackWindup;
        }
    }

    void HandleSpawnCommands()
    {
        if (spawnEnemy)
        {
            spawnEnemy = false;
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnTestEnemy();
            }
        }

        if (clearAllEnemies)
        {
            clearAllEnemies = false;
            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var e in enemies) Destroy(e.gameObject);
        }
    }

    void SpawnTestEnemy()
    {
        Vector3 center = player != null ? player.transform.position : Vector3.zero;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
        pos.y = 1f;

        var enemyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyGO.name = "Debug Enemy";
        enemyGO.transform.position = pos;

        var rb = enemyGO.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var enemy = enemyGO.AddComponent<Enemy>();
        enemy.detectionRange = enemyDetectionRange;
        enemy.attackRange = enemyAttackRange;
        enemy.attackDamage = enemyDamage;

        // Red tint
        var mr = enemyGO.GetComponent<MeshRenderer>();
        mr.material.color = Color.red;
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        // Player info panel
        GUILayout.BeginArea(new Rect(10, 10, 320, 300));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.UpperLeft;

        string playerInfo = "Player: NOT FOUND (set up player in scene)";
        if (player != null)
        {
            // Use reflection to get private state
            var stateField = typeof(PlayerController).GetField("currentState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var state = stateField != null ? stateField.GetValue(player) : "?";

            var comboField = typeof(PlayerController).GetField("comboStep",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var combo = comboField != null ? comboField.GetValue(player) : "?";

            playerInfo = $"Player State: {state}\n" +
                $"  HP: {player.currentHealth:F0}/{player.maxHealth:F0}\n" +
                $"  Stamina: {player.currentStamina:F0}/{player.maxStamina:F0}\n" +
                $"  Combo Step: {combo}\n" +
                $"  Ranged: {player.currentRangedCharges}/{player.maxRangedCharges}\n" +
                $"  God Mode: {(godMode ? "ON" : "OFF")}";
        }

        int enemyCount = FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length;

        GUILayout.Box(
            $"=== COMBAT DEBUG ===\n\n" +
            $"{playerInfo}\n\n" +
            $"Enemies: {enemyCount}\n" +
            $"VFX: {(enableSlashVFX ? "ON" : "OFF")} | " +
            $"Shake: {(enableScreenShake ? "ON" : "OFF")} | " +
            $"Hitstop: {(enableHitstop ? "ON" : "OFF")}",
            style);

        GUILayout.EndArea();

        // State labels above enemies
        if (showStateLabels)
        {
            DrawStateLabels();
        }
    }

    void DrawStateLabels()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        var labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = Color.yellow;
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(enemy.transform.position + Vector3.up * 2.5f);
            if (screenPos.z > 0)
            {
                var stateField = typeof(Enemy).GetField("currentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                string state = stateField != null ? stateField.GetValue(enemy).ToString() : "?";

                float healthPct = enemy.health / 100f;
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 30),
                    $"{state}\nHP:{enemy.health:F0}", labelStyle);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (showHitboxes && player != null)
        {
            // Player attack range
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(player.transform.position + player.transform.forward * attackRange * 0.5f, attackRange);

            // Dodge i-frame indicator
            if (showIFrameFlash)
            {
                var stateField = typeof(PlayerController).GetField("currentState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (stateField != null)
                {
                    var state = (PlayerState)stateField.GetValue(player);
                    if (state == PlayerState.Dodging)
                    {
                        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                        Gizmos.DrawWireSphere(player.transform.position, 1.5f);
                    }
                }
            }
        }

        if (showDetectionRanges)
        {
            var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                // Detection range
                Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
                Gizmos.DrawWireSphere(enemy.transform.position, enemyDetectionRange);

                // Attack range
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireSphere(enemy.transform.position, enemyAttackRange);
            }
        }

        // Spawn radius
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
        Vector3 center = player != null ? player.transform.position : Vector3.zero;
        DrawGizmoCircle(center, spawnRadius, 32);
    }

    void DrawGizmoCircle(Vector3 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float a1 = (i / (float)segments) * Mathf.PI * 2f;
            float a2 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1) * radius, 0.1f, Mathf.Sin(a1) * radius),
                center + new Vector3(Mathf.Cos(a2) * radius, 0.1f, Mathf.Sin(a2) * radius));
        }
    }
}
