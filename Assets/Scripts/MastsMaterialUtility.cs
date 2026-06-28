using UnityEngine;

/// <summary>
/// Assigns brig spar/rigging materials to every renderer under the Masts hierarchy.
/// </summary>
public static class MastsMaterialUtility
{
    public static void AssignMaterials(Transform mastsRoot)
    {
        if (mastsRoot == null)
            return;

        var sparMaterial = BrigRigMaterials.Spar;
        var riggingMaterial = BrigRigMaterials.Rigging;

        if (sparMaterial == null || riggingMaterial == null)
        {
            Debug.LogError("MastsMaterialUtility: Missing BrigSpar.mat or BrigRigging.mat in Assets/Materials.");
            return;
        }

        int meshCount = 0;
        int lineCount = 0;

        foreach (var renderer in mastsRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterial = sparMaterial;
            meshCount++;
        }

        foreach (var line in mastsRoot.GetComponentsInChildren<LineRenderer>(true))
        {
            line.sharedMaterial = riggingMaterial;
            lineCount++;
        }

        Debug.Log($"Assigned materials under {mastsRoot.name}: {meshCount} mesh renderers, {lineCount} line renderers.");
    }
}
