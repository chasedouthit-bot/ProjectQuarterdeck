/// <summary>
/// Ideal sail-order speeds for a brig in moderate breeze.
/// Weather, damage, and crew modifiers will be applied here later — not in UI code.
/// </summary>
public static class SailOrderSpeedTable
{
    public static float GetIdealSpeedKnots(SailOrder order)
    {
        return order switch
        {
            SailOrder.StrikeSail => 0f,
            SailOrder.HeaveTo => 0.8f,
            SailOrder.StormSail => 3f,
            SailOrder.BattleSail => 5.5f,
            SailOrder.CruisingSail => 8f,
            SailOrder.FullSail => 11.5f,
            _ => 0f
        };
    }

    public static string GetDisplayName(SailOrder order)
    {
        return order switch
        {
            SailOrder.StrikeSail => "Strike Sail",
            SailOrder.HeaveTo => "Heave To",
            SailOrder.StormSail => "Storm Sail",
            SailOrder.BattleSail => "Battle Sail",
            SailOrder.CruisingSail => "Cruising Sail",
            SailOrder.FullSail => "Full Sail",
            _ => order.ToString()
        };
    }
}
