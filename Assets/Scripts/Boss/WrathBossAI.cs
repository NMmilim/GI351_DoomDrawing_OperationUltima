using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// State Machine AI for Wrath (The First Sin) — A brutal, heavy behemoth boss.
/// Model-agnostic: works on any custom Prefab with or without an Animator or WrathBossVisuals.
/// </summary>
[RequireComponent(typeof(BossHealth))]
[RequireComponent(typeof(BossTelegraph))]
public class WrathBossAI : MonoBehaviour
{
    public enum BossState
    {
        Stalking,
        Roaming,
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
    public float roamSpeed = 2.8f;
    public float roamRadius = 14f;
    public float rotationSpeed = 120f;
    public float idealCombatDistance = 8f;

    [Header("Optional Art / Animation Hook")]
    [Tooltip("Optional Animator on your 3D model. Auto-found in children if left empty. Safe to leave unassigned.")]
    public Animator animator;

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
    private HashSet<string>     _animParams;

    // State Machine Variables
    private BossState _state = BossState.Stalking;
    private float     _stateTimer;
    private float     _attackCooldown;
    private Vector3   _chargeDirection;
    private Vector3   _chargeStartPos;
    private float     _chargeMaxTime;
    private float     _chargeElapsed;
    private bool      _hasDealtChargeHit;
    private Coroutine _activeAttackCoroutine;
    private bool      _isPlayerHidden;
    private Vector3   _roamTarget;
    private float     _roamTimer;
    private float     _roamPauseTimer;

    public BossState CurrentState => _state;
    public bool IsPlayerHidden => _isPlayerHidden;
    public event System.Action<BossState> OnStateChanged;

    private void Awake()
    {
        _health    = GetComponent<BossHealth>();
        _telegraph = GetComponent<BossTelegraph>();
        _navAgent  = GetComponent<NavMeshAgent>();
        _characterController = GetComponent<CharacterController>();
        _visuals   = GetComponent<WrathBossVisuals>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        CacheAnimatorParameters();

        if (_navAgent != null)
        {
            _navAgent.updateRotation = false; // We rotate toward player manually
            _navAgent.speed = stalkSpeed;
        }
    }

    private void CacheAnimatorParameters()
    {
        _animParams = new HashSet<string>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            _animParams.Add(param.name);
        }
    }

    private void SetState(BossState newState)
    {
        if (_state == newState) return;

        _state = newState;
        OnStateChanged?.Invoke(newState);

        bool isMoving = newState == BossState.Stalking ||
                        newState == BossState.Roaming;

        TrySetAnimBool("IsMoving", isMoving);
        TrySetAnimBool("IsStaggered", newState == BossState.Staggered);
        TrySetAnimBool("IsDead", newState == BossState.Defeated);

        switch (newState)
        {
            case BossState.BullCharge_Windup:
                TrySetAnimTrigger("DashPrepare");
                break;

            case BossState.BullCharge_Rush:
                TrySetAnimTrigger("Dash");
                break;

            case BossState.GroundSlam_Windup:
                TrySetAnimTrigger("Slam");
                break;

            case BossState.GroundSlam_Impact:
                TrySetAnimTrigger("SlamImpact");
                break;

            case BossState.MagmaBurst:
                TrySetAnimTrigger("ShootFire");
                break;

            case BossState.EnragedTransition:
                TrySetAnimTrigger("Enrage");
                break;

            case BossState.Defeated:
                TrySetAnimTrigger("Death");
                break; 
        }
    }

    private void TrySetAnimTrigger(string paramName)
    {
        if (animator != null && _animParams != null && _animParams.Contains(paramName))
            animator.SetTrigger(paramName);
    }

    private void TrySetAnimBool(string paramName, bool value)
    {
        if (animator != null && _animParams != null && _animParams.Contains(paramName))
            animator.SetBool(paramName, value);
    }

