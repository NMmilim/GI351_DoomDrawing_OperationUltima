using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AwesomeAnimationScript : MonoBehaviour
{
    [Header("Animators")]
    public Animator anim1; // Arms Animator
    public Animator anim2; // Rifle Animator

    [Header("Shooting & Raycast")]
    public Transform mainCamera;      // Drag your Main Camera here
    public Transform firePoint;       // Drag your FirePoint (muzzle tip) here
    public GameObject muzzleFlashPrefab; // Optional muzzle flash particle
    public float maxRange = 100f;
    public float fireRate = 0.12f;

    private float nextFireTime = 0f;

    void Start()
    {
        if (mainCamera == null && Camera.main != null)
            mainCamera = Camera.main.transform;
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        // 1. Sprinting (Hold Left Shift)
        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        anim1.SetBool("isSprinting", isSprinting);
        anim2.SetBool("isSprinting", isSprinting);

        // 2. Shooting (Hold or Click Left Mouse Button)
        if (Mouse.current.leftButton.isPressed && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        // 3. Reloading (Press R)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            anim1.SetTrigger("Reload");
            anim2.SetTrigger("Reload");
        }
    }

    void Shoot()
    {
        // Play Shoot animations on both Arms and Rifle
        anim1.SetTrigger("Shoot");
        anim2.SetTrigger("Shoot");

        // Spawn Muzzle Flash at the gun barrel tip
        if (muzzleFlashPrefab != null && firePoint != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation, firePoint);
            Destroy(flash, 0.1f);
        }

        // Fire Raycast from Main Camera ignoring anything tagged "Player"
        if (mainCamera != null && FireRaycastIgnoringPlayer(mainCamera, maxRange, out RaycastHit hit))
        {
            Debug.Log("Hit: " + hit.collider.name + " at " + hit.point);
            // Add your bullet damage or impact effect code here using 'hit'
        }
    }

    bool FireRaycastIgnoringPlayer(Transform cam, float range, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, range, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Player"))
                continue;

            validHit = hit;
            return true;
        }

        validHit = default;
        return false;
    }
}
