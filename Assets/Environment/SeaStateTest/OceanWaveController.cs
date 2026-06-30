using UnityEngine;

/// <summary>
/// Drives a subdivided ocean mesh with layered sine waves and exposes height sampling
/// for floating objects. Attach to the Ocean root object.
/// </summary>
[DisallowMultipleComponent]
public class OceanWaveController : MonoBehaviour
{
    const string OceanShaderName = "Quarterdeck/PrototypeOcean";

    [Header("Mesh")]
    [SerializeField] int gridResolution = 96;
    [SerializeField] float oceanSizeMeters = 400f;

    [Header("Current Wave Settings")]
    [SerializeField] float waveHeight = 0.35f;
    [SerializeField] float waveSpeed = 0.6f;
    [SerializeField] float primaryWavelengthMeters = 100f;
    [SerializeField] float chopWavelengthMeters = 40f;
    [SerializeField] float chopHeightRatio = 0.2f;
    [SerializeField] Color waterColor = new Color(0.15f, 0.45f, 0.62f, 1f);

    [Header("Surface Detail")]
    [SerializeField] Vector2 rippleScrollSpeed = new(0.025f, 0.012f);
    [SerializeField] float rippleTextureScale = 28f;
    [SerializeField] int rippleTextureSize = 128;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
    static readonly int WavePhaseId = Shader.PropertyToID("_WavePhase");
    static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
    static readonly int RippleStrengthId = Shader.PropertyToID("_RippleStrength");
    static readonly int FoamStrengthId = Shader.PropertyToID("_FoamStrength");

    Mesh _mesh;
    Vector3[] _baseVertices;
    Vector3[] _workingVertices;
    MeshRenderer _renderer;
    Material _waterMaterial;
    Texture2D _rippleTexture;
    Vector2 _rippleScrollOffset;

    public float WaveHeight => waveHeight;
    public float WaveSpeed => waveSpeed;
    public float PrimaryWavelengthMeters => primaryWavelengthMeters;

    void Awake()
    {
        BuildMesh();
        EnsureMaterial();
    }

    void Update()
    {
        UpdateMeshVertices();
        UpdateMaterialAnimation();
    }

    void OnDestroy()
    {
        if (_waterMaterial != null)
            Destroy(_waterMaterial);

        if (_rippleTexture != null)
            Destroy(_rippleTexture);
    }

    /// <summary>
    /// Sample procedural wave height at a world-space position (Y is ignored).
    /// </summary>
    public float GetWaveHeight(Vector3 worldPosition)
    {
        return EvaluateWaveHeight(worldPosition.x, worldPosition.z, Time.time);
    }

    /// <summary>
    /// Apply a preset from <see cref="SeaStateManager"/>.
    /// Wavelength is the distance between wave crests in meters (larger = longer swells).
    /// </summary>
    public void ApplyWaveSettings(
        float height,
        float speed,
        float primaryWavelength,
        float chopWavelength,
        float chopRatio,
        Color color)
    {
        waveHeight = height;
        waveSpeed = speed;
        primaryWavelengthMeters = Mathf.Max(primaryWavelength, 8f);
        chopWavelengthMeters = Mathf.Max(chopWavelength, 4f);
        chopHeightRatio = Mathf.Clamp01(chopRatio);
        waterColor = color;

        ApplyWaterColorsToMaterial();
        UpdateMaterialWaveParams();
    }

    float EvaluateWaveHeight(float worldX, float worldZ, float time)
    {
        float t = time * waveSpeed;
        float primaryK = Wavenumber(primaryWavelengthMeters);
        float chopK = Wavenumber(chopWavelengthMeters);

        float wave = 0f;
        wave += Mathf.Sin(worldX * primaryK + t) * 0.42f;
        wave += Mathf.Sin(worldZ * primaryK * 1.08f + t * 0.92f) * 0.33f;
        wave += Mathf.Sin((worldX + worldZ) * primaryK * 0.72f + t * 0.8f) * 0.25f;

        float chop = 0f;
        chop += Mathf.Sin(worldX * chopK + t * 1.35f) * 0.45f;
        chop += Mathf.Sin((worldX * 0.7f - worldZ * 1.1f) * chopK * 1.2f + t * 1.5f) * 0.35f;

        return (wave + chop * chopHeightRatio) * waveHeight;
    }

    static float Wavenumber(float wavelengthMeters)
    {
        return (Mathf.PI * 2f) / Mathf.Max(wavelengthMeters, 1f);
    }

    void BuildMesh()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<MeshRenderer>();

        _mesh = new Mesh { name = "OceanWaveGrid" };
        _mesh.MarkDynamic();

        int vertCount = (gridResolution + 1) * (gridResolution + 1);
        _baseVertices = new Vector3[vertCount];
        _workingVertices = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        var triangles = new int[gridResolution * gridResolution * 6];

