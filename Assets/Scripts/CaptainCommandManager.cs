using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

/// <summary>
/// Captain's Command Mode — modular framework for ship management instruments.
/// Hold Left Shift to enter; release to return to normal gameplay.
/// All UI actions route through this manager; replace placeholder UI without changing gameplay code.
/// </summary>
public class CaptainCommandManager : MonoBehaviour
{
    public enum CommandPanel
    {
        None,
        Main,
        SailOrders,
        CourseBearing,
        BatteryStatus,
        ShipState
    }

    [Header("UI Roots")]
    [Tooltip("Child canvas/content root toggled on/off. Must NOT be this GameObject.")]
    [SerializeField] private GameObject overlayUiRoot;

    [Header("Screens")]
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject sailOrdersPanel;
    [SerializeField] private GameObject courseBearingPanel;
    [SerializeField] private GameObject batteryStatusPanel;
    [SerializeField] private GameObject shipStatePanel;

    [Header("Player")]
    [SerializeField] private StarterAssetsInputs playerInputs;

    [Header("Instruments")]
    [SerializeField] private ShipMovementPrototype shipMovement;
    [SerializeField] private SailOrdersInstrumentView sailOrdersView;
    [SerializeField] private CourseBearingPanel courseBearingView;
    [SerializeField] private BatteryStatusPanel batteryStatusView;

    CommandPanel _activePanel = CommandPanel.None;
    bool _commandModeActive;
    bool _shiftHeldLastFrame;
    bool _dismissedUntilShiftRelease;
    bool _hasSailOrderSelection;
    SailOrder _selectedSailOrder;
    Coroutine _layoutRefreshRoutine;

    static readonly Vector2 MainPanelSize = new Vector2(920f, 680f);
    static readonly Vector2 DetailPanelSize = new Vector2(760f, 520f);

    // --- Lifecycle ---

    void Awake()
    {
        ResolveReferences();
        ExitCommandModeImmediate();
    }

    void Update()
    {
        UpdateCommandModeInput();

        if (!_commandModeActive)
            return;

        if (WasEscapePressed())
            HandleEscape();

        if (_activePanel == CommandPanel.SailOrders)
            RefreshSailOrdersDisplay();

        if (_activePanel == CommandPanel.CourseBearing)
            RefreshCourseBearingDisplay();

        if (_activePanel == CommandPanel.BatteryStatus)
            RefreshBatteryStatusDisplay();
    }

    void LateUpdate()
    {
        if (_commandModeActive)
            SuppressSprintInput();
    }

    // --- Public API (wire UI buttons to these methods) ---

    public void OpenSailOrders() => ShowPanel(CommandPanel.SailOrders);
    public void OpenCourseBearing() => ShowPanel(CommandPanel.CourseBearing);
    public void OpenBatteryStatus() => ShowPanel(CommandPanel.BatteryStatus);
    public void OpenShipState() => ShowPanel(CommandPanel.ShipState);

    public void ShowMainScreen() => ShowPanel(CommandPanel.Main);

    public void OnBackPressed() => ShowMainScreen();

    public bool IsCommandModeActive => _commandModeActive;
    public SailOrder SelectedSailOrder => _selectedSailOrder;
    public bool HasSailOrderSelection => _hasSailOrderSelection;

    public void SelectStrikeSail() => ApplySailOrder(SailOrder.StrikeSail);
    public void SelectHeaveTo() => ApplySailOrder(SailOrder.HeaveTo);
    public void SelectStormSail() => ApplySailOrder(SailOrder.StormSail);
    public void SelectBattleSail() => ApplySailOrder(SailOrder.BattleSail);
    public void SelectCruisingSail() => ApplySailOrder(SailOrder.CruisingSail);
    public void SelectFullSail() => ApplySailOrder(SailOrder.FullSail);

    // --- Command mode ---

    void UpdateCommandModeInput()
    {
        bool shiftHeld = IsLeftShiftHeld();

        if (_dismissedUntilShiftRelease)
        {
            if (!shiftHeld)
                _dismissedUntilShiftRelease = false;
        }
        else
        {
            if (shiftHeld && !_shiftHeldLastFrame)
                EnterCommandMode();
            else if (!shiftHeld && _shiftHeldLastFrame)
                ExitCommandMode();
        }

        _shiftHeldLastFrame = shiftHeld;
    }

