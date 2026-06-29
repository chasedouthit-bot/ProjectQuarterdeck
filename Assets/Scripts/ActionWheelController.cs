using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Press R to toggle the actions wheel overlay.
/// Add this component to any GameObject in the TaylorsDemo scene.
/// CrewShantyController is found automatically if not assigned.
/// </summary>
[DisallowMultipleComponent]
public class ActionWheelController : MonoBehaviour
{
    [Header("References")]
    public CrewShantyController crewShantyController;

    [Header("Wheel Layout")]
    [SerializeField] private float segmentOrbitRadius = 220f;
    [SerializeField] private float segmentWidth = 240f;
    [SerializeField] private float segmentHeight = 160f;

    private Canvas _canvas;
    private GameObject _wheelRoot;
    private bool _isOpen;
    private CursorLockMode _prevLockMode;
    private bool _prevCursorVisible;

    private class Segment
    {
        public string label;
        public Func<bool> isActive;
        public Action onToggle;
        public Image background;
        public Text iconText;
        public Text labelText;
    }

    private readonly List<Segment> _segments = new List<Segment>();

    // Palette — dark nautical blues
    static readonly Color ColBg        = new Color(0.04f, 0.07f, 0.12f, 0.93f);
    static readonly Color ColSegIdle   = new Color(0.10f, 0.16f, 0.24f, 0.95f);
    static readonly Color ColSegActive = new Color(0.08f, 0.36f, 0.56f, 0.95f);
    static readonly Color ColTxtIdle   = new Color(0.78f, 0.86f, 0.94f, 1.00f);
    static readonly Color ColTxtActive = new Color(0.65f, 0.95f, 1.00f, 1.00f);
    static readonly Color ColIconIdle  = new Color(0.45f, 0.52f, 0.60f, 1.00f);
    static readonly Color ColIconOn    = new Color(0.40f, 0.92f, 0.65f, 1.00f);
    static readonly Color ColCenter    = new Color(0.50f, 0.62f, 0.72f, 0.80f);
    static readonly Color ColHint      = new Color(0.38f, 0.44f, 0.52f, 0.70f);

    void Start()
    {
        if (crewShantyController == null)
            crewShantyController = FindObjectOfType<CrewShantyController>();

        EnsureEventSystem();
        BuildCanvas();
        BuildWheelPanel();
        RegisterSegments();
        BuildSegmentButtons();
        SetWheelVisible(false);
    }

    void Update()
    {
        if (WasPressedThisFrame(KeyCode.R))
            ToggleWheel();
        else if (_isOpen && WasPressedThisFrame(KeyCode.Escape))
            ToggleWheel();

        if (_isOpen)
            RefreshVisuals();
    }

    bool WasPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        switch (key)
        {
            case KeyCode.R:      return Keyboard.current.rKey.wasPressedThisFrame;
            case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
            default:             return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }

    void ToggleWheel()
    {
        _isOpen = !_isOpen;
        SetWheelVisible(_isOpen);
    }

    void SetWheelVisible(bool visible)
    {
        _wheelRoot.SetActive(visible);
        if (visible)
        {
            _prevLockMode = Cursor.lockState;
            _prevCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = _prevLockMode;
            Cursor.visible = _prevCursorVisible;
        }
    }

    // ── segment registration ──────────────────────────────────────────────────

    void RegisterSegments()
    {
        _segments.Add(new Segment
        {
            label = "Sea Shanties",
            isActive = () => crewShantyController != null && crewShantyController.IsPlaying,
            onToggle = () => { if (crewShantyController != null) crewShantyController.ToggleShanty(); }
        });

        // Add future wheel options here by appending more _segments.Add(...)
    }

    // ── UI construction ───────────────────────────────────────────────────────

    void EnsureEventSystem()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGO.AddComponent<StandaloneInputModule>();
#endif
            return;
        }