        float half = oceanSizeMeters * 0.5f;
        int index = 0;
        for (int z = 0; z <= gridResolution; z++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                float px = Mathf.Lerp(-half, half, x / (float)gridResolution);
                float pz = Mathf.Lerp(-half, half, z / (float)gridResolution);
                _baseVertices[index] = new Vector3(px, 0f, pz);
                uvs[index] = new Vector2(x / (float)gridResolution, z / (float)gridResolution);
                index++;
            }
        }

        int tri = 0;
        for (int z = 0; z < gridResolution; z++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                int i = z * (gridResolution + 1) + x;
                triangles[tri++] = i;
                triangles[tri++] = i + gridResolution + 1;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + 1;
                triangles[tri++] = i + gridResolution + 1;
                triangles[tri++] = i + gridResolution + 2;
            }
        }

        _mesh.vertices = _baseVertices;
        _mesh.uv = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        meshFilter.sharedMesh = _mesh;
    }

    void UpdateMeshVertices()
    {
        if (_mesh == null || _baseVertices == null || _workingVertices == null)
            return;

        float time = Time.time;
        Vector3 worldOffset = transform.position;

        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 local = _baseVertices[i];
            float worldX = worldOffset.x + local.x;
            float worldZ = worldOffset.z + local.z;
            local.y = EvaluateWaveHeight(worldX, worldZ, time);
            _workingVertices[i] = local;
        }

        _mesh.vertices = _workingVertices;
        _mesh.RecalculateNormals();
    }

    void EnsureMaterial()
    {
        if (_renderer == null)
            return;

        Shader shader = Shader.Find(OceanShaderName);
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        _waterMaterial = new Material(shader);
        _rippleTexture = BuildRippleTexture(rippleTextureSize);
        _waterMaterial.SetTexture(BaseMapId, _rippleTexture);
        _waterMaterial.SetTextureScale(BaseMapId, new Vector2(rippleTextureScale, rippleTextureScale));

        if (_waterMaterial.HasProperty("_Smoothness"))
            _waterMaterial.SetFloat("_Smoothness", 0.94f);
        if (_waterMaterial.HasProperty("_SpecularStrength"))
            _waterMaterial.SetFloat("_SpecularStrength", 0.9f);

        ApplyWaterColorsToMaterial();
        UpdateMaterialWaveParams();

        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = true;
        _renderer.sharedMaterial = _waterMaterial;
    }

    void ApplyWaterColorsToMaterial()
    {
        if (_waterMaterial == null)
            return;

        Color deep = Color.Lerp(waterColor * 0.55f, Color.black, 0.15f);
        deep.a = 0.92f;
        Color shallow = Color.Lerp(waterColor, Color.white, 0.18f);
        shallow.a = 0.86f;

        if (_waterMaterial.HasProperty(BaseColorId))
            _waterMaterial.SetColor(BaseColorId, deep);
        if (_waterMaterial.HasProperty(ShallowColorId))
            _waterMaterial.SetColor(ShallowColorId, shallow);
    }

    void UpdateMaterialWaveParams()
    {
        if (_waterMaterial == null)
            return;

        if (_waterMaterial.HasProperty(WaveHeightId))
            _waterMaterial.SetFloat(WaveHeightId, Mathf.Max(waveHeight, 0.05f));
        if (_waterMaterial.HasProperty(RippleStrengthId))
            _waterMaterial.SetFloat(RippleStrengthId, Mathf.Lerp(0.22f, 0.48f, chopHeightRatio));
        if (_waterMaterial.HasProperty(FoamStrengthId))
            _waterMaterial.SetFloat(FoamStrengthId, Mathf.Lerp(0.35f, 1.1f, chopHeightRatio + waveHeight * 0.08f));
    }

    void UpdateMaterialAnimation()
    {
        if (_waterMaterial == null)
            return;

        _rippleScrollOffset += rippleScrollSpeed * Time.deltaTime;
        _waterMaterial.SetTextureOffset(BaseMapId, _rippleScrollOffset);

        if (_waterMaterial.HasProperty(WavePhaseId))
            _waterMaterial.SetFloat(WavePhaseId, Time.time * waveSpeed);
    }

    static Texture2D BuildRippleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear,
            name = "SeaStateOcean_Ripples"
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float n1 = Mathf.PerlinNoise(u * 5f, v * 5f);
                float n2 = Mathf.PerlinNoise(u * 11f + 1.7f, v * 11f + 0.4f);
                float n3 = Mathf.PerlinNoise(u * 22f + 3.1f, v * 22f + 2.8f);
                float ripple = Mathf.Clamp01(n1 * 0.5f + n2 * 0.32f + n3 * 0.18f);
                pixels[y * size + x] = new Color(ripple, ripple, ripple, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}
