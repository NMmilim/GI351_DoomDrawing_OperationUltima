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
    public float tacticalReloadTime = 2f;
    public float emptyReloadTime = 2.5f;

    [Header("Movement Animation")]
    public float sprintSpeedThreshold = 6.5f;

    private float nextFireTime = 0f;
    private Gun gun;
    private bool isReloading;
    private PlayerMovement playerMovement;
    private CharacterController characterController;

    private void Awake()
    {
        gun = GetComponentInParent<Gun>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        characterController = GetComponentInParent<CharacterController>();
    }

    private void OnEnable()
    {
        if (gun == null)
        {
            return;
        }

        gun.OnShot += HandleShot;
        gun.OnReloadStarted += HandleReloadStarted;
        gun.OnReloadStateChanged += HandleReloadStateChanged;
    }

    private void OnDisable()
    {
        if (gun == null)
        {
            return;
        }

        gun.OnShot -= HandleShot;
        gun.OnReloadStarted -= HandleReloadStarted;
        gun.OnReloadStateChanged -= HandleReloadStateChanged;
    }

    void Start()
    {
        if (mainCamera == null && Camera.main != null)
            mainCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            return;
        }

        float currentSpeed = 0f;

        if (characterController != null)
        {
            Vector3 velocity = characterController.velocity;
            velocity.y = 0f;
            currentSpeed = velocity.magnitude;
        }

        bool isSprinting = !isReloading &&
                           playerMovement.isSprinting &&
                           currentSpeed > playerMovement.DefaultSpeed;

        if (anim1 != null)
        {
            anim1.SetFloat("Speed", currentSpeed);
            anim1.SetBool("isSprinting", isSprinting);
        }

        if (anim2 != null)
        {
            anim2.SetFloat("Speed", currentSpeed);
            anim2.SetBool("isSprinting", isSprinting);
        }
    }

    private void HandleReloadStarted(bool wasEmpty)
    {
        string triggerName = wasEmpty
            ? "ReloadEmpty"
            : "ReloadTactical";

        if (anim1 != null)
        {
            anim1.SetTrigger(triggerName);
        }

        if (anim2 != null)
        {
            anim2.SetTrigger(triggerName);
        }
    }

    private void HandleAmmoChanged(int currentAmmo, int maximumAmmo)
    {
        bool isEmpty = currentAmmo <= 0;

        if (anim1 != null)
        {
            anim1.SetBool("IsEmpty", isEmpty);
        }

        if (anim2 != null)
        {
            anim2.SetBool("IsEmpty", isEmpty);
        }
    }

    void Shoot()
    {
        // Don't shoot if reloading
        if (isReloading)
            return;

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

    private void HandleShot()
    {
        if (isReloading)
        {
            return;
        }

        if (anim1 != null)
        {
            anim1.SetTrigger("Shoot");
        }

        if (anim2 != null)
        {
            anim2.SetTrigger("Shoot");
        }
    }

    private void HandleReloadStateChanged(bool reloadState)
    {
        isReloading = reloadState;

        if (reloadState)
        {
            if (anim1 != null)
            {
                anim1.SetBool("isSprinting", false);
            }

            if (anim2 != null)
            {
                anim2.SetBool("isSprinting", false);
            }
        }
    }
}
