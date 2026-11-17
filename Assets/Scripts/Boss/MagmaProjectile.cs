using UnityEngine;

/// <summary>
/// Volcanic projectile spat by Wrath during ranged attacks.
/// Flies forward, explodes on contact, and deals AOE damage.
/// </summary>
public class MagmaProjectile : MonoBehaviour
{
    public float speed = 18f;
    public int   damage = 25;
    public float blastRadius = 3f;
    public float lifetime = 5f;

    private Vector3 _velocity;
    private bool    _exploded;

    public void Init(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        _velocity = dir * speed;
        transform.rotation = Quaternion.LookRotation(dir);
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (_exploded) return;
        transform.position += _velocity * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exploded) return;
        // Ignore boss self-collisions
        if (other.GetComponentInParent<BossHealth>() != null) return;

        Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_exploded) return;
        if (collision.collider.GetComponentInParent<BossHealth>() != null) return;

        Explode();
    }

    private void Explode()
    {
        _exploded = true;

        // Damage Player if in blast radius
        Collider[] hits = Physics.OverlapSphere(transform.position, blastRadius);
        foreach (Collider col in hits)
        {
            PlayerHealth player = col.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
        }

        // Spawn visual burst
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.transform.position = transform.position;
        burst.transform.localScale = Vector3.one * (blastRadius * 0.8f);
        Collider bc = burst.GetComponent<Collider>();
        if (bc != null) DestroyImmediate(bc);

        Renderer r = burst.GetComponent<Renderer>();
        if (r != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material m = new Material(shader) { color = new Color(1f, 0.35f, 0f, 0.8f) };
                r.material = m;
            }
        }

        Destroy(burst, 0.15f);
        Destroy(gameObject);
    }
}