    private void StopActiveAttackCoroutine()
    {
        if (_activeAttackCoroutine != null)
        {
            StopCoroutine(_activeAttackCoroutine);
            _activeAttackCoroutine = null;
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
            if (p != null)
            {
                playerTarget = p.transform;
            }
            else
            {
                // Fallback: search for PlayerMovement (works even if Player isn't tagged "Player")
                PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
                if (pm != null)
                {
                    playerTarget = pm.transform;
                }
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

            case BossState.Roaming:
                TickRoaming();
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
        bool shouldWalk = dist > idealCombatDistance * 0.75f;

        TrySetAnimBool("IsMoving", shouldWalk);

        if (shouldWalk)
        {
            Vector3 toPlayer = playerTarget.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.001f)
            {
                MoveInDirection(toPlayer.normalized, stalkSpeed);
            }
        }

        _attackCooldown -= Time.deltaTime;

        if (_attackCooldown <= 0f)
        {
            ChooseNextAttack(dist);
        }
    }

    // -----------------------------------------------------------------
    //  State: Roaming (Lost sight of Player, e.g. inside Smoke Orbital)
    // -----------------------------------------------------------------
    public void SetPlayerHidden(bool hidden)
    {
        _isPlayerHidden = hidden;

        if (_state == BossState.Defeated) return;

        if (hidden)
        {
            if (_state != BossState.Staggered && _state != BossState.EnragedTransition)
            {
                StopActiveAttackCoroutine();
                _telegraph.HideAll();
                PickNewRoamPoint();
                SetState(BossState.Roaming);
            }
        }
        else if (_state == BossState.Roaming)
        {
            // Detected player again!
            _attackCooldown = Mathf.Min(_attackCooldown, 1.5f);
            SetState(BossState.Stalking);
        }
    }

    private void TickRoaming()
    {
        if (!_isPlayerHidden)
        {
            SetState(BossState.Stalking);
            return;
        }

        if (_roamPauseTimer > 0f)
        {
            _roamPauseTimer -= Time.deltaTime;
            if (_roamPauseTimer <= 0f)
            {
                PickNewRoamPoint();
            }
            return;
        }

        _roamTimer -= Time.deltaTime;
        Vector3 toPoint = _roamTarget - transform.position;
        toPoint.y = 0f;

        if (toPoint.sqrMagnitude <= 2.25f || _roamTimer <= 0f)
        {
            _roamPauseTimer = Random.Range(0.8f, 1.6f);
            return;
        }

        LookAtTarget(_roamTarget, rotationSpeed * 0.65f);
        MoveInDirection(toPoint.normalized, roamSpeed);
    }

    private void PickNewRoamPoint()
    {
        Vector2 offset = Random.insideUnitCircle * roamRadius;
        if (offset.sqrMagnitude < 16f) offset = offset.normalized * 6f;
        _roamTarget = transform.position + new Vector3(offset.x, 0f, offset.y);
        _roamTimer = 4f;
        _roamPauseTimer = 0f;
    }

    private BossState GetIdleCombatState()
    {
        if (_isPlayerHidden)
        {
            PickNewRoamPoint();
            return BossState.Roaming;
        }
        return BossState.Stalking;
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
            else _activeAttackCoroutine = StartCoroutine(ExecuteMagmaBurst());
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
        SetState(BossState.BullCharge_Windup);
        float windupDuration = _health.IsEnraged ? chargeWindup * 0.7f : chargeWindup;
        _stateTimer = windupDuration;

        // Lock charge direction towards the player
        Vector3 toPlayer = (playerTarget.position - transform.position);
        toPlayer.y = 0f;
        _chargeDirection = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;

        // Show danger lane on the floor at the boss's feet
        _telegraph.ShowChargeLane(GetFeetPosition(), _chargeDirection, chargeDistance, chargeLaneWidth);
    }

    private void TickChargeWindup()
    {
        LookAtTarget(transform.position + _chargeDirection * 10f, rotationSpeed * 2f);
        _stateTimer -= Time.deltaTime;

        if (_stateTimer <= 0f)
        {
            _telegraph.HideChargeLane();
            SetState(BossState.BullCharge_Rush);
            _chargeStartPos = transform.position;
            _hasDealtChargeHit = false;
            float speed = _health.IsEnraged ? chargeSpeed * 1.25f : chargeSpeed;
            _chargeMaxTime = (chargeDistance / Mathf.Max(1f, speed)) + 0.5f;
            _chargeElapsed = 0f;
        }
    }

    private void TickChargeRush()
    {
        float speed = _health.IsEnraged ? chargeSpeed * 1.25f : chargeSpeed;
        MoveInDirection(_chargeDirection, speed);
        _chargeElapsed += Time.deltaTime;

        // Check horizontal (XZ) distance from boss capsule center so tall bosses still hit players on the ground
        if (!_hasDealtChargeHit && playerTarget != null)
        {
            Vector3 bossCenter = GetCapsuleWorldCenter();
            Vector2 bossXZ = new Vector2(bossCenter.x, bossCenter.z);
            Vector2 playerXZ = new Vector2(playerTarget.position.x, playerTarget.position.z);
            float hitRadius = Mathf.Max(chargeLaneWidth * 0.6f, (_characterController != null ? _characterController.radius : 1.5f) + 1.2f);
            float verticalGap = Mathf.Abs(GetFeetPosition().y - playerTarget.position.y);

            if (Vector2.Distance(bossXZ, playerXZ) <= hitRadius && verticalGap <= 4.5f)
            {
                _hasDealtChargeHit = true;
                PlayerHealth ph = playerTarget.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(chargeDamage);
                }
            }
        }

        // End rush if distance reached OR time expired (prevents getting stuck forever against a wall)
        float traveled = Vector3.Distance(_chargeStartPos, transform.position);
        if (traveled >= chargeDistance || _chargeElapsed >= _chargeMaxTime)
        {
            SetState(BossState.BullCharge_Recovery);
            _stateTimer = _health.IsEnraged ? 0.5f : 0.9f;
        }
    }

