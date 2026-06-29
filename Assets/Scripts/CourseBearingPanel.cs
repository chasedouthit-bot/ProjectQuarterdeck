using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Prototype Course / Bearing instrument. Builds its own UI, handles compass clicks,
/// and routes ordered headings to <see cref="ShipMovementPrototype"/>.
/// </summary>
public class CourseBearingPanel : MonoBehaviour
{
    const float CompassDiameter = 280f;
    const float LabelRadius = 118f;
    const float DegreeLabelRadius = 96f;

    static Sprite _circleSprite;

    [SerializeField] RectTransform compassDisc;
    [SerializeField] RectTransform headingPointer;
    [SerializeField] Text currentHeadingText;
    [SerializeField] Text orderedHeadingText;
    [SerializeField] Text currentTurnRateText;

    ShipMovementPrototype _shipMovement;
    float _orderedHeadingDegrees;
    bool _hasOrderedHeading;

    public bool IsBuilt => compassDisc != null && headingPointer != null;

    public void Initialize(
        RectTransform disc,
        RectTransform pointer,
        Text currentHeading,
        Text orderedHeading,
        Text turnRate,
        ShipMovementPrototype shipMovement)
    {
        compassDisc = disc;
        headingPointer = pointer;
        currentHeadingText = currentHeading;
        orderedHeadingText = orderedHeading;
        currentTurnRateText = turnRate;
        _shipMovement = shipMovement;

        if (_hasOrderedHeading)
            SetPointerRotation(_orderedHeadingDegrees);
    }

    public void BindShipMovement(ShipMovementPrototype shipMovement)
    {
        _shipMovement = shipMovement;
    }

    public void RefreshStatus()
    {
        if (!IsBuilt)
            return;

        if (_shipMovement != null && _shipMovement.IsFollowingOrderedHeading)
        {
            _orderedHeadingDegrees = _shipMovement.TargetHeadingDegrees;
            _hasOrderedHeading = true;
            SetPointerRotation(_orderedHeadingDegrees);
        }

        float currentHeading = _shipMovement != null
            ? _shipMovement.CurrentHeadingDegrees
            : 0f;

        currentHeadingText.text = $"Current Heading:\n{currentHeading:F0}°";
        orderedHeadingText.text = _hasOrderedHeading
            ? $"Ordered Heading:\n{_orderedHeadingDegrees:F0}°"
            : "Ordered Heading:\n—";

        float turnRate = _shipMovement != null
            ? _shipMovement.CurrentTurnRateDegreesPerSecond
            : 0f;
        currentTurnRateText.text = $"Current Turn Rate:\n{turnRate:F1}°/sec";
    }

