using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Health, damage receiving, and death handling for the First-Person Hunter.
/// Includes screen damage flash and bottom-left player HUD.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public event System.Action<float, float> OnHealthChanged;
    public event System.Action OnDamaged;
    [Header("Player Health Settings")]
    public int maxHp = 100;
    private int _currentHp;
    private bool _isDead;

    [Header("Death Fade")]
    public float deathFadeDuration = 1.5f;

    [Header("Death UI")]
    [SerializeField]
    private GameObject playerStatusUi;

    public int  CurrentHp => _currentHp;
    public bool IsDead    => _isDead;

    // HUD Elements
    private Slider _playerHealthSlider;
    private Image  _damageFlashOverlay;
    private float  _flashTimer;
    private Image _deathFadeOverlay;
    private Coroutine _deathFadeCoroutine;

    private void Awake()
    {
        _currentHp = maxHp;
        
    }

    private void Update()
    {

        // Fade out damage flash overlay
        if (_damageFlashOverlay != null && _flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            Color c = _damageFlashOverlay.color;
            c.a = Mathf.Clamp01(_flashTimer / 0.4f) * 0.35f;
            _damageFlashOverlay.color = c;
        }

        // Restart on R if dead
        if (_isDead && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || amount <= 0) return;

        _currentHp = Mathf.Max(0, _currentHp - amount);

        if (_playerHealthSlider != null)
            _playerHealthSlider.value = (float)_currentHp / maxHp;

        // Trigger red damage flash
        if (_damageFlashOverlay != null)
        {
            _flashTimer = 0.4f;
            Color c = _damageFlashOverlay.color;
            c.a = 0.35f;
            _damageFlashOverlay.color = c;
        }

        if (_currentHp <= 0)
        {
            Die();
        }
        OnHealthChanged?.Invoke(_currentHp, maxHp);
        OnDamaged?.Invoke();
    }

    private void Die()
    {
        if (_isDead) return;

        _isDead = true;

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        HidePlayerStatusUi();

        BossUI bossUi = FindAnyObjectByType<BossUI>();

        if (bossUi != null)
        {
            bossUi.HideGameplayHud();
        }

        StratagemHUD stratagemHud = FindAnyObjectByType<StratagemHUD>();

        if (stratagemHud != null)
        {
            stratagemHud.gameObject.SetActive(false);
        }

        CreateDeathFadeOverlay();

        if (_deathFadeCoroutine != null)
        {
            StopCoroutine(_deathFadeCoroutine);
        }

        _deathFadeCoroutine = StartCoroutine(FadeToBlack());

        if (bossUi != null)
        {
            bossUi.ShowDefeatBanner();
        }
    }

    private void CreateDeathFadeOverlay()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("DeathFadeCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject overlayObject = new GameObject("DeathFadeOverlay");
        overlayObject.transform.SetParent(canvas.transform, false);
        overlayObject.transform.SetAsFirstSibling();

        _deathFadeOverlay = overlayObject.AddComponent<Image>();
        _deathFadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        _deathFadeOverlay.raycastTarget = false;

        RectTransform rectTransform =
            _deathFadeOverlay.rectTransform;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeToBlack()
    {
        float elapsed = 0f;

        while (elapsed < deathFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha = Mathf.Clamp01(
                elapsed / deathFadeDuration);

            Color color = _deathFadeOverlay.color;
            color.a = alpha;
            _deathFadeOverlay.color = color;

            yield return null;
        }

        Color finalColor = _deathFadeOverlay.color;
        finalColor.a = 1f;
        _deathFadeOverlay.color = finalColor;
    }

    private void BuildPlayerHUD()
    {
        // Check for existing Canvas or create one
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PlayerHUDCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Fullscreen Red Damage Flash
        GameObject flashObj = new GameObject("DamageFlashOverlay");
        flashObj.transform.SetParent(canvas.transform, false);
        _damageFlashOverlay = flashObj.AddComponent<Image>();
        _damageFlashOverlay.color = new Color(0.9f, 0f, 0f, 0f);
        _damageFlashOverlay.raycastTarget = false;
        RectTransform flashRT = flashObj.GetComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.sizeDelta = Vector2.zero;

        // Bottom-left Player Health Container
        GameObject container = new GameObject("PlayerHealthContainer");
        container.transform.SetParent(canvas.transform, false);
        RectTransform cRT = container.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 0f);
        cRT.anchorMax = new Vector2(0f, 0f);
        cRT.pivot     = new Vector2(0f, 0f);
        cRT.anchoredPosition = new Vector2(30f, 30f);
        cRT.sizeDelta        = new Vector2(250f, 25f);

        // Player Health Slider
        GameObject sliderObj = new GameObject("PlayerHealthBar");
        sliderObj.transform.SetParent(container.transform, false);
        _playerHealthSlider = sliderObj.AddComponent<Slider>();
        _playerHealthSlider.minValue = 0f;
        _playerHealthSlider.maxValue = 1f;
        _playerHealthSlider.value    = 1f;
        RectTransform sRT = sliderObj.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero;
        sRT.anchorMax = Vector2.one;
        sRT.sizeDelta = Vector2.zero;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.15f, 0.85f, 0.25f); // Green hunter health
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        _playerHealthSlider.fillRect = fillRT;
    }

    private void HidePlayerStatusUi()
    {
        GameObject playerStatus = GameObject.Find("Player Status");

        if (playerStatus != null)
        {
            playerStatus.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "PlayerHealth: Could not find UI object named 'Player Status'.",
                this);
        }
    }
}
