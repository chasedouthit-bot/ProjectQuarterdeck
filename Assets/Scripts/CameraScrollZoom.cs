using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
public class CameraScrollZoom : CinemachineExtension
{
    [Header("Zoom Limits")]
    [SerializeField] private float minDistance = 20f;
    [SerializeField] private float maxDistance = 150f;
    [SerializeField] private float zoomSpeed = 30f;
    [SerializeField] private float smoothTime = 0.12f;

    [Header("Orbit Limits & Speed")]
    [SerializeField] private float orbitSensitivity = 3f;
    [SerializeField] private float minPitch = 5f;
    [SerializeField] private float maxPitch = 80f;

    private float _targetDistance;
    private float _currentDistance;
    private float _zoomVelocity;

    private float _yaw;
    private float _pitch;
    private float _targetYaw;
    private float _targetPitch;
    private float _yawVelocity;
    private float _pitchVelocity;

    void Start()
    {
        // Bootstrap orbit state from the existing CinemachineFollow offset (read-only, never written back)
        var follow = GetComponent<CinemachineFollow>();
        Vector3 offset = follow != null ? follow.FollowOffset : new Vector3(0f, 25f, -65f);

        _currentDistance = offset.magnitude < 0.1f ? 65f : offset.magnitude;
        _yaw = Mathf.Atan2(offset.x, -offset.z) * Mathf.Rad2Deg;
        float planarDist = new Vector2(offset.x, offset.z).magnitude;
        _pitch = Mathf.Atan2(offset.y, planarDist) * Mathf.Rad2Deg;

        _targetDistance = _currentDistance;
        _targetYaw = _yaw;
        _targetPitch = _pitch;
    }

    void Update()
    {
        HandleZoom();
        HandleOrbit();

        _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _zoomVelocity, smoothTime);
        _yaw   = Mathf.SmoothDampAngle(_yaw,   _targetYaw,   ref _yawVelocity,   smoothTime);
        _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVelocity, smoothTime);
    }

    // Runs after each Cinemachine pipeline stage. We override the Body stage result so the
    // camera position is computed from our runtime orbit state rather than from FollowOffset.
    // CameraState is a value type passed by ref — nothing here is serialized, so no save prompt.
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Body || vcam.Follow == null) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
        state.RawPosition    = vcam.Follow.position + offset;
        state.RawOrientation = Quaternion.LookRotation(-offset.normalized);
    }

    private void HandleZoom()
    {
        float scroll = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            scroll = Mouse.current.scroll.ReadValue().y * 0.05f;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Abs(scroll) < 0.001f)
            scroll = Input.mouseScrollDelta.y;
#endif

        if (Mathf.Abs(scroll) > 0.001f)
            _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    private void HandleOrbit()
    {
        bool isRmbPressed = false;
        float deltaX = 0f;
        float deltaY = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            isRmbPressed = Mouse.current.rightButton.isPressed;
            deltaX = Mouse.current.delta.x.ReadValue() * 0.05f;
            deltaY = Mouse.current.delta.y.ReadValue() * 0.05f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!isRmbPressed)
        {
            isRmbPressed = Input.GetMouseButton(1);
            deltaX = Input.GetAxis("Mouse X");
            deltaY = Input.GetAxis("Mouse Y");
        }
#endif

        if (isRmbPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _targetYaw   += deltaX * orbitSensitivity;
            _targetPitch -= deltaY * orbitSensitivity;
            _targetPitch  = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
