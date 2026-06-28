using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Quarterdeck.Editor
{
    public static class PrototypeOceanSetup
    {
        const string OceanName = "Prototype_Ocean";
        const string MaterialPath = "Assets/Environment/PrototypeOcean/PrototypeOcean.mat";
        const string SkyboxPath = "Assets/StarterAssets/Environment/Art/Skybox/SkyboxLite.mat";

        [MenuItem("Quarterdeck/Create Prototype Ocean")]
        public static void CreatePrototypeOcean()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError("Prototype ocean material not found at " + MaterialPath);
                return;
            }

            var existing = GameObject.Find(OceanName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = OceanName;
            Undo.RegisterCreatedObjectUndo(ocean, "Create Prototype Ocean");

            ocean.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ocean.transform.localScale = new Vector3(500f, 1f, 500f);

            var collider = ocean.GetComponent<Collider>();
            if (collider != null)
                Undo.DestroyObjectImmediate(collider);

            ocean.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (ocean.GetComponent<PrototypeOceanMotion>() == null)
                Undo.AddComponent<PrototypeOceanMotion>(ocean);

            ApplyAtmosphere();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = ocean;
            EditorGUIUtility.PingObject(ocean);
            Debug.Log("Prototype ocean created and selected.");
        }

        public static void ApplyAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.76f, 0.86f, 1f);
            RenderSettings.fogStartDistance = 250f;
            RenderSettings.fogEndDistance = 2200f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;

            var skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }
    }
}
