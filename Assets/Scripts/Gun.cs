using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class Gun : MonoBehaviour
{
    public Transform bulletprefab;
    public Transform firepos;
    public float bulletlife = 3f;
    public float bulletSpeed = 20f;


    public int maxammo = 30;
    public int ammo;
    public float reloadTime = 2f;
    private bool isReloading = false;
    public event System.Action<int, int> OnAmmoChanged;    
    public event System.Action<bool> OnReloadStateChanged;
    void Start()
    {
        ammo = maxammo;
        OnAmmoChanged?.Invoke(ammo, maxammo);
    }
    void Update() 
    {
       
        bool shootable = (ammo > 0 && !isReloading);
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame&&shootable)
        {
            Shoot();
        }
        bool reloadable = (ammo < maxammo);
      
        bool rPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        if (!isReloading && (ammo <= 0 || (rPressed && ammo < maxammo)))
        {
            StartCoroutine(ReloadRoutine());
        }

    }

    void Shoot()
    {

        
        if (bulletprefab == null || firepos == null) return;
ammo--;
        OnAmmoChanged?.Invoke(ammo, maxammo);

        // Combine gun's rotation with the prefab's rotation so 90-degree tilt is preserved
        Quaternion bulletRotation = firepos.rotation * bulletprefab.rotation;
        Transform bullet = Instantiate(bulletprefab, firepos.position, bulletRotation);
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
            rb.AddForce(firepos.forward.normalized * bulletSpeed, ForceMode.Impulse);
        }

        Destroy(bullet.gameObject, bulletlife);
    }
     IEnumerator ReloadRoutine()
    {
        isReloading = true;
        Debug.Log("Reloading...");
        OnReloadStateChanged?.Invoke(true);
        yield return new WaitForSeconds(reloadTime); // Pauses HERE for 2 seconds!                                 

        ammo = maxammo;
        
        isReloading = false;
        Debug.Log("Reload complete!");
        OnReloadStateChanged?.Invoke(false);
        OnAmmoChanged?.Invoke(ammo, maxammo);
    }
}
