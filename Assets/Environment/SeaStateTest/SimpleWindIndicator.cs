using UnityEngine;

/// <summary>
/// Rotates a small arrow to show wind direction for the current sea state.
/// </summary>
[DisallowMultipleComponent]
public class SimpleWindIndicator : MonoBehaviour
{
    [SerializeField] Transform arrow;
    [SerializeField] Vector3 windDirection = new Vector3(1f, 0f, 0.3f);
    [SerializeField] float rotationSpeed = 2f;

    public void SetWindDirection(Vector3 direction)
    {
        windDirection = direction;
    }

    void LateUpdate()
    {
        if (arrow == null)
            arrow = transform;

        Vector3 flat = new Vector3(windDirection.x, 0f, windDirection.z);
        if (flat.sqrMagnitude < 0.001f)
            return;

        Quaternion target = Quaternion.LookRotation(flat.normalized, Vector3.up);
        arrow.rotation = Quaternion.Slerp(arrow.rotation, target, Time.deltaTime * rotationSpeed);
    }
}
