using System;
using UnityEngine;

/// <summary>
/// Health and stagger management for Boss encounters.
/// Manages total HP, part damage accumulation, enrage thresholds, and death events.
/// </summary>
public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public string bossName = "WRATH - THE CONSUMING FLAME";
    public int maxHp = 1200;
    private int _currentHp;
    private bool _isDead;

    [Header("Enrage & Stagger Mechanics")]
    [Tooltip("Health percentage (0..1) that triggers the fiery Enraged Phase 2.")]
    [Range(0.1f, 0.9f)]
    public float enrageThreshold = 0.5f;
    private bool _isEnraged;

    [Tooltip("Damage dealt to limbs required to trip/stagger the boss.")]
    public int damageToStagger = 300;
    private int _staggerAccumulator;

    public int  CurrentHp   => _currentHp;
    public int  MaxHp       => maxHp;
    public bool IsDead      => _isDead;
    public bool IsEnraged   => _isEnraged;
    public float HealthRatio => (float)_currentHp / maxHp;

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnStaggered;
    public event Action OnEnraged;
    public event Action OnDefeated;

    private void Awake()
    {
        _currentHp = maxHp;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(_currentHp, maxHp);
    }

    public void TakeDamage(int amount)
    {
        RegisterDamage(amount, BossWeakpoint.BodyPart.ArmoredBody, transform.position);
    }

    public void RegisterDamage(int damage, BossWeakpoint.BodyPart part, Vector3 hitPoint)
    {
        if (_isDead || damage <= 0) return;

        _currentHp = Mathf.Max(0, _currentHp - damage);
        OnHealthChanged?.Invoke(_currentHp, maxHp);

        // Check Stagger Accumulation (Attacks on legs and head contribute heavily)
        if (!_isDead)
        {
            int staggerValue = (part == BossWeakpoint.BodyPart.Head || part == BossWeakpoint.BodyPart.Legs)
                ? Mathf.RoundToInt(damage * 1.5f)
                : damage;

            _staggerAccumulator += staggerValue;
            if (_staggerAccumulator >= damageToStagger)
            {
                _staggerAccumulator = 0;
                OnStaggered?.Invoke();
            }

            // Check Enrage Threshold
            if (!_isEnraged && HealthRatio <= enrageThreshold)
            {
                _isEnraged = true;
                OnEnraged?.Invoke();
            }
        }

        // Check Death
        if (_currentHp <= 0 && !_isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnDefeated?.Invoke();
    }
}
