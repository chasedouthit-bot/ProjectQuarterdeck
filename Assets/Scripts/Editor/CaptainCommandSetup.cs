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
    public static class CaptainCommandSetup
    {
        const string ManagerName = "CaptainCommandManager";
        const string UiRootName = "OverlayUI";

        [MenuItem("Quarterdeck/Create Captain Command UI")]
        public static void CreateCaptainCommandUi()
        {
            EnsureEventSystem();
            RemoveLegacyOverlay();

            var existing = GameObject.Find(ManagerName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var managerGo = new GameObject(ManagerName);
            Undo.RegisterCreatedObjectUndo(managerGo, "Create Captain Command UI");
            var manager = managerGo.AddComponent<CaptainCommandManager>();

            var uiRoot = new GameObject(UiRootName);
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create Captain Command UI");
            uiRoot.transform.SetParent(null, false);

            var canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            uiRoot.AddComponent<GraphicRaycaster>();
            StretchFull(uiRoot.GetComponent<RectTransform>());

            CreateImage("DimBackground", uiRoot.transform, new Color(0f, 0f, 0f, 0.55f), StretchFull);

            var mainScreen = CreatePanel("MainScreen", uiRoot.transform, new Color(0.10f, 0.10f, 0.12f, 0.94f));
            StretchCenter(mainScreen.rectTransform, new Vector2(920f, 680f));

            CreateTitle(mainScreen.transform, "PROJECT QUARTERDECK", font, 38, new Vector2(0f, -48f));
            CreateTitle(mainScreen.transform, "Captain's Command", font, 26, new Vector2(0f, -98f), FontStyle.Italic);

            var grid = CreateGrid(mainScreen.transform, new Vector2(340f, 130f), new Vector2(20f, 20f));
            SetRect(grid.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 280f), new Vector2(0f, -30f));

            var sailBtn = CreateButton(grid.transform, "SAIL ORDERS", font, 22);
            var courseBtn = CreateButton(grid.transform, "COURSE / BEARING", font, 22);
            var batteryBtn = CreateButton(grid.transform, "BATTERY STATUS", font, 22);
            var stateBtn = CreateButton(grid.transform, "SHIP STATE", font, 22);

            var sailPanel = CreateSailOrdersPanel(uiRoot.transform, manager, font);
            var coursePanel = CreateDetailPanel(
                uiRoot.transform, "CourseBearingPanel", "COURSE / BEARING",
                "Future brass compass.", font, out var courseBack);
            var batteryPanel = CreateDetailPanel(
                uiRoot.transform, "BatteryStatusPanel", "BATTERY STATUS",
                "Future battery management panel.", font, out var batteryBack);
            var statePanel = CreateDetailPanel(
                uiRoot.transform, "ShipStatePanel", "SHIP STATE",
                "Future ship state dial.", font, out var stateBack);

            WireMainButtons(manager, sailBtn, courseBtn, batteryBtn, stateBtn);
            WireBackButtons(manager, courseBack, batteryBack, stateBack);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("overlayUiRoot").objectReferenceValue = uiRoot;
            serialized.FindProperty("mainScreen").objectReferenceValue = mainScreen.gameObject;
            serialized.FindProperty("sailOrdersPanel").objectReferenceValue = sailPanel;
            serialized.FindProperty("courseBearingPanel").objectReferenceValue = coursePanel;
            serialized.FindProperty("batteryStatusPanel").objectReferenceValue = batteryPanel;
            serialized.FindProperty("shipStatePanel").objectReferenceValue = statePanel;
            serialized.FindProperty("sailOrdersView").objectReferenceValue = sailPanel.GetComponent<SailOrdersInstrumentView>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            uiRoot.SetActive(false);
            Selection.activeGameObject = managerGo;
            EditorGUIUtility.PingObject(managerGo);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Captain Command UI created. Hold Left Shift in Play Mode to enter Captain's Command Mode.");
        }

        [MenuItem("Quarterdeck/Upgrade Sail Orders Instrument v0.1")]
        public static void UpgradeSailOrdersInstrument()
        {
            var manager = Object.FindAnyObjectByType<CaptainCommandManager>();
            if (manager == null)
            {
                Debug.LogError("CaptainCommandManager not found in the active scene.");
                return;
            }

            var serializedManager = new SerializedObject(manager);
            var sailPanelRef = serializedManager.FindProperty("sailOrdersPanel").objectReferenceValue as GameObject;
            if (sailPanelRef == null)
            {
                Debug.LogError("SailOrdersPanel reference missing on CaptainCommandManager.");
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ClearSailOrdersContent(sailPanelRef.transform);

            var instrument = sailPanelRef.GetComponent<SailOrdersInstrumentView>();
            if (instrument == null)
                instrument = Undo.AddComponent<SailOrdersInstrumentView>(sailPanelRef);

            BuildSailOrdersContent(sailPanelRef.transform, manager, instrument, font);

            serializedManager.FindProperty("sailOrdersView").objectReferenceValue = instrument;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = sailPanelRef;
            Debug.Log("Sail Orders instrument v0.1 upgraded on existing Captain Command UI.");
        }

        static void ClearSailOrdersContent(Transform panelTransform)
        {
            for (int i = panelTransform.childCount - 1; i >= 0; i--)
            {
                var child = panelTransform.GetChild(i);
                if (child.name == "Back")
                    continue;

                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        static GameObject CreateSailOrdersPanel(Transform parent, CaptainCommandManager manager, Font font)
        {
            var panel = CreatePanel("SailOrdersPanel", parent, new Color(0.10f, 0.10f, 0.12f, 0.94f));
            StretchCenter(panel.rectTransform, new Vector2(760f, 520f));
            panel.gameObject.SetActive(false);

            var instrument = panel.gameObject.AddComponent<SailOrdersInstrumentView>();
            BuildSailOrdersContent(panel.transform, manager, instrument, font);

            return panel.gameObject;
        }

        static void BuildSailOrdersContent(
            Transform panelTransform,
            CaptainCommandManager manager,
            SailOrdersInstrumentView instrument,
            Font font)
        {
            CreateTitle(panelTransform, "SAIL ORDERS", font, 32, new Vector2(0f, -40f));

            var buttonColumn = new GameObject("OrderButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(buttonColumn, "Build Sail Orders Instrument");
            buttonColumn.transform.SetParent(panelTransform, false);
            var buttonLayout = buttonColumn.GetComponent<VerticalLayoutGroup>();
            buttonLayout.spacing = 8f;
            buttonLayout.childAlignment = TextAnchor.UpperCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;
            SetRect(buttonColumn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(420f, 300f), new Vector2(0f, -120f));

            var strikeBtn = CreateButton(buttonColumn.transform, "Strike Sail", font, 18);
            var heaveBtn = CreateButton(buttonColumn.transform, "Heave To", font, 18);
            var stormBtn = CreateButton(buttonColumn.transform, "Storm Sail", font, 18);
            var battleBtn = CreateButton(buttonColumn.transform, "Battle Sail", font, 18);
            var cruisingBtn = CreateButton(buttonColumn.transform, "Cruising Sail", font, 18);
            var fullBtn = CreateButton(buttonColumn.transform, "Full Sail", font, 18);

            SetButtonHeight(strikeBtn, 40f);
            SetButtonHeight(heaveBtn, 40f);
            SetButtonHeight(stormBtn, 40f);
            SetButtonHeight(battleBtn, 40f);
            SetButtonHeight(cruisingBtn, 40f);
            SetButtonHeight(fullBtn, 40f);

            var statusColumn = new GameObject("StatusColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(statusColumn, "Build Sail Orders Instrument");
            statusColumn.transform.SetParent(panelTransform, false);
            var statusLayout = statusColumn.GetComponent<VerticalLayoutGroup>();
            statusLayout.spacing = 10f;
            statusLayout.childAlignment = TextAnchor.UpperCenter;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = true;
            statusLayout.childForceExpandWidth = true;
            statusLayout.childForceExpandHeight = false;
            SetRect(statusColumn.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(520f, 120f), new Vector2(0f, 92f));

            var currentOrderText = CreateStatusLabel(statusColumn.transform, "Current Sail Order:\n—", font);
            var targetSpeedText = CreateStatusLabel(statusColumn.transform, "Target Speed:\n—", font);
            var currentSpeedText = CreateStatusLabel(statusColumn.transform, "Current Speed:\n0.0 knots", font);

            GameObject backButton = panelTransform.Find("Back")?.gameObject;
            if (backButton == null)
            {
                backButton = CreateButton(panelTransform, "Back", font, 20);
                SetRect(backButton.GetComponent<RectTransform>(),
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(160f, 48f), new Vector2(0f, 28f));
                UnityEventTools.AddPersistentListener(backButton.GetComponent<Button>().onClick, manager.OnBackPressed);
            }

            WireSailOrderButtons(manager, strikeBtn, heaveBtn, stormBtn, battleBtn, cruisingBtn, fullBtn);

            var serialized = new SerializedObject(instrument);
            serialized.FindProperty("strikeSailButton").objectReferenceValue = strikeBtn.GetComponent<Button>();
            serialized.FindProperty("heaveToButton").objectReferenceValue = heaveBtn.GetComponent<Button>();
            serialized.FindProperty("stormSailButton").objectReferenceValue = stormBtn.GetComponent<Button>();
            serialized.FindProperty("battleSailButton").objectReferenceValue = battleBtn.GetComponent<Button>();
            serialized.FindProperty("cruisingSailButton").objectReferenceValue = cruisingBtn.GetComponent<Button>();
            serialized.FindProperty("fullSailButton").objectReferenceValue = fullBtn.GetComponent<Button>();
            serialized.FindProperty("currentSailOrderText").objectReferenceValue = currentOrderText;
            serialized.FindProperty("targetSpeedText").objectReferenceValue = targetSpeedText;
            serialized.FindProperty("currentSpeedText").objectReferenceValue = currentSpeedText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Text CreateStatusLabel(Transform parent, string text, Font font)
        {
            var go = new GameObject(text.Split('\n')[0], typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = 20;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.88f, 0.86f, 0.78f);
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 34f;
            return label;
        }

        static void SetButtonHeight(GameObject buttonGo, float height)
        {
            var layout = buttonGo.GetComponent<LayoutElement>();
            if (layout == null)
                layout = buttonGo.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
        }

        static void WireSailOrderButtons(
            CaptainCommandManager manager,
            GameObject strike,
            GameObject heave,
            GameObject storm,
            GameObject battle,
            GameObject cruising,
            GameObject full)
        {
            UnityEventTools.AddPersistentListener(strike.GetComponent<Button>().onClick, manager.SelectStrikeSail);
            UnityEventTools.AddPersistentListener(heave.GetComponent<Button>().onClick, manager.SelectHeaveTo);
            UnityEventTools.AddPersistentListener(storm.GetComponent<Button>().onClick, manager.SelectStormSail);
            UnityEventTools.AddPersistentListener(battle.GetComponent<Button>().onClick, manager.SelectBattleSail);
            UnityEventTools.AddPersistentListener(cruising.GetComponent<Button>().onClick, manager.SelectCruisingSail);
            UnityEventTools.AddPersistentListener(full.GetComponent<Button>().onClick, manager.SelectFullSail);
        }

        static void RemoveLegacyOverlay()
        {
            var legacy = GameObject.Find("ShipManagementOverlay");
            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy);
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create Captain Command UI");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        static GameObject CreateDetailPanel(
            Transform parent, string name, string title, string placeholder, Font font, out GameObject backButton)
        {
            var panel = CreatePanel(name, parent, new Color(0.10f, 0.10f, 0.12f, 0.94f));
            StretchCenter(panel.rectTransform, new Vector2(760f, 520f));
            panel.gameObject.SetActive(false);

            CreateTitle(panel.transform, title, font, 32, new Vector2(0f, -40f));
            CreatePlaceholder(panel.transform, placeholder, font);

            backButton = CreateButton(panel.transform, "Back", font, 20);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 48f), new Vector2(0f, 28f));

            return panel.gameObject;
        }

        static void WireMainButtons(
            CaptainCommandManager manager,
            GameObject sail,
            GameObject course,
            GameObject battery,
            GameObject state)
        {
            UnityEventTools.AddPersistentListener(sail.GetComponent<Button>().onClick, manager.OpenSailOrders);
            UnityEventTools.AddPersistentListener(course.GetComponent<Button>().onClick, manager.OpenCourseBearing);
            UnityEventTools.AddPersistentListener(battery.GetComponent<Button>().onClick, manager.OpenBatteryStatus);
            UnityEventTools.AddPersistentListener(state.GetComponent<Button>().onClick, manager.OpenShipState);
        }

        static void WireBackButtons(CaptainCommandManager manager, params GameObject[] backButtons)
        {
            foreach (var back in backButtons)
                UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, manager.OnBackPressed);
        }

        static Image CreatePanel(string name, Transform parent, Color color) =>
            CreateImage(name, parent, color, _ => { });

        static Image CreateImage(string name, Transform parent, Color color, System.Action<RectTransform> layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            layout(go.GetComponent<RectTransform>());
            return go.GetComponent<Image>();
        }

        static void CreateTitle(Transform parent, string text, Font font, int size, Vector2 offset, FontStyle style = FontStyle.Bold)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.92f, 0.88f, 0.78f);
            SetRect(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(800f, 56f), offset);
        }

        static void CreatePlaceholder(Transform parent, string text, Font font)
        {
            var go = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.75f, 0.75f, 0.75f);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 120f), Vector2.zero);
        }

        static GameObject CreateGrid(Transform parent, Vector2 cellSize, Vector2 spacing)
        {
            var go = new GameObject("MainButtonGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var grid = go.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
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

            return go;
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void StretchCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
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
