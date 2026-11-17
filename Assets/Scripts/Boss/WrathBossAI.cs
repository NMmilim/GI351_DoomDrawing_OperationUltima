using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// State Machine AI for Wrath (The First Sin) — A brutal, heavy behemoth boss.
/// Supports both NavMesh and direct-steering navigation so it functions immediately in any scene.
/// </summary>
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(BossTelegraph))]
public class WrathBossAI : MonoBehaviour
{
    public enum BossState
    {
        Stalking,
        BullCharge_Windup,
        BullCharge_Rush,
        BullCharge_Recovery,
        GroundSlam_Windup,
        GroundSlam_Impact,
        GroundSlam_Recovery,
        MagmaBurst,
        Staggered,
        EnragedTransition,
        Defeated
    }

    [Header("Targeting & Motion")]
    public Transform playerTarget;
    public float stalkSpeed = 4.5f;
    public float rotationSpeed = 120f;
    public float idealCombatDistance = 8f;

    [Header("Bull Charge Attack")]
    public float chargeWindup = 1.2f;
    public float chargeSpeed = 22f;
    public float chargeDistance = 25f;
    public float chargeLaneWidth = 3.5f;
    public int   chargeDamage = 35;

    [Header("Ground Slam Attack")]
    public float slamWindup = 1.4f;
    public float slamRadius = 7f;
    public int   slamDamage = 40;

    [Header("Magma Burst Attack")]
    public GameObject magmaProjectilePrefab;
    public Transform mouthPosition;
    public int magmaShots = 3;
    public float timeBetweenShots = 0.25f;

    [Header("Cooldowns & Timers")]
    public float attackIntervalMin = 2.5f;
    public float attackIntervalMax = 4.0f;
    public float staggerDuration = 4.0f;

    // Component References
    private BossHealth          _health;
    private BossTelegraph       _telegraph;
    private NavMeshAgent        _navAgent;
    private CharacterController _characterController;
    private WrathBossVisuals    _visuals;

    // State Machine Variables
    private BossState _state = BossState.Stalking;
    private float     _stateTimer;
    private float     _attackCooldown;
    private Vector3   _chargeDirection;
    private Vector3   _chargeStartPos;
    private bool      _hasDealtChargeHit;

    public BossState CurrentState => _state;

    private void Awake()
    {
        _health    = GetComponent<BossHealth>();
        _telegraph = GetComponent<BossTelegraph>();
        _navAgent  = GetComponent<NavMeshAgent>();
        _characterController = GetComponent<CharacterController>();
        _visuals   = GetComponent<WrathBossVisuals>();

        if (_navAgent != null)
        {
            _navAgent.updateRotation = false; // We rotate toward player manually
            _navAgent.speed = stalkSpeed;
        }
    }

    private void OnEnable()
    {
        _health.OnStaggered += HandleStaggered;
        _health.OnEnraged   += HandleEnraged;
        _health.OnDefeated  += HandleDefeated;
    }

    private void OnDisable()
    {
        _health.OnStaggered -= HandleStaggered;
        _health.OnEnraged   -= HandleEnraged;
        _health.OnDefeated  -= HandleDefeated;
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTarget = p.transform;
            else
            {
                // Fallback: search for CharacterController
                CharacterController cc = FindAnyObjectByType<CharacterController>();
                if (cc != null && cc.gameObject != gameObject)
                    playerTarget = cc.transform;
            }
        }

