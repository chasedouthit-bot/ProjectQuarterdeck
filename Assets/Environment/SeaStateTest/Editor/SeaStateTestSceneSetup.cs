using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Quarterdeck.Editor
{
    public static class SeaStateTestSceneSetup
    {
        const string ScenePath = "Assets/Scenes/Ocean_SeaState_Test.unity";

        [MenuItem("Quarterdeck/Rebuild Ocean Sea State HUD")]
        public static void RebuildHudInActiveScene()
        {
            var manager = Object.FindFirstObjectByType<SeaStateManager>();
            if (manager == null)
            {
                Debug.LogError("SeaStateManager not found in the active scene.");
                return;
            }

            var uiRoot = GameObject.Find("UI");
            if (uiRoot != null)
                Undo.DestroyObjectImmediate(uiRoot);

            Text stateLabel = SeaStateHudBuilder.BuildHud();
            Undo.RegisterCreatedObjectUndo(GameObject.Find("UI"), "Rebuild Sea State HUD");

            var so = new SerializedObject(manager);
            so.FindProperty("stateLabel").objectReferenceValue = stateLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Sea state HUD rebuilt.");
        }

        [MenuItem("Quarterdeck/Create Ocean Sea State Test Scene")]
        public static void CreateSeaStateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLightingRoot(out Light sun);
            GameObject ocean = CreateOcean();
            GameObject ship = CreateTestShip(ocean.GetComponent<OceanWaveController>());
            CreateBuoys(ocean.GetComponent<OceanWaveController>());
            CreateWeather(out ParticleSystem rain);
            GameObject managers = CreateManagers();
            CreateUi(out Text stateLabel);
            GameObject deckCamera = CreateDeckCamera(ship);
            CreateWindIndicator(deckCamera, managers);
            WireManagerReferences(managers.GetComponent<SeaStateManager>(), ocean, sun, rain, stateLabel);

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Ocean Sea State test scene saved to {ScenePath}");
        }

        static void CreateLightingRoot(out Light sun)
        {
            var root = new GameObject("Lighting");
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(root.transform);
            sunGo.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.05f;

            var skybox = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/StarterAssets/Environment/Art/Skybox/SkyboxLite.mat");
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }

        static GameObject CreateOcean()
        {
            var ocean = new GameObject("Ocean");
            ocean.AddComponent<OceanWaveController>();
            return ocean;
        }

        static GameObject CreateTestShip(OceanWaveController ocean)
        {
            var root = new GameObject("Test Ship");
            root.transform.position = new Vector3(0f, 0.5f, 0f);

            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(root.transform, false);
            hull.transform.localScale = new Vector3(8f, 1.2f, 18f);
            hull.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            Object.DestroyImmediate(hull.GetComponent<Collider>());

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(root.transform, false);
            deck.transform.localScale = new Vector3(7.5f, 0.15f, 17f);
            deck.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            Object.DestroyImmediate(deck.GetComponent<Collider>());

            ApplyColor(hull, new Color(0.35f, 0.28f, 0.2f));
            ApplyColor(deck, new Color(0.45f, 0.36f, 0.24f));

            var floater = root.AddComponent<FloatingObject>();
            var so = new SerializedObject(floater);
            so.FindProperty("ocean").objectReferenceValue = ocean;
            so.FindProperty("floatMode").enumValueIndex = (int)FloatingObject.FloatMode.ShipPitchRoll;
            so.FindProperty("heightOffset").floatValue = 0.8f;
            so.FindProperty("bowSampleLocal").vector3Value = new Vector3(0f, 0f, 9f);
            so.FindProperty("sternSampleLocal").vector3Value = new Vector3(0f, 0f, -9f);
            so.FindProperty("portSampleLocal").vector3Value = new Vector3(-4f, 0f, 0f);
            so.FindProperty("starboardSampleLocal").vector3Value = new Vector3(4f, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<BowSprayEffect>();

            return root;
        }

        static void CreateBuoys(OceanWaveController ocean)
        {
            var root = new GameObject("Buoys");
            Vector2[] positions =
            {
                new(20f, 0f),
                new(-20f, 0f),
                new(0f, 20f),
                new(0f, -20f),
                new(14f, 14f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var buoy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                buoy.name = $"Buoy_{i + 1}";
                buoy.transform.SetParent(root.transform);
                buoy.transform.position = new Vector3(positions[i].x, 0f, positions[i].y);
                buoy.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                Object.DestroyImmediate(buoy.GetComponent<Collider>());
                ApplyColor(buoy, new Color(0.85f, 0.15f, 0.1f));

                var floater = buoy.AddComponent<FloatingObject>();
                var so = new SerializedObject(floater);
                so.FindProperty("ocean").objectReferenceValue = ocean;
                so.FindProperty("floatMode").enumValueIndex = (int)FloatingObject.FloatMode.SimpleBob;
                so.FindProperty("heightOffset").floatValue = 0.6f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void CreateWeather(out ParticleSystem rain)
        {
            var root = new GameObject("Weather");
            var rainGo = new GameObject("Rain");
            rainGo.transform.SetParent(root.transform);
            rainGo.transform.position = new Vector3(0f, 25f, 0f);
            rain = rainGo.AddComponent<ParticleSystem>();
            ConfigureRain(rain);
        }

        static void ConfigureRain(ParticleSystem ps)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.2f;
            main.startSpeed = 18f;
            main.startSize = 0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 5000;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(80f, 1f, 80f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 0.4f;
        }

        static GameObject CreateManagers()
        {
            return new GameObject("Managers").AddComponent<SeaStateManager>().gameObject;
        }

        static void CreateUi(out Text stateLabel)
        {
            stateLabel = SeaStateHudBuilder.BuildHud();
        }

        static GameObject CreateDeckCamera(GameObject ship)
        {
            var camGo = new GameObject("DeckCamera");
            camGo.transform.SetParent(ship.transform);
            camGo.transform.localPosition = new Vector3(0f, 2.2f, -2f);
            camGo.transform.localRotation = Quaternion.Euler(8f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 5000f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<SeaStateDeckLook>();
            return camGo;
        }

        static void CreateWindIndicator(GameObject deckCamera, GameObject managers)
        {
            var windRoot = new GameObject("WindIndicator");
            windRoot.transform.SetParent(deckCamera.transform);
            windRoot.transform.localPosition = new Vector3(1.2f, 0.4f, 0.8f);
            windRoot.transform.localRotation = Quaternion.identity;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(windRoot.transform);
            pole.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
            pole.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Arrow";
            arrow.transform.SetParent(windRoot.transform);
            arrow.transform.localScale = new Vector3(0.15f, 0.05f, 0.8f);
            arrow.transform.localPosition = new Vector3(0f, 1.05f, 0.35f);
            Object.DestroyImmediate(arrow.GetComponent<Collider>());
            ApplyColor(arrow, Color.yellow);

            var indicator = windRoot.AddComponent<SimpleWindIndicator>();
            var so = new SerializedObject(indicator);
            so.FindProperty("arrow").objectReferenceValue = arrow.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var managerSo = new SerializedObject(managers.GetComponent<SeaStateManager>());
            managerSo.FindProperty("windIndicator").objectReferenceValue = indicator;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireManagerReferences(SeaStateManager manager, GameObject ocean, Light sun, ParticleSystem rain, Text stateLabel)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("ocean").objectReferenceValue = ocean.GetComponent<OceanWaveController>();
            so.FindProperty("sunLight").objectReferenceValue = sun;
            so.FindProperty("rainParticles").objectReferenceValue = rain;
            so.FindProperty("stateLabel").objectReferenceValue = stateLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
