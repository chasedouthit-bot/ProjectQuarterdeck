using UnityEngine;

/// <summary>
/// Makes a transform follow procedural ocean waves. Ships get pitch/roll from sample points;
/// buoys only bob up and down.
/// </summary>
[DisallowMultipleComponent]
public class FloatingObject : MonoBehaviour
{
    public enum FloatMode
    {
        SimpleBob,
        ShipPitchRoll
    }

    [SerializeField] FloatMode floatMode = FloatMode.SimpleBob;

    public FloatMode Mode => floatMode;

    [SerializeField] OceanWaveController ocean;
    [SerializeField] float heightOffset = 0f;
    [SerializeField] float followSmoothing = 6f;
    [SerializeField] float rotationSmoothing = 4f;

    [Header("Ship sample points (local space)")]
    [SerializeField] Vector3 bowSampleLocal = new Vector3(0f, 0f, 6f);
    [SerializeField] Vector3 sternSampleLocal = new Vector3(0f, 0f, -6f);
    [SerializeField] Vector3 portSampleLocal = new Vector3(-3f, 0f, 0f);
    [SerializeField] Vector3 starboardSampleLocal = new Vector3(3f, 0f, 0f);
    [SerializeField] float pitchRollStrength = 1f;

    Vector3 _restLocalPosition;
    Quaternion _restLocalRotation;

    void Start()
    {
        if (ocean == null)
            ocean = FindFirstObjectByType<OceanWaveController>();

        _restLocalPosition = transform.localPosition;
        _restLocalRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (ocean == null)
            return;

        if (floatMode == FloatMode.SimpleBob)
            UpdateSimpleBob();
        else
            UpdateShipPitchRoll();
    }

    void UpdateSimpleBob()
    {
        Vector3 worldPos = transform.position;
        float targetY = ocean.GetWaveHeight(worldPos) + heightOffset;
        Vector3 target = new Vector3(worldPos.x, targetY, worldPos.z);
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followSmoothing);
    }

    void UpdateShipPitchRoll()
    {
        Transform parent = transform.parent;
        Vector3 parentPos = parent != null ? parent.position : Vector3.zero;

        // Sample wave height at bow, stern, port, and starboard in world space.
        Vector3 bowWorld = transform.TransformPoint(bowSampleLocal);
        Vector3 sternWorld = transform.TransformPoint(sternSampleLocal);
        Vector3 portWorld = transform.TransformPoint(portSampleLocal);
        Vector3 starboardWorld = transform.TransformPoint(starboardSampleLocal);
        Vector3 centerWorld = transform.position;

        float bowH = ocean.GetWaveHeight(bowWorld);
        float sternH = ocean.GetWaveHeight(sternWorld);
        float portH = ocean.GetWaveHeight(portWorld);
        float starboardH = ocean.GetWaveHeight(starboardWorld);
        float centerH = ocean.GetWaveHeight(centerWorld);

        float targetY = centerH + heightOffset;
        Vector3 targetPos = new Vector3(centerWorld.x, targetY, centerWorld.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothing);

        // Pitch: bow vs stern. Roll: port vs starboard.
        float pitch = Mathf.Atan2(bowH - sternH, Vector3.Distance(bowSampleLocal, sternSampleLocal)) * Mathf.Rad2Deg;
        float roll = Mathf.Atan2(portH - starboardH, Vector3.Distance(portSampleLocal, starboardSampleLocal)) * Mathf.Rad2Deg;

        Quaternion targetRot = _restLocalRotation * Quaternion.Euler(-pitch * pitchRollStrength, 0f, roll * pitchRollStrength);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * rotationSmoothing);
    }
}
