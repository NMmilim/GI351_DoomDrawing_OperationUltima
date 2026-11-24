using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects your HUDCanvas Prefab's Boss HP bar, delayed chip-damage bar, name text, and Win/Lose banners
/// to BossHealth events. No procedural Canvas/UI is generated in code.
/// </summary>
public class BossUI : MonoBehaviour
{
    public static BossUI Instance { get; private set; }

    [Header("Target Boss (leave empty to auto-find in scene)")]
    public BossHealth targetBoss;

    [Header("HUDCanvas References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider delayedSlider;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject winBanner;
    [SerializeField] private GameObject loseBanner;

    [Header("Bar Style")]
    [SerializeField] private Color enragedBarColor = new Color(1f, 0.15f, 0f);
    [SerializeField] private float chipDelay = 0.4f;
    [SerializeField] private float chipSpeed = 0.4f;

    private Image _fillImage;
    private float _chipTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (healthSlider != null && healthSlider.fillRect != null)
            _fillImage = healthSlider.fillRect.GetComponent<Image>();

        // Hide win/lose banners at start so you can leave them visible while editing HUDCanvas
        if (winBanner != null) winBanner.SetActive(false);
        if (loseBanner != null) loseBanner.SetActive(false);
    }

    private void Start()
    {
        if (targetBoss == null)
            targetBoss = FindAnyObjectByType<BossHealth>();

        if (targetBoss != null)
        {
            targetBoss.OnHealthChanged += HandleHealthChanged;
            targetBoss.OnEnraged       += HandleEnraged;
            targetBoss.OnDefeated      += HandleDefeated;

            if (nameText != null) nameText.text = targetBoss.bossName;
            HandleHealthChanged(targetBoss.CurrentHp, targetBoss.MaxHp);
            if (delayedSlider != null && healthSlider != null)
                delayedSlider.value = healthSlider.value;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (targetBoss != null)
        {
            targetBoss.OnHealthChanged -= HandleHealthChanged;
            targetBoss.OnEnraged       -= HandleEnraged;
            targetBoss.OnDefeated      -= HandleDefeated;
        }
    }

    private void Update()
    {
        if (healthSlider == null || delayedSlider == null) return;

        // Smoothly catch up the delayed yellow damage bar
        if (delayedSlider.value > healthSlider.value)
        {
            _chipTimer += Time.deltaTime;
            if (_chipTimer > chipDelay)
            {
                delayedSlider.value = Mathf.MoveTowards(delayedSlider.value, healthSlider.value, Time.deltaTime * chipSpeed);
            }
        }
        else
        {
            _chipTimer = 0f;
            delayedSlider.value = healthSlider.value;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (healthSlider != null && max > 0)
            healthSlider.value = (float)current / max;
    }

    private void HandleEnraged()
    {
        if (_fillImage != null) _fillImage.color = enragedBarColor;
        if (nameText != null && targetBoss != null)
            nameText.text = $"{targetBoss.bossName} [ENRAGED]";
    }

    private void HandleDefeated()
    {
        if (winBanner != null) winBanner.SetActive(true);
    }

    public void ShowDefeatBanner()
    {
        if (loseBanner != null) loseBanner.SetActive(true);
    }
}
