using System.Collections;
using System.Collections.Generic;
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
        // Combine gun's rotation with the prefab's rotation so your 90-degree tilt is preserved
        Quaternion bulletRotation = firepos.rotation * bulletprefab.rotation;
        Transform bullet = Instantiate(bulletprefab, firepos.position, bulletRotation);

        // Apply force to the clone
        bullet.GetComponent<Rigidbody>().AddForce(firepos.forward.normalized * bulletSpeed, ForceMode.Impulse);

        // Destroy the clone after 3 seconds
        Destroy(bullet.gameObject, bulletlife);
    }
}
