using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps battery side + cannon number to scene <see cref="CannonVisual"/> instances.
/// </summary>
public static class CannonVisualRegistry
{
    static readonly Dictionary<(BatterySide side, int number), CannonVisual> Visuals = new();

    public static void Register(CannonVisual visual)
    {
        if (visual == null)
            return;

        Visuals[(visual.Side, visual.CannonNumber)] = visual;
    }

    public static void Unregister(CannonVisual visual)
    {
        if (visual == null)
            return;

        var key = (visual.Side, visual.CannonNumber);
        if (Visuals.TryGetValue(key, out CannonVisual existing) && existing == visual)
            Visuals.Remove(key);
    }

    public static void TryFireVisual(BatterySide side, int number)
    {
        if (Visuals.TryGetValue((side, number), out CannonVisual visual) && visual != null)
        {
            visual.FireVisual();
            return;
        }

        Debug.LogWarning($"No CannonVisual found for {CannonBattery.FormatSide(side)} Cannon {number}");
    }

    public static void Clear()
    {
        Visuals.Clear();
    }
}