    public void HandleCompassClick(PointerEventData eventData)
    {
        if (compassDisc == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                compassDisc, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        float radius = compassDisc.rect.width * 0.5f;
        if (localPoint.magnitude > radius)
            return;

        float heading = HeadingFromLocalPoint(localPoint);
        ApplyOrderedHeading(heading);
    }

    void ApplyOrderedHeading(float headingDegrees)
    {
        _orderedHeadingDegrees = NormalizeHeading(headingDegrees);
        _hasOrderedHeading = true;

        SetPointerRotation(_orderedHeadingDegrees);

        if (_shipMovement != null)
            _shipMovement.SetTargetHeading(_orderedHeadingDegrees);

        Debug.Log($"Ordered Heading:\n{_orderedHeadingDegrees:F0}°");

        RefreshStatus();
    }

    void SetPointerRotation(float headingDegrees)
    {
        if (headingPointer == null)
            return;

        headingPointer.localRotation = Quaternion.Euler(0f, 0f, -headingDegrees);
    }

    public static float HeadingFromLocalPoint(Vector2 localPoint)
    {
        float heading = Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg;
        return NormalizeHeading(heading);
    }

    static float NormalizeHeading(float degrees)
    {
        degrees %= 360f;
        if (degrees < 0f)
            degrees += 360f;
        return degrees;
    }

    /// <summary>
    /// Builds the prototype compass UI under the given panel root.
    /// </summary>
    public static CourseBearingPanel Build(
        GameObject panelRoot,
        CaptainCommandManager manager,
        ShipMovementPrototype shipMovement)
    {
        if (panelRoot == null || manager == null)
            return null;

        ClearPlaceholderContent(panelRoot.transform);

        var panel = panelRoot.GetComponent<CourseBearingPanel>();
        if (panel == null)
            panel = panelRoot.AddComponent<CourseBearingPanel>();

        if (panel.IsBuilt)
        {
            panel.BindShipMovement(shipMovement);
            return panel;
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var circleSprite = GetCircleSprite();

        CreateTitle(panelRoot.transform, "COURSE / BEARING", font, 32, new Vector2(0f, -40f));

        var compassRoot = CreateRect("CompassRoot", panelRoot.transform);
        SetRect(compassRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(CompassDiameter, CompassDiameter), new Vector2(0f, 20f));

        var discGo = CreateImageObject("CompassDisc", compassRoot, circleSprite, Color.white);
        var discRect = discGo.GetComponent<RectTransform>();
        StretchFull(discRect);

        var clickHandler = discGo.AddComponent<CompassClickRelay>();
        clickHandler.Panel = panel;

        CreateCardinalLabel(compassRoot, "N", font, 22, 0f);
        CreateCardinalLabel(compassRoot, "E", font, 22, 90f);
        CreateCardinalLabel(compassRoot, "S", font, 22, 180f);
        CreateCardinalLabel(compassRoot, "W", font, 22, 270f);

        for (int i = 0; i < 8; i++)
            CreateDegreeLabel(compassRoot, i * 45, font, 16);

        var pointerRoot = CreateRect("HeadingPointer", compassRoot);
        StretchFull(pointerRoot);
        var pointerImage = CreateImageObject("PointerShape", pointerRoot, null,
            new Color(1f, 0.85f, 0.1f, 1f));
        var pointerRect = pointerImage.GetComponent<RectTransform>();
        pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        pointerRect.pivot = new Vector2(0.5f, 0f);
        pointerRect.sizeDelta = new Vector2(8f, CompassDiameter * 0.42f);
        pointerRect.anchoredPosition = Vector2.zero;

        var statusColumn = CreateVerticalColumn(panelRoot.transform, "StatusColumn",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(520f, 110f), new Vector2(0f, 88f), 8f);

        var currentHeading = CreateStatusLabel(statusColumn.transform, "Current Heading:\n—", font);
        var orderedHeading = CreateStatusLabel(statusColumn.transform, "Ordered Heading:\n—", font);
        var turnRate = CreateStatusLabel(statusColumn.transform, "Current Turn Rate:\n0.0°/sec", font);

        EnsureBackButton(panelRoot.transform, manager, font);

        panel.Initialize(
            discRect,
            pointerRoot,
            currentHeading,
            orderedHeading,
            turnRate,
            shipMovement);

        return panel;
    }

    static void ClearPlaceholderContent(Transform panelTransform)
    {
        for (int i = panelTransform.childCount - 1; i >= 0; i--)
        {
            var child = panelTransform.GetChild(i);
            if (child.name == "Back" || child.name == "CompassRoot" || child.name == "StatusColumn")
                continue;

            if (child.name == "COURSE / BEARING")
                continue;

            Object.DestroyImmediate(child.gameObject);
        }
    }

    static void EnsureBackButton(Transform panelTransform, CaptainCommandManager manager, Font font)
    {
        if (panelTransform.Find("Back") != null)
            return;

        var backButton = CreateButton(panelTransform, "Back", font, 20, 48f);
        SetRect(backButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(160f, 48f), new Vector2(0f, 28f));
        backButton.onClick.AddListener(manager.OnBackPressed);
    }

    static Sprite GetCircleSprite()
    {
        if (_circleSprite != null)
            return _circleSprite;

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circleSprite;
    }

    static void CreateCardinalLabel(Transform parent, string text, Font font, int fontSize, float headingDegrees)
    {
        PlaceCompassLabel(parent, text, font, fontSize, headingDegrees, LabelRadius, FontStyle.Bold,
            new Color(0.95f, 0.92f, 0.86f));
    }

    static void CreateDegreeLabel(Transform parent, int degrees, Font font, int fontSize)
    {
        PlaceCompassLabel(parent, $"{degrees}°", font, fontSize, degrees, DegreeLabelRadius, FontStyle.Normal,
            new Color(0.75f, 0.75f, 0.75f));
    }

    static void PlaceCompassLabel(
        Transform parent, string text, Font font, int fontSize,
        float headingDegrees, float radius, FontStyle style, Color color)
    {
        var radians = headingDegrees * Mathf.Deg2Rad;
        var offset = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;

        var go = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(48f, 28f), offset);
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
        layout.preferredHeight = 32f;
        return label;
    }

    static GameObject CreateImageObject(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
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

    sealed class CompassClickRelay : MonoBehaviour, IPointerClickHandler
    {
        public CourseBearingPanel Panel;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Panel != null)
                Panel.HandleCompassClick(eventData);
        }
    }
}
