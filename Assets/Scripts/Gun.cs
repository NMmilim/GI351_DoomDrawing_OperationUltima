using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    Transform bulletprefab;
    Transform firepos;
    float bulletlife = 3;
    public float bulletSpeed = 20f;
    void Start()  { }
    void Update() 
    { 
    if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }
    void Shoot()
    {
                                                                     //quaternion.identity 
        Instantiate(bulletprefab, firepos.position, firepos.rotation);
        bulletprefab.GetComponent<Rigidbody>().AddForce(firepos.forward.normalized * bulletSpeed, ForceMode.Impulse);
        // Destroy the bullet after a certain time to prevent clutter
        Destroy(bulletprefab.gameObject, bulletlife);
    }
}
