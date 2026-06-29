using UnityEngine;

/// <summary>
/// Corrects imported cannon mesh orientation and exposes muzzle forward for placement.
/// Assumes the source model barrel points up (+Y) when upright in the asset preview.
/// </summary>
public class GunDeckCannonVisual : MonoBehaviour
{
    [SerializeField] Transform modelRoot;
    [SerializeField] Vector3 modelCorrectionEuler = new Vector3(0f, 0f, -90f);
    [SerializeField] float targetBarrelLengthMeters = 1.6f;
    [SerializeField] Vector3 muzzleLocalDirection = Vector3.right;

    public Vector3 MuzzleWorldDirection => transform.TransformDirection(muzzleLocalDirection.normalized);

    void Reset()
    {
        if (modelRoot == null && transform.childCount > 0)
            modelRoot = transform.GetChild(0);
    }

    public void ConfigureModel(Transform importedModel, Vector3 correctionEuler, float uniformScale)
    {
        modelRoot = importedModel;
        importedModel.localRotation = Quaternion.Euler(correctionEuler);
        importedModel.localScale = Vector3.one * uniformScale;
    }

    public void ApplyModelCorrection(Transform importedModel)
    {
        ConfigureModel(importedModel, modelCorrectionEuler, 1f);
        FitScaleToTargetLength(importedModel);
    }

    void FitScaleToTargetLength(Transform importedModel)
    {
        var renderers = importedModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (longest <= 0.001f)
            return;

        float scale = targetBarrelLengthMeters / longest;
        importedModel.localScale = Vector3.one * scale;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;
        Vector3 muzzle = origin + MuzzleWorldDirection * targetBarrelLengthMeters * 0.5f;
        Gizmos.DrawLine(origin, muzzle);
        Gizmos.DrawSphere(muzzle, 0.05f);
    }
#endif
}
