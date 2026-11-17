using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal Screen HUD: Shows top Boss health bar, "You won" upon victory, and death banner upon player death.
/// Keeps the screen clean and free of unnecessary text clutter.
/// </summary>
public class BossUI : MonoBehaviour
{
    public static BossUI Instance { get; private set; }

    [Header("Target Boss")]
    public BossHealth targetBoss;

    // UI Elements
    private GameObject _hudRoot;
    private Slider     _healthSlider;
    private Slider     _delayedSlider;
    private Text       _nameText;
    private Text       _bannerText;
    private Image      _fillImage;

    private float _chipTimer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        BuildBossCanvas();
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
            _nameText.text = targetBoss.bossName;
            HandleHealthChanged(targetBoss.CurrentHp, targetBoss.MaxHp);
        }
    }

    private void OnDestroy()
    {
        if (targetBoss != null)
        {
            targetBoss.OnHealthChanged -= HandleHealthChanged;
            targetBoss.OnEnraged       -= HandleEnraged;
            targetBoss.OnDefeated      -= HandleDefeated;
        }
    }

    private void Update()
    {
        if (_healthSlider == null || _delayedSlider == null) return;

        // Smoothly catch up the delayed yellow damage bar
        if (_delayedSlider.value > _healthSlider.value)
        {
            _chipTimer += Time.deltaTime;
            if (_chipTimer > 0.4f)
            {
                _delayedSlider.value = Mathf.MoveTowards(_delayedSlider.value, _healthSlider.value, Time.deltaTime * 0.4f);
            }
        }
        else
        {
            _chipTimer = 0f;
            _delayedSlider.value = _healthSlider.value;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        float ratio = (float)current / max;
        if (_healthSlider != null) _healthSlider.value = ratio;
    }

    private void HandleEnraged()
    {
        if (_fillImage != null) _fillImage.color = new Color(1f, 0.15f, 0f);
        if (_nameText != null) _nameText.text = $"{targetBoss.bossName} [ENRAGED]";
    }

    private void HandleDefeated()
    {
        if (_bannerText != null)
        {
            _bannerText.gameObject.SetActive(true);
            _bannerText.fontSize = 54;
            _bannerText.text = "You won";
            _bannerText.color = new Color(1f, 0.85f, 0.2f);
        }
    }

    public void ShowDefeatBanner()
    {
        if (_bannerText != null)
        {
            _bannerText.gameObject.SetActive(true);
            _bannerText.fontSize = 38;
            _bannerText.text = "YOU DIED\nPRESS R TO RESTART";
            _bannerText.color = new Color(0.9f, 0.1f, 0.1f);
        }
    }

    // -----------------------------------------------------------------
    //  Procedural UI Canvas Construction
    // -----------------------------------------------------------------
    private void BuildBossCanvas()
    {
        GameObject canvasObj = new GameObject("BossHUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        _hudRoot = new GameObject("BossBarContainer");
        _hudRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = _hudRoot.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -25f);
        rt.sizeDelta        = new Vector2(650f, 24f);

        // Name Text (minimal above bar)
        GameObject nameObj = new GameObject("BossNameText");
        nameObj.transform.SetParent(_hudRoot.transform, false);
        _nameText = nameObj.AddComponent<Text>();
        _nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _nameText.fontSize = 16;
        _nameText.alignment = TextAnchor.MiddleCenter;
        _nameText.color = new Color(0.9f, 0.9f, 0.9f);
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 1f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.anchoredPosition = new Vector2(0f, 14f);
        nameRT.sizeDelta = new Vector2(0f, 24f);

        // Delayed Bar (Yellow chip damage)
        _delayedSlider = CreateBar("DelayedBar", _hudRoot.transform, new Color(1f, 0.85f, 0.2f));

        // Main Health Bar (Red)
        _healthSlider  = CreateBar("HealthBar", _hudRoot.transform, new Color(0.85f, 0.15f, 0.15f));
        _fillImage     = _healthSlider.fillRect.GetComponent<Image>();

        // Center Banner Text: Only used for "You won" or "YOU DIED\nPRESS R TO RESTART"
        GameObject bannerObj = new GameObject("CenterBannerText");
        bannerObj.transform.SetParent(canvasObj.transform, false);
        _bannerText = bannerObj.AddComponent<Text>();
        _bannerText.font = _nameText.font;
        _bannerText.fontSize = 48;
        _bannerText.alignment = TextAnchor.MiddleCenter;
        RectTransform bannerRT = bannerObj.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRT.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRT.sizeDelta = new Vector2(800f, 150f);
        bannerObj.SetActive(false);
    }

    private Slider CreateBar(string name, Transform parent, Color fillColor)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform sliderRT = sliderObj.GetComponent<RectTransform>();
        sliderRT.anchorMin = Vector2.zero;
        sliderRT.anchorMax = Vector2.one;
        sliderRT.sizeDelta = Vector2.zero;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);
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
        fillImg.color = fillColor;
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;

        slider.fillRect = fillRT;
        return slider;
    }
}
