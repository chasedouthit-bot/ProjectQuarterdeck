using UnityEngine;
using UnityEngine.EventSystems;


// Used to be based on the following. A lot of it was torn out and converted to be a LOT simpler.
// Just drop it on the camera object and use the basic input to fly around.
// by Jehremie Woods - Salty Sea Dogs Games - www.saltyseadogs.net

// Very simple smooth mouselook modifier for the MainCamera in Unity
// by Francis R. Griffiths-Keam - www.runningdimensions.com
// http://forum.unity3d.com/threads/a-free-simple-smooth-mouselook.73117/

    namespace SaltySeaDogs
{
    public class SimpleMovement : MonoBehaviour
    {
        Vector2 _mouseAbsolute;
        Vector2 _smoothMouse;

        public Vector2 clampInDegrees = new Vector2(360, 180);
        public Vector2 sensitivity = new Vector2(2, 2);
        public Vector2 smoothing = new Vector2(3, 3);
        public Vector2 targetDirection;
        public Vector2 targetCharacterDirection;

        private bool _mouselookEnabled = false;
        public float flySpeed = 0.5f;


        void Start()
        {
            // Set target direction to the camera's initial orientation.
            targetDirection = transform.localRotation.eulerAngles;
        }

        void Update()
        {

            if (EventSystem.current.IsPointerOverGameObject()) { return; }

            _mouselookEnabled = Input.GetMouseButton(0);

            //ensure these stay this way
            Cursor.lockState = (_mouselookEnabled) ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_mouselookEnabled;

            if (!_mouselookEnabled)
                return;

            // Allow the script to clamp based on a desired target value.
            var targetOrientation = Quaternion.Euler(targetDirection);

            // Get raw mouse input for a cleaner reading on more sensitive mice.
            var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            // Scale input against the sensitivity setting and multiply that against the smoothing value.
            mouseDelta = Vector2.Scale(mouseDelta, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));

            // Interpolate mouse movement over time to apply smoothing delta.
            _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
            _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

            // Find the absolute mouse movement value from point zero.
            _mouseAbsolute += _smoothMouse;

            // Clamp and apply the local x value first, so as not to be affected by world transforms.
            if (clampInDegrees.x < 360)
                _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

            // Then clamp and apply the global y value.
            if (clampInDegrees.y < 360)
                _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

            var xRotation = Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right);
            transform.localRotation = xRotation * targetOrientation;
            var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, transform.InverseTransformDirection(Vector3.up));
            transform.localRotation *= yRotation;

            //movement
            if (Input.GetAxis("Vertical") != 0)
            {
                transform.Translate(transform.forward * flySpeed * Input.GetAxis("Vertical"), Space.World);
            }
            if (Input.GetAxis("Horizontal") != 0)
            {
                transform.Translate(transform.right * flySpeed * Input.GetAxis("Horizontal"), Space.World);
            }
            if (Input.GetKey(KeyCode.Q))
            {
                transform.Translate(transform.up * flySpeed * 0.5f, Space.World);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                transform.Translate(-transform.up * flySpeed * 0.5f, Space.World);
            }
        }
    }
}

