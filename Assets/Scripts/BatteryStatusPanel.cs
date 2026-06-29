using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prototype Battery Status instrument UI. Gameplay lives in <see cref="CannonBattery"/>.
/// </summary>
public class BatteryStatusPanel : MonoBehaviour
{
    static readonly Color ReadyColor = new Color(0.22f, 0.62f, 0.28f, 1f);
    static readonly Color ReloadingColor = new Color(0.72f, 0.18f, 0.18f, 1f);
    static readonly Color UnmannedColor = new Color(0.42f, 0.42f, 0.42f, 1f);
    static readonly Color FireAllNormalColor = new Color(0.28f, 0.26f, 0.22f, 1f);

    [SerializeField] Text portStatusText;
    [SerializeField] Text starboardStatusText;
    [SerializeField] Button[] portCannonButtons = new Button[CannonBattery.CannonsPerSide];
    [SerializeField] Button[] starboardCannonButtons = new Button[CannonBattery.CannonsPerSide];
    [SerializeField] Button fireAllPortButton;
    [SerializeField] Button fireAllStarboardButton;

    CannonBattery _battery;

    public bool IsBuilt => portStatusText != null && portCannonButtons[0] != null;

    public void Initialize(
        CannonBattery battery,
        Text portStatus,
        Text starboardStatus,
        Button[] portButtons,
        Button[] starboardButtons,
        Button fireAllPort,
        Button fireAllStarboard)
    {
        _battery = battery;
        portStatusText = portStatus;
        starboardStatusText = starboardStatus;
        portCannonButtons = portButtons;
        starboardCannonButtons = starboardButtons;
        fireAllPortButton = fireAllPort;
        fireAllStarboardButton = fireAllStarboard;

        if (_battery != null)
        {
            _battery.StateChanged -= RefreshDisplay;
            _battery.StateChanged += RefreshDisplay;
        }

        RefreshDisplay();
    }

    void OnDestroy()
    {
        if (_battery != null)
            _battery.StateChanged -= RefreshDisplay;
    }

    public void RefreshDisplay()
    {
        if (!IsBuilt || _battery == null)
            return;

        UpdateSideStatus(BatterySide.Port, portStatusText, portCannonButtons);
        UpdateSideStatus(BatterySide.Starboard, starboardStatusText, starboardCannonButtons);
    }

    void UpdateSideStatus(BatterySide side, Text statusText, Button[] buttons)
    {
        (int ready, int reloading, int unmanned) = _battery.GetSideCounts(side);
        statusText.text =
            $"Ready: {ready}\nReloading: {reloading}\nUnmanned: {unmanned}";

        for (int i = 0; i < buttons.Length; i++)
        {
            int number = i + 1;
            CannonBattery.Cannon cannon = _battery.FindCannon(side, number);
            ApplyCannonButtonState(buttons[i], cannon);
        }
    }

    static void ApplyCannonButtonState(Button button, CannonBattery.Cannon cannon)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        bool canFire = cannon != null && cannon.CanFire;
        button.interactable = canFire;

        if (image == null || cannon == null)
            return;

