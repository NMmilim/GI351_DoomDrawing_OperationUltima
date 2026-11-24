using UnityEngine;

/// <summary>
/// Ensures combat HUD/PlayerHealth exist and optionally spawns a Boss Prefab if no boss is already in the scene.
/// Prefab-first: place your own Boss Prefab in the scene OR assign it to bossPrefab below.
/// </summary>
public class WrathBossSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Optional Boss Prefab to spawn if no WrathBossAI already exists in the scene.")]
    public GameObject bossPrefab;

    [Tooltip("World position to spawn bossPrefab (only used when bossPrefab is assigned and no boss is in the scene).")]
    public Vector3 bossSpawnPosition = new Vector3(0f, 0f, 15f);

    [Tooltip("If true, automatically ensures Player has PlayerHealth and BossUI exists.")]
    public bool autoInitializeSystems = true;

    [Tooltip("Fallback ONLY for quick testing without a Prefab: builds the old procedural cube boss.")]
    public bool spawnProceduralFallback = false;

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
        if (pm != null && pm.GetComponent<PlayerHealth>() == null)
        {
            pm.gameObject.AddComponent<PlayerHealth>();
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
        // If you already placed your own Boss Prefab in the scene, do nothing!
        if (FindAnyObjectByType<WrathBossAI>() != null) return;

        // Spawn your custom Boss Prefab if assigned
        if (bossPrefab != null)
        {
            Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
            return;
        }

        // Only build the procedural cube monster if explicitly opted in
        if (!spawnProceduralFallback) return;

        GameObject bossRoot = new GameObject("Boss_Wrath_Behemoth");
        bossRoot.transform.position = bossSpawnPosition;

        // Add CharacterController BEFORE WrathBossAI so WrathBossAI.Awake() finds it!
        CharacterController cc = bossRoot.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1.8f, 0f);
        cc.radius = 1.6f;
        cc.height = 3.6f;

        BossHealth health = bossRoot.AddComponent<BossHealth>();
        bossRoot.AddComponent<BossTelegraph>();
        WrathBossVisuals visuals = bossRoot.AddComponent<WrathBossVisuals>();
        bossRoot.AddComponent<WrathBossAI>();

        visuals.BuildBehemothModel(health);
    }
}
