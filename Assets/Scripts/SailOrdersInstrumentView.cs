using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placeholder Sail Orders instrument UI. Displays status only — speed is set via CaptainCommandManager.
/// </summary>
public class SailOrdersInstrumentView : MonoBehaviour
{
    static readonly Color NormalButtonColor = new Color(0.28f, 0.26f, 0.22f, 1f);
    static readonly Color SelectedButtonColor = new Color(0.48f, 0.40f, 0.22f, 1f);

    [SerializeField] Button strikeSailButton;
    [SerializeField] Button heaveToButton;
    [SerializeField] Button stormSailButton;
    [SerializeField] Button battleSailButton;
    [SerializeField] Button cruisingSailButton;
    [SerializeField] Button fullSailButton;

    [SerializeField] Text currentSailOrderText;
    [SerializeField] Text targetSpeedText;
    [SerializeField] Text currentSpeedText;

    bool _hasSelection;

    public bool IsBuilt => strikeSailButton != null;

    public void Initialize(
        Button strike, Button heave, Button storm, Button battle, Button cruising, Button full,
        Text currentOrder, Text target, Text current)
    {
        strikeSailButton = strike;
        heaveToButton = heave;
        stormSailButton = storm;
        battleSailButton = battle;
        cruisingSailButton = cruising;
        fullSailButton = full;
        currentSailOrderText = currentOrder;
        targetSpeedText = target;
        currentSpeedText = current;
    }

    public void Refresh(SailOrder order, float targetSpeedKnots, float currentSpeedKnots)
    {
        if (currentSailOrderText == null)
            return;

        _hasSelection = true;
        UpdateButtonHighlights(order);
        currentSailOrderText.text = $"Current Sail Order:\n{SailOrderSpeedTable.GetDisplayName(order)}";
        targetSpeedText.text = $"Target Speed:\n{targetSpeedKnots:F1} knots";
        currentSpeedText.text = $"Current Speed:\n{currentSpeedKnots:F1} knots";
    }

    public void RefreshCurrentSpeed(float currentSpeedKnots)
    {
        currentSpeedText.text = $"Current Speed:\n{currentSpeedKnots:F1} knots";
    }

    public void ClearSelectionDisplay(float currentSpeedKnots)
    {
        if (currentSailOrderText == null)
            return;

        _hasSelection = false;
        UpdateButtonHighlights(null);
        currentSailOrderText.text = "Current Sail Order:\n—";
        targetSpeedText.text = "Target Speed:\n—";
        currentSpeedText.text = $"Current Speed:\n{currentSpeedKnots:F1} knots";
    }

    void UpdateButtonHighlights(SailOrder? selected)
    {
        SetButtonSelected(strikeSailButton, selected == SailOrder.StrikeSail);
        SetButtonSelected(heaveToButton, selected == SailOrder.HeaveTo);
        SetButtonSelected(stormSailButton, selected == SailOrder.StormSail);
        SetButtonSelected(battleSailButton, selected == SailOrder.BattleSail);
        SetButtonSelected(cruisingSailButton, selected == SailOrder.CruisingSail);
        SetButtonSelected(fullSailButton, selected == SailOrder.FullSail);
    }

    static void SetButtonSelected(Button button, bool selected)
    {
        if (button == null)
            return;

        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = selected ? SelectedButtonColor : NormalButtonColor;
    }
}
