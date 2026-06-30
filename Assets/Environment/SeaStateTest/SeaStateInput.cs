using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Input helpers for the sea state test scene. Project Quarterdeck uses the new Input System.
/// </summary>
public static class SeaStateInput
{
    public static bool WasCalmSeaPressed()
    {
        return WasKeyPressed(Key.F1);
    }

    public static bool WasChoppySeaPressed()
    {
        return WasKeyPressed(Key.F2);
    }

    public static bool WasStormSeaPressed()
    {
        return WasKeyPressed(Key.F3);
    }

    public static bool WasEscapePressed()
    {
        return WasKeyPressed(Key.Escape);
    }

    public static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.delta.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#else
        return Vector2.zero;
#endif
    }

    static bool WasKeyPressed(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current[key].wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(ToLegacyKeyCode(key));
#else
        return false;
#endif
    }

#if ENABLE_LEGACY_INPUT_MANAGER
    static KeyCode ToLegacyKeyCode(Key key)
    {
        return key switch
        {
            Key.F1 => KeyCode.F1,
            Key.F2 => KeyCode.F2,
            Key.F3 => KeyCode.F3,
            Key.Escape => KeyCode.Escape,
            _ => KeyCode.None
        };
    }
#endif
}
