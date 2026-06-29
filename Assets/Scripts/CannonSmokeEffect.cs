using UnityEngine;

/// <summary>
/// Prototype cannon smoke and muzzle flash burst. Particles are configured in code — no external assets.
/// </summary>
[DisallowMultipleComponent]
public class CannonSmokeEffect : MonoBehaviour
{
    const float SmokeLifetimeSeconds = 3.5f;

    static Material _particleMaterial;

    [SerializeField] ParticleSystem smokeParticles;
    [SerializeField] ParticleSystem flashParticles;

    void Awake()
    {
        EnsureConfigured();
    }

    public void Play()
    {
        EnsureConfigured();

        if (smokeParticles != null)
        {
            smokeParticles.Clear(true);
            smokeParticles.Play(true);
        }

        if (flashParticles != null)
        {
            flashParticles.Clear(true);
            flashParticles.Play(true);
        }
    }

    public static CannonSmokeEffect SpawnAt(Transform muzzlePoint, CannonSmokeEffect prefab)
    {
        if (muzzlePoint == null)
            return null;

        CannonSmokeEffect instance = prefab != null
            ? Instantiate(prefab, muzzlePoint.position, muzzlePoint.rotation)
            : CreateRuntimeInstance(muzzlePoint.position, muzzlePoint.rotation);

        instance.Play();
        Destroy(instance.gameObject, SmokeLifetimeSeconds);
        return instance;
    }

    static CannonSmokeEffect CreateRuntimeInstance(Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("CannonSmokeEffect");
        root.transform.SetPositionAndRotation(position, rotation);
        var effect = root.AddComponent<CannonSmokeEffect>();
        effect.EnsureConfigured();
        return effect;
    }

#if UNITY_EDITOR
    void Reset()
    {
        EnsureConfigured();
    }
#endif

    public void EnsureConfigured()
    {
        if (smokeParticles == null)
        {
            Transform existing = transform.Find("Smoke");
            smokeParticles = existing != null
                ? existing.GetComponent<ParticleSystem>()
                : CreateSmokeSystem(transform);
        }

        if (flashParticles == null)
        {
            Transform existing = transform.Find("Flash");
            flashParticles = existing != null
                ? existing.GetComponent<ParticleSystem>()
                : CreateFlashSystem(transform);
        }
    }

    static ParticleSystem CreateSmokeSystem(Transform parent)
    {
        var go = new GameObject("Smoke", typeof(ParticleSystem));
        go.transform.SetParent(parent, false);

        var ps = go.GetComponent<ParticleSystem>();
        StopAndClear(ps);

        var main = ps.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.maxParticles = 64;
        main.startColor = new Color(0.75f, 0.75f, 0.75f, 0.9f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.1f;
        shape.rotation = Vector3.zero;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 0f),
                new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 1.6f));

        ApplyParticleMaterial(go.GetComponent<ParticleSystemRenderer>());
        return ps;
    }

    static ParticleSystem CreateFlashSystem(Transform parent)
    {
        var go = new GameObject("Flash", typeof(ParticleSystem));
        go.transform.SetParent(parent, false);

        var ps = go.GetComponent<ParticleSystem>();
        StopAndClear(ps);

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.12f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.maxParticles = 16;
        main.startColor = new Color(1f, 0.8f, 0.35f, 1f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.06f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ApplyParticleMaterial(go.GetComponent<ParticleSystemRenderer>());
        return ps;
    }

    static void StopAndClear(ParticleSystem ps)
    {
        var main = ps.main;
        main.playOnAwake = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    static void ApplyParticleMaterial(ParticleSystemRenderer renderer)
    {
        if (renderer == null)
            return;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetParticleMaterial();
    }

    static Material GetParticleMaterial()
    {
        if (_particleMaterial != null)
            return _particleMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        _particleMaterial = shader != null ? new Material(shader) : null;
        return _particleMaterial;
    }
}
