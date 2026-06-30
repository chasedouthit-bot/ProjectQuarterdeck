using UnityEngine;

/// <summary>
/// Minimal mouse-look for the deck camera in the sea state test scene.
/// </summary>
[DisallowMultipleComponent]
public class SeaStateDeckLook : MonoBehaviour
{
    [Tooltip("Degrees of rotation per pixel of mouse movement. Typical range 0.01–0.04.")]
    [SerializeField] float mouseSensitivity = 0.02f;
    [SerializeField] float minPitch = -25f;
    [SerializeField] float maxPitch = 35f;

    float _pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _pitch = transform.localEulerAngles.x;
    }

    void Update()
    {
        if (SeaStateInput.WasEscapePressed())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = SeaStateInput.ReadMouseDelta();
        float yaw = mouseDelta.x * mouseSensitivity;
        _pitch -= mouseDelta.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(_pitch, transform.localEulerAngles.y + yaw, 0f);
    }
}
