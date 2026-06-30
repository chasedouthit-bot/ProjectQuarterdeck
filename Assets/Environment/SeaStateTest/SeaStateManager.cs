using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Switches between Calm, Choppy, and Storm sea states with F1, F2, and F3.
/// Updates ocean waves, fog, lighting, rain, UI text, and wind.
/// </summary>
[DisallowMultipleComponent]
public class SeaStateManager : MonoBehaviour
{
    public enum SeaStateMode
    {
        Calm,
        Choppy,
        Storm
    }

    [System.Serializable]
    public class SeaStatePreset
    {
        public string displayName = "Calm Sea";
        public float waveHeight = 0.35f;
        public float waveSpeed = 0.6f;
        [Tooltip("Distance between wave crests in meters. Larger = longer, slower swells.")]
        public float waveWavelengthMeters = 100f;
        [Tooltip("Shorter secondary waves on top of the primary swell.")]
        public float chopWavelengthMeters = 45f;
        [Range(0f, 1f)] public float chopHeightRatio = 0.15f;
        public Color waterColor = new Color(0.15f, 0.45f, 0.62f);
        public Color fogColor = new Color(0.72f, 0.84f, 0.92f);
        public float fogStart = 200f;
        public float fogEnd = 2500f;
        public Color ambientLight = new Color(0.55f, 0.62f, 0.72f);
        public float sunIntensity = 1.2f;
        public Color sunColor = new Color(1f, 0.97f, 0.88f);
        public Vector3 windDirection = new Vector3(0.4f, 0f, 0.2f);
        public bool enableRain;
        public int rainEmissionRate = 0;
        public Color skyColor = new Color(0.55f, 0.72f, 0.88f);
    }

    [SerializeField] OceanWaveController ocean;
    [SerializeField] Light sunLight;
    [SerializeField] ParticleSystem rainParticles;
    [SerializeField] BowSprayEffect bowSpray;
    [SerializeField] Text stateLabel;
    [SerializeField] SimpleWindIndicator windIndicator;
    [SerializeField] SeaStatePreset calmPreset = new SeaStatePreset
    {
        displayName = "Calm Sea",
        waveHeight = 0.25f,
        waveSpeed = 0.45f,
        waveWavelengthMeters = 110f,
        chopWavelengthMeters = 50f,
        chopHeightRatio = 0.12f,
        waterColor = new Color(0.18f, 0.52f, 0.68f),
        fogColor = new Color(0.72f, 0.86f, 0.95f),
        fogStart = 300f,
        fogEnd = 3000f,
        ambientLight = new Color(0.58f, 0.68f, 0.78f),
        sunIntensity = 1.25f,
        sunColor = new Color(1f, 0.98f, 0.9f),
        windDirection = new Vector3(0.3f, 0f, 0.15f),
        enableRain = false,
        rainEmissionRate = 0,
        skyColor = new Color(0.55f, 0.78f, 0.92f)
    };
    [SerializeField] SeaStatePreset choppyPreset = new SeaStatePreset
    {
        displayName = "Choppy Sea",
        waveHeight = 0.85f,
        waveSpeed = 0.95f,
        waveWavelengthMeters = 48f,
        chopWavelengthMeters = 22f,
        chopHeightRatio = 0.35f,
        waterColor = new Color(0.08f, 0.28f, 0.42f),
        fogColor = new Color(0.45f, 0.55f, 0.62f),
        fogStart = 120f,
        fogEnd = 1400f,
        ambientLight = new Color(0.38f, 0.44f, 0.5f),
        sunIntensity = 0.95f,
        sunColor = new Color(0.92f, 0.92f, 0.88f),
        windDirection = new Vector3(0.8f, 0f, 0.35f),
        enableRain = false,
        rainEmissionRate = 0,
        skyColor = new Color(0.42f, 0.5f, 0.58f)
    };
    [SerializeField] SeaStatePreset stormPreset = new SeaStatePreset
    {
        displayName = "Storm Sea",
        waveHeight = 3.5f,
        waveSpeed = 1.1f,
        waveWavelengthMeters = 290f,
        chopWavelengthMeters = 38f,
        chopHeightRatio = 0.18f,
        waterColor = new Color(0.04f, 0.12f, 0.2f),
        fogColor = new Color(0.18f, 0.22f, 0.28f),
        fogStart = 40f,
        fogEnd = 650f,
        ambientLight = new Color(0.22f, 0.25f, 0.3f),
        sunIntensity = 0.45f,
        sunColor = new Color(0.65f, 0.68f, 0.72f),
        windDirection = new Vector3(1f, 0f, 0.55f),
        enableRain = true,
        rainEmissionRate = 1200,
        skyColor = new Color(0.12f, 0.14f, 0.18f)
    };

