using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a modular Age-of-Sail mast assembly as children of an existing mast anchor.
/// Does not move the anchor transform — only hides the legacy cylinder and adds rig geometry.
/// </summary>
[DisallowMultipleComponent]
public class BrigMastRig : MonoBehaviour
{
    private const string RigRootName = "Rig";

    [Header("Mast Role")]
    [SerializeField] private bool isMainMast;

    [Header("Appearance")]
    [SerializeField] private Material mastMaterial;
    [SerializeField] private Material riggingMaterial;
    [SerializeField] private Color mastColor = new(0.42f, 0.30f, 0.18f);
    [SerializeField] private Color riggingColor = new(0.18f, 0.17f, 0.16f);

    [Header("Lower Mast (meters, relative to anchor center)")]
    [SerializeField] private float lowerMastBaseY = -8f;
    [SerializeField] private float lowerMastTopY = 8f;
    [SerializeField] private float lowerMastBaseRadius = 0.26f;
    [SerializeField] private float lowerMastTopRadius = 0.18f;

    [Header("Upper Spars (meters)")]
    [SerializeField] private float topmastTopY = 11f;
    [SerializeField] private float topgallantTopY = 13.5f;
    [SerializeField] private float royalTopY = 15f;

    [Header("Fighting Top")]
    [SerializeField] private float fightingTopY = 7.6f;
    [SerializeField] private float fightingTopRadius = 1.15f;

    [Header("Yard Half-Lengths (meters, port & starboard)")]
    [SerializeField] private float courseYardHalf = 7.5f;
    [SerializeField] private float topsailYardHalf = 5.8f;
    [SerializeField] private float topgallantYardHalf = 4.2f;
    [SerializeField] private float royalYardHalf = 2.6f;

    [Header("Yard Heights (meters)")]
    [SerializeField] private float courseYardY = 6.4f;
    [SerializeField] private float topsailYardY = 9.1f;
    [SerializeField] private float topgallantYardY = 11.9f;
    [SerializeField] private float royalYardY = 13.8f;

    private Transform _rigRoot;

    private void OnEnable()
    {
        EnsureMaterials();
        RefreshExistingMaterials();
        if (Application.isPlaying)
            Rebuild();
    }

    private void RefreshExistingMaterials()
    {
        EnsureMaterials();
        MastsMaterialUtility.AssignMaterials(transform);
    }

    [ContextMenu("Rebuild Rig")]
    public void Rebuild()
    {
        EnsureMaterials();
        HideLegacyMesh();
        EnsureRigRoot();
        ClearRigChildren();

        BuildTaperedMast("LowerMast", lowerMastBaseY, lowerMastTopY, lowerMastBaseRadius, lowerMastTopRadius);
        BuildFightingTop();
        BuildTaperedMast("Topmast", lowerMastTopY, topmastTopY, 0.16f, 0.11f);
        BuildTaperedMast("TopgallantMast", topmastTopY, topgallantTopY, 0.11f, 0.08f);

        if (isMainMast)
            BuildTaperedMast("RoyalMast", topgallantTopY, royalTopY, 0.08f, 0.055f);

        float mainCourse = isMainMast ? courseYardHalf + 1.2f : courseYardHalf;
        BuildYard("CourseYard", courseYardY, mainCourse, 0.11f);
        BuildYard("TopsailYard", topsailYardY, topsailYardHalf, 0.09f);
        BuildYard("TopgallantYard", topgallantYardY, topgallantYardHalf, 0.07f);

        if (isMainMast)
            BuildYard("RoyalYard", royalYardY, royalYardHalf, 0.05f);

        BuildStandingRigging();
        BuildRunningRigging();
    }

    private void EnsureMaterials()
    {
        BrigRigMaterials.ApplyDefaults(ref mastMaterial, ref riggingMaterial);
    }

    private void HideLegacyMesh()
    {
        if (TryGetComponent<MeshRenderer>(out var renderer))
            renderer.enabled = false;
    }

