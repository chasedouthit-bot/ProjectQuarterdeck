using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shared material references for brig mast/bowsprit graybox rigs.
/// </summary>
public static class BrigRigMaterials
{
    private const string SparAssetPath = "Assets/Materials/BrigSpar.mat";
    private const string RiggingAssetPath = "Assets/Materials/BrigRigging.mat";

    private static Material _spar;
    private static Material _rigging;

    public static Material Spar
    {
        get
        {
            if (_spar == null)
                _spar = LoadMaterial(SparAssetPath);
            return _spar;
        }
    }

    public static Material Rigging
    {
        get
        {
            if (_rigging == null)
                _rigging = LoadMaterial(RiggingAssetPath);
            return _rigging;
        }
    }

    public static void ApplyDefaults(ref Material sparMaterial, ref Material riggingMaterial)
    {
        if (sparMaterial == null)
            sparMaterial = Spar;
        if (riggingMaterial == null)
            riggingMaterial = Rigging;
    }

    private static Material LoadMaterial(string assetPath)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
#else
        return null;
#endif
    }
}