        image.color = cannon.CurrentState switch
        {
            CannonState.Ready => ReadyColor,
            CannonState.Reloading => ReloadingColor,
            CannonState.Unmanned => UnmannedColor,
            _ => UnmannedColor
        };
    }

    public static BatteryStatusPanel Build(GameObject panelRoot, CaptainCommandManager manager)
    {
        if (panelRoot == null || manager == null)
            return null;

        ClearPlaceholderContent(panelRoot.transform);

        var panel = panelRoot.GetComponent<BatteryStatusPanel>();
        if (panel == null)
            panel = panelRoot.AddComponent<BatteryStatusPanel>();

        CannonBattery battery = manager.GetComponent<CannonBattery>();
        if (battery == null)
            battery = manager.gameObject.AddComponent<CannonBattery>();

        if (panel.IsBuilt)
        {
            panel.Initialize(
                battery,
                panel.portStatusText,
                panel.starboardStatusText,
                panel.portCannonButtons,
                panel.starboardCannonButtons,
                panel.fireAllPortButton,
                panel.fireAllStarboardButton);
            return panel;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateTitle(panelRoot.transform, "BATTERY STATUS", font, 32, new Vector2(0f, -40f));

        BuildBatteryColumn(
            panelRoot.transform, "PortColumn", "PORT BATTERY", font,
            BatterySide.Port, battery, new Vector2(-190f, 10f),
            out Text portStatus, out Button[] portButtons, out Button fireAllPort);

        BuildBatteryColumn(
            panelRoot.transform, "StarboardColumn", "STARBOARD BATTERY", font,
            BatterySide.Starboard, battery, new Vector2(190f, 10f),
            out Text starboardStatus, out Button[] starboardButtons, out Button fireAllStarboard);

        EnsureBackButton(panelRoot.transform, manager, font);

        panel.Initialize(
            battery,
            portStatus,
            starboardStatus,
            portButtons,
            starboardButtons,
            fireAllPort,
            fireAllStarboard);

        return panel;
    }

    static void BuildBatteryColumn(
        Transform parent,
        string columnName,
        string header,
        Font font,
        BatterySide side,
        CannonBattery battery,
        Vector2 position,
        out Text statusText,
        out Button[] cannonButtons,
        out Button fireAllButton)
    {
        var column = CreateRect(columnName, parent);
        SetRect(column, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 420f), position);

        CreateColumnLabel(column, header, font, 20, new Vector2(0f, 170f), FontStyle.Bold);
        statusText = CreateColumnLabel(column, "Ready: 7\nReloading: 0\nUnmanned: 0", font, 16, new Vector2(0f, 118f), FontStyle.Normal);

        var grid = new GameObject("CannonGrid", typeof(RectTransform), typeof(VerticalLayoutGroup));
        grid.transform.SetParent(column, false);
        var layout = grid.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        SetRect(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(120f, 280f), new Vector2(0f, -20f));

        cannonButtons = new Button[CannonBattery.CannonsPerSide];
        for (int number = 1; number <= CannonBattery.CannonsPerSide; number++)
        {
            int capturedNumber = number;
            var button = CreateCannonButton(grid.transform, number.ToString(), font);
            button.onClick.AddListener(() => battery.TryFire(side, capturedNumber));
            cannonButtons[number - 1] = button;
        }

        string fireAllLabel = side == BatterySide.Port ? "Fire All Port" : "Fire All Starboard";
        fireAllButton = CreateActionButton(column, fireAllLabel, font, 18, 44f);
        SetRect(fireAllButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(220f, 44f), new Vector2(0f, 8f));

        if (side == BatterySide.Port)
            fireAllButton.onClick.AddListener(battery.FireAllPort);
        else
            fireAllButton.onClick.AddListener(battery.FireAllStarboard);
    }

    static void ClearPlaceholderContent(Transform panelTransform)
    {
        for (int i = panelTransform.childCount - 1; i >= 0; i--)
        {
            var child = panelTransform.GetChild(i);
            if (child.name == "Back" || child.name == "PortColumn" || child.name == "StarboardColumn")
                continue;

            if (child.name == "BATTERY STATUS")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    static void EnsureBackButton(Transform panelTransform, CaptainCommandManager manager, Font font)
    {
        if (panelTransform.Find("Back") != null)
            return;

        var backButton = CreateActionButton(panelTransform, "Back", font, 20, 48f);
        SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(160f, 48f), new Vector2(0f, 28f));
        backButton.onClick.AddListener(manager.OnBackPressed);
    }

    static Text CreateColumnLabel(
        Transform parent, string text, Font font, int fontSize, Vector2 position, FontStyle style)
    {
        var go = new GameObject(text.Split('\n')[0], typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.88f, 0.86f, 0.78f);
        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(260f, 72f), position);
        return label;
    }

    static Button CreateCannonButton(Transform parent, string label, Font font)
    {
        var button = CreateActionButton(parent, label, font, 18, 34f);
        button.GetComponent<Image>().color = ReadyColor;
        return button;
    }

    static Button CreateActionButton(Transform parent, string label, Font font, int fontSize, float height)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = FireAllNormalColor;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.42f, 0.38f, 0.30f);
        colors.pressedColor = new Color(0.20f, 0.18f, 0.16f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.8f);
        button.colors = colors;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.92f, 0.86f);
        StretchFull(text.rectTransform);

        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        return button;
    }

    static void CreateTitle(Transform parent, string text, Font font, int size, Vector2 offset)
    {
        if (parent.Find(text) != null)
            return;

        var go = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.92f, 0.88f, 0.78f);
        SetRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(800f, 56f), offset);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
    }
}