    void EnterCommandMode()
    {
        _commandModeActive = true;
        ShowPanel(CommandPanel.Main);
        ApplyGameplayInputForCommandMode(true);
        ScheduleOverlayLayoutRefresh();
    }

    void ExitCommandMode()
    {
        _commandModeActive = false;
        ShowPanel(CommandPanel.None);
        ApplyGameplayInputForCommandMode(false);
    }

    void ExitCommandModeImmediate()
    {
        _commandModeActive = false;
        _activePanel = CommandPanel.None;
        SetOverlayVisible(false);
        SetAllPanelsActive(false);
        ApplyGameplayInputForCommandMode(false);
    }

    void HandleEscape()
    {
        if (_activePanel != CommandPanel.Main)
        {
            ShowMainScreen();
            return;
        }

        _dismissedUntilShiftRelease = true;
        ExitCommandMode();
    }

    // --- Panels ---

    void ShowPanel(CommandPanel panel)
    {
        _activePanel = panel;

        bool overlayVisible = panel != CommandPanel.None;
        SetOverlayVisible(overlayVisible);

        SetAllPanelsActive(false);

        switch (panel)
        {
            case CommandPanel.Main:
                SetPanelActive(mainScreen, true);
                break;
            case CommandPanel.SailOrders:
                SetPanelActive(sailOrdersPanel, true);
                RefreshSailOrdersDisplay();
                break;
            case CommandPanel.CourseBearing:
                SetPanelActive(courseBearingPanel, true);
                RefreshCourseBearingDisplay();
                break;
            case CommandPanel.BatteryStatus:
                SetPanelActive(batteryStatusPanel, true);
                RefreshBatteryStatusDisplay();
                break;
            case CommandPanel.ShipState:
                SetPanelActive(shipStatePanel, true);
                break;
        }
    }

    void SetOverlayVisible(bool visible)
    {
        if (overlayUiRoot == null || overlayUiRoot == gameObject)
            return;

        if (visible)
        {
            EnsureOverlayLayout();
            ScheduleOverlayLayoutRefresh();
        }

        overlayUiRoot.SetActive(visible);
    }

    void ScheduleOverlayLayoutRefresh()
    {
        if (_layoutRefreshRoutine != null)
            StopCoroutine(_layoutRefreshRoutine);

        _layoutRefreshRoutine = StartCoroutine(RefreshOverlayLayoutAfterEnable());
    }

    IEnumerator RefreshOverlayLayoutAfterEnable()
    {
        yield return null;
        EnsureOverlayLayout();
        Canvas.ForceUpdateCanvases();
        _layoutRefreshRoutine = null;
    }

    /// <summary>
    /// Overlay canvas must be a scene-root Screen Space canvas so centered panels align to the screen.
    /// </summary>
    void EnsureOverlayLayout()
    {
        EnsureOverlayCanvasRoot();
        EnsureCenteredCommandPanels();
    }

    void EnsureOverlayCanvasRoot()
    {
        if (overlayUiRoot == null)
            return;

        if (overlayUiRoot.transform.parent != null)
            overlayUiRoot.transform.SetParent(null, false);

        var canvas = overlayUiRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
        }

        var rect = overlayUiRoot.GetComponent<RectTransform>();
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

    void EnsureCenteredCommandPanels()
    {
        CenterPanel(mainScreen, MainPanelSize);
        CenterPanel(sailOrdersPanel, DetailPanelSize);
        CenterPanel(courseBearingPanel, DetailPanelSize);
        CenterPanel(batteryStatusPanel, DetailPanelSize);
        CenterPanel(shipStatePanel, DetailPanelSize);
    }