        _attackCooldown = 2.0f; // Initial opening delay
    }

    private void Update()
    {
        if (_state == BossState.Defeated) return;

        if (playerTarget == null) return;

        switch (_state)
        {
            case BossState.Stalking:
                TickStalking();
                break;

            case BossState.BullCharge_Windup:
                TickChargeWindup();
                break;

            case BossState.BullCharge_Rush:
                TickChargeRush();
                break;

            case BossState.BullCharge_Recovery:
                TickChargeRecovery();
                break;

            case BossState.GroundSlam_Windup:
                TickSlamWindup();
                break;

            case BossState.GroundSlam_Recovery:
                TickSlamRecovery();
                break;

            case BossState.Staggered:
                TickStaggered();
                break;

            case BossState.EnragedTransition:
                TickEnragedTransition();
                break;
        }
    }

    // -----------------------------------------------------------------
    //  State: Stalking
    // -----------------------------------------------------------------
    private void TickStalking()
    {
        LookAtTarget(playerTarget.position, rotationSpeed);

        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // Move toward player until in combat range
        if (dist > idealCombatDistance * 0.75f)
        {
            MoveInDirection((playerTarget.position - transform.position).normalized, stalkSpeed);
        }

        _attackCooldown -= Time.deltaTime;
        if (_attackCooldown <= 0f)
        {
            ChooseNextAttack(dist);
        }
    }

    private void ChooseNextAttack(float distToPlayer)
    {
        // Enrage speeds up attack pace
        float speedMod = _health.IsEnraged ? 0.7f : 1.0f;
        _attackCooldown = Random.Range(attackIntervalMin, attackIntervalMax) * speedMod;

        // Pick attack based on distance
        if (distToPlayer > 12f)
        {
            // At long range: Bull Charge or Magma Spit
            if (Random.value < 0.65f) StartBullCharge();
            else StartCoroutine(ExecuteMagmaBurst());
        }
        else
        {
            // In close quarters: Ground Slam or Bull Charge
            if (Random.value < 0.6f) StartGroundSlam();
            else StartBullCharge();
        }
    }

    // -----------------------------------------------------------------
    //  Attack: Bull Charge (Rushing Ram)
    // -----------------------------------------------------------------
    private void StartBullCharge()
    {
        _state = BossState.BullCharge_Windup;
        float windupDuration = _health.IsEnraged ? chargeWindup * 0.7f : chargeWindup;
        _stateTimer = windupDuration;

        // Lock charge direction towards the player
        Vector3 toPlayer = (playerTarget.position - transform.position);
        toPlayer.y = 0f;
        _chargeDirection = toPlayer.normalized;

        // Show danger lane on the floor
        _telegraph.ShowChargeLane(transform.position, _chargeDirection, chargeDistance, chargeLaneWidth);
    }

    private void TickChargeWindup()
    {
        LookAtTarget(transform.position + _chargeDirection * 10f, rotationSpeed * 2f);
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0f)
        {
            _telegraph.HideChargeLane();
            _state = BossState.BullCharge_Rush;
            _chargeStartPos = transform.position;
            _hasDealtChargeHit = false;
        }
    }

    private void TickChargeRush()
    {
        float speed = _health.IsEnraged ? chargeSpeed * 1.25f : chargeSpeed;
        MoveInDirection(_chargeDirection, speed);

        // Check if hit player during charge
        if (!_hasDealtChargeHit && playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distToPlayer <= (chargeLaneWidth * 0.6f))
            {
                _hasDealtChargeHit = true;
                PlayerHealth ph = playerTarget.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(chargeDamage);
                }
            }
        }

        // Check if charge distance exceeded
        float traveled = Vector3.Distance(_chargeStartPos, transform.position);
        if (traveled >= chargeDistance)
        {
            _state = BossState.BullCharge_Recovery;
            _stateTimer = _health.IsEnraged ? 0.5f : 0.9f;
        }
    }

    private void TickChargeRecovery()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = BossState.Stalking;
        }
    }

    // -----------------------------------------------------------------
    //  Attack: Ground Slam (Shockwave Ring)
    // -----------------------------------------------------------------
    private void StartGroundSlam()
    {
        _state = BossState.GroundSlam_Windup;
        float windupDuration = _health.IsEnraged ? slamWindup * 0.75f : slamWindup;
        _stateTimer = windupDuration;

        // Show danger circle on the ground
        _telegraph.ShowSlamCircle(transform.position, slamRadius);
    }

    private void TickSlamWindup()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            ExecuteSlamImpact();
        }
    }

    private void ExecuteSlamImpact()
    {
        _telegraph.HideSlamCircle();

        // Check damage in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        foreach (Collider col in hits)
        {
            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(slamDamage);
            }
        }

        // Spawn visual ground dust ring
        SpawnShockwaveVFX();

        _state = BossState.GroundSlam_Recovery;
        _stateTimer = _health.IsEnraged ? 0.6f : 1.1f;
    }

    private void SpawnShockwaveVFX()
    {
        GameObject shock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shock.transform.position = transform.position + Vector3.up * 0.1f;
        shock.transform.localScale = new Vector3(slamRadius * 2f, 0.05f, slamRadius * 2f);
        Collider sc = shock.GetComponent<Collider>();
        if (sc != null) DestroyImmediate(sc);

        Renderer r = shock.GetComponent<Renderer>();
        if (r != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (s != null)
            {
                Material m = new Material(s) { color = new Color(1f, 0.4f, 0.1f, 0.7f) };
                r.material = m;
            }
        }

        Destroy(shock, 0.25f);
    }

    private void TickSlamRecovery()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = BossState.Stalking;
        }
    }

    // -----------------------------------------------------------------
    //  Attack: Magma Burst (Volcanic Spit)
    // -----------------------------------------------------------------
    private IEnumerator ExecuteMagmaBurst()
    {
        _state = BossState.MagmaBurst;

        Vector3 spawnPoint = (mouthPosition != null) ? mouthPosition.position : (transform.position + Vector3.up * 2.5f + transform.forward * 2f);

        for (int i = 0; i < magmaShots; i++)
        {
            if (playerTarget == null) break;

            LookAtTarget(playerTarget.position, rotationSpeed * 2f);

            GameObject proj;
            if (magmaProjectilePrefab != null)
            {
                proj = Instantiate(magmaProjectilePrefab, spawnPoint, Quaternion.identity);
            }
            else
            {
                // Procedural magma fireball
                proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                proj.name = "MagmaFireball";
                proj.transform.position = spawnPoint;
                proj.transform.localScale = Vector3.one * 0.8f;
                Renderer pr = proj.GetComponent<Renderer>();
                if (pr != null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                    if (s != null) pr.material = new Material(s) { color = new Color(1f, 0.2f, 0.05f) };
                }
            }

            MagmaProjectile mp = proj.GetComponent<MagmaProjectile>() ?? proj.AddComponent<MagmaProjectile>();
            mp.Init(playerTarget.position + Vector3.up * 0.5f);

            yield return new WaitForSeconds(timeBetweenShots);
        }

        yield return new WaitForSeconds(0.6f);
        _state = BossState.Stalking;
    }

    // -----------------------------------------------------------------
    //  Special Transitions: Stagger, Enrage, Death
    // -----------------------------------------------------------------
    private void HandleStaggered()
    {
        if (_state == BossState.Defeated) return;

        _telegraph.HideAll();
        _state = BossState.Staggered;
        _stateTimer = staggerDuration;

        if (_visuals != null)
        {
            _visuals.ShowDizzyStars(staggerDuration);
        }
    }

    private void TickStaggered()
    {
        _stateTimer -= Time.deltaTime;
        float elapsed = staggerDuration - _stateTimer;

        // Procedural knockdown curve:
        // 0.0s - 0.5s: Collapse down to floor (0 -> 1)
        // 0.5s - (staggerDuration - 0.8s): Down on floor vulnerable
        // (staggerDuration - 0.8s) - staggerDuration: Stand back up (1 -> 0)
        float collapseDuration = 0.5f;
        float getUpDuration = 0.8f;
        float getUpStart = Mathf.Max(collapseDuration, staggerDuration - getUpDuration);

        float poseProgress = 1f;
        if (elapsed < collapseDuration)
        {
            poseProgress = Mathf.SmoothStep(0f, 1f, elapsed / collapseDuration);
        }
        else if (elapsed >= getUpStart)
        {
            float getUpElapsed = elapsed - getUpStart;
            poseProgress = Mathf.SmoothStep(1f, 0f, getUpElapsed / getUpDuration);
        }

        if (_visuals != null)
        {
            _visuals.SetKnockdownPose(poseProgress);
        }

        if (_stateTimer <= 0f)
        {
            if (_visuals != null)
            {
                _visuals.SetKnockdownPose(0f);
                _visuals.HideDizzyStars();
            }
            _state = BossState.Stalking;
        }
    }

    private void HandleEnraged()
    {
        if (_state == BossState.Defeated) return;

        _telegraph.HideAll();
        _state = BossState.EnragedTransition;
        _stateTimer = 1.8f;

        // Trigger red magma emission on materials
        WrathBossVisuals visuals = GetComponent<WrathBossVisuals>();
        if (visuals != null) visuals.SetEnragedAppearance(true);
    }

    private void TickEnragedTransition()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            _state = BossState.Stalking;
        }
    }

    private void HandleDefeated()
    {
        _state = BossState.Defeated;
        _telegraph.HideAll();

        if (_navAgent != null) _navAgent.isStopped = true;

        // Disable weakpoint colliders so no further hits register
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        if (_visuals != null)
        {
            _visuals.SetDefeatedPose();
        }
    }

    // -----------------------------------------------------------------
    //  Movement & Rotation Helpers
    // -----------------------------------------------------------------
    private void LookAtTarget(Vector3 targetPos, float speed)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, speed * Time.deltaTime);
    }

    private void MoveInDirection(Vector3 direction, float speed)
    {
        if (_characterController != null && _characterController.enabled)
        {
            Vector3 move = direction * speed + Vector3.up * -9.81f;
            _characterController.Move(move * Time.deltaTime);
        }
        else
        {
            transform.position += direction * (speed * Time.deltaTime);
        }
    }
}
