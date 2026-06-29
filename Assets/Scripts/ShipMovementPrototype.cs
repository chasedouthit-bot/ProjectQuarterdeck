using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Prototype ship movement for Project Quarterdeck.
/// TFGH drives the ship via Transform only (no Rigidbody).
/// Player WASD movement is handled separately by the First Person Controller.
/// </summary>
public class ShipMovementPrototype : MonoBehaviour
{
    // 1 knot = 1 nautical mile per hour.
    private const float KnotsToMetersPerSecond = 0.514444f;

    [Header("Speed (Knots)")]
    [Tooltip("Top speed while holding T (ahead).")]
    [SerializeField] private float maxSpeedKnots = 10f;

    [Tooltip("How quickly speed increases toward max speed (knots per second).")]
    [SerializeField] private float accelerationRate = 1.25f;

    [Tooltip("How quickly speed decreases toward zero while holding G (astern) (knots per second).")]
    [SerializeField] private float decelerationRate = 2f;

    [Header("Turning (Degrees)")]
    [Tooltip("Maximum turn rate at full rudder (degrees per second).")]
    [SerializeField] private float turnSpeed = 18f;

    [Tooltip("How quickly turn rate ramps up and down (degrees per second squared).")]
    [SerializeField] private float turnAcceleration = 18f;

    [Header("Tuning")]
    [Tooltip("Ship must be moving faster than this (knots) before it can turn.")]
    [SerializeField] private float minSpeedToTurnKnots = 0.15f;

    [Tooltip("Optional coasting drag when neither T nor G is held (knots per second). Set to 0 to maintain speed.")]
    [SerializeField] private float idleDragKnotsPerSecond = 0f;

    [Header("Debug (Read Only)")]
    [SerializeField] private float currentSpeedKnots;
    [SerializeField] private float targetSpeedKnots;
    [SerializeField] private float currentHeadingDegrees;
    [SerializeField] private float targetHeadingDegrees;

    // Runtime turn state.
    private float _currentTurnRateDegreesPerSecond;
    private float _targetTurnRateDegreesPerSecond;
    private bool _followSailOrder;
    private bool _followOrderedHeading;
    private SailOrder _activeSailOrder;

    public float CurrentSpeedKnots => currentSpeedKnots;
    public float TargetSpeedKnots => targetSpeedKnots;
    public float CurrentHeadingDegrees => currentHeadingDegrees;
    public float TargetHeadingDegrees => targetHeadingDegrees;
    public float CurrentTurnRateDegreesPerSecond => _currentTurnRateDegreesPerSecond;
    public SailOrder ActiveSailOrder => _activeSailOrder;
    public bool IsFollowingSailOrder => _followSailOrder;
    public bool IsFollowingOrderedHeading => _followOrderedHeading;

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateSpeed(deltaTime);
        UpdateTurning(deltaTime);
        ApplyMovement(deltaTime);

