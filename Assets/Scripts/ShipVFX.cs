using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShipMovementPrototype))]
public class ShipVFX : MonoBehaviour
{
    [Header("Wake Settings")]
    [SerializeField] private float baseWakeEmission = 15f;
    [SerializeField] private float baseSplashEmission = 10f;
    [SerializeField] private float maxEmissionSpeedKnots = 10f;

    private ShipMovementPrototype _movement;
    private ParticleSystem _sternWake;
    private ParticleSystem _bowSplashPort;
    private ParticleSystem _bowSplashStbd;

    void Start()
    {
        _movement = GetComponent<ShipMovementPrototype>();

        // Create stern wake particle system
        _sternWake = CreateParticleSystem("SternWake", new Vector3(0f, -0.2f, -18f), new Vector3(180f, 0f, 0f), 2.5f);
        
        // Create bow splash particle systems
        _bowSplashPort = CreateParticleSystem("BowSplashPort", new Vector3(-2.2f, -0.2f, 14f), new Vector3(0f, -45f, 0f), 1.2f);
        _bowSplashStbd = CreateParticleSystem("BowSplashStbd", new Vector3(2.2f, -0.2f, 14f), new Vector3(0f, 45f, 0f), 1.2f);
    }

    void Update()
    {
        if (_movement == null) return;

        float currentSpeed = _movement.CurrentSpeedKnots;
        float speedRatio = Mathf.Clamp01(currentSpeed / maxEmissionSpeedKnots);

        // Update Stern Wake Emission
        if (_sternWake != null)
        {
            var emission = _sternWake.emission;
            emission.rateOverTime = baseWakeEmission * speedRatio;
        }

        // Update Bow Splash Emission
        if (_bowSplashPort != null)
        {
            var emission = _bowSplashPort.emission;
            emission.rateOverTime = baseSplashEmission * speedRatio;
        }
        if (_bowSplashStbd != null)
        {
            var emission = _bowSplashStbd.emission;
            emission.rateOverTime = baseSplashEmission * speedRatio;
        }
    }

    private ParticleSystem CreateParticleSystem(string sysName, Vector3 localPos, Vector3 localRotation, float startSize)
    {
        GameObject go = new GameObject(sysName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localRotation);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        // Configure Main Module
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.startLifetime = 4.0f;
        main.startSpeed = sysName.Contains("Stern") ? 1.5f : 3.5f;
        main.startSize = startSize;
        main.startColor = new Color(0.95f, 0.98f, 1f, 0.35f);
        main.gravityModifier = sysName.Contains("Stern") ? 0f : 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Critical: particles must float in world space!

        // Configure Emission Module (starts at 0)
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        // Configure Shape Module
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.5f;

        // Configure Size over Lifetime
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(1f, 2.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Configure Color over Lifetime (fade out)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0.95f, 0.98f, 1f), 0.0f), new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.35f, 0.0f), new GradientAlphaKey(0.2f, 0.1f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        // Material setup: Use default particle shader if possible
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingFudge = 50f; // Make sure translucent particles render correctly
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader != null)
            {
                Material particleMat = new Material(particleShader);
                particleMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
                // Set blending properties for translucent foam
                particleMat.SetFloat("_Blend", 0f); // Alpha blend
                renderer.sharedMaterial = particleMat;
            }
        }

        return ps;
    }
}
