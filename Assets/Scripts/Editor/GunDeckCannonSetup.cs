using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quarterdeck.Editor
{
    public static class GunDeckCannonSetup
    {
        const string CannonModelPath = "Assets/Models/Cannon.fbx";
        const string CannonPrefabPath = "Assets/Models/GunDeckCannon.prefab";

        [MenuItem("Quarterdeck/Place Gun Deck Cannons")]
        public static void PlaceGunDeckCannonsInActiveScene()
        {
            var ship = GameObject.Find("Quarterdeck_Graybox");
            if (ship == null)
            {
                Debug.LogError("Quarterdeck_Graybox not found in the active scene.");
                return;
            }

            var gunDeck = ship.transform.Find("GunDeck");
            if (gunDeck == null)
            {
                Debug.LogError("GunDeck not found under Quarterdeck_Graybox.");
                return;
            }

            var placement = ship.GetComponent<GunDeckCannonPlacement>();
            if (placement == null)
                placement = Undo.AddComponent<GunDeckCannonPlacement>(ship);

            var cannonPrefab = LoadCannonPrefab();
            if (cannonPrefab == null)
                return;

            var serialized = new SerializedObject(placement);
            serialized.FindProperty("cannonModelPrefab").objectReferenceValue = cannonPrefab;
            serialized.FindProperty("gunDeckRoot").objectReferenceValue = gunDeck;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (placement.TryCaptureFromReferenceCannon())
                Debug.Log("Captured gun-deck cannon placement from existing reference cannon.");

            placement.PlaceCannons();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = ship;
            Debug.Log("Gun deck cannons placed at every active gun port.");
        }

        static GameObject LoadCannonPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CannonPrefabPath);
            if (prefab != null)
                return prefab;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CannonModelPath);
            if (model == null)
            {
                Debug.LogError($"Cannon model not found at {CannonModelPath}");
                return null;
            }

            var root = PrefabUtility.InstantiatePrefab(model) as GameObject;
            root.name = "GunDeckCannon";
            PrefabUtility.SaveAsPrefabAsset(root, CannonPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(CannonPrefabPath);
        }
    }

    public static class GunDeckCannonBatch
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Quarterdeck_Ontario_Inspection.unity");
            GunDeckCannonSetup.PlaceGunDeckCannonsInActiveScene();
            EditorSceneManager.SaveOpenScenes();
        }
    }
}
