using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays the 2 Stratagems, live arrow-key input matching, and cooldown/active timers
/// inside your HUDCanvas -> Stratagem panel.
/// </summary>
public class StratagemHUD : MonoBehaviour
{
    [Header("Optional Manual References (auto-created inside Stratagem panel if left empty)")]
    [SerializeField] private StratagemSystem stratagemSystem;
    [SerializeField] private TMP_Text displayText;

    private readonly StringBuilder _sb = new StringBuilder(256);

    public void Bind(StratagemSystem system)
    {
        stratagemSystem = system;
    }

    private void Start()
    {
        if (stratagemSystem == null)
            stratagemSystem = FindAnyObjectByType<StratagemSystem>();

        if (displayText == null)
            displayText = GetComponentInChildren<TMP_Text>();

        if (displayText == null)
            CreateDisplayText();
    }

    private void CreateDisplayText()
    {
        GameObject textObj = new GameObject("StratagemListText");
        textObj.transform.SetParent(transform, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(28f, -28f);
        rt.sizeDelta = new Vector2(520f, 240f);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;
        tmp.richText = true;
        displayText = tmp;
    }

    private void Update()
    {
        if (stratagemSystem == null || displayText == null) return;

        _sb.Clear();

        if (stratagemSystem.IsMenuOpen)
        {
            string headerColor = stratagemSystem.InputErrorFlash ? "#FF4444" : "#FFD94A";
            _sb.AppendLine($"<color={headerColor}><b>AIR SUPPORTS [Q + ARROWS]</b></color>");
        }
        else
        {
            _sb.AppendLine("<color=#CCCCCC><b>AIR SUPPORT</b> [Hold Q]</color>");
        }

        // 1. Air Strike
        AppendStratagemRow(
            stratagemSystem.airStrikeName,
            stratagemSystem.airStrikeCode,
            stratagemSystem.AirStrikeCooldownRemaining,
            stratagemSystem.AirStrikeIncomingRemaining > 0f
                ? $"<color=#FF5533>INCOMING {stratagemSystem.AirStrikeIncomingRemaining:0.0}s</color>"
                : null
        );

        // 2. Smoke Orbital
        string smokeStatus = null;
        if (stratagemSystem.SmokeActiveRemaining > 0f)
        {
            smokeStatus = stratagemSystem.IsPlayerConcealed
                ? $"<color=#66E0FF>HIDDEN ({stratagemSystem.SmokeActiveRemaining:0.0}s)</color>"
                : $"<color=#AAAAAA>SMOKE ({stratagemSystem.SmokeActiveRemaining:0.0}s)</color>";
        }

        AppendStratagemRow(
            stratagemSystem.smokeName,
            stratagemSystem.smokeCode,
            stratagemSystem.SmokeCooldownRemaining,
            smokeStatus
        );

        displayText.text = _sb.ToString();
    }

    private void AppendStratagemRow(string stratName, StratagemDir[] code, float cdRemaining, string activeOverride)
    {
        bool ready = cdRemaining <= 0f;
        string nameColor = ready ? "#FFFFFF" : "#888888";

        _sb.Append($"<color={nameColor}><b>{stratName}</b></color>  ");

        if (!string.IsNullOrEmpty(activeOverride))
        {
            _sb.AppendLine(activeOverride);
            return;
        }

        if (!ready)
        {
            _sb.AppendLine($"<color=#FF8866>CD {cdRemaining:0.0}s</color>");
            return;
        }

        bool prefixMatches = stratagemSystem.IsMenuOpen && stratagemSystem.MatchesPrefix(code);
        int matchedCount = prefixMatches ? stratagemSystem.CurrentInput.Count : 0;

        for (int i = 0; i < code.Length; i++)
        {
            string arrow = DirToSymbol(code[i]);
            if (i < matchedCount)
                _sb.Append($"<color=#4CFF4C><b>[{arrow}]</b></color> ");
            else if (stratagemSystem.IsMenuOpen && !prefixMatches && stratagemSystem.CurrentInput.Count > 0)
                _sb.Append($"<color=#555555>[{arrow}]</color> ");
            else
                _sb.Append($"<color=#EEEEEE>[{arrow}]</color> ");
        }

        _sb.AppendLine();
    }

    private static string DirToSymbol(StratagemDir dir)
    {
        switch (dir)
        {
            case StratagemDir.Up:    return "^";
            case StratagemDir.Down:  return "v";
            case StratagemDir.Left:  return "<";
            case StratagemDir.Right: return ">";
            default:                 return "?";
        }
    }
}
