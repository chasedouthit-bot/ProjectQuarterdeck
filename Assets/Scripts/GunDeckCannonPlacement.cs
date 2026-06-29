using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places cannon visuals at GunPort markers on the graybox gun deck.
/// </summary>
public class GunDeckCannonPlacement : MonoBehaviour
{
    const string GunPortPrefix = "GunPort_";
    const string CannonsRootName = "GunDeckCannons";

    [SerializeField] GameObject cannonModelPrefab;
    [SerializeField] Vector3 portLocalOffset = new Vector3(0.85f, 0.35f, 0f);
    [SerializeField] Vector3 starboardLocalOffset = new Vector3(-0.85f, 0.35f, 0f);
    [SerializeField] Vector3 modelCorrectionEuler = new Vector3(0f, 0f, -90f);
    [SerializeField] float modelUniformScale = 0.5f;
    [SerializeField] float portSideParentYaw = 180f;

    [Header("Optional — limits placement to this gun deck root")]
    [SerializeField] Transform gunDeckRoot;

    void Awake()
    {
        EnsureCannonVisualComponents();
    }

    public void EnsureCannonVisualComponents()
    {
        Transform cannonsRoot = transform.Find(CannonsRootName);
        if (cannonsRoot == null)
            return;

        for (int i = 0; i < cannonsRoot.childCount; i++)
        {
            Transform cannon = cannonsRoot.GetChild(i);
            if (cannon.GetComponent<CannonVisual>() != null)
                continue;

            if (!TryParseCannonObjectName(cannon.name, out BatterySide side, out int number))
                continue;

            var visual = cannon.gameObject.AddComponent<CannonVisual>();
            visual.Configure(side, number);
        }
    }

    static bool TryParseCannonObjectName(string name, out BatterySide side, out int number)
    {
        side = BatterySide.Port;
        number = 0;

        if (name.StartsWith("Port_"))
            side = BatterySide.Port;
        else if (name.StartsWith("Starboard_"))
            side = BatterySide.Starboard;
        else
            return false;

        const string marker = "GunPort_";
        int markerIndex = name.IndexOf(marker, System.StringComparison.Ordinal);
        if (markerIndex < 0)
            return false;

        int start = markerIndex + marker.Length;
        int end = name.IndexOf('_', start);
        string numberText = end > start ? name.Substring(start, end - start) : name.Substring(start);
        return int.TryParse(numberText, out number);
    }