    SeaStateMode _currentMode = SeaStateMode.Calm;

    void Start()
    {
        if (ocean == null)
            ocean = FindFirstObjectByType<OceanWaveController>();

        EnsureBowSpray();
        stateLabel = SeaStateHudBuilder.EnsureBuilt(stateLabel);
        ApplySeaState(SeaStateMode.Calm);
    }

    void EnsureBowSpray()
    {
        if (bowSpray != null)
            return;

        bowSpray = FindFirstObjectByType<BowSprayEffect>();
        if (bowSpray != null)
            return;

        var ship = FindFirstObjectByType<FloatingObject>();
        if (ship == null || ship.Mode != FloatingObject.FloatMode.ShipPitchRoll)
            return;

        bowSpray = ship.gameObject.AddComponent<BowSprayEffect>();
    }

    void Update()
    {
        if (SeaStateInput.WasCalmSeaPressed())
            ApplySeaState(SeaStateMode.Calm);
        else if (SeaStateInput.WasChoppySeaPressed())
            ApplySeaState(SeaStateMode.Choppy);
        else if (SeaStateInput.WasStormSeaPressed())
            ApplySeaState(SeaStateMode.Storm);
    }

    public void ApplySeaState(SeaStateMode mode)
    {
        _currentMode = mode;
        SeaStatePreset preset = GetPreset(mode);
        ApplyPreset(preset);
    }

    SeaStatePreset GetPreset(SeaStateMode mode)
    {
        return mode switch
        {
            SeaStateMode.Choppy => choppyPreset,
            SeaStateMode.Storm => stormPreset,
            _ => calmPreset
        };
    }

    void ApplyPreset(SeaStatePreset preset)
    {
        if (ocean != null)
            ocean.ApplyWaveSettings(
                preset.waveHeight,
                preset.waveSpeed,
                preset.waveWavelengthMeters,
                preset.chopWavelengthMeters,
                preset.chopHeightRatio,
                preset.waterColor);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = preset.fogColor;
        RenderSettings.fogStartDistance = preset.fogStart;
        RenderSettings.fogEndDistance = preset.fogEnd;
        RenderSettings.ambientLight = preset.ambientLight;

        if (sunLight != null)
        {
            sunLight.intensity = preset.sunIntensity;
            sunLight.color = preset.sunColor;
        }

        if (stateLabel != null)
            stateLabel.text = preset.displayName;

        if (windIndicator != null)
            windIndicator.SetWindDirection(preset.windDirection);

        ApplyRain(preset);

        if (bowSpray != null)
            bowSpray.SetActiveForSeaState(_currentMode);

        Camera cam = Camera.main;
        if (cam != null)
            cam.backgroundColor = preset.skyColor;
    }

    void ApplyRain(SeaStatePreset preset)
    {
        if (rainParticles == null)
            return;

        var emission = rainParticles.emission;
        emission.rateOverTime = preset.enableRain ? preset.rainEmissionRate : 0f;

        if (preset.enableRain && !rainParticles.isPlaying)
            rainParticles.Play();
        else if (!preset.enableRain && rainParticles.isPlaying)
            rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
