using UnityEngine;

/// <summary>
/// Automatically provisions and spawns Wrath (The First Sin) and the Boss Combat systems into the scene.
/// Ensures everything is running immediately upon hitting Play with zero manual Editor hierarchy setup needed.
/// </summary>
public class WrathBossSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("World position to spawn the Wrath Boss.")]
    public Vector3 bossSpawnPosition = new Vector3(0f, 0f, 15f);

    [Tooltip("If true, automatically ensures Player has PlayerHealth and BossUI exists.")]
    public bool autoInitializeSystems = true;

    private void Awake()
    {
        if (autoInitializeSystems)
        {
            EnsurePlayerSystems();
            EnsureBossUI();
        }

        SpawnWrathBossIfMissing();
    }

    private void EnsurePlayerSystems()
    {
        // Find player and attach PlayerHealth if missing
        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            if (pm.GetComponent<PlayerHealth>() == null)
            {
                pm.gameObject.AddComponent<PlayerHealth>();
            }
        }
    }

    private void EnsureBossUI()
    {
        if (FindAnyObjectByType<BossUI>() == null)
        {
            GameObject uiObj = new GameObject("BossHUDManager");
            uiObj.AddComponent<BossUI>();
        }
    }

    private void SpawnWrathBossIfMissing()
    {
        if (FindAnyObjectByType<WrathBossAI>() != null) return;

        // Create the Boss root GameObject
        GameObject bossRoot = new GameObject("Boss_Wrath_Behemoth");
        bossRoot.transform.position = bossSpawnPosition;

        // Add Core Components
        BossHealth health = bossRoot.AddComponent<BossHealth>();
        bossRoot.AddComponent<BossTelegraph>();
        WrathBossVisuals visuals = bossRoot.AddComponent<WrathBossVisuals>();
        WrathBossAI ai = bossRoot.AddComponent<WrathBossAI>();

        // Build procedural 3D model with weakpoints
        visuals.BuildBehemothModel(health);

        // Add a CharacterController or NavMeshAgent for 3D physics collision & movement
        CharacterController cc = bossRoot.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1.8f, 0f);
        cc.radius = 1.6f;
        cc.height = 3.6f;
    }
}
