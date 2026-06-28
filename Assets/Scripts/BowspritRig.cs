using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple bowsprit and spritsail yard for the brig graybox.
/// Parent should sit at the Masts root — does not move existing masts.
/// </summary>
[DisallowMultipleComponent]
public class BowspritRig : MonoBehaviour
{
    private const string RigRootName = "BowspritRig";

    [Header("Appearance")]
    [SerializeField] private Material sparMaterial;
    [SerializeField] private Material riggingMaterial;
    [SerializeField] private Color sparColor = new(0.42f, 0.30f, 0.18f);
    [SerializeField] private Color riggingColor = new(0.18f, 0.17f, 0.16f);

    [Header("Bowsprit (meters, ship-local)")]
    [SerializeField] private Vector3 heelPosition = new(0f, 4.35f, 10.35f);
    [SerializeField] private float length = 9.2f;
    [SerializeField] private float elevationDegrees = 12f;
    [SerializeField] private float radius = 0.16f;

    [Header("Spritsail Yard")]
    [SerializeField] private float yardHalfLength = 3.2f;
    [SerializeField] private float yardRadius = 0.06f;
    [SerializeField] private float yardInsetFromTip = 1.8f;

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
        EnsureRigRoot();
        ClearRigChildren();

        float elevationRad = elevationDegrees * Mathf.Deg2Rad;
        Vector3 forward = new(0f, Mathf.Sin(elevationRad), Mathf.Cos(elevationRad));
        Vector3 tip = heelPosition + forward * length;
        Vector3 center = (heelPosition + tip) * 0.5f;

        var bowsprit = CreatePrimitive(PrimitiveType.Cylinder, "Bowsprit", _rigRoot);
        bowsprit.transform.localPosition = center;
        // Unity cylinders are aligned on local Y — tilt forward over the bow (+Z).
        bowsprit.transform.localRotation = Quaternion.Euler(elevationDegrees, 0f, 0f);
        bowsprit.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

        Vector3 yardCenter = tip - forward * yardInsetFromTip;
        var yard = CreatePrimitive(PrimitiveType.Cylinder, "SpritsailYard", _rigRoot);
        yard.transform.localPosition = yardCenter;
        yard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        yard.transform.localScale = new Vector3(yardRadius * 2f, yardHalfLength, yardRadius * 2f);

        CreateRiggingLine("BowspritBobstay", heelPosition, tip + new Vector3(0f, -2.2f, -1.2f));
        CreateRiggingLine("SpritsailLift", tip, yardCenter + new Vector3(yardHalfLength, 0f, 0f));
    }

    private void EnsureMaterials()
    {
        BrigRigMaterials.ApplyDefaults(ref sparMaterial, ref riggingMaterial);
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

        go.GetComponent<MeshRenderer>().sharedMaterial = sparMaterial;
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
        line.startWidth = 0.03f;
        line.endWidth = 0.02f;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
    }
}
