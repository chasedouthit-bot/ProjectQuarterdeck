using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the placeholder Sail Orders instrument UI at runtime or from the editor upgrade menu.
/// </summary>
public static class SailOrdersInstrumentBuilder
{
    public static SailOrdersInstrumentView Build(GameObject panel, CaptainCommandManager manager)
    {
        if (panel == null || manager == null)
            return null;

        ClearPlaceholderContent(panel.transform);

        var instrument = panel.GetComponent<SailOrdersInstrumentView>();
        if (instrument == null)
            instrument = panel.AddComponent<SailOrdersInstrumentView>();

        if (instrument.IsBuilt)
            return instrument;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateTitle(panel.transform, "SAIL ORDERS", font, 32, new Vector2(0f, -40f));

        var buttonColumn = CreateVerticalColumn(panel.transform, "OrderButtonColumn",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(420f, 300f), new Vector2(0f, -120f), 8f);

        var strikeBtn = CreateButton(buttonColumn.transform, "Strike Sail", font, 18, 40f);
        var heaveBtn = CreateButton(buttonColumn.transform, "Heave To", font, 18, 40f);
        var stormBtn = CreateButton(buttonColumn.transform, "Storm Sail", font, 18, 40f);
        var battleBtn = CreateButton(buttonColumn.transform, "Battle Sail", font, 18, 40f);
        var cruisingBtn = CreateButton(buttonColumn.transform, "Cruising Sail", font, 18, 40f);
        var fullBtn = CreateButton(buttonColumn.transform, "Full Sail", font, 18, 40f);

        var statusColumn = CreateVerticalColumn(panel.transform, "StatusColumn",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(520f, 120f), new Vector2(0f, 92f), 10f);

        var currentOrderText = CreateStatusLabel(statusColumn.transform, "Current Sail Order:\n—", font);
        var targetSpeedText = CreateStatusLabel(statusColumn.transform, "Target Speed:\n—", font);
        var currentSpeedText = CreateStatusLabel(statusColumn.transform, "Current Speed:\n0.0 knots", font);

        EnsureBackButton(panel.transform, manager, font);

        strikeBtn.onClick.AddListener(manager.SelectStrikeSail);
        heaveBtn.onClick.AddListener(manager.SelectHeaveTo);
        stormBtn.onClick.AddListener(manager.SelectStormSail);
        battleBtn.onClick.AddListener(manager.SelectBattleSail);
        cruisingBtn.onClick.AddListener(manager.SelectCruisingSail);
        fullBtn.onClick.AddListener(manager.SelectFullSail);

        instrument.Initialize(
            strikeBtn, heaveBtn, stormBtn, battleBtn, cruisingBtn, fullBtn,
            currentOrderText, targetSpeedText, currentSpeedText);

        return instrument;
    }

    static void ClearPlaceholderContent(Transform panelTransform)
    {
        for (int i = panelTransform.childCount - 1; i >= 0; i--)
        {
            var child = panelTransform.GetChild(i);
            if (child.name == "Back" || child.name == "OrderButtonColumn" || child.name == "StatusColumn")
                continue;

            if (child.name == "SAIL ORDERS")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    static void EnsureBackButton(Transform panelTransform, CaptainCommandManager manager, Font font)
    {
        var existing = panelTransform.Find("Back");
        if (existing != null)
            return;

        var backButton = CreateButton(panelTransform, "Back", font, 20, 48f);
        SetRect(backButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(160f, 48f), new Vector2(0f, 28f));
        backButton.onClick.AddListener(manager.OnBackPressed);
    }

    static GameObject CreateVerticalColumn(
        Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 size, Vector2 position, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);

        var layout = go.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, size, position);
        return go;
    }

    static Button CreateButton(Transform parent, string label, Font font, int fontSize, float height)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.28f, 0.26f, 0.22f, 1f);

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.42f, 0.38f, 0.30f);
        colors.pressedColor = new Color(0.20f, 0.18f, 0.16f);
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

    static Text CreateStatusLabel(Transform parent, string text, Font font)
    {
        var go = new GameObject(text.Split('\n')[0], typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = 20;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.88f, 0.86f, 0.78f);
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 34f;
        return label;
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
