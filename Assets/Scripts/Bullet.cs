using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Tooltip("Base damage dealt to target on impact.")]
    public int damage = 35;

    private Collider _bulletCol;

    private void Awake()
    {
        _bulletCol = GetComponent<Collider>();
        IgnorePlayerColliders();
    }

    private void Start()
    {
        IgnorePlayerColliders();
    }

    public void IgnorePlayerColliders()
    {
        if (_bulletCol == null) _bulletCol = GetComponent<Collider>();
        if (_bulletCol == null) return;

        PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
        if (pm != null)
        {
            Collider[] playerCols = pm.GetComponentsInChildren<Collider>(true);
            foreach (Collider c in playerCols)
            {
                if (c != null && c.enabled && _bulletCol != null && _bulletCol.enabled)
                {
                    Physics.IgnoreCollision(_bulletCol, c, true);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
        Vector3 hitNormal = collision.contactCount > 0 ? collision.contacts[0].normal : -transform.forward;
        ProcessHit(collision.collider, hitPoint, hitNormal);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other, transform.position, -transform.forward);
    }

    private void ProcessHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hitCollider == null) return;

        // 1. NEVER DAMAGE THE PLAYER WHO SHOT THE BULLET
        if (hitCollider.GetComponentInParent<PlayerMovement>() != null ||
            hitCollider.GetComponentInParent<PlayerHealth>() != null ||
            hitCollider.CompareTag("Player"))
        {
            return;
        }

        // 2. Check for Monster weakpoint
        IBossHittable weakpoint = hitCollider.GetComponentInParent<IBossHittable>();
        if (weakpoint != null)
        {
            weakpoint.OnHit(damage, hitPoint, hitNormal);
            Destroy(gameObject);
            return;
        }

        // 3. Fallback to generic IDamageable (strictly non-player)
        IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
        if (damageable != null && !(damageable is PlayerHealth))
        {
            damageable.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