    private void TickChargeRecovery()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            SetState(GetIdleCombatState());
        }
    }

    // -----------------------------------------------------------------
    //  Attack: Ground Slam (Shockwave Ring)
    // -----------------------------------------------------------------
    private void StartGroundSlam()
    {
        SetState(BossState.GroundSlam_Windup);
        float windupDuration = _health.IsEnraged ? slamWindup * 0.75f : slamWindup;
        _stateTimer = windupDuration;

        // Show danger circle on the ground at the boss's feet
        _telegraph.ShowSlamCircle(GetFeetPosition(), slamRadius);
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
        SetState(BossState.GroundSlam_Impact);

        CameraShake cameraShake = FindAnyObjectByType<CameraShake>();
        if (cameraShake != null)
        {
            cameraShake.Shake(1.5f, 0.35f);
        }

        _telegraph.HideSlamCircle();
        Vector3 feetPos = GetFeetPosition();

        Collider[] hits = Physics.OverlapSphere(feetPos, slamRadius);
        HashSet<PlayerHealth> damagedPlayers =
            new HashSet<PlayerHealth>();

        foreach (Collider col in hits)
        {
            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();

            if (ph != null && damagedPlayers.Add(ph))
            {
                ph.TakeDamage(slamDamage);
            }
        }

        SpawnShockwaveVFX(feetPos);

        SetState(BossState.GroundSlam_Recovery);
        _stateTimer = _health.IsEnraged ? 0.6f : 1.1f;
    }

    private void SpawnShockwaveVFX(Vector3 feetPos)
    {
        GameObject shock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shock.transform.position = feetPos + Vector3.up * 0.1f;
        shock.transform.localScale = new Vector3(slamRadius * 2f, 0.05f, slamRadius * 2f);
        Collider sc = shock.GetComponent<Collider>();
        if (sc != null) sc.enabled = false;

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
            SetState(GetIdleCombatState());
        }
    }

    // -----------------------------------------------------------------
    //  Attack: Magma Burst (Volcanic Spit)
    // -----------------------------------------------------------------
    private IEnumerator ExecuteMagmaBurst()
    {
        SetState(BossState.MagmaBurst);

        for (int i = 0; i < magmaShots; i++)
        {
            if (_state != BossState.MagmaBurst || playerTarget == null)
            {
                _activeAttackCoroutine = null;
                yield break;
            }

            LookAtTarget(playerTarget.position, rotationSpeed * 2f);

            // Recalculate spawnPoint each shot so it follows the boss's current rotation/mouth
            Vector3 spawnPoint = (mouthPosition != null)
                ? mouthPosition.position
                : (transform.position + Vector3.up * 2.5f + transform.forward * 2f);

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
        _activeAttackCoroutine = null;

        if (_state == BossState.MagmaBurst)
        {
            SetState(GetIdleCombatState());
        }
    }

    // -----------------------------------------------------------------
    //  Special Transitions: Stagger, Enrage, Death
    // -----------------------------------------------------------------
    private void HandleStaggered()
    {
        if (_state == BossState.Defeated) return;

        StopActiveAttackCoroutine();
        _telegraph.HideAll();
        SetState(BossState.Staggered);
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

        // Procedural knockdown curve (only used if WrathBossVisuals is attached)
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
            SetState(GetIdleCombatState());
        }
    }

    private void HandleEnraged()
    {
        if (_state == BossState.Defeated) return;

        StopActiveAttackCoroutine();
        _telegraph.HideAll();
        SetState(BossState.EnragedTransition);
        _stateTimer = 1.8f;

        if (_visuals != null) _visuals.SetEnragedAppearance(true);
    }

    private void TickEnragedTransition()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            SetState(GetIdleCombatState());
        }
    }

    private void HandleDefeated()
    {
        StopActiveAttackCoroutine();
        SetState(BossState.Defeated);
        _telegraph.HideAll();

        if (_navAgent != null) _navAgent.isStopped = true;

        // Disable colliders so no further hits register
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders) col.enabled = false;

        if (_visuals != null)
        {
            _visuals.SetDefeatedPose();
        }
    }

    // -----------------------------------------------------------------
    //  Movement, Ground-Foot & Rotation Helpers
    // -----------------------------------------------------------------
    public Vector3 GetCapsuleWorldCenter()
    {
        if (_characterController != null)
            return transform.TransformPoint(_characterController.center);
        return transform.position;
    }

    /// <summary>
    /// Returns the world position at the bottom of the Boss's CharacterController capsule (the feet),
    /// even if the Prefab's root pivot is centered in the torso.
    /// </summary>
    public Vector3 GetFeetPosition()
    {
        if (_characterController != null)
        {
            Vector3 worldCenter = transform.TransformPoint(_characterController.center);
            float halfHeight = (_characterController.height * 0.5f) * Mathf.Abs(transform.lossyScale.y);
            return new Vector3(worldCenter.x, worldCenter.y - halfHeight, worldCenter.z);
        }
        return transform.position;
    }

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
