using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Quarterdeck.Editor
{
    public static class ShipManagementOverlaySetup
    {
        const string ControllerName = "ShipManagementOverlay";
        const string CanvasChildName = "OverlayUI";

        [MenuItem("Quarterdeck/Create Ship Management Overlay UI")]
        public static void CreateShipManagementOverlayUi()
        {
            EnsureEventSystem();

            var existing = GameObject.Find(ControllerName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var controllerGo = new GameObject(ControllerName);
            Undo.RegisterCreatedObjectUndo(controllerGo, "Create Ship Management Overlay UI");
            var overlay = controllerGo.AddComponent<ShipManagementOverlay>();

            var canvasGo = new GameObject(CanvasChildName);
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Ship Management Overlay UI");
            canvasGo.transform.SetParent(controllerGo.transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var dim = CreateImage("DimBackground", canvasGo.transform, new Color(0f, 0f, 0f, 0.55f));
            StretchFull(dim.rectTransform);

            var mainPanel = CreatePanel("MainPanel", canvasGo.transform, new Color(0.12f, 0.12f, 0.14f, 0.92f));
            StretchCenter(mainPanel.rectTransform, new Vector2(900f, 620f));

            CreateTitle(mainPanel.transform, "SHIP MANAGEMENT", font, 42);

            var mainGrid = CreateGrid(mainPanel.transform, 2, 2, new Vector2(320f, 120f), new Vector2(24f, 24f));
            SetRect(mainGrid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(664f, 280f), Vector2.zero);

            var sailOrdersButton = CreateButton(mainGrid.transform, "Sail Orders", font, 24);
            var courseButton = CreateButton(mainGrid.transform, "Course / Bearing", font, 24);
            var batteryButton = CreateButton(mainGrid.transform, "Battery Status", font, 24);
            var shipStateButton = CreateButton(mainGrid.transform, "Ship State", font, 24);

            var sailPanel = CreateSubPanel(canvasGo.transform, "SailOrdersPanel", "SAIL ORDERS", font, out var sailContent);
            WireNamedButtons(overlay, sailContent, new (string label, string method)[]
            {
                ("Strike Sail", nameof(ShipManagementOverlay.SelectStrikeSail)),
                ("Heave To", nameof(ShipManagementOverlay.SelectHeaveTo)),
                ("Storm Sail", nameof(ShipManagementOverlay.SelectStormSail)),
                ("Battle Sail", nameof(ShipManagementOverlay.SelectBattleSail)),
                ("Cruising Sail", nameof(ShipManagementOverlay.SelectCruisingSail)),
                ("Full Sail", nameof(ShipManagementOverlay.SelectFullSail))
            });

            var coursePanel = CreateSubPanel(canvasGo.transform, "CourseBearingPanel", "COURSE / BEARING", font, out var courseContent);
            CreatePlaceholderText(courseContent, "Compass UI will go here.", font);

            var batteryPanel = CreateSubPanel(canvasGo.transform, "BatteryStatusPanel", "BATTERY STATUS", font, out var batteryContent);
            CreatePlaceholderText(batteryContent, "Gun deck battery UI will go here.", font);
            var batteryButtons = CreateHorizontalButtonRow(batteryContent, font);
            CreateButton(batteryButtons.transform, "Fire All Port", font, 20);
            CreateButton(batteryButtons.transform, "Fire All Starboard", font, 20);

            var statePanel = CreateSubPanel(canvasGo.transform, "ShipStatePanel", "SHIP STATE", font, out var stateContent);
            WireNamedButtons(overlay, stateContent, new (string label, string method)[]
            {
                ("Secure Ship", nameof(ShipManagementOverlay.SelectSecureShip)),
                ("Clear for Action", nameof(ShipManagementOverlay.SelectClearForAction)),
                ("Heavy Weather", nameof(ShipManagementOverlay.SelectHeavyWeather)),
                ("Damage Control", nameof(ShipManagementOverlay.SelectDamageControl))
            });

            WireMainButtons(overlay, sailOrdersButton, courseButton, batteryButton, shipStateButton);
            WireBackButtons(overlay, sailPanel, coursePanel, batteryPanel, statePanel);
            WireBatteryButtons(overlay, batteryButtons);

            var serialized = new SerializedObject(overlay);
            serialized.FindProperty("overlayRoot").objectReferenceValue = canvasGo;
            serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel.gameObject;
            serialized.FindProperty("sailOrdersPanel").objectReferenceValue = sailPanel;
            serialized.FindProperty("courseBearingPanel").objectReferenceValue = coursePanel;
            serialized.FindProperty("batteryStatusPanel").objectReferenceValue = batteryPanel;
            serialized.FindProperty("shipStatePanel").objectReferenceValue = statePanel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            canvasGo.SetActive(false);
            Selection.activeGameObject = controllerGo;
            EditorGUIUtility.PingObject(controllerGo);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Ship Management Overlay UI created. Hold Space in Play Mode to open.");
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create Ship Management Overlay UI");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        static Image CreatePanel(string name, Transform parent, Color color)
        {
            var image = CreateImage(name, parent, color);
            return image;
        }

        static GameObject CreateSubPanel(Transform parent, string name, string title, Font font, out RectTransform contentArea)
        {
            var panel = CreatePanel(name, parent, new Color(0.12f, 0.12f, 0.14f, 0.95f));
            StretchCenter(panel.rectTransform, new Vector2(760f, 560f));
            panel.gameObject.SetActive(false);

            CreateTitle(panel.transform, title, font, 34);

            var content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            contentArea = content.AddComponent<RectTransform>();
            StretchCenter(contentArea, new Vector2(640f, 360f));
            contentArea.anchoredPosition = new Vector2(0f, -20f);

            var backButton = CreateButton(panel.transform, "Back", font, 20);
            var backRect = backButton.GetComponent<RectTransform>();
            SetRect(backRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 48f), new Vector2(0f, 28f));

            return panel.gameObject;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        static void CreateTitle(Transform parent, string text, Font font, int size)
        {
            var go = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.92f, 0.88f, 0.78f);

            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640f, 64f), new Vector2(0f, -36f));
        }

        static void CreatePlaceholderText(RectTransform parent, string text, Font font)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.78f, 0.78f, 0.78f);

            StretchFull(label.rectTransform);
        }

        static GameObject CreateGrid(Transform parent, int columns, int rows, Vector2 cellSize, Vector2 spacing)
        {
            var go = new GameObject("MainButtonGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            return go;
        }

        static GameObject CreateHorizontalButtonRow(RectTransform parent, Font font)
        {
            var go = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(520f, 56f), new Vector2(0f, 24f));
            return go;
        }

        static GameObject CreateButton(Transform parent, string label, Font font, int fontSize)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.28f, 0.26f, 0.22f, 1f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.42f, 0.38f, 0.30f);
            colors.pressedColor = new Color(0.20f, 0.18f, 0.16f);
            button.colors = colors;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.92f, 0.86f);
            StretchFull(text.rectTransform);

            var rect = go.GetComponent<RectTransform>();
            if (parent.GetComponent<GridLayoutGroup>() == null &&
                parent.GetComponent<HorizontalLayoutGroup>() == null &&
                parent.GetComponent<VerticalLayoutGroup>() == null)
            {
                SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280f, 56f), Vector2.zero);
            }
            else if (parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(240f, 56f), Vector2.zero);
            }

            return go;
        }

        static void WireNamedButtons(ShipManagementOverlay overlay, RectTransform parent, (string label, string method)[] entries)
        {
            var column = new GameObject("ButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            StretchFull(column.GetComponent<RectTransform>());

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            foreach (var entry in entries)
            {
                var button = CreateButton(column.transform, entry.label, font, 22);
                var method = typeof(ShipManagementOverlay).GetMethod(entry.method);
                if (method != null)
                {
                    UnityEventTools.AddPersistentListener(
                        button.GetComponent<Button>().onClick,
                        (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                            typeof(UnityEngine.Events.UnityAction), overlay, method));
                }
            }
        }

        static void WireMainButtons(
            ShipManagementOverlay overlay,
            GameObject sailOrders,
            GameObject course,
            GameObject battery,
            GameObject shipState)
        {
            UnityEventTools.AddPersistentListener(
                sailOrders.GetComponent<Button>().onClick,
                overlay.OpenSailOrdersPanel);
            UnityEventTools.AddPersistentListener(
                course.GetComponent<Button>().onClick,
                overlay.OpenCourseBearingPanel);
            UnityEventTools.AddPersistentListener(
                battery.GetComponent<Button>().onClick,
                overlay.OpenBatteryStatusPanel);
            UnityEventTools.AddPersistentListener(
                shipState.GetComponent<Button>().onClick,
                overlay.OpenShipStatePanel);
        }

        static void WireBackButtons(ShipManagementOverlay overlay, params GameObject[] panels)
        {
            foreach (var panel in panels)
            {
                var back = panel.transform.Find("Back");
                if (back == null)
                    continue;

                UnityEventTools.AddPersistentListener(
                    back.GetComponent<Button>().onClick,
                    overlay.ShowMainPanel);
            }
        }

        static void WireBatteryButtons(ShipManagementOverlay overlay, GameObject row)
        {
            var buttons = row.GetComponentsInChildren<Button>(true);
            if (buttons.Length >= 2)
            {
                UnityEventTools.AddPersistentListener(buttons[0].onClick, overlay.OnFireAllPort);
                UnityEventTools.AddPersistentListener(buttons[1].onClick, overlay.OnFireAllStarboard);
            }
        }

        static void StretchFull(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void StretchCenter(RectTransform rect, Vector2 size)
        {
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, Vector2.zero);
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
