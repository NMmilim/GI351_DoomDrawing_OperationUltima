using Unity.Entities;
using Unity.Physics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
public class PlayerHUD : MonoBehaviour
{
    [Header("UI References (drag from HUDCanvas)")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image damageFlashImage;

        [Header("Damage Flash Settings")]
        [SerializeField] private float flashDuration = 0.4f;
        [SerializeField] private float maxFlashAlpha = 0.35f;
    

        private PlayerHealth _playerHealth;
    private PlayerMovement _playerMovement;
    private float _flashTimer;
    void Start()
    {
        // Auto-find the live Player in the scene (no need to drag Scene objects into a Prefab!)
            _playerHealth = FindAnyObjectByType<PlayerHealth>();
        _playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += UpdateHealthBar;
            _playerHealth.OnDamaged += TriggerDamageFlash;
            // Sync initial value immediately (never trust which script's Start() ran first!)    
            
                UpdateHealthBar(_playerHealth.CurrentHp, _playerHealth.maxHp);
        }
        if (_playerMovement != null)
        {
            _playerMovement.OnStaminaChanged += UpdateStaminaBar;
            UpdateStaminaBar(_playerMovement.stamina, _playerMovement.maxStamina);
        }
    }
    private void OnDestroy()
    {
        // Always unsubscribe (-=) when UI is destroyed to prevent memory leaks / errors!   
        
            if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthBar;
            _playerHealth.OnDamaged -= TriggerDamageFlash;
        }
        if (_playerMovement != null)
        {
            _playerMovement.OnStaminaChanged -= UpdateStaminaBar;
        }
    }
    private void Update()
    {
        // Fade the red damage flash smoothly back to 0 alpha                          
        if (damageFlashImage != null && _flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            Color c = damageFlashImage.color;
            c.a = Mathf.Clamp01(_flashTimer / flashDuration) * maxFlashAlpha;
            damageFlashImage.color = c;
        }
    }
    private void UpdateHealthBar(float current, float max)
    {
        if (hpSlider != null && max > 0)
            hpSlider.value = (float)current / max; // Cast to float! int/int would drop decimals!    
    
         }
    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaSlider != null && max > 0f)
            staminaSlider.value = current / max;
    }

    private void TriggerDamageFlash()
    {
        _flashTimer = flashDuration;
    }
}
