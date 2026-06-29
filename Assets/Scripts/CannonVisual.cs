using System.Collections;
using UnityEngine;

/// <summary>
/// Scene cannon that plays a smoke burst (and optional recoil) when its battery gun fires.
/// </summary>
[DisallowMultipleComponent]
public class CannonVisual : MonoBehaviour
{
    [SerializeField] BatterySide side;
    [SerializeField] int cannonNumber = 1;
    [SerializeField] Transform muzzlePoint;
    [SerializeField] Transform recoilTransform;
    [SerializeField] CannonSmokeEffect smokeEffectPrefab;
    [SerializeField] bool enableRecoil = true;
    [SerializeField] float recoilDistance = 0.32f;
    [SerializeField] float recoilOutDuration = 0.15f;
    [SerializeField] float recoilReturnDuration = 1.2f;
    [SerializeField] float muzzleForwardOffset = 0.85f;

    Vector3 _recoilRestLocalPosition;
    Coroutine _recoilRoutine;

    public BatterySide Side => side;
    public int CannonNumber => cannonNumber;

    void Awake()
    {
        TryAutoConfigureFromName();
        EnsureMuzzlePoint();
        EnsureRecoilTransform();
        CannonVisualRegistry.Register(this);
    }

    void TryAutoConfigureFromName()
    {
        string name = gameObject.name;
        if (!name.Contains("GunPort_"))
            return;

        if (name.StartsWith("Port_"))
            side = BatterySide.Port;
        else if (name.StartsWith("Starboard_"))
            side = BatterySide.Starboard;
        else
            return;

        const string marker = "GunPort_";
        int markerIndex = name.IndexOf(marker, System.StringComparison.Ordinal);
        if (markerIndex < 0)
            return;

        int start = markerIndex + marker.Length;
        int end = name.IndexOf('_', start);
        string numberText = end > start ? name.Substring(start, end - start) : name.Substring(start);
        if (int.TryParse(numberText, out int parsed))
            cannonNumber = parsed;
    }

    void OnDestroy()
    {
        CannonVisualRegistry.Unregister(this);
    }

    void OnValidate()
    {
        cannonNumber = Mathf.Clamp(cannonNumber, 1, CannonBattery.CannonsPerSide);
    }

    public void Configure(BatterySide batterySide, int number)
    {
        side = batterySide;
        cannonNumber = number;
    }

    public void FireVisual()
    {
        EnsureMuzzlePoint();
        CannonSmokeEffect.SpawnAt(muzzlePoint, smokeEffectPrefab);

        if (enableRecoil && recoilTransform != null)
            StartRecoil();
    }

    void EnsureMuzzlePoint()
    {
        if (muzzlePoint != null)
            return;

        Transform existing = transform.Find("MuzzlePoint");
        if (existing != null)
        {
            muzzlePoint = existing;
            return;
        }

        var muzzleGo = new GameObject("MuzzlePoint");
        muzzleGo.transform.SetParent(transform, false);

        Vector3 muzzleDirection = GetMuzzleDirection();
        muzzleGo.transform.localPosition = transform.InverseTransformDirection(muzzleDirection) * muzzleForwardOffset;
        muzzleGo.transform.rotation = Quaternion.LookRotation(muzzleDirection);
        muzzlePoint = muzzleGo.transform;
    }

    void EnsureRecoilTransform()
    {
        if (recoilTransform != null)
            return;

        Transform model = transform.Find("CannonModel");
        recoilTransform = model != null ? model : transform;
        _recoilRestLocalPosition = recoilTransform.localPosition;
    }

    Vector3 GetMuzzleDirection()
    {
        var deckVisual = GetComponent<GunDeckCannonVisual>();
        if (deckVisual != null)
            return deckVisual.MuzzleWorldDirection.normalized;

        return transform.right;
    }

    void StartRecoil()
    {
        if (_recoilRoutine != null)
            StopCoroutine(_recoilRoutine);

        _recoilRestLocalPosition = recoilTransform.localPosition;
        _recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    IEnumerator RecoilRoutine()
    {
        Vector3 recoilAxisLocal = transform.InverseTransformDirection(-GetMuzzleDirection()).normalized;
        Vector3 recoilLocalOffset = recoilAxisLocal * recoilDistance;
        Vector3 start = _recoilRestLocalPosition;
        Vector3 kicked = start + recoilLocalOffset;

        float elapsed = 0f;
        while (elapsed < recoilOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoilOutDuration);
            recoilTransform.localPosition = Vector3.Lerp(start, kicked, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < recoilReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoilReturnDuration);
            recoilTransform.localPosition = Vector3.Lerp(kicked, start, t);
            yield return null;
        }

        recoilTransform.localPosition = start;
        _recoilRoutine = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform muzzle = muzzlePoint != null ? muzzlePoint : transform.Find("MuzzlePoint");
        if (muzzle == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(muzzle.position, 0.06f);
        Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * 0.35f);
    }
#endif
}
