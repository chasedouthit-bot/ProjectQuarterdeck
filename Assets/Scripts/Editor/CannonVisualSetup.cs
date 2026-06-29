using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quarterdeck.Editor
{
    public static class CannonVisualSetup
    {
        const string SmokeEffectPrefabPath = "Assets/Prefabs/CannonSmokeEffect.prefab";

        [MenuItem("Quarterdeck/Setup Cannon Visuals")]
        public static void SetupCannonVisualsInActiveScene()
        {
            EnsureSmokeEffectPrefab();

            var smokePrefab = AssetDatabase.LoadAssetAtPath<CannonSmokeEffect>(SmokeEffectPrefabPath);
            var cannonsRoot = GameObject.Find("Quarterdeck_Graybox/GunDeckCannons");
            if (cannonsRoot == null)
            {
                Debug.LogError("GunDeckCannons not found under Quarterdeck_Graybox.");
                return;
            }

            int configured = 0;
            for (int i = 0; i < cannonsRoot.transform.childCount; i++)
            {
                Transform cannon = cannonsRoot.transform.GetChild(i);
                if (!TryParseCannonName(cannon.name, out BatterySide side, out int number))
                    continue;

                var visual = cannon.GetComponent<CannonVisual>();
                if (visual == null)
                    visual = Undo.AddComponent<CannonVisual>(cannon.gameObject);

                Undo.RecordObject(visual, "Setup Cannon Visual");
                visual.Configure(side, number);

                var serialized = new SerializedObject(visual);
                serialized.FindProperty("smokeEffectPrefab").objectReferenceValue = smokePrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EnsureMuzzlePoint(cannon, visual);
                configured++;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"Configured CannonVisual on {configured} gun-deck cannons.");
        }

        [MenuItem("Quarterdeck/Create Cannon Smoke Effect Prefab")]
        public static void EnsureSmokeEffectPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CannonSmokeEffect>(SmokeEffectPrefabPath);
            if (existing != null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var root = new GameObject("CannonSmokeEffect");
            var effect = root.AddComponent<CannonSmokeEffect>();
            effect.EnsureConfigured();
            PrefabUtility.SaveAsPrefabAsset(root, SmokeEffectPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
        }

        static void EnsureMuzzlePoint(Transform cannon, CannonVisual visual)
        {
            Transform muzzle = cannon.Find("MuzzlePoint");
            if (muzzle == null)
            {
                var muzzleGo = new GameObject("MuzzlePoint");
                Undo.RegisterCreatedObjectUndo(muzzleGo, "Create MuzzlePoint");
                muzzleGo.transform.SetParent(cannon, false);
                muzzle = muzzleGo.transform;
            }

            var deckVisual = cannon.GetComponent<GunDeckCannonVisual>();
            Vector3 direction = deckVisual != null
                ? deckVisual.MuzzleWorldDirection.normalized
                : cannon.right;

            Undo.RecordObject(muzzle, "Position MuzzlePoint");
            muzzle.position = cannon.position + direction * 0.85f;
            muzzle.rotation = Quaternion.LookRotation(direction);

            var serialized = new SerializedObject(visual);
            serialized.FindProperty("muzzlePoint").objectReferenceValue = muzzle;
            Transform model = cannon.Find("CannonModel");
            serialized.FindProperty("recoilTransform").objectReferenceValue = model != null ? model : cannon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool TryParseCannonName(string name, out BatterySide side, out int number)
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
    }
}
