using System;
using UnityEngine;

public enum BatterySide
{
    Port,
    Starboard
}

/// <summary>
/// Tracks port and starboard gun batteries. Runs on an always-active object so reload timers continue off-panel.
/// </summary>
public class CannonBattery : MonoBehaviour
{
    public const int CannonsPerSide = 7;
    public const float ReloadDurationSeconds = 15f;

    [Serializable]
    public class Cannon
    {
        public BatterySide Side;
        public int Number;
        public CannonState CurrentState = CannonState.Ready;
        public float ReloadTimer;

        public bool CanFire => CurrentState == CannonState.Ready;
    }

    [SerializeField] Cannon[] cannons = Array.Empty<Cannon>();

    public event Action StateChanged;

    void Awake()
    {
        EnsureInitialized();
    }

    void Update()
    {
        if (!EnsureInitialized())
            return;

        bool anyReloaded = false;
        for (int i = 0; i < cannons.Length; i++)
        {
            Cannon cannon = cannons[i];
            if (cannon.CurrentState != CannonState.Reloading)
                continue;

            cannon.ReloadTimer -= Time.deltaTime;
            if (cannon.ReloadTimer > 0f)
                continue;

            cannon.ReloadTimer = 0f;
            cannon.CurrentState = CannonState.Ready;
            Debug.Log($"{FormatSide(cannon.Side)} Cannon {cannon.Number} Reloaded");
            anyReloaded = true;
        }

        if (anyReloaded)
            StateChanged?.Invoke();
    }

    public bool TryFire(BatterySide side, int number)
    {
        if (!EnsureInitialized())
            return false;

        Cannon cannon = FindCannon(side, number);
        if (cannon == null || !cannon.CanFire)
            return false;

        Debug.Log($"{FormatSide(side)} Cannon {number} Fired");
        cannon.CurrentState = CannonState.Reloading;
        cannon.ReloadTimer = ReloadDurationSeconds;
        StateChanged?.Invoke();
        CannonVisualRegistry.TryFireVisual(side, number);
        return true;
    }

    public void FireAll(BatterySide side)
    {
        if (!EnsureInitialized())
            return;

        for (int number = 1; number <= CannonsPerSide; number++)
            TryFire(side, number);
    }

    public void FireAllPort() => FireAll(BatterySide.Port);

    public void FireAllStarboard() => FireAll(BatterySide.Starboard);

    public void SetCannonState(BatterySide side, int number, CannonState state)
    {
        if (!EnsureInitialized())
            return;

        Cannon cannon = FindCannon(side, number);
        if (cannon == null)
            return;

        cannon.CurrentState = state;
        cannon.ReloadTimer = state == CannonState.Reloading ? ReloadDurationSeconds : 0f;
        StateChanged?.Invoke();
    }

    public Cannon FindCannon(BatterySide side, int number)
    {
        if (!EnsureInitialized())
            return null;

        for (int i = 0; i < cannons.Length; i++)
        {
            Cannon cannon = cannons[i];
            if (cannon.Side == side && cannon.Number == number)
                return cannon;
        }

        return null;
    }

    public (int ready, int reloading, int unmanned) GetSideCounts(BatterySide side)
    {
        if (!EnsureInitialized())
            return (0, 0, 0);

        int ready = 0;
        int reloading = 0;
        int unmanned = 0;

        for (int i = 0; i < cannons.Length; i++)
        {
            Cannon cannon = cannons[i];
            if (cannon.Side != side)
                continue;

            switch (cannon.CurrentState)
            {
                case CannonState.Ready:
                    ready++;
                    break;
                case CannonState.Reloading:
                    reloading++;
                    break;
                case CannonState.Unmanned:
                    unmanned++;
                    break;
            }
        }

        return (ready, reloading, unmanned);
    }

    bool EnsureInitialized()
    {
        if (cannons != null && cannons.Length == CannonsPerSide * 2)
            return true;

        cannons = new Cannon[CannonsPerSide * 2];
        int index = 0;
        for (int number = 1; number <= CannonsPerSide; number++)
            cannons[index++] = CreateCannon(BatterySide.Port, number);
        for (int number = 1; number <= CannonsPerSide; number++)
            cannons[index++] = CreateCannon(BatterySide.Starboard, number);

        return true;
    }

    static Cannon CreateCannon(BatterySide side, int number)
    {
        return new Cannon
        {
            Side = side,
            Number = number,
            CurrentState = CannonState.Ready,
            ReloadTimer = 0f
        };
    }

    public static string FormatSide(BatterySide side)
    {
        return side == BatterySide.Port ? "Port" : "Starboard";
    }
}
