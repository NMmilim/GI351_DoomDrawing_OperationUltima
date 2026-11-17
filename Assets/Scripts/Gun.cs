using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public Transform bulletprefab;
    public Transform firepos;
    public float bulletlife = 3f;
    public float bulletSpeed = 20f;

    void Start() { }

    void Update() 
    { 
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletprefab == null || firepos == null) return;

        // Combine gun's rotation with the prefab's rotation so 90-degree tilt is preserved
        Quaternion bulletRotation = firepos.rotation * bulletprefab.rotation;
        Transform bullet = Instantiate(bulletprefab, firepos.position, bulletRotation);

        // Ignore collisions between this bullet and the entire player
        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bulletCol != null)
        {
            Collider[] parentColliders = GetComponentsInParent<Collider>(true);
            foreach (Collider c in parentColliders)
            {
                if (c != null && c.enabled)
                {
                    Physics.IgnoreCollision(bulletCol, c, true);
                }
            }
        }

        // Apply impulse velocity
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(firepos.forward.normalized * bulletSpeed, ForceMode.Impulse);
        }

        Destroy(bullet.gameObject, bulletlife);
    }
}
