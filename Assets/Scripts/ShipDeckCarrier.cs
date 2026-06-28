using UnityEngine;

/// <summary>
/// Keeps a CharacterController glued to a moving ship without parenting conflicts.
/// Add to the player — do not modify FirstPersonController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)]
public class ShipDeckCarrier : MonoBehaviour
{
    [SerializeField] private Transform ship;
    [SerializeField] private bool rotateWithShip = true;

    private CharacterController _controller;
    private Vector3 _lastShipPosition;
    private Quaternion _lastShipRotation;
    private bool _initialized;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (ship == null)
        {
            var shipObject = GameObject.Find("Quarterdeck_Graybox");
            if (shipObject != null)
                ship = shipObject.transform;
        }

        if (ship == null)
        {
            Debug.LogWarning("ShipDeckCarrier: No ship assigned.");
            return;
        }

        // CharacterController fights transform parenting — stay in world space instead.
        if (transform.parent == ship)
            transform.SetParent(null, true);

        _lastShipPosition = ship.position;
        _lastShipRotation = ship.rotation;
        _initialized = true;
    }

    private void LateUpdate()
    {
        if (!_initialized || ship == null)
            return;

        Vector3 shipDeltaPosition = ship.position - _lastShipPosition;
        Quaternion shipDeltaRotation = ship.rotation * Quaternion.Inverse(_lastShipRotation);

        // Carry the player with the ship's translation and rotation about the ship origin.
        Vector3 offsetFromShip = transform.position - _lastShipPosition;
        Vector3 rotationCarry = shipDeltaRotation * offsetFromShip - offsetFromShip;
        Vector3 platformMotion = shipDeltaPosition + rotationCarry;

        if (platformMotion.sqrMagnitude > 0f)
            _controller.Move(platformMotion);

        if (rotateWithShip)
        {
            float deltaYaw = Mathf.DeltaAngle(_lastShipRotation.eulerAngles.y, ship.eulerAngles.y);
            if (Mathf.Abs(deltaYaw) > 0.001f)
                transform.Rotate(0f, deltaYaw, 0f, Space.World);
        }

        _lastShipPosition = ship.position;
        _lastShipRotation = ship.rotation;
    }
}
