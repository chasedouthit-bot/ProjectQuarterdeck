using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipMovementPrototype))]
public class ShipAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip oceanAmbianceClip;
    [SerializeField] private AudioClip shipMovementClip;

    [Header("Audio Volumes")]
    [Range(0f, 1f)] [SerializeField] private float maxOceanVolume = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float maxShipVolume = 0.8f;
    [SerializeField] private float maxVolumeSpeedKnots = 10f;

    private ShipMovementPrototype _movement;
    private AudioSource _oceanAmbianceSource;
    private AudioSource _shipMovementSource;

    void Start()
    {
        _movement = GetComponent<ShipMovementPrototype>();

        // Load clips programmatically if not manually assigned in inspector
        if (oceanAmbianceClip == null)
        {
            oceanAmbianceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/OceanWavesAmbiance.wav");
        }
        if (shipMovementClip == null)
        {
            shipMovementClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ShipSailingMovement.wav");
        }

        // Set up global ambient ocean sound (non-spatial, attached to camera or ship root)
        _oceanAmbianceSource = gameObject.AddComponent<AudioSource>();
        _oceanAmbianceSource.clip = oceanAmbianceClip;
        _oceanAmbianceSource.loop = true;
        _oceanAmbianceSource.spatialBlend = 0f; // 2D background sound
        _oceanAmbianceSource.volume = maxOceanVolume;
        _oceanAmbianceSource.playOnAwake = true;

        if (oceanAmbianceClip != null)
        {
            _oceanAmbianceSource.Play();
        }

        // Set up ship movement sound (spatial 3D sound originating from the ship itself)
        _shipMovementSource = gameObject.AddComponent<AudioSource>();
        _shipMovementSource.clip = shipMovementClip;
        _shipMovementSource.loop = true;
        _shipMovementSource.spatialBlend = 1f; // 3D spatialized sound
        _shipMovementSource.minDistance = 5f;
        _shipMovementSource.maxDistance = 100f;
        _shipMovementSource.volume = 0f; // Start silent, increases with speed
        _shipMovementSource.playOnAwake = true;

        if (shipMovementClip != null)
        {
            _shipMovementSource.Play();
        }
    }

    void Update()
    {
        if (_movement == null || _shipMovementSource == null) return;

        float speed = _movement.CurrentSpeedKnots;
        float speedRatio = Mathf.Clamp01(speed / maxVolumeSpeedKnots);

        // Adjust ship sound volume and slightly pitch-bend it for realism
        _shipMovementSource.volume = Mathf.Lerp(0.05f * maxShipVolume, maxShipVolume, speedRatio);
        _shipMovementSource.pitch = Mathf.Lerp(0.95f, 1.1f, speedRatio);
    }
}
