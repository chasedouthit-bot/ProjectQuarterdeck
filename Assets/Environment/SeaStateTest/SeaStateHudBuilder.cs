using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a readable HUD panel for the sea state test scene.
/// </summary>
public static class SeaStateHudBuilder
{
    const string UiRootName = "UI";
    const float PanelWidth = 540f;
    const float PanelHeight = 168f;

    public static Text EnsureBuilt(Text existingLabel)
    {
        Transform existingPanel = existingLabel != null
            ? existingLabel.transform.parent
            : null;

        if (existingLabel != null && existingPanel != null && existingPanel.name == "HudPanel")
        {
            ConfigureCanvas(existingLabel.GetComponentInParent<Canvas>());
            ApplyLayout(existingPanel.GetComponent<RectTransform>(), existingLabel,
                existingPanel.Find("HelpLabel")?.GetComponent<Text>());
            return existingLabel;
        }

        var oldUi = GameObject.Find(UiRootName);
        if (oldUi != null)
        {
            if (Application.isPlaying)
                Object.Destroy(oldUi);
            else
                Object.DestroyImmediate(oldUi);
        }

        return BuildHud();
    }

    public static Text BuildHud()
    {
        var uiRoot = new GameObject(UiRootName);
        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(uiRoot.transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        ConfigureScaler(scaler);
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("HudPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        panelGo.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.72f);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        var stateLabel = CreateLabel(panelGo.transform, "StateLabel", "Calm Sea", 30, FontStyle.Bold,
            new Color(0.95f, 0.93f, 0.86f));

        var helpLabel = CreateLabel(panelGo.transform, "HelpLabel",
            "F1 = Calm Sea\nF2 = Choppy Sea\nF3 = Storm Sea",
            18, FontStyle.Normal, new Color(0.82f, 0.86f, 0.92f, 1f));
        helpLabel.lineSpacing = 1.2f;

        ApplyLayout(panelRect, stateLabel, helpLabel);
        return stateLabel;
    }

    static void ApplyLayout(RectTransform panelRect, Text stateLabel, Text helpLabel)
    {
        if (panelRect != null)
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        if (stateLabel != null)
        {
            ConfigureLabel(stateLabel, 30, FontStyle.Bold, new Color(0.95f, 0.93f, 0.86f));
            PlaceTopBand(stateLabel.rectTransform, topInset: 14f, height: 42f);
        }

        if (helpLabel != null)
        {
            ConfigureHelpLabel(helpLabel);
            PlaceTopBand(helpLabel.rectTransform, topInset: 62f, height: 88f);
        }
    }

    static Text CreateLabel(
        Transform parent,
        string name,
        string text,
        int fontSize,
        FontStyle style,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.supportRichText = false;
        return label;
    }

    static void PlaceTopBand(RectTransform rect, float topInset, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topInset);
        rect.sizeDelta = new Vector2(-32f, height);
    }

    static void ConfigureHelpLabel(Text help)
    {
        help.text = "F1 = Calm Sea\nF2 = Choppy Sea\nF3 = Storm Sea";
        help.lineSpacing = 1.2f;
        ConfigureLabel(help, 18, FontStyle.Normal, new Color(0.82f, 0.86f, 0.92f, 1f));
    }

    static void ConfigureLabel(Text label, int fontSize, FontStyle style, Color color)
    {
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.alignment = TextAnchor.UpperLeft;
    }

    static void ConfigureCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        ConfigureScaler(canvas.GetComponent<CanvasScaler>());
    }

    static void ConfigureScaler(CanvasScaler scaler)
    {
        if (scaler == null)
            return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }
}