    private void EnsureRigRoot()
    {
        var existing = transform.Find(RigRootName);
        if (existing != null)
        {
            _rigRoot = existing;
            return;
        }

        var root = new GameObject(RigRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        // Counteract non-uniform parent scale so children are built in meters.
        var parentScale = transform.localScale;
        root.transform.localScale = new Vector3(
            parentScale.x > 0.0001f ? 1f / parentScale.x : 1f,
            parentScale.y > 0.0001f ? 1f / parentScale.y : 1f,
            parentScale.z > 0.0001f ? 1f / parentScale.z : 1f);
        _rigRoot = root.transform;
    }

    private void ClearRigChildren()
    {
        var toDestroy = new List<GameObject>();
        for (int i = 0; i < _rigRoot.childCount; i++)
            toDestroy.Add(_rigRoot.GetChild(i).gameObject);

        if (Application.isPlaying)
        {
            foreach (var go in toDestroy)
                Destroy(go);
        }
        else
        {
            foreach (var go in toDestroy)
                DestroyImmediate(go);
        }
    }

    private void BuildTaperedMast(string name, float baseY, float topY, float baseRadius, float topRadius)
    {
        if (topY <= baseY)
            return;

        const int segments = 4;
        float height = topY - baseY;
        float segmentHeight = height / segments;

        for (int i = 0; i < segments; i++)
        {
            float t0 = i / (float)segments;
            float t1 = (i + 1) / (float)segments;
            float y0 = baseY + height * t0;
            float y1 = baseY + height * t1;
            float r0 = Mathf.Lerp(baseRadius, topRadius, t0);
            float r1 = Mathf.Lerp(baseRadius, topRadius, t1);
            float radius = (r0 + r1) * 0.5f;
            float yCenter = (y0 + y1) * 0.5f;

            var segment = CreatePrimitive(PrimitiveType.Cylinder, $"{name}_{i + 1}", _rigRoot);
            segment.transform.localPosition = new Vector3(0f, yCenter, 0f);
            segment.transform.localScale = new Vector3(radius * 2f, (y1 - y0) * 0.5f, radius * 2f);
        }
    }

    private void BuildFightingTop()
    {
        var top = CreatePrimitive(PrimitiveType.Cylinder, "FightingTop", _rigRoot);
        top.transform.localPosition = new Vector3(0f, fightingTopY, 0f);
        top.transform.localScale = new Vector3(fightingTopRadius * 2f, 0.14f, fightingTopRadius * 2f);

        var rail = CreatePrimitive(PrimitiveType.Cylinder, "FightingTop_Rail", top.transform);
        rail.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        rail.transform.localScale = new Vector3(1.08f, 0.04f, 1.08f);
    }

    private void BuildYard(string name, float y, float halfLength, float radius)
    {
        var yard = CreatePrimitive(PrimitiveType.Cylinder, name, _rigRoot);
        yard.transform.localPosition = new Vector3(0f, y, 0f);
        yard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        yard.transform.localScale = new Vector3(radius * 2f, halfLength, radius * 2f);
    }

    private void BuildStandingRigging()
    {
        float deckY = lowerMastBaseY;
        float shroudTopY = lowerMastTopY - 0.4f;
        float sideOffset = fightingTopRadius + 0.35f;

        for (int i = 0; i < 4; i++)
        {
            float side = i < 2 ? -1f : 1f;
            float zOffset = (i % 2 == 0) ? -0.55f : 0.55f;
            var start = new Vector3(side * sideOffset, shroudTopY, zOffset);
            var end = new Vector3(side * (sideOffset + 1.6f), deckY + 0.5f, zOffset * 0.5f);
            CreateRiggingLine($"Shroud_{i + 1}", start, end);
        }

        float headY = isMainMast ? royalTopY : topgallantTopY;
        Vector3 mastHead = new(0f, headY, 0f);

        if (!isMainMast)
        {
            // Forestay runs forward to the bowsprit area (local +Z).
            CreateRiggingLine("Forestay", mastHead, new Vector3(0f, 3.8f, 14.5f));
        }
        else
        {
            CreateRiggingLine("MainStay", mastHead, new Vector3(0f, topgallantTopY, 7.16f));
            CreateRiggingLine("Backstay_Port", mastHead, new Vector3(-2.8f, deckY + 0.4f, -6.5f));
            CreateRiggingLine("Backstay_Starboard", mastHead, new Vector3(2.8f, deckY + 0.4f, -6.5f));
        }
    }

    private void BuildRunningRigging()
    {
        float headY = isMainMast ? royalTopY : topgallantTopY;
        Vector3 mastHead = new(0f, headY, 0f);

        CreateRiggingLine("Lift_Topgallant_Port", mastHead, new Vector3(-topgallantYardHalf, topgallantYardY, 0f));
        CreateRiggingLine("Lift_Topgallant_Starboard", mastHead, new Vector3(topgallantYardHalf, topgallantYardY, 0f));
        CreateRiggingLine("Lift_Topsail_Port", new Vector3(0f, topmastTopY, 0f), new Vector3(-topsailYardHalf, topsailYardY, 0f));
        CreateRiggingLine("Lift_Topsail_Starboard", new Vector3(0f, topmastTopY, 0f), new Vector3(topsailYardHalf, topsailYardY, 0f));

        if (isMainMast)
            CreateRiggingLine("Lift_Royal", mastHead, new Vector3(royalYardHalf, royalYardY, 0f));
    }

    private GameObject CreatePrimitive(PrimitiveType type, string objectName, Transform parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objectName;
        go.transform.SetParent(parent, false);

        if (go.TryGetComponent<Collider>(out var collider))
        {
            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        go.GetComponent<MeshRenderer>().sharedMaterial = mastMaterial;
        return go;
    }

    private void CreateRiggingLine(string objectName, Vector3 start, Vector3 end)
    {
        var lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(_rigRoot, false);

        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.sharedMaterial = riggingMaterial;
        line.startWidth = 0.035f;
        line.endWidth = 0.025f;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }
}