    public void ClearPlacedCannons()
    {
        Transform existing = transform.Find(CannonsRootName);
        if (existing == null)
            return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    public bool TryCaptureFromReferenceCannon()
    {
        Transform cannonsRoot = transform.Find(CannonsRootName);
        if (cannonsRoot == null || cannonsRoot.childCount == 0)
            return false;

        Transform referenceCannon = null;
        for (int i = 0; i < cannonsRoot.childCount; i++)
        {
            Transform child = cannonsRoot.GetChild(i);
            if (child.childCount == 0)
                continue;

            referenceCannon = child;
            break;
        }

        if (referenceCannon == null)
            return false;

        Transform model = referenceCannon.Find("CannonModel");
        if (model == null && referenceCannon.childCount > 0)
            model = referenceCannon.GetChild(0);

        if (model != null)
        {
            modelCorrectionEuler = model.localEulerAngles;
            modelUniformScale = model.localScale.x;
        }

        Transform searchRoot = gunDeckRoot != null ? gunDeckRoot : transform;
        Transform nearestPort = FindNearestGunPort(searchRoot, referenceCannon.position, out bool isPort);
        if (nearestPort == null)
            return false;

        Vector3 capturedOffset = nearestPort.InverseTransformPoint(referenceCannon.position);
        if (isPort)
            portLocalOffset = capturedOffset;
        else
            starboardLocalOffset = capturedOffset;

        Quaternion relativeRotation = Quaternion.Inverse(nearestPort.rotation) * referenceCannon.rotation;
        if (isPort)
            portSideParentYaw = relativeRotation.eulerAngles.y;
        else if (Mathf.Abs(Mathf.DeltaAngle(relativeRotation.eulerAngles.y, 0f)) > 0.01f)
            portSideParentYaw = 180f;

        return true;
    }

    public void PlaceCannons()
    {
        if (cannonModelPrefab == null)
        {
            Debug.LogError("GunDeckCannonPlacement: assign cannonModelPrefab.");
            return;
        }

        ClearPlacedCannons();

        var cannonsRoot = new GameObject(CannonsRootName);
        cannonsRoot.transform.SetParent(transform, false);

        Transform searchRoot = gunDeckRoot != null ? gunDeckRoot : transform;
        var gunPorts = CollectGunPorts(searchRoot);
        var seen = new HashSet<string>();
        gunPorts.RemoveAll(port =>
        {
            string key = $"{GetSideLabel(port)}_{ExtractGunPortNumber(port.name)}";
            return !seen.Add(key);
        });
        gunPorts.Sort((a, b) => CompareGunPorts(a, b));

        foreach (Transform gunPort in gunPorts)
        {
            bool isPort = IsPortSide(gunPort);
            Vector3 localOffset = isPort ? portLocalOffset : starboardLocalOffset;
            Quaternion sideRotation = isPort
                ? Quaternion.Euler(0f, portSideParentYaw, 0f)
                : Quaternion.identity;

            var cannonGo = new GameObject($"{GetSideLabel(gunPort)}_{gunPort.name}_Cannon");
            cannonGo.transform.SetParent(cannonsRoot.transform, false);
            cannonGo.transform.SetPositionAndRotation(
                gunPort.TransformPoint(localOffset),
                gunPort.rotation * sideRotation);

            var visual = cannonGo.AddComponent<GunDeckCannonVisual>();
            var cannonVisual = cannonGo.AddComponent<CannonVisual>();
            cannonVisual.Configure(
                isPort ? BatterySide.Port : BatterySide.Starboard,
                ExtractGunPortNumber(gunPort.name));

            GameObject modelInstance = Instantiate(cannonModelPrefab, cannonGo.transform);
            modelInstance.name = "CannonModel";
            modelInstance.transform.localPosition = Vector3.zero;
            visual.ConfigureModel(modelInstance.transform, modelCorrectionEuler, modelUniformScale);
        }

        Debug.Log($"Placed {gunPorts.Count} gun-deck cannons under {searchRoot.name}.");
    }

    static Transform FindNearestGunPort(Transform searchRoot, Vector3 worldPosition, out bool isPort)
    {
        isPort = false;
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform gunPort in CollectGunPorts(searchRoot))
        {
            float distance = Vector3.SqrMagnitude(gunPort.position - worldPosition);
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearest = gunPort;
            isPort = IsPortSide(gunPort);
        }

        return nearest;
    }

    static int CompareGunPorts(Transform a, Transform b)
    {
        int side = string.CompareOrdinal(GetSideLabel(a), GetSideLabel(b));
        if (side != 0)
            return side;

        return ExtractGunPortNumber(a.name).CompareTo(ExtractGunPortNumber(b.name));
    }

    static string GetSideLabel(Transform gunPort)
    {
        return IsPortSide(gunPort) ? "Port" : "Starboard";
    }

    static int ExtractGunPortNumber(string gunPortName)
    {
        if (!gunPortName.StartsWith(GunPortPrefix))
            return 0;

        string suffix = gunPortName.Substring(GunPortPrefix.Length);
        return int.TryParse(suffix, out int number) ? number : 0;
    }

    static List<Transform> CollectGunPorts(Transform root)
    {
        var results = new List<Transform>();
        CollectGunPortsRecursive(root, results);
        return results;
    }

    static void CollectGunPortsRecursive(Transform node, List<Transform> results)
    {
        if (node.name.StartsWith(GunPortPrefix) && IsUnderActiveGunDeckHull(node))
            results.Add(node);

        for (int i = 0; i < node.childCount; i++)
            CollectGunPortsRecursive(node.GetChild(i), results);
    }

    static bool IsUnderActiveGunDeckHull(Transform gunPort)
    {
        bool foundHull = false;
        bool foundGunDeck = false;
        Transform current = gunPort;
        while (current != null)
        {
            if (current.name == "PortHull" || current.name == "StarboardHull")
                foundHull = true;

            if (current.name == "GunDeck")
                foundGunDeck = true;

            current = current.parent;
        }

        return foundHull && foundGunDeck;
    }

    static bool IsPortSide(Transform gunPort)
    {
        Transform current = gunPort;
        while (current != null)
        {
            if (current.name == "PortHull")
                return true;

            if (current.name == "StarboardHull")
                return false;

            current = current.parent;
        }

        return gunPort.localPosition.x < 0f;
    }
}
