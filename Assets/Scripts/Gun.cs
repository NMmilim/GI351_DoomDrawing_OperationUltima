using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class Gun : MonoBehaviour
{
    public Transform bulletprefab;
    public Transform firepos;
    public float bulletlife = 3f;
    public float bulletSpeed = 960f;


    public int maxammo = 20;
    public int ammo;
    public int ammomax;
    public float tacticalReloadTime = 3.25f;
    public float emptyReloadTime = 4f;

    private bool isReloading = false;
    public event System.Action<int, int> OnAmmoChanged;    
    public event System.Action<bool> OnReloadStateChanged;
    public event System.Action<bool> OnReloadStarted;
    public event System.Action OnShot;
    private PlayerMovement playerMovement;
    void Start()
    {
        ammo = maxammo;
        OnAmmoChanged?.Invoke(ammo, maxammo);
    }
    void Update() 
    {
        bool isSprinting = playerMovement != null &&
                           playerMovement.isSprinting;

        bool wallBlocked = IsWallBlockingShot();

        bool shootable = ammo > 0 &&
                         !isReloading &&
                         !isSprinting &&
                         !wallBlocked &&
                         Time.time >= nextFireTime;

        if (Mouse.current != null)
        {
            bool firePressed = fireMode == FireMode.Automatic
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;

            if (firePressed && shootable)
            {
                float secondsPerShot = 60f / roundsPerMinute;
                nextFireTime = Time.time + secondsPerShot;

                Shoot();
            }
        }

        bool rPressed = Keyboard.current != null &&
                        Keyboard.current.rKey.wasPressedThisFrame;

        if (!isReloading &&
            (ammo <= 0 || (rPressed && ammo < maxammo)))
        {
            StartCoroutine(ReloadRoutine());
        }
    }

    private void Shoot()
    {
        if (bulletprefab == null || firepos == null)
        {
            return;
        }

        ammo--;
        OnAmmoChanged?.Invoke(ammo, maxammo);
        OnShot?.Invoke();

        Quaternion bulletRotation = firepos.rotation * bulletprefab.rotation;
        Transform bullet = Instantiate(
            bulletprefab,
            firepos.position,
            bulletRotation);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound3D("Rifle_Fire", firepos.position);
        }
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
            Vector3 muzzleVelocity =
                firepos.forward.normalized * bulletSpeed;

            rb.AddForce(
                muzzleVelocity,
                ForceMode.VelocityChange);
        }

        Destroy(bullet.gameObject, bulletlife);
    }
     private IEnumerator ReloadRoutine()
    {
        bool wasEmpty = ammo <= 0;

        float currentReloadTime = wasEmpty
            ? emptyReloadTime
            : tacticalReloadTime;

        isReloading = true;

        OnReloadStarted?.Invoke(wasEmpty);
        OnReloadStateChanged?.Invoke(true);

        yield return new WaitForSeconds(currentReloadTime);

        ammo = maxammo;
        isReloading = false;

        OnReloadStateChanged?.Invoke(false);
        OnAmmoChanged?.Invoke(ammo, maxammo);
    }

    private void Awake()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    [Header("Fire Settings")]
    public FireMode fireMode = FireMode.SemiAutomatic;

    [Min(1f)]
    public float roundsPerMinute = 600f;

    private float nextFireTime;
    public enum FireMode
    {
        SemiAutomatic,
        Automatic
    }

    [Header("Wall Check")]
    public LayerMask wallLayers;
    public float wallCheckDistance = 0.4f;
    public float wallCheckRadius = 0.05f;

    [Range(0f, 90f)]
    public float maximumWallAngle = 60f;

    private bool IsWallBlockingShot()
    {
        if (firepos == null)
        {
            return true;
        }

        Vector3 direction = firepos.forward.normalized;

        bool hitSomething = Physics.SphereCast(
            firepos.position,
            wallCheckRadius,
            direction,
            out RaycastHit hit,
            wallCheckDistance,
            wallLayers,
            QueryTriggerInteraction.Ignore);

        if (!hitSomething)
        {
            return false;
        }

        float wallAngle = Vector3.Angle(
            direction,
            -hit.normal);

        return wallAngle <= maximumWallAngle;
    }
}
