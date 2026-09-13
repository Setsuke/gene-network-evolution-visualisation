#if UNITY_EDITOR

using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Menu;
using RbnVisualisation.Trajectory;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RbnVisualisation.Menu.Editor
{
    public static class AppSceneSetupMenu
    {
        private const string BatchGenomeMdsJsonFileName = "unity_batch_genome_mds_path.json";

        [MenuItem("RBN/Setup Application")]
        public static void SetupApplication()
        {
            AppSceneCleanupMenu.CleanSceneDuplicates();
            EnsureEventSystem();

            AppMenuController controller = FindOrCreateController();
            GameObject genomeMdsPathRoot = FindOrCreateGenomeMdsPathRoot();
            GameObject genomeMdsFitnessPathRoot = FindOrCreateGenomeMdsFitnessPathRoot();
            GameObject booleanAttractorLandscapeRoot = FindOrCreateBooleanAttractorLandscapeRoot();
            GameObject sharedGenomeMdsPathRoot = FindOrCreateSharedGenomeMdsPathRoot();
            GameObject batchGenomeMdsPathRoot = FindOrCreateBatchGenomeMdsPathRoot();
            GameObject batchGenomeMdsFitnessPathRoot = FindOrCreateBatchGenomeMdsFitnessPathRoot();
            Option10DualPaneMdsAttractorViewController option10DualPaneViewController =
                FindOrCreateOption10DualPaneViewRoot();
            GenomeMdsPathVisualiser genomeMdsPathVisualiser =
                genomeMdsPathRoot.GetComponent<GenomeMdsPathVisualiser>();
            GenomeMdsFitnessPathVisualiser genomeMdsFitnessPathVisualiser =
                genomeMdsFitnessPathRoot.GetComponent<GenomeMdsFitnessPathVisualiser>();
            BooleanAttractorLandscapeVisualiser booleanAttractorLandscapeVisualiser =
                booleanAttractorLandscapeRoot.GetComponent<BooleanAttractorLandscapeVisualiser>();
            SharedGenomeMdsPathVisualiser sharedGenomeMdsPathVisualiser =
                sharedGenomeMdsPathRoot.GetComponent<SharedGenomeMdsPathVisualiser>();
            BatchGenomeMdsPathVisualiser batchGenomeMdsPathVisualiser =
                batchGenomeMdsPathRoot.GetComponent<BatchGenomeMdsPathVisualiser>();
            BatchGenomeMdsFitnessPathVisualiser batchGenomeMdsFitnessPathVisualiser =
                batchGenomeMdsFitnessPathRoot.GetComponent<BatchGenomeMdsFitnessPathVisualiser>();
            Camera mainCamera = SetupMainCamera(genomeMdsPathRoot.transform);
            TrajectorySceneCamera trajectorySceneCamera = mainCamera.GetComponent<TrajectorySceneCamera>();

            Canvas canvas = FindOrCreateCanvas();
            GameObject mainMenuPanel = BuildMainMenuPanel(canvas.transform, controller);
            GameObject genomeEmbeddingPathUiPanel = BuildGenomeEmbeddingPathUiPanel(canvas.transform, controller);
            GameObject option10DualPaneUiPanel = BuildOption10DualPaneUiPanel(canvas.transform, controller);

            WireController(
                controller,
                mainMenuPanel,
                genomeMdsPathRoot,
                genomeMdsFitnessPathRoot,
                booleanAttractorLandscapeRoot,
                sharedGenomeMdsPathRoot,
                batchGenomeMdsPathRoot,
                batchGenomeMdsFitnessPathRoot,
                option10DualPaneViewController,
                option10DualPaneUiPanel,
                genomeEmbeddingPathUiPanel,
                genomeMdsPathVisualiser,
                genomeMdsFitnessPathVisualiser,
                booleanAttractorLandscapeVisualiser,
                sharedGenomeMdsPathVisualiser,
                batchGenomeMdsPathVisualiser,
                batchGenomeMdsFitnessPathVisualiser,
                mainCamera,
                trajectorySceneCamera);

            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
            Debug.Log("RBN application setup complete. Press Play to open the main menu.");
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null
                    && eventSystem.GetComponent<StandaloneInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static AppMenuController FindOrCreateController()
        {
            AppMenuController existing = Object.FindFirstObjectByType<AppMenuController>();
            if (existing != null)
            {
                existing.gameObject.name = "AppMenuController";
                return existing;
            }

            GameObject controllerObject = new GameObject("AppMenuController");
            return controllerObject.AddComponent<AppMenuController>();
        }

        private static GameObject FindOrCreateGenomeMdsPathRoot()
        {
            GameObject existing = GameObject.Find("GenomeMdsPathRoot");
            if (existing != null)
            {
                return existing;
            }

            GameObject genomeMdsPathRoot = new GameObject("GenomeMdsPathRoot");
            genomeMdsPathRoot.AddComponent<GenomeMdsPathVisualiser>();
            return genomeMdsPathRoot;
        }

        private static Option10DualPaneMdsAttractorViewController FindOrCreateOption10DualPaneViewRoot()
        {
            GameObject existing = GameObject.Find("Option10DualPaneMdsAttractorViewRoot");
            if (existing != null)
            {
                Option10DualPaneMdsAttractorViewController existingController =
                    existing.GetComponent<Option10DualPaneMdsAttractorViewController>()
                    ?? existing.AddComponent<Option10DualPaneMdsAttractorViewController>();
                existingController.EnsureReady(Camera.main);
                ConfigureOption10PlateauDriftVisualiser(
                    existingController.GetComponentInChildren<PlateauDriftPathVisualiser>(true));
                return existingController;
            }

            GameObject root = new GameObject("Option10DualPaneMdsAttractorViewRoot");
            Option10DualPaneMdsAttractorViewController controller =
                root.AddComponent<Option10DualPaneMdsAttractorViewController>();

            GameObject leftAnchor = new GameObject("LeftPaneAnchor");
            leftAnchor.transform.SetParent(root.transform, false);
            leftAnchor.transform.localPosition = new Vector3(-DualPaneHorizontalOffset, 0f, 0f);

            GameObject plateauRoot = new GameObject("PlateauDriftPathRoot");
            plateauRoot.transform.SetParent(leftAnchor.transform, false);
            PlateauDriftPathVisualiser plateauVisualiser =
                plateauRoot.AddComponent<PlateauDriftPathVisualiser>();
            ConfigureOption10PlateauDriftVisualiser(plateauVisualiser);

            GameObject dualMdsRoot = new GameObject("DualPaneMdsPathRoot");
            dualMdsRoot.transform.SetParent(leftAnchor.transform, false);
            GenomeMdsPathVisualiser dualMdsVisualiser = dualMdsRoot.AddComponent<GenomeMdsPathVisualiser>();
            dualMdsVisualiser.enabled = false;
            dualMdsRoot.SetActive(false);

            GameObject rightAnchor = new GameObject("RightPaneAnchor");
            rightAnchor.transform.SetParent(root.transform, false);
            rightAnchor.transform.localPosition = new Vector3(DualPaneHorizontalOffset, 0f, 0f);

            GameObject dualAttractorRoot = new GameObject("DualPaneAttractorRoot");
            dualAttractorRoot.transform.SetParent(rightAnchor.transform, false);
            BooleanAttractorLandscapeVisualiser dualAttractorVisualiser =
                dualAttractorRoot.AddComponent<BooleanAttractorLandscapeVisualiser>();

            Camera template = Camera.main;
            GameObject leftCameraObject = new GameObject("DualPaneLeftCamera");
            leftCameraObject.transform.SetParent(root.transform, false);
            Camera leftCamera = leftCameraObject.AddComponent<Camera>();
            TrajectorySceneCamera leftSceneCamera = leftCameraObject.AddComponent<TrajectorySceneCamera>();
            ConfigureDualPaneCamera(leftCamera, template, 10);

            GameObject rightCameraObject = new GameObject("DualPaneRightCamera");
            rightCameraObject.transform.SetParent(root.transform, false);
            Camera rightCamera = rightCameraObject.AddComponent<Camera>();
            TrajectorySceneCamera rightSceneCamera = rightCameraObject.AddComponent<TrajectorySceneCamera>();
            ConfigureDualPaneCamera(rightCamera, template, 11);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("leftPaneAnchor").objectReferenceValue = leftAnchor.transform;
            serializedController.FindProperty("rightPaneAnchor").objectReferenceValue = rightAnchor.transform;
            serializedController.FindProperty("plateauDriftPathVisualiser").objectReferenceValue = plateauVisualiser;
            serializedController.FindProperty("mdsPathVisualiser").objectReferenceValue = dualMdsVisualiser;
            serializedController.FindProperty("attractorVisualiser").objectReferenceValue = dualAttractorVisualiser;
            serializedController.FindProperty("leftCamera").objectReferenceValue = leftCamera;
            serializedController.FindProperty("rightCamera").objectReferenceValue = rightCamera;
            serializedController.FindProperty("leftSceneCamera").objectReferenceValue = leftSceneCamera;
            serializedController.FindProperty("rightSceneCamera").objectReferenceValue = rightSceneCamera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            DualPaneRenderIsolation.Apply(
                plateauVisualiser.transform,
                dualAttractorVisualiser.transform,
                leftCamera,
                rightCamera);

            root.SetActive(false);
            return controller;
        }

        private static void ConfigureOption10PlateauDriftVisualiser(PlateauDriftPathVisualiser visualiser)
        {
            if (visualiser == null)
            {
                return;
            }

            SerializedObject serializedVisualiser = new SerializedObject(visualiser);
            serializedVisualiser.FindProperty("jsonFileName").stringValue = BatchGenomeMdsJsonFileName;
            serializedVisualiser.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject FindOrCreateSharedGenomeMdsPathRoot()
        {
            GameObject existing = GameObject.Find("SharedGenomeMdsPathRoot");
            if (existing != null)
            {
                return existing;
            }

            GameObject sharedGenomeMdsPathRoot = new GameObject("SharedGenomeMdsPathRoot");
            sharedGenomeMdsPathRoot.AddComponent<SharedGenomeMdsPathVisualiser>();
            return sharedGenomeMdsPathRoot;
        }

        private static GameObject FindOrCreateBatchGenomeMdsPathRoot()
        {
            GameObject existing = GameObject.Find("BatchGenomeMdsPathRoot");
            if (existing != null)
            {
                if (existing.GetComponent<BatchGenomeMdsPathVisualiser>() == null)
                {
                    existing.AddComponent<BatchGenomeMdsPathVisualiser>();
                }

                return existing;
            }

            GameObject root = new GameObject("BatchGenomeMdsPathRoot");
            root.AddComponent<BatchGenomeMdsPathVisualiser>();
            return root;
        }

        private static GameObject FindOrCreateBatchGenomeMdsFitnessPathRoot()
        {
            GameObject existing = GameObject.Find("BatchGenomeMdsFitnessPathRoot");
            if (existing != null)
            {
                if (existing.GetComponent<BatchGenomeMdsFitnessPathVisualiser>() == null)
                {
                    existing.AddComponent<BatchGenomeMdsFitnessPathVisualiser>();
                }

                return existing;
            }

            GameObject root = new GameObject("BatchGenomeMdsFitnessPathRoot");
            BatchGenomeMdsFitnessPathVisualiser visualiser =
                root.AddComponent<BatchGenomeMdsFitnessPathVisualiser>();
            SerializedObject serializedVisualiser = new SerializedObject(visualiser);
            serializedVisualiser.FindProperty("jsonFileName").stringValue = BatchGenomeMdsJsonFileName;
            serializedVisualiser.FindProperty("maxRunCount").intValue = 200;
            serializedVisualiser.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }


        private const float DualPaneHorizontalOffset = 0f;


        private static void ConfigureDualPaneCamera(Camera camera, Camera template, int depth)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = template != null
                ? template.backgroundColor
                : new Color(0.08f, 0.09f, 0.12f, 1f);
            camera.nearClipPlane = template != null ? template.nearClipPlane : 0.1f;
            camera.farClipPlane = template != null ? template.farClipPlane : 500f;
            camera.depth = depth;
            camera.enabled = false;
        }

        [MenuItem("RBN/Add Option 6 (Shared Genome MDS Path)")]
        public static void AddSharedGenomeMdsPathOption()
        {
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                Debug.LogError("AppMenuController not found. Run RBN/Setup Application first.");
                return;
            }

            GameObject sharedGenomeMdsPathRoot = FindOrCreateSharedGenomeMdsPathRoot();
            SharedGenomeMdsPathVisualiser visualiser =
                sharedGenomeMdsPathRoot.GetComponent<SharedGenomeMdsPathVisualiser>();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found.");
                return;
            }

            Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel not found.");
                return;
            }

            EnsureOption6Button(mainMenuPanel, controller);

            GameObject genomeEmbeddingPathUiPanel = canvas.transform.Find("GenomeMdsPathUiPanel")?.gameObject
                ?? canvas.transform.Find("GenomeEmbeddingPathUiPanel")?.gameObject;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("sharedGenomeMdsPathViewRoot").objectReferenceValue =
                sharedGenomeMdsPathRoot;
            serializedController.FindProperty("sharedGenomeMdsPathVisualiser").objectReferenceValue = visualiser;
            if (genomeEmbeddingPathUiPanel != null)
            {
                serializedController.FindProperty("genomeEmbeddingPathUiPanel").objectReferenceValue =
                    genomeEmbeddingPathUiPanel;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            Debug.Log(
                "Option 6 (Shared Genome MDS Path) added. Export with demo.py --shared-initial-genome first.");
        }

        [MenuItem("RBN/Add Option 8 (Batch Genome MDS Path)")]
        public static void AddBatchGenomeMdsPathOption()
        {
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                Debug.LogError("AppMenuController not found. Run RBN/Setup Application first.");
                return;
            }

            GameObject batchGenomeMdsPathRoot = FindOrCreateBatchGenomeMdsPathRoot();
            BatchGenomeMdsPathVisualiser visualiser =
                batchGenomeMdsPathRoot.GetComponent<BatchGenomeMdsPathVisualiser>();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found.");
                return;
            }

            Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel not found.");
                return;
            }

            EnsureOption8Button(mainMenuPanel, controller);

            GameObject genomeEmbeddingPathUiPanel = canvas.transform.Find("GenomeMdsPathUiPanel")?.gameObject
                ?? canvas.transform.Find("GenomeEmbeddingPathUiPanel")?.gameObject;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("batchGenomeMdsPathViewRoot").objectReferenceValue =
                batchGenomeMdsPathRoot;
            serializedController.FindProperty("batchGenomeMdsPathVisualiser").objectReferenceValue = visualiser;
            if (genomeEmbeddingPathUiPanel != null)
            {
                serializedController.FindProperty("genomeEmbeddingPathUiPanel").objectReferenceValue =
                    genomeEmbeddingPathUiPanel;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            Debug.Log(
                "Option 8 (Batch Genome MDS Path) added. Export with demo.py without --shared-initial-genome.");
        }



        private static GameObject FindOrCreateBooleanAttractorLandscapeRoot()
        {
            GameObject existing = GameObject.Find("BooleanAttractorLandscapeRoot");
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("BooleanAttractorLandscapeRoot");
            root.AddComponent<BooleanAttractorLandscapeVisualiser>();
            return root;
        }

        [MenuItem("RBN/Add Option 5 (Boolean Attractor Landscape)")]
        public static void AddBooleanAttractorLandscapeOption()
        {
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                Debug.LogError("AppMenuController not found. Run RBN/Setup Application first.");
                return;
            }

            GameObject booleanAttractorLandscapeRoot = FindOrCreateBooleanAttractorLandscapeRoot();
            BooleanAttractorLandscapeVisualiser visualiser =
                booleanAttractorLandscapeRoot.GetComponent<BooleanAttractorLandscapeVisualiser>();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found.");
                return;
            }

            Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel not found.");
                return;
            }

            EnsureOption5Button(mainMenuPanel, controller);

            GameObject genomeEmbeddingPathUiPanel = canvas.transform.Find("GenomeMdsPathUiPanel")?.gameObject
                ?? canvas.transform.Find("GenomeEmbeddingPathUiPanel")?.gameObject;
            Camera mainCamera = Object.FindFirstObjectByType<Camera>();
            TrajectorySceneCamera trajectorySceneCamera =
                mainCamera != null ? mainCamera.GetComponent<TrajectorySceneCamera>() : null;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("booleanAttractorLandscapeViewRoot").objectReferenceValue =
                booleanAttractorLandscapeRoot;
            serializedController.FindProperty("booleanAttractorLandscapeVisualiser").objectReferenceValue =
                visualiser;
            if (genomeEmbeddingPathUiPanel != null)
            {
                serializedController.FindProperty("genomeEmbeddingPathUiPanel").objectReferenceValue =
                    genomeEmbeddingPathUiPanel;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            Debug.Log("Option 5 (Boolean Attractor Landscape) added.");
        }

        [MenuItem("RBN/Add Option 10 (Plateau Drift + Attractor)")]
        public static void AddOption10DualPaneMdsAttractorOption()
        {
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                Debug.LogError("AppMenuController not found. Run RBN/Setup Application first.");
                return;
            }

            Option10DualPaneMdsAttractorViewController option10DualPaneViewController =
                FindOrCreateOption10DualPaneViewRoot();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found.");
                return;
            }

            Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel not found.");
                return;
            }

            EnsureOption11Button(mainMenuPanel, controller);

            GameObject option10DualPaneUiPanel = canvas.transform.Find("Option10DualPaneUiPanel")?.gameObject
                ?? BuildOption10DualPaneUiPanel(canvas.transform, controller);

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("option10DualPaneMdsAttractorViewRoot").objectReferenceValue =
                option10DualPaneViewController.gameObject;
            serializedController.FindProperty("option10DualPaneMdsAttractorViewController").objectReferenceValue =
                option10DualPaneViewController;
            serializedController.FindProperty("option10DualPaneUiPanel").objectReferenceValue = option10DualPaneUiPanel;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            MainMenuLayout.Apply(mainMenuPanel);
            Debug.Log(
                "Plateau Drift dual-pane view added. Uses unity_genome_mds_path.json and " +
                "unity_boolean_attractor_landscape.json.");
        }

        [MenuItem("RBN/Add Option 11 (Batch MDS + Fitness Height)")]
        public static void AddBatchGenomeMdsFitnessPathOption()
        {
            AppMenuController controller = Object.FindFirstObjectByType<AppMenuController>();
            if (controller == null)
            {
                Debug.LogError("AppMenuController not found. Run RBN/Setup Application first.");
                return;
            }

            GameObject batchGenomeMdsFitnessPathRoot = FindOrCreateBatchGenomeMdsFitnessPathRoot();
            BatchGenomeMdsFitnessPathVisualiser visualiser =
                batchGenomeMdsFitnessPathRoot.GetComponent<BatchGenomeMdsFitnessPathVisualiser>();

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found.");
                return;
            }

            Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");
            if (mainMenuPanel == null)
            {
                Debug.LogError("MainMenuPanel not found.");
                return;
            }

            EnsureOption12Button(mainMenuPanel, controller);

            GameObject genomeEmbeddingPathUiPanel = canvas.transform.Find("GenomeMdsPathUiPanel")?.gameObject
                ?? canvas.transform.Find("GenomeEmbeddingPathUiPanel")?.gameObject;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("batchGenomeMdsFitnessPathViewRoot").objectReferenceValue =
                batchGenomeMdsFitnessPathRoot;
            serializedController.FindProperty("batchGenomeMdsFitnessPathVisualiser").objectReferenceValue = visualiser;
            if (genomeEmbeddingPathUiPanel != null)
            {
                serializedController.FindProperty("genomeEmbeddingPathUiPanel").objectReferenceValue =
                    genomeEmbeddingPathUiPanel;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            MainMenuLayout.Apply(mainMenuPanel);
            Debug.Log(
                "Option 11 added. Shows first 200 runs from unity_batch_genome_mds_path.json " +
                "with X/Z = MDS and Y = total fitness.");
        }

        private static GameObject FindOrCreateGenomeMdsFitnessPathRoot()
        {
            GameObject existing = GameObject.Find("GenomeMdsFitnessPathRoot");
            if (existing != null)
            {
                return existing;
            }

            GameObject genomeMdsFitnessPathRoot = new GameObject("GenomeMdsFitnessPathRoot");
            genomeMdsFitnessPathRoot.AddComponent<GenomeMdsFitnessPathVisualiser>();
            return genomeMdsFitnessPathRoot;
        }

        private static Camera SetupMainCamera(Transform focus)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
                cameraObject.AddComponent<AudioListener>();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            TrajectorySceneCamera orbit = camera.GetComponent<TrajectorySceneCamera>();
            if (orbit == null)
            {
                orbit = camera.gameObject.AddComponent<TrajectorySceneCamera>();
            }

            SerializedObject serializedOrbit = new SerializedObject(orbit);
            serializedOrbit.FindProperty("focus").objectReferenceValue = focus;
            serializedOrbit.ApplyModifiedPropertiesWithoutUndo();
            orbit.enabled = false;
            orbit.PlaceCameraFixed();

            TrajectoryLightingSetup.Apply();

            return camera;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasObject = new GameObject("UI Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject BuildMainMenuPanel(Transform canvasTransform, AppMenuController controller)
        {
            Transform existing = canvasTransform.Find("MainMenuPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = CreateUiPanel("MainMenuPanel", canvasTransform, new Color(0.07f, 0.08f, 0.11f, 0.98f));

            CreateUiText(
                "Title",
                panel.transform,
                "RBN Visualisation",
                56,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -80f),
                new Vector2(900f, 100f));

            EnsureMenuOptionButton(
                panel.transform,
                controller,
                0,
                MainMenuLayout.ButtonNames[0],
                controller.ShowGenomeMdsPathView);
            EnsureMenuOptionButton(
                panel.transform,
                controller,
                1,
                MainMenuLayout.ButtonNames[1],
                controller.ShowGenomeMdsFitnessPathView);
            EnsureMenuOptionButton(
                panel.transform,
                controller,
                2,
                MainMenuLayout.ButtonNames[2],
                controller.ShowBooleanAttractorLandscapeView);
            EnsureMenuOptionButton(
                panel.transform,
                controller,
                3,
                MainMenuLayout.ButtonNames[3],
                controller.ShowSharedGenomeMdsPathView);
            EnsureMenuOptionButton(
                panel.transform,
                controller,
                4,
                MainMenuLayout.ButtonNames[4],
                controller.ShowBatchGenomeMdsPathView);

            Transform legacyOption3Button = panel.transform.Find("Option3Button");
            if (legacyOption3Button != null)
            {
                Object.DestroyImmediate(legacyOption3Button.gameObject);
            }

            Transform legacyOption7Button = panel.transform.Find("Option7Button");
            if (legacyOption7Button != null)
            {
                Object.DestroyImmediate(legacyOption7Button.gameObject);
            }

            Transform legacyOption8Button = panel.transform.Find("Option8Button");
            if (legacyOption8Button != null)
            {
                Object.DestroyImmediate(legacyOption8Button.gameObject);
            }

            EnsureMenuOptionButton(
                panel.transform,
                controller,
                5,
                MainMenuLayout.ButtonNames[5],
                controller.ShowOption10DualPaneMdsAttractorView);
            EnsureMenuOptionButton(
                panel.transform,
                controller,
                6,
                MainMenuLayout.ButtonNames[6],
                controller.ShowBatchGenomeMdsFitnessPathView);

            return panel;
        }

        private static void EnsureMenuOptionButton(
            Transform mainMenuPanel,
            AppMenuController controller,
            int layoutIndex,
            string buttonName,
            UnityAction onClick)
        {
            Transform existing = mainMenuPanel.Find(buttonName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Button button = CreateUiButton(
                buttonName,
                mainMenuPanel,
                MainMenuLayout.GetMenuLabel(layoutIndex),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, MainMenuLayout.ButtonY[layoutIndex]),
                new Vector2(MainMenuLayout.ButtonWidth, MainMenuLayout.ButtonHeight));

            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        private static void EnsureOption4Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                1,
                MainMenuLayout.ButtonNames[1],
                controller.ShowGenomeMdsFitnessPathView);
        }

        private static void EnsureOption5Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                2,
                MainMenuLayout.ButtonNames[2],
                controller.ShowBooleanAttractorLandscapeView);
        }

        private static void EnsureOption6Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                3,
                MainMenuLayout.ButtonNames[3],
                controller.ShowSharedGenomeMdsPathView);
        }

        private static void EnsureOption8Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                4,
                MainMenuLayout.ButtonNames[4],
                controller.ShowBatchGenomeMdsPathView);
        }


        private static void EnsureOption11Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                5,
                MainMenuLayout.ButtonNames[5],
                controller.ShowOption10DualPaneMdsAttractorView);
        }

        private static void EnsureOption12Button(Transform mainMenuPanel, AppMenuController controller)
        {
            EnsureMenuOptionButton(
                mainMenuPanel,
                controller,
                6,
                MainMenuLayout.ButtonNames[6],
                controller.ShowBatchGenomeMdsFitnessPathView);
        }




        private static GameObject BuildOption10DualPaneUiPanel(Transform canvasTransform, AppMenuController controller)
        {
            Transform existing = canvasTransform.Find("Option10DualPaneUiPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject panel = CreateUiPanel(
                "Option10DualPaneUiPanel",
                canvasTransform,
                new Color(0f, 0f, 0f, 0f),
                receiveRaycasts: false);
            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildGenomeEmbeddingPathUiPanel(Transform canvasTransform, AppMenuController controller)
        {
            Transform existing = canvasTransform.Find("GenomeEmbeddingPathUiPanel");
            if (existing == null)
            {
                existing = canvasTransform.Find("GenomeMdsPathUiPanel");
            }

            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform legacyPanel = canvasTransform.Find("FitnessLayerUiPanel");
            if (legacyPanel != null)
            {
                Object.DestroyImmediate(legacyPanel.gameObject);
            }

            GameObject panel = CreateUiPanel(
                "GenomeEmbeddingPathUiPanel",
                canvasTransform,
                new Color(0f, 0f, 0f, 0f),
                receiveRaycasts: false);
            panel.SetActive(false);

            Text viewTitle = CreateUiText(
                "ViewTitle",
                panel.transform,
                "Evolutionary genome path (MDS embedding of accepted genomes)",
                24,
                FontStyle.Bold,
                TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -36f),
                new Vector2(1200f, 56f));
            RectTransform titleRect = viewTitle.GetComponent<RectTransform>();
            titleRect.pivot = new Vector2(0.5f, 1f);

            CreateUiText(
                "ViewSubtitle",
                panel.transform,
                "run_001",
                18,
                FontStyle.Normal,
                TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -96f),
                new Vector2(1200f, 28f),
                new Color(0.82f, 0.84f, 0.88f, 1f));

            Button backButton = CreateUiButton(
                "BackButton",
                panel.transform,
                "Back to Menu",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(120f, -36f),
                new Vector2(200f, 52f));

            UnityEventTools.AddPersistentListener(backButton.onClick, controller.ShowMainMenu);

            CreateUiText(
                "ZoomLabel",
                panel.transform,
                "Zoom",
                18,
                FontStyle.Normal,
                TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-190f, 34f),
                new Vector2(80f, 28f),
                new Color(0.82f, 0.84f, 0.88f, 1f));

            Slider zoomSlider = CreateZoomSlider(panel.transform);
            TrajectoryZoomSlider zoomControl = zoomSlider.gameObject.AddComponent<TrajectoryZoomSlider>();

            SerializedObject serializedZoom = new SerializedObject(zoomControl);
            serializedZoom.FindProperty("slider").objectReferenceValue = zoomSlider;
            serializedZoom.FindProperty("sceneCamera").objectReferenceValue =
                Object.FindFirstObjectByType<TrajectorySceneCamera>();
            serializedZoom.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }

        private static Slider CreateZoomSlider(Transform parent)
        {
            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = "ZoomSlider";
            sliderObject.transform.SetParent(parent, false);

            RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(20f, 24f);
            rectTransform.sizeDelta = new Vector2(320f, 24f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            return slider;
        }

        private static void WireController(
            AppMenuController controller,
            GameObject mainMenuPanel,
            GameObject genomeMdsPathRoot,
            GameObject genomeMdsFitnessPathRoot,
            GameObject booleanAttractorLandscapeRoot,
            GameObject sharedGenomeMdsPathRoot,
            GameObject batchGenomeMdsPathRoot,
            GameObject batchGenomeMdsFitnessPathRoot,
            Option10DualPaneMdsAttractorViewController option10DualPaneViewController,
            GameObject option10DualPaneUiPanel,
            GameObject genomeEmbeddingPathUiPanel,
            GenomeMdsPathVisualiser genomeMdsPathVisualiser,
            GenomeMdsFitnessPathVisualiser genomeMdsFitnessPathVisualiser,
            BooleanAttractorLandscapeVisualiser booleanAttractorLandscapeVisualiser,
            SharedGenomeMdsPathVisualiser sharedGenomeMdsPathVisualiser,
            BatchGenomeMdsPathVisualiser batchGenomeMdsPathVisualiser,
            BatchGenomeMdsFitnessPathVisualiser batchGenomeMdsFitnessPathVisualiser,
            Camera mainCamera,
            TrajectorySceneCamera trajectorySceneCamera)
        {
            genomeMdsPathRoot.SetActive(false);
            genomeMdsFitnessPathRoot.SetActive(false);
            booleanAttractorLandscapeRoot.SetActive(false);
            sharedGenomeMdsPathRoot.SetActive(false);
            batchGenomeMdsPathRoot.SetActive(false);
            batchGenomeMdsFitnessPathRoot.SetActive(false);
            if (option10DualPaneViewController != null)
            {
                option10DualPaneViewController.SetViewActive(false, mainCamera);
            }

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            serializedController.FindProperty("genomeMdsPathViewRoot").objectReferenceValue = genomeMdsPathRoot;
            serializedController.FindProperty("genomeMdsFitnessPathViewRoot").objectReferenceValue =
                genomeMdsFitnessPathRoot;
            serializedController.FindProperty("booleanAttractorLandscapeViewRoot").objectReferenceValue =
                booleanAttractorLandscapeRoot;
            serializedController.FindProperty("sharedGenomeMdsPathViewRoot").objectReferenceValue =
                sharedGenomeMdsPathRoot;
            serializedController.FindProperty("batchGenomeMdsPathViewRoot").objectReferenceValue =
                batchGenomeMdsPathRoot;
            serializedController.FindProperty("batchGenomeMdsFitnessPathViewRoot").objectReferenceValue =
                batchGenomeMdsFitnessPathRoot;
            serializedController.FindProperty("option10DualPaneMdsAttractorViewRoot").objectReferenceValue =
                option10DualPaneViewController != null ? option10DualPaneViewController.gameObject : null;
            serializedController.FindProperty("option10DualPaneMdsAttractorViewController").objectReferenceValue =
                option10DualPaneViewController;
            serializedController.FindProperty("option10DualPaneUiPanel").objectReferenceValue = option10DualPaneUiPanel;
            serializedController.FindProperty("genomeEmbeddingPathUiPanel").objectReferenceValue =
                genomeEmbeddingPathUiPanel;
            serializedController.FindProperty("genomeMdsPathVisualiser").objectReferenceValue = genomeMdsPathVisualiser;
            serializedController.FindProperty("genomeMdsFitnessPathVisualiser").objectReferenceValue =
                genomeMdsFitnessPathVisualiser;
            serializedController.FindProperty("booleanAttractorLandscapeVisualiser").objectReferenceValue =
                booleanAttractorLandscapeVisualiser;
            serializedController.FindProperty("sharedGenomeMdsPathVisualiser").objectReferenceValue =
                sharedGenomeMdsPathVisualiser;
            serializedController.FindProperty("batchGenomeMdsPathVisualiser").objectReferenceValue =
                batchGenomeMdsPathVisualiser;
            serializedController.FindProperty("batchGenomeMdsFitnessPathVisualiser").objectReferenceValue =
                batchGenomeMdsFitnessPathVisualiser;
            serializedController.FindProperty("mainCamera").objectReferenceValue = mainCamera;
            serializedController.FindProperty("trajectorySceneCamera").objectReferenceValue = trajectorySceneCamera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(controller);
        }

        private static GameObject CreateUiPanel(
            string name,
            Transform parent,
            Color color,
            bool receiveRaycasts = true)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = receiveRaycasts;
            return panel;
        }

        private static Text CreateUiText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color? color = null)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color ?? Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateUiButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.fontSize = 24;
            labelText.color = Color.white;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.16f, 0.34f, 0.62f, 1f);
            return buttonObject.GetComponent<Button>();
        }
    }
}

#endif