        currentSpeedKnots = Mathf.Max(0f, currentSpeedKnots);
        currentHeadingDegrees = NormalizeHeading(transform.eulerAngles.y);
    }

    private void UpdateSpeed(float deltaTime)
    {
        if (IsShipKeyPressed(KeyCode.T))
        {
            _followSailOrder = false;
            currentSpeedKnots = Mathf.MoveTowards(
                currentSpeedKnots,
                maxSpeedKnots,
                accelerationRate * deltaTime);
            targetSpeedKnots = maxSpeedKnots;
            return;
        }

        if (IsShipKeyPressed(KeyCode.G))
        {
            _followSailOrder = false;
            currentSpeedKnots = Mathf.MoveTowards(
                currentSpeedKnots,
                0f,
                decelerationRate * deltaTime);
            targetSpeedKnots = 0f;
            return;
        }

        if (_followSailOrder)
        {
            float rate = targetSpeedKnots >= currentSpeedKnots ? accelerationRate : decelerationRate;
            currentSpeedKnots = Mathf.MoveTowards(
                currentSpeedKnots,
                targetSpeedKnots,
                rate * deltaTime);
            return;
        }

        if (idleDragKnotsPerSecond > 0f && currentSpeedKnots > 0f)
        {
            currentSpeedKnots = Mathf.MoveTowards(
                currentSpeedKnots,
                0f,
                idleDragKnotsPerSecond * deltaTime);
        }
    }

    private void UpdateTurning(float deltaTime)
    {
        bool canTurn = currentSpeedKnots > minSpeedToTurnKnots;

        if (canTurn && IsShipKeyPressed(KeyCode.F))
        {
            _followOrderedHeading = false;
            _targetTurnRateDegreesPerSecond = -turnSpeed;
        }
        else if (canTurn && IsShipKeyPressed(KeyCode.H))
        {
            _followOrderedHeading = false;
            _targetTurnRateDegreesPerSecond = turnSpeed;
        }
        else if (canTurn && _followOrderedHeading)
        {
            float headingError = Mathf.DeltaAngle(currentHeadingDegrees, targetHeadingDegrees);
            if (Mathf.Abs(headingError) <= 0.5f)
                _targetTurnRateDegreesPerSecond = 0f;
            else
                _targetTurnRateDegreesPerSecond = Mathf.Sign(headingError) * turnSpeed;
        }
        else
        {
            _targetTurnRateDegreesPerSecond = 0f;
        }

        _currentTurnRateDegreesPerSecond = Mathf.MoveTowards(
            _currentTurnRateDegreesPerSecond,
            _targetTurnRateDegreesPerSecond,
            turnAcceleration * deltaTime);

        if (Mathf.Abs(_currentTurnRateDegreesPerSecond) > 0.001f)
        {
            float yaw = transform.eulerAngles.y + _currentTurnRateDegreesPerSecond * deltaTime;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    private void ApplyMovement(float deltaTime)
    {
        if (currentSpeedKnots <= 0f)
            return;

        float speedMetersPerSecond = currentSpeedKnots * KnotsToMetersPerSecond;
        transform.position += transform.forward * (speedMetersPerSecond * deltaTime);
    }

    /// <summary>
    /// Reads keyboard state via the Input System (required when project uses Input System only).
    /// Falls back to legacy Input when both systems are enabled.
    /// </summary>
    private static bool IsShipKeyPressed(KeyCode keyCode)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            Key inputSystemKey = KeyFromKeyCode(keyCode);
            if (inputSystemKey != Key.None)
                return Keyboard.current[inputSystemKey].isPressed;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(keyCode);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static Key KeyFromKeyCode(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.T => Key.T,
            KeyCode.G => Key.G,
            KeyCode.F => Key.F,
            KeyCode.H => Key.H,
            _ => Key.None
        };
    }
#endif

    /// <summary>
    /// Applies a captain sail order. Manual T/G input overrides until the next order is issued.
    /// </summary>
    public void ApplySailOrder(SailOrder order, float idealSpeedKnots)
    {
        _activeSailOrder = order;
        targetSpeedKnots = Mathf.Max(0f, idealSpeedKnots);
        _followSailOrder = true;
    }

    /// <summary>
    /// Legacy hook for older overlay code. Prefer ApplySailOrder via CaptainCommandManager.
    /// </summary>
    public void SetTargetSpeedKnots(float knots)
    {
        targetSpeedKnots = Mathf.Max(0f, knots);
        _followSailOrder = true;
    }

    /// <summary>
    /// Sets an ordered heading from the Course / Bearing instrument. Manual F/H input overrides until the next order.
    /// </summary>
    public void SetTargetHeading(float headingDegrees)
    {
        targetHeadingDegrees = NormalizeHeading(headingDegrees);
        _followOrderedHeading = true;
    }

    private static float NormalizeHeading(float yawDegrees)
    {
        yawDegrees %= 360f;
        if (yawDegrees < 0f)
            yawDegrees += 360f;
        return yawDegrees;
    }

    private void OnGUI()
    {
        const int boxWidth = 240;
        const int boxHeight = 88;
        var rect = new Rect(12f, 12f, boxWidth, boxHeight);

        GUI.Box(rect, "Ship Movement (TFGH)");
        GUILayout.BeginArea(new Rect(rect.x + 8f, rect.y + 22f, boxWidth - 16f, boxHeight - 28f));
        GUILayout.Label($"Speed: {currentSpeedKnots:F1} kn");
        GUILayout.Label($"Target: {targetSpeedKnots:F1} kn");
        GUILayout.Label($"Heading: {currentHeadingDegrees:F0}°");
        GUILayout.Label($"Target Hdg: {targetHeadingDegrees:F0}°");
        GUILayout.Label($"Turn rate: {_currentTurnRateDegreesPerSecond:F1}°/s");
        GUILayout.Label("T ahead  G astern  F/H turn");
        GUILayout.EndArea();
    }
}
