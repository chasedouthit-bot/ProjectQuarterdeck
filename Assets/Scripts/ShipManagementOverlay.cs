using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using StarterAssets;

/// <summary>
/// Space-held ship management overlay for ship-level controls (not officer orders).
/// </summary>
public class ShipManagementOverlay : MonoBehaviour
{
    public enum SailOrder
    {
        StrikeSail,
        HeaveTo,
        StormSail,
        BattleSail,
        CruisingSail,
        FullSail
    }

    public enum ShipState
    {
        SecureShip,
        ClearForAction,
        HeavyWeather,
        DamageControl
    }

    enum OverlayView
    {
        Hidden,
        Main,
        SailOrders,
        CourseBearing,
        BatteryStatus,
        ShipState
    }

    [Header("UI Roots")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject sailOrdersPanel;
    [SerializeField] private GameObject courseBearingPanel;
    [SerializeField] private GameObject batteryStatusPanel;
    [SerializeField] private GameObject shipStatePanel;

    [Header("References")]
    [SerializeField] private StarterAssetsInputs playerInputs;
    [SerializeField] private ShipMovementPrototype shipMovement;

    OverlayView _currentView = OverlayView.Hidden;
    bool _spaceHeldLastFrame;

    void Awake()
    {
        ResolveReferences();
        SetView(OverlayView.Hidden);
    }

    void Update()
    {
        bool spaceHeld = IsSpaceHeld();

        if (spaceHeld && !_spaceHeldLastFrame)
            ShowOverlay();
        else if (!spaceHeld && _spaceHeldLastFrame)
            HideOverlay();

        _spaceHeldLastFrame = spaceHeld;

        if (_currentView == OverlayView.Hidden)
            return;

        if (WasEscapePressed())
            DismissToGameplay();
    }

    void LateUpdate()
    {
        if (_currentView != OverlayView.Hidden)
            SuppressJumpWhileOpen();
    }

    public void ShowMainPanel()
    {
        if (_currentView == OverlayView.Hidden)
            return;

        SetView(OverlayView.Main);
    }

    public void OpenSailOrdersPanel() => OpenSubPanel(OverlayView.SailOrders);
    public void OpenCourseBearingPanel() => OpenSubPanel(OverlayView.CourseBearing);
    public void OpenBatteryStatusPanel() => OpenSubPanel(OverlayView.BatteryStatus);
    public void OpenShipStatePanel() => OpenSubPanel(OverlayView.ShipState);

    public void SelectSailOrder(int sailOrderIndex) =>
        ApplySailOrder((SailOrder)sailOrderIndex);

    public void SelectShipState(int shipStateIndex) =>
        ApplyShipState((ShipState)shipStateIndex);

    public void SelectStrikeSail() => ApplySailOrder(SailOrder.StrikeSail);
    public void SelectHeaveTo() => ApplySailOrder(SailOrder.HeaveTo);
    public void SelectStormSail() => ApplySailOrder(SailOrder.StormSail);
    public void SelectBattleSail() => ApplySailOrder(SailOrder.BattleSail);
    public void SelectCruisingSail() => ApplySailOrder(SailOrder.CruisingSail);
    public void SelectFullSail() => ApplySailOrder(SailOrder.FullSail);

    public void SelectSecureShip() => ApplyShipState(ShipState.SecureShip);
    public void SelectClearForAction() => ApplyShipState(ShipState.ClearForAction);
    public void SelectHeavyWeather() => ApplyShipState(ShipState.HeavyWeather);
    public void SelectDamageControl() => ApplyShipState(ShipState.DamageControl);

    public void OnFireAllPort() =>
        Debug.Log("Battery Status: Fire All Port (placeholder).");

    public void OnFireAllStarboard() =>
        Debug.Log("Battery Status: Fire All Starboard (placeholder).");

    void OpenSubPanel(OverlayView view)
    {
        if (_currentView == OverlayView.Hidden)
            return;

        SetView(view);
    }

    void ApplySailOrder(SailOrder order)
    {
        float knots = order switch
        {
            SailOrder.StrikeSail => 0f,
            SailOrder.HeaveTo => 0.5f,
            SailOrder.StormSail => 2f,
            SailOrder.BattleSail => 4f,
            SailOrder.CruisingSail => 7f,
            SailOrder.FullSail => 10f,
            _ => 0f
        };

        Debug.Log($"Selected Sail Order: {FormatSailOrder(order)}");

        if (shipMovement != null)
            shipMovement.SetTargetSpeedKnots(knots);
    }

    void ApplyShipState(ShipState state)
    {
        Debug.Log($"Selected Ship State: {FormatShipState(state)}");
    }

    void ShowOverlay()
    {
        if (overlayRoot != null && overlayRoot != gameObject)
            overlayRoot.SetActive(true);

        SetView(OverlayView.Main);
        ApplyCursorForOverlay(true);
        SuppressJumpWhileOpen();
    }

    void HideOverlay()
    {
        SetView(OverlayView.Hidden);
        ApplyCursorForOverlay(false);
    }

    void DismissToGameplay()
    {
        HideOverlay();
    }

    void SetView(OverlayView view)
    {
        _currentView = view;

        if (overlayRoot != null && overlayRoot != gameObject)
            overlayRoot.SetActive(view != OverlayView.Hidden);

        SetPanelActive(mainPanel, view == OverlayView.Main);
        SetPanelActive(sailOrdersPanel, view == OverlayView.SailOrders);
        SetPanelActive(courseBearingPanel, view == OverlayView.CourseBearing);
        SetPanelActive(batteryStatusPanel, view == OverlayView.BatteryStatus);
        SetPanelActive(shipStatePanel, view == OverlayView.ShipState);
    }

    static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    void ApplyCursorForOverlay(bool overlayVisible)
    {
        if (playerInputs != null)
        {
            playerInputs.cursorInputForLook = !overlayVisible;
            playerInputs.cursorLocked = !overlayVisible;
            if (overlayVisible)
                playerInputs.look = Vector2.zero;
        }

        Cursor.lockState = overlayVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = overlayVisible;
    }

    void ResolveReferences()
    {
        if (overlayRoot == null)
        {
            var uiTransform = transform.Find("OverlayUI");
            if (uiTransform != null)
                overlayRoot = uiTransform.gameObject;
        }

        if (playerInputs == null)
            playerInputs = FindAnyObjectByType<StarterAssetsInputs>();

        if (shipMovement == null)
            shipMovement = FindAnyObjectByType<ShipMovementPrototype>();
    }

    void SuppressJumpWhileOpen()
    {
        if (playerInputs != null)
            playerInputs.jump = false;
    }

    static bool IsSpaceHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.spaceKey.isPressed;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Space);
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

    static string FormatSailOrder(SailOrder order) => order switch
    {
        SailOrder.StrikeSail => "Strike Sail",
        SailOrder.HeaveTo => "Heave To",
        SailOrder.StormSail => "Storm Sail",
        SailOrder.BattleSail => "Battle Sail",
        SailOrder.CruisingSail => "Cruising Sail",
        SailOrder.FullSail => "Full Sail",
        _ => order.ToString()
    };

    static string FormatShipState(ShipState state) => state switch
    {
        ShipState.SecureShip => "Secure Ship",
        ShipState.ClearForAction => "Clear For Action",
        ShipState.HeavyWeather => "Heavy Weather",
        ShipState.DamageControl => "Damage Control",
        _ => state.ToString()
    };
}
