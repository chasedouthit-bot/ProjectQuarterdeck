using UnityEngine;

/// <summary>
/// Simple procedural motion for the prototype ocean material.
/// Scrolls a generated ripple texture and drives shader wave phase / color pulse.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class PrototypeOceanMotion : MonoBehaviour
{
    [SerializeField] Vector2 scrollSpeed = new(0.03f, 0.015f);
    [SerializeField] float waveSpeed = 0.25f;
    [SerializeField] float colorPulseAmount = 0.05f;
    [SerializeField] int textureSize = 128;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int WavePhaseId = Shader.PropertyToID("_WavePhase");
    static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");

    Material _material;
    Vector2 _scrollOffset;
    Color _baseShallowColor;
    Texture2D _rippleTexture;

    void Awake()
    {
        var renderer = GetComponent<Renderer>();
        _material = renderer.material;
        _rippleTexture = BuildRippleTexture(textureSize);
        _material.SetTexture(BaseMapId, _rippleTexture);

        if (_material.HasProperty(ShallowColorId))
            _baseShallowColor = _material.GetColor(ShallowColorId);
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);

        if (_rippleTexture != null)
            Destroy(_rippleTexture);
    }

    void Update()
    {
        if (_material == null)
            return;

        _scrollOffset += scrollSpeed * Time.deltaTime;
        _material.SetTextureOffset(BaseMapId, _scrollOffset);
        _material.SetTextureScale(BaseMapId, new Vector2(24f, 24f));

        if (_material.HasProperty(WavePhaseId))
            _material.SetFloat(WavePhaseId, Time.time * waveSpeed);

        if (_material.HasProperty(ShallowColorId))
        {
            float pulse = Mathf.Sin(Time.time * 0.6f) * colorPulseAmount;
            _material.SetColor(ShallowColorId, _baseShallowColor * (1f + pulse));
        }
    }

    static Texture2D BuildRippleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "PrototypeOcean_Ripples"
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float n1 = Mathf.PerlinNoise(u * 6f, v * 6f);
                float n2 = Mathf.PerlinNoise(u * 14f + 2.3f, v * 14f + 1.1f);
                float ripple = Mathf.Clamp01(n1 * 0.65f + n2 * 0.35f);
                pixels[y * size + x] = new Color(ripple, ripple, ripple, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
