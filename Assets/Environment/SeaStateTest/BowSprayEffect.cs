using UnityEngine;

/// <summary>
/// Bow wave spray driven by how far the bow dips into the water during heavy seas.
/// Intended for storm state — enable via <see cref="SetActiveForSeaState"/>.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class BowSprayEffect : MonoBehaviour
{
    [SerializeField] FloatingObject ship;
    [SerializeField] OceanWaveController ocean;
    [SerializeField] Transform sprayOrigin;
    [SerializeField] Vector3 sprayOriginLocal = new Vector3(0f, 0.15f, 8.85f);
    [SerializeField] float submergeSensitivity = 55f;
    [SerializeField] float impactSensitivity = 35f;
    [SerializeField] float pitchSensitivity = 18f;
    [SerializeField] float maxEmissionRate = 420f;
    [SerializeField] float burstThreshold = 2.5f;
    [SerializeField] int maxBurstCount = 28;

    ParticleSystem _spray;
    ParticleSystem _mist;
    float _previousSubmerge;
    float _previousPitch;
    bool _stormActive;

    void Awake()
    {
        if (ship == null)
            ship = GetComponent<FloatingObject>();

        if (ocean == null)
            ocean = FindFirstObjectByType<OceanWaveController>();

        EnsureSprayOrigin();
        _spray = CreateSpraySystem("BowSpray", 0.12f, 0.35f, 6f, 16f, 520);
        _mist = CreateSpraySystem("BowMist", 0.35f, 0.85f, 2f, 7f, 220);
        SetStormActive(false);
    }

    void LateUpdate()
    {
        if (!_stormActive || ocean == null || sprayOrigin == null)
        {
            StopSpray();
            return;
        }

        Vector3 bowPos = sprayOrigin.position;
        float waterY = ocean.GetWaveHeight(bowPos);
        float submerge = waterY - bowPos.y;
        float deltaSubmerge = (submerge - _previousSubmerge) / Mathf.Max(Time.deltaTime, 0.0001f);
        _previousSubmerge = submerge;

        float pitch = NormalizePitch(transform.localEulerAngles.x);
        float pitchRate = (pitch - _previousPitch) / Mathf.Max(Time.deltaTime, 0.0001f);
        _previousPitch = pitch;

        float intensity = 0f;
        if (submerge > -0.35f)
            intensity += Mathf.Max(0f, submerge + 0.15f) * submergeSensitivity;

        if (deltaSubmerge > 0f)
            intensity += deltaSubmerge * impactSensitivity;

        if (pitch > 0.5f)
            intensity += pitch * pitchSensitivity;

        if (pitchRate > 0f)
            intensity += pitchRate * 0.35f;

        intensity = Mathf.Clamp(intensity, 0f, maxEmissionRate * 0.35f);

        float rate = Mathf.Clamp(intensity * 14f, 0f, maxEmissionRate);
        SetEmissionRate(_spray, rate);
        SetEmissionRate(_mist, rate * 0.35f);

        if (intensity >= burstThreshold)
        {
            int burst = Mathf.Clamp(Mathf.RoundToInt(intensity * 0.45f), 4, maxBurstCount);
            _spray.Emit(burst);
            if (intensity >= burstThreshold * 1.6f)
                _mist.Emit(Mathf.Max(2, burst / 3));
        }

        if (rate > 0f)
        {
            if (!_spray.isPlaying)
                _spray.Play();
            if (!_mist.isPlaying)
                _mist.Play();
        }
        else
        {
            StopSpray();
        }
    }

    public void SetActiveForSeaState(SeaStateManager.SeaStateMode mode)
    {
        SetStormActive(mode == SeaStateManager.SeaStateMode.Storm);
    }

    public void SetStormActive(bool active)
    {
        _stormActive = active;
        if (!active)
            StopSpray();
    }

    void EnsureSprayOrigin()
    {
        if (sprayOrigin != null)
            return;

        var originGo = new GameObject("BowSprayOrigin");
        originGo.transform.SetParent(transform, false);
        originGo.transform.localPosition = sprayOriginLocal;
        originGo.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        sprayOrigin = originGo.transform;
    }

    ParticleSystem CreateSpraySystem(
        string objectName,
        float minSize,
        float maxSize,
        float minSpeed,
        float maxSpeed,
        int maxParticles)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(sprayOrigin, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.gravityModifier = 1.15f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.96f, 1f, 0.75f),
            new Color(0.78f, 0.88f, 0.95f, 0.45f));

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.35f;
        shape.radiusThickness = 0.6f;
        shape.rotation = new Vector3(-70f, 0f, 0f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-2.5f, -0.5f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.82f, 0.9f, 0.96f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.4f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateSprayMaterial();

        return ps;
    }

    static Material CreateSprayMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");

        var material = new Material(shader);
        material.color = Color.white;
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        return material;
    }

    static void SetEmissionRate(ParticleSystem ps, float rate)
    {
        var emission = ps.emission;
        emission.rateOverTime = rate;
    }

    void StopSpray()
    {
        if (_spray != null)
            SetEmissionRate(_spray, 0f);
        if (_mist != null)
            SetEmissionRate(_mist, 0f);

        if (_spray != null && _spray.isPlaying)
            _spray.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (_mist != null && _mist.isPlaying)
            _mist.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        _previousSubmerge = 0f;
        _previousPitch = 0f;
    }

    static float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f)
            eulerX -= 360f;
        return eulerX;
    }
}
