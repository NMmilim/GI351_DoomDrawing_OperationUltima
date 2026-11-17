using UnityEngine;

/// <summary>
/// Interface for actors capable of taking damage.
/// </summary>
public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(int amount);
}

/// <summary>
/// Specialized contract for multi-part monster/boss damage.
/// Passes hit point and normal for impact VFX and weakpoint calculation.
/// </summary>
public interface IBossHittable
{
    void OnHit(int rawDamage, Vector3 hitPoint, Vector3 hitNormal, float multiplier = 1f);
}
