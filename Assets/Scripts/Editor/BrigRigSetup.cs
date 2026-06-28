using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BrigRigSetup
{
    [MenuItem("Quarterdeck/Build Brig Mast Rig")]
    public static void BuildBrigRig()
    {
        var fore = GameObject.Find("Mast_Fore");
        var main = GameObject.Find("Mast_Mizzen");
        var mastsRoot = GameObject.Find("Masts");

        if (fore == null || main == null || mastsRoot == null)
        {
            Debug.LogError("BrigRigSetup: Could not find Mast_Fore, Mast_Mizzen, or Masts.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(mastsRoot, "Build Brig Mast Rig");

        var sparMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrigSpar.mat");
        var riggingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrigRigging.mat");

        if (sparMaterial == null || riggingMaterial == null)
        {
            Debug.LogError("BrigRigSetup: Missing BrigSpar.mat or BrigRigging.mat in Assets/Materials.");
            return;
        }

        var foreRig = GetOrAdd<BrigMastRig>(fore);
        AssignMastMaterials(foreRig, sparMaterial, riggingMaterial);
        foreRig.Rebuild();

        var mainRig = GetOrAdd<BrigMastRig>(main);
        var mainSerialized = new SerializedObject(mainRig);
        mainSerialized.FindProperty("isMainMast").boolValue = true;
        AssignMastMaterials(mainRig, sparMaterial, riggingMaterial);
        mainSerialized.ApplyModifiedPropertiesWithoutUndo();
        mainRig.Rebuild();

        var bowspritAnchor = mastsRoot.transform.Find("Bowsprit");
        if (bowspritAnchor == null)
        {
            var bowspritObject = new GameObject("Bowsprit");
            Undo.RegisterCreatedObjectUndo(bowspritObject, "Build Brig Mast Rig");
            bowspritObject.transform.SetParent(mastsRoot.transform, false);
            bowspritObject.transform.localPosition = Vector3.zero;
            bowspritObject.transform.localRotation = Quaternion.identity;
            bowspritObject.transform.localScale = Vector3.one;
            bowspritAnchor = bowspritObject.transform;
        }

        var bowspritRig = GetOrAdd<BowspritRig>(bowspritAnchor.gameObject);
        AssignBowspritMaterials(bowspritRig, sparMaterial, riggingMaterial);
        bowspritRig.Rebuild(force: true);

        MastsMaterialUtility.AssignMaterials(mastsRoot.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Brig mast rig built. Mast positions were not changed.");
    }

    [MenuItem("Quarterdeck/Assign Masts Materials")]
    public static void AssignMastsMaterials()
    {
        var mastsRoot = GameObject.Find("Masts");
        if (mastsRoot == null)
        {
            Debug.LogError("BrigRigSetup: Could not find Masts.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(mastsRoot, "Assign Masts Materials");

        var sparMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrigSpar.mat");
        var riggingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BrigRigging.mat");
        if (sparMaterial == null || riggingMaterial == null)
        {
            Debug.LogError("BrigRigSetup: Missing BrigSpar.mat or BrigRigging.mat in Assets/Materials.");
            return;
        }

        foreach (var rig in mastsRoot.GetComponentsInChildren<BrigMastRig>(true))
            AssignMastMaterials(rig, sparMaterial, riggingMaterial);

        foreach (var rig in mastsRoot.GetComponentsInChildren<BowspritRig>(true))
            AssignBowspritMaterials(rig, sparMaterial, riggingMaterial);

        MastsMaterialUtility.AssignMaterials(mastsRoot.transform);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void AssignMastMaterials(BrigMastRig rig, Material spar, Material rigging)
    {
        var serialized = new SerializedObject(rig);
        serialized.FindProperty("mastMaterial").objectReferenceValue = spar;
        serialized.FindProperty("riggingMaterial").objectReferenceValue = rigging;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignBowspritMaterials(BowspritRig rig, Material spar, Material rigging)
    {
        var serialized = new SerializedObject(rig);
        serialized.FindProperty("sparMaterial").objectReferenceValue = spar;
        serialized.FindProperty("riggingMaterial").objectReferenceValue = rigging;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