    static void CenterPanel(GameObject panel, Vector2 size)
    {
        if (panel == null)
            return;

        var rect = panel.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    void OnDestroy()
    {
        if (overlayUiRoot != null && overlayUiRoot.transform.parent == null)
            Destroy(overlayUiRoot);
    }

    void SetAllPanelsActive(bool active)
    {
        SetPanelActive(mainScreen, active);
        SetPanelActive(sailOrdersPanel, active);
        SetPanelActive(courseBearingPanel, active);
        SetPanelActive(batteryStatusPanel, active);
        SetPanelActive(shipStatePanel, active);
    }

    static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    // --- Player input / cursor ---

    void ApplyGameplayInputForCommandMode(bool commandMode)
    {
        if (playerInputs != null)
        {
            playerInputs.cursorInputForLook = !commandMode;
            playerInputs.cursorLocked = !commandMode;
            if (commandMode)
            {
                playerInputs.look = Vector2.zero;
                playerInputs.sprint = false;
            }
        }

        Cursor.lockState = commandMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = commandMode;
    }

    void SuppressSprintInput()
    {
        if (playerInputs != null)
            playerInputs.sprint = false;
    }

    void ResolveReferences()
    {
        if (overlayUiRoot == null)
        {
            var ui = transform.Find("OverlayUI");
            if (ui != null)
                overlayUiRoot = ui.gameObject;
        }

        if (playerInputs == null)
            playerInputs = FindAnyObjectByType<StarterAssetsInputs>();

        if (shipMovement == null)
            shipMovement = FindAnyObjectByType<ShipMovementPrototype>();

        EnsureSailOrdersInstrument();
        EnsureCourseBearingInstrument();
        EnsureBatteryStatusInstrument();
        EnsureOverlayLayout();
    }

    void EnsureBatteryStatusInstrument()
    {
        if (batteryStatusPanel == null)
            return;

        if (batteryStatusView == null)
            batteryStatusView = batteryStatusPanel.GetComponent<BatteryStatusPanel>();

        if (batteryStatusView == null || !batteryStatusView.IsBuilt)
            batteryStatusView = BatteryStatusPanel.Build(batteryStatusPanel, this);
    }

    void EnsureCourseBearingInstrument()
    {
        if (courseBearingPanel == null)
            return;

        if (courseBearingView == null)
            courseBearingView = courseBearingPanel.GetComponent<CourseBearingPanel>();

        if (courseBearingView == null || !courseBearingView.IsBuilt)
            courseBearingView = CourseBearingPanel.Build(courseBearingPanel, this, shipMovement);
        else
            courseBearingView.BindShipMovement(shipMovement);
    }

    void EnsureSailOrdersInstrument()
    {
        if (sailOrdersPanel == null)
            return;

        if (sailOrdersView == null)
            sailOrdersView = sailOrdersPanel.GetComponent<SailOrdersInstrumentView>();

        if (sailOrdersView == null || !sailOrdersView.IsBuilt)
            sailOrdersView = SailOrdersInstrumentBuilder.Build(sailOrdersPanel, this);
    }

    void ApplySailOrder(SailOrder order)
    {
        _selectedSailOrder = order;
        _hasSailOrderSelection = true;

        float idealSpeedKnots = SailOrderSpeedTable.GetIdealSpeedKnots(order);
        string displayName = SailOrderSpeedTable.GetDisplayName(order);

        if (shipMovement != null)
            shipMovement.ApplySailOrder(order, idealSpeedKnots);

        Debug.Log(
            $"Selected Sail Order:\n{displayName}\n\nTarget Speed:\n{idealSpeedKnots:F1} knots");

        RefreshSailOrdersDisplay();
    }

    void RefreshSailOrdersDisplay()
    {
        if (sailOrdersView == null)
            return;

        float currentSpeed = shipMovement != null ? shipMovement.CurrentSpeedKnots : 0f;

        if (!_hasSailOrderSelection)
        {
            sailOrdersView.ClearSelectionDisplay(currentSpeed);
            return;
        }

        float targetSpeed = shipMovement != null
            ? shipMovement.TargetSpeedKnots
            : SailOrderSpeedTable.GetIdealSpeedKnots(_selectedSailOrder);

        sailOrdersView.Refresh(_selectedSailOrder, targetSpeed, currentSpeed);
    }

    void RefreshCourseBearingDisplay()
    {
        if (courseBearingView != null)
            courseBearingView.RefreshStatus();
    }

    void RefreshBatteryStatusDisplay()
    {
        if (batteryStatusView != null)
            batteryStatusView.RefreshDisplay();
    }

    // --- Input helpers ---

    static bool IsLeftShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.leftShiftKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#else
        return false;
#endif
    }

    static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