#if ENABLE_INPUT_SYSTEM
        // If the scene already has an EventSystem with the legacy module, swap it out
        var legacyModule = es.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            Destroy(legacyModule);
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#endif
    }

    void BuildCanvas()
    {
        var go = new GameObject("ActionWheelCanvas");
        go.transform.SetParent(transform, false);
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 150;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        go.AddComponent<GraphicRaycaster>();
    }

    void BuildWheelPanel()
    {
        // Full-screen root
        _wheelRoot = MakeGO("WheelRoot", _canvas.transform);
        StretchToFill(_wheelRoot);

        // Semi-transparent dimmer — click it to close the wheel
        var dimmer = MakeGO("Dimmer", _wheelRoot.transform);
        StretchToFill(dimmer);
        dimmer.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.30f);
        var dimBtn = dimmer.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(ToggleWheel);

        // Circular dark background centered on screen
        float bgSize = (segmentOrbitRadius + segmentWidth * 0.5f + 30f) * 2f;
        var bgGO = MakeGO("WheelBg", _wheelRoot.transform);
        var bgRect = bgGO.AddComponent<RectTransform>();
        CenterRect(bgRect, new Vector2(bgSize, bgSize), Vector2.zero);
        bgGO.AddComponent<Image>().color = ColBg;

        AddText(bgGO.transform, "CenterLabel", "ACTIONS",
            30, FontStyle.Bold, TextAnchor.MiddleCenter, ColCenter,
            Vector2.zero, new Vector2(200f, 48f));

        AddText(bgGO.transform, "Hint", "[R] or [Esc] to close",
            20, FontStyle.Normal, TextAnchor.MiddleCenter, ColHint,
            new Vector2(0f, -bgSize * 0.5f + 28f), new Vector2(340f, 30f));
    }

    void BuildSegmentButtons()
    {
        var bgTf = _wheelRoot.transform.Find("WheelBg");
        float angleStep = _segments.Count > 0 ? 360f / _segments.Count : 360f;

        for (int i = 0; i < _segments.Count; i++)
        {
            Segment seg = _segments[i];
            float angle = (90f - i * angleStep) * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * segmentOrbitRadius;

            var segGO = MakeGO("Seg_" + i, bgTf);
            var segRect = segGO.AddComponent<RectTransform>();
            CenterRect(segRect, new Vector2(segmentWidth, segmentHeight), pos);

            seg.background = segGO.AddComponent<Image>();
            seg.background.color = ColSegIdle;

            var iconGO = AddText(segGO.transform, "Icon", "~",
                36, FontStyle.Bold, TextAnchor.MiddleCenter, ColIconIdle,
                new Vector2(0f, 36f), new Vector2(segmentWidth - 12f, 50f));
            seg.iconText = iconGO.GetComponent<Text>();

            var labelGO = AddText(segGO.transform, "Label", seg.label,
                26, FontStyle.Normal, TextAnchor.MiddleCenter, ColTxtIdle,
                new Vector2(0f, -30f), new Vector2(segmentWidth - 12f, 70f));
            seg.labelText = labelGO.GetComponent<Text>();

            var btn = segGO.AddComponent<Button>();
            ColorBlock cb = ColorBlock.defaultColorBlock;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.35f, 1f);
            cb.pressedColor     = new Color(0.85f, 0.85f, 0.95f, 1f);
            btn.colors = cb;

            int captured = i;
            btn.onClick.AddListener(() =>
            {
                _segments[captured].onToggle?.Invoke();
                RefreshVisuals();
            });
        }
    }

    void RefreshVisuals()
    {
        foreach (Segment seg in _segments)
        {
            if (seg.background == null) continue;
            bool on = seg.isActive != null && seg.isActive();
            seg.background.color   = on ? ColSegActive : ColSegIdle;
            seg.labelText.color    = on ? ColTxtActive : ColTxtIdle;
            seg.iconText.color     = on ? ColIconOn    : ColIconIdle;
            seg.iconText.text      = on ? "ON"         : "OFF";
        }
    }

    // ── static helpers ────────────────────────────────────────────────────────

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchToFill(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        if (r == null) r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static void CenterRect(RectTransform r, Vector2 size, Vector2 pos)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    static GameObject AddText(Transform parent, string name, string text,
        int fontSize, FontStyle style, TextAnchor anchor, Color color,
        Vector2 pos, Vector2 size)
    {
        var go = MakeGO(name, parent);
        var r = go.AddComponent<RectTransform>();
        CenterRect(r, size, pos);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = anchor;
        t.color = color;
        return go;
    }
}
