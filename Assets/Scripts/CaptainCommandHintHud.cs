using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placeholder tester hint for Captain's Command. Remove this component (and child canvas) when no longer needed.
/// </summary>
public class CaptainCommandHintHud : MonoBehaviour
{
    const string HintText = "Captain's Command  [Left Shift]";
    const float VisibleAlpha = 0.75f;
    const float FadeSpeed = 8f;

    [SerializeField] CaptainCommandManager commandManager;
    [SerializeField] CanvasGroup canvasGroup;

    bool _built;

    void Awake()
    {
        if (commandManager == null)
            commandManager = FindAnyObjectByType<CaptainCommandManager>();

        EnsureBuilt();
    }

    void Update()
    {
        if (!_built || canvasGroup == null)
            return;

        bool commandModeActive = commandManager != null && commandManager.IsCommandModeActive;
        float targetAlpha = commandModeActive ? 0f : VisibleAlpha;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, FadeSpeed * Time.deltaTime);
    }

    void EnsureBuilt()
    {
        if (_built)
            return;

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 199;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = VisibleAlpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (transform.parent != null)
            transform.SetParent(null, false);

        StretchCanvasRoot(GetComponent<RectTransform>());

        var textGo = new GameObject("HintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(transform, false);

        var text = textGo.GetComponent<Text>();
        text.font = font;
        text.text = HintText;
        text.fontSize = 14;
        text.alignment = TextAnchor.LowerLeft;
        text.color = Color.white;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(20f, 20f);
        rect.sizeDelta = new Vector2(360f, 24f);

        _built = true;
    }

    static void StretchCanvasRoot(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
