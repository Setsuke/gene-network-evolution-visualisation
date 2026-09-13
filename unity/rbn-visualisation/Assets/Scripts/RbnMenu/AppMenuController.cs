using UnityEngine;
using UnityEngine.UI;
using RbnVisualisation.AttractorLandscape;

namespace RbnVisualisation.Menu
{
    public class AppMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject genomeMdsPathViewRoot;
        [SerializeField] private GameObject genomeMdsFitnessPathViewRoot;
        [SerializeField] private GameObject booleanAttractorLandscapeViewRoot;
        [SerializeField] private GameObject sharedGenomeMdsPathViewRoot;
        [SerializeField] private GameObject batchGenomeMdsPathViewRoot;
        [SerializeField] private GameObject batchGenomeMdsFitnessPathViewRoot;
        [SerializeField] private GameObject option10DualPaneMdsAttractorViewRoot;
        [SerializeField] private GameObject option10DualPaneUiPanel;
        [SerializeField] private Option10DualPaneMdsAttractorViewController option10DualPaneMdsAttractorViewController;
        [SerializeField] private GameObject genomeEmbeddingPathUiPanel;
        [SerializeField] private GenomePath.GenomeMdsPathVisualiser genomeMdsPathVisualiser;
        [SerializeField] private GenomePath.GenomeMdsFitnessPathVisualiser genomeMdsFitnessPathVisualiser;
        [SerializeField] private GenomePath.SharedGenomeMdsPathVisualiser sharedGenomeMdsPathVisualiser;
        [SerializeField] private GenomePath.BatchGenomeMdsPathVisualiser batchGenomeMdsPathVisualiser;
        [SerializeField] private GenomePath.BatchGenomeMdsFitnessPathVisualiser batchGenomeMdsFitnessPathVisualiser;
        [SerializeField] private BooleanAttractorLandscapeVisualiser booleanAttractorLandscapeVisualiser;
        [SerializeField] private Camera mainCamera;
        [SerializeField]         private Trajectory.TrajectorySceneCamera trajectorySceneCamera;
        private GenomePath.GenomeMdsFitnessPathUiHandles genomeMdsFitnessPathUi;
        private GenomePath.BatchGenomeMdsFitnessPathUiHandles batchGenomeMdsFitnessPathUi;
        private BooleanAttractorLandscapeUiHandles booleanAttractorLandscapeUi;
        private Option10DualPaneViewUiHandles option10DualPaneViewUi;

        private void Start()
        {
            if (trajectorySceneCamera == null && mainCamera != null)
            {
                trajectorySceneCamera = mainCamera.GetComponent<Trajectory.TrajectorySceneCamera>();
            }

            ShowMainMenu();
            mainMenuPanel = MainMenuVisibility.ResolveMainMenuPanel(mainMenuPanel);
            RemoveLegacyMenuText();
            RemoveLegacyOption1Button();
            RemoveRetiredMenuButtons();
            DeactivateLegacyTrajectoryView();
            MainMenuLayout.Apply(mainMenuPanel != null ? mainMenuPanel.transform : null);
        }

        private static void RemoveLegacyMenuText()
        {
            DestroyIfExists("Subtitle");
            DestroyIfExists("Hint");
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null)
            {
                Destroy(target);
            }
        }

        private void RemoveLegacyOption1Button()
        {
            if (mainMenuPanel == null)
            {
                return;
            }

            Transform option1Button = mainMenuPanel.transform.Find("Option1Button");
            if (option1Button != null)
            {
                Destroy(option1Button.gameObject);
            }
        }


        private void RemoveRetiredMenuButtons()
        {
            if (mainMenuPanel == null)
            {
                return;
            }

            foreach (string buttonName in new[] { "Option10Button", "Option13Button", "Option14Button" })
            {
                Transform button = mainMenuPanel.transform.Find(buttonName);
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
        }

        private static void DeactivateLegacyTrajectoryView()
        {
            GameObject trajectoryRoot = GameObject.Find("TrajectoryRoot");
            if (trajectoryRoot != null)
            {
                trajectoryRoot.SetActive(false);
            }

            GameObject trajectoryUiPanel = GameObject.Find("TrajectoryUiPanel");
            if (trajectoryUiPanel != null)
            {
                trajectoryUiPanel.SetActive(false);
            }
        }

        public void ShowMainMenu()
        {
            if (genomeEmbeddingPathUiPanel != null)
            {
                Trajectory.TrajectoryViewUiFactory.SetViewModeScreenBackdrop(
                    genomeEmbeddingPathUiPanel.transform,
                    visible: false);
            }

            if (option10DualPaneUiPanel != null)
            {
                Trajectory.TrajectoryViewUiFactory.SetViewModeScreenBackdrop(
                    option10DualPaneUiPanel.transform,
                    visible: false);
            }

            MainMenuVisibility.ShowMainMenu(mainMenuPanel);
            mainMenuPanel = MainMenuVisibility.ResolveMainMenuPanel(mainMenuPanel);

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, false);
            HideOption10DualPaneViewUi();
            SetViewActive(genomeEmbeddingPathUiPanel, false);
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();

            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }

            if (trajectorySceneCamera != null)
            {
                trajectorySceneCamera.enabled = false;
            }

        }

        public void ShowGenomeMdsPathView()
        {
            ShowGenomeEmbeddingPathView(
                genomeMdsPathViewRoot,
                genomeMdsPathVisualiser,
                "Evolutionary genome path (MDS embedding of accepted genomes)",
                limitVerticalOrbit: true);
        }

        public void ShowOption10DualPaneMdsAttractorView()
        {
            EnsureMainMenuHidden();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, false);
            SetViewActive(genomeEmbeddingPathUiPanel, false);
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();

            Option10DualPaneMdsAttractorViewController option10Controller = ResolveOption10DualPaneViewController();
            if (option10Controller != null)
            {
                option10Controller.ConfigureCameras(mainCamera);
                option10Controller.SetViewActive(true, mainCamera);
                option10Controller.RebuildVisualisations();
            }

            if (option10DualPaneUiPanel != null)
            {
                option10DualPaneUiPanel.SetActive(true);
                Trajectory.TrajectoryViewUiFactory.SetViewModeScreenBackdrop(
                    option10DualPaneUiPanel.transform,
                    visible: true);
                option10DualPaneUiPanel.transform.SetAsLastSibling();
                ConfigureOption10DualPaneViewUi();
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(option10DualPaneUiPanel);
                DualPaneViewUiFactory.BringShelfTabBarsToFront(option10DualPaneUiPanel.transform);
                Transform option10Controls = option10DualPaneUiPanel.transform.Find("DualPaneControls");
                if (option10Controls != null)
                {
                    Trajectory.TrajectoryViewUiFactory.BringInteractiveControlsToFront(
                        option10Controls.gameObject);
                }
            }

            if (trajectorySceneCamera != null)
            {
                trajectorySceneCamera.enabled = false;
            }

            Trajectory.TrajectoryLightingSetup.Apply();
        }

        public void ShowBooleanAttractorLandscapeView()
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, true);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, false);
            HideOption10DualPaneViewUi();
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();

            const string title =
                "Fixed Boolean state attractor landscape (Hamming MDS, 32 nodes)";

            if (genomeEmbeddingPathUiPanel != null)
            {
                Transform layeredControls = genomeEmbeddingPathUiPanel.transform.Find(
                    "LayeredBooleanAttractorLandscapeControls");
                if (layeredControls != null)
                {
                    layeredControls.gameObject.SetActive(false);
                }

                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                HideBooleanAttractorLandscapeLegends();
                BooleanAttractorLandscapeLegend.EnsureOnPanel(genomeEmbeddingPathUiPanel.transform);
                GenomePath.GenomeMdsPathLegend.SetVisible(genomeEmbeddingPathUiPanel.transform, false);
                GenomePath.SharedGenomeMdsPathLegend.SetVisible(genomeEmbeddingPathUiPanel.transform, false);
                BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    true);
                BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    false);
            }

            if (booleanAttractorLandscapeVisualiser != null)
            {
                booleanAttractorLandscapeVisualiser.RebuildVisualisation();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    SetViewSubtitle(
                        genomeEmbeddingPathUiPanel.transform,
                        booleanAttractorLandscapeVisualiser.RunLabelText);
                }

                ConfigureBooleanAttractorLandscapeSceneCamera();
                ConfigureBooleanAttractorLandscapeUi();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                    BooleanAttractorLandscapeUiFactory.RefreshInteraction(booleanAttractorLandscapeUi);
                }
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
                HideBooleanAttractorLandscapeUi();
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        private void EnsureMainMenuHidden()
        {
            mainMenuPanel = MainMenuVisibility.HideMainMenu(mainMenuPanel);
        }

        private void ActivateEmbeddingPathUiPanel(Rect? figureViewport = null)
        {
            EnsureMainMenuHidden();

            if (genomeEmbeddingPathUiPanel == null)
            {
                return;
            }

            genomeEmbeddingPathUiPanel.SetActive(true);
            Rect viewport = figureViewport ?? new Rect(0f, 0f, 1f, 1f);
            Trajectory.TrajectoryViewUiFactory.SetViewModeScreenBackdrop(
                genomeEmbeddingPathUiPanel.transform,
                visible: true,
                viewport);
            genomeEmbeddingPathUiPanel.transform.SetAsLastSibling();
        }

        private static void SetUiChildActive(Transform parent, string childName, bool active)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private void ResetMainCameraViewport()
        {
            DualPaneCameraUtility.DisableAllDualPaneCameras();
            if (mainCamera != null)
            {
                DualPaneCameraUtility.PrepareMainCameraForSinglePaneView(mainCamera);
                mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
            }

            trajectorySceneCamera?.SetOrbitViewportOverride(null);
            trajectorySceneCamera?.SetRestrictOrbitToViewport(false);
        }

        private void ConfigureBooleanAttractorLandscapeSceneCamera()
        {
            if (trajectorySceneCamera == null || booleanAttractorLandscapeVisualiser == null)
            {
                return;
            }

            DualPaneCameraUtility.DisableAllDualPaneCameras();
            DualPaneCameraUtility.PrepareMainCameraForSinglePaneView(mainCamera);

            trajectorySceneCamera.enabled = true;
            trajectorySceneCamera.SetVerticalOrbitLimited(false);
            trajectorySceneCamera.SetFocus(booleanAttractorLandscapeVisualiser.OrbitFocus);
            trajectorySceneCamera.SetLookAtHeight(0f);
            trajectorySceneCamera.SetDistanceLimits(
                booleanAttractorLandscapeVisualiser.MinOrbitDistance,
                Mathf.Max(booleanAttractorLandscapeVisualiser.ContentExtent * 1.6f, 14f));
            trajectorySceneCamera.SetDistance(booleanAttractorLandscapeVisualiser.RecommendedFocusDistance);
            trajectorySceneCamera.PlaceCameraFixed();
        }

        private void ConfigureBooleanAttractorLandscapeUi()
        {
            if (genomeEmbeddingPathUiPanel == null || booleanAttractorLandscapeVisualiser == null)
            {
                return;
            }

            Trajectory.TrajectoryZoomSlider zoomSlider =
                genomeEmbeddingPathUiPanel.GetComponentInChildren<Trajectory.TrajectoryZoomSlider>(true);

            booleanAttractorLandscapeUi = BooleanAttractorLandscapeUiFactory.EnsureOnPanel(
                genomeEmbeddingPathUiPanel.transform,
                booleanAttractorLandscapeVisualiser,
                trajectorySceneCamera,
                zoomSlider);
            BooleanAttractorLandscapeUiFactory.SetVisible(booleanAttractorLandscapeUi, true);
            booleanAttractorLandscapeUi.Explorer?.GoToFirstStep();
            booleanAttractorLandscapeUi.Explorer?.SyncContextPathDisplay();
        }

        private void HideBooleanAttractorLandscapeUi()
        {
            BooleanAttractorLandscapeUiFactory.SetVisible(booleanAttractorLandscapeUi, false);
            booleanAttractorLandscapeUi?.Explorer?.ResetExplorer();
            HideBooleanAttractorLandscapeLegends();
            trajectorySceneCamera?.ResetDistanceLimits();
        }

        private void HideBooleanAttractorLandscapeLegends()
        {
            if (genomeEmbeddingPathUiPanel == null)
            {
                return;
            }

            Transform panelRoot = genomeEmbeddingPathUiPanel.transform;
            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
        }

        private static void ShowGenomeMdsPathLegends(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetVisible(panelRoot, false);
            GenomePath.GenomeMdsPathLegend.EnsureOnPanel(panelRoot.gameObject);
            GenomePath.GenomeMdsPathLegend.SetVisible(panelRoot, true);
        }

        private static void ShowSharedGenomeMdsPathLegends(Transform panelRoot, string legendTitle)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
            GenomePath.GenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchDriftCategoriesVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchDriftingGenomesVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.EnsureOnPanel(panelRoot.gameObject, legendTitle);
            GenomePath.SharedGenomeMdsPathLegend.SetVisible(panelRoot, true);
            GenomePath.SharedGenomeMdsPathLegend.SetDriftCategoriesVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetDriftingGenomesVisible(panelRoot, false);
        }

        private void HideSharedGenomeMdsPathViewControls()
        {
            if (genomeEmbeddingPathUiPanel == null)
            {
                return;
            }

            GenomePath.SharedGenomeMdsPathViewControls.SetVisible(
                genomeEmbeddingPathUiPanel.transform,
                false);
        }

        private void HideBatchGenomeMdsPathViewControls()
        {
            if (genomeEmbeddingPathUiPanel == null)
            {
                return;
            }

            GenomePath.BatchGenomeMdsPathViewControls.SetVisible(
                genomeEmbeddingPathUiPanel.transform,
                false);
        }

        public void ShowSharedGenomeMdsPathView()
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, true);
            SetBatchGenomeViewRootsActive(false, false);
            HideOption10DualPaneViewUi();
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();
            HideBatchGenomeMdsPathViewControls();

            const string title = "Shared initial genome MDS";

            if (genomeEmbeddingPathUiPanel != null)
            {
                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                ShowSharedGenomeMdsPathLegends(
                    genomeEmbeddingPathUiPanel.transform,
                    "Shared genome MDS");
                GenomePath.SharedGenomeMdsPathViewControls.SetVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    true);
            }

            if (sharedGenomeMdsPathVisualiser != null)
            {
                sharedGenomeMdsPathVisualiser.RebuildVisualisation();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    GenomePath.SharedGenomeMdsPathViewControls.EnsureOnPanel(
                        genomeEmbeddingPathUiPanel,
                        sharedGenomeMdsPathVisualiser,
                        trajectorySceneCamera);
                    SetViewSubtitle(
                        genomeEmbeddingPathUiPanel.transform,
                        sharedGenomeMdsPathVisualiser.RunLabelText);
                }

                trajectorySceneCamera?.ResetDistanceLimits();
                ConfigureEmbeddingSceneCamera(
                    sharedGenomeMdsPathVisualiser.OrbitFocus,
                    sharedGenomeMdsPathVisualiser.ContentExtent,
                    limitVerticalOrbit: true);
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        private static void ShowBatchGenomeMdsPathLegends(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
            GenomePath.GenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetDriftCategoriesVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchVisible(panelRoot, true);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchDriftCategoriesVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchDriftingGenomesVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.EnsureBatchOnPanel(
                panelRoot.gameObject,
                "Batch genome MDS");
        }

        private static void ShowBatchGenomeMdsFitnessPathLegends(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
            GenomePath.GenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetVisible(panelRoot, false);
            GenomePath.SharedGenomeMdsPathLegend.SetBatchVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.EnsureOnPanel(panelRoot.gameObject);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetVisible(panelRoot, true);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetMoveClassificationVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetDriftMovesVisible(panelRoot, false);
            GenomePath.BatchGenomeMdsFitnessPathLegend.SetDriftMovesByCategoryVisible(panelRoot, false);
        }

        public void ShowBatchGenomeMdsPathView()
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(true, false);
            HideOption10DualPaneViewUi();
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();
            HideSharedGenomeMdsPathViewControls();

            const string title =
                "Distinct initial genomes MDS (all run paths, initial + solution)";

            if (genomeEmbeddingPathUiPanel != null)
            {
                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                ShowBatchGenomeMdsPathLegends(genomeEmbeddingPathUiPanel.transform);
                GenomePath.BatchGenomeMdsPathViewControls.SetVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    true);
            }

            if (batchGenomeMdsPathVisualiser != null)
            {
                batchGenomeMdsPathVisualiser.RebuildVisualisation();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    GenomePath.BatchGenomeMdsPathViewControls.EnsureOnPanel(
                        genomeEmbeddingPathUiPanel,
                        batchGenomeMdsPathVisualiser);
                    SetViewSubtitle(
                        genomeEmbeddingPathUiPanel.transform,
                        batchGenomeMdsPathVisualiser.RunLabelText);
                }

                trajectorySceneCamera?.ResetDistanceLimits();
                ConfigureEmbeddingSceneCamera(
                    batchGenomeMdsPathVisualiser.OrbitFocus,
                    batchGenomeMdsPathVisualiser.ContentExtent,
                    limitVerticalOrbit: true);
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        public void ShowBatchGenomeMdsFitnessPathView()
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, true);
            HideOption10DualPaneViewUi();
            HideGenomeMdsFitnessPathUi();
            HideBatchGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();

            const string title =
                "First 200 runs: X/Z = MDS, Y = total fitness (all paths together)";

            if (genomeEmbeddingPathUiPanel != null)
            {
                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                ShowBatchGenomeMdsFitnessPathLegends(genomeEmbeddingPathUiPanel.transform);
            }

            if (batchGenomeMdsFitnessPathVisualiser != null)
            {
                batchGenomeMdsFitnessPathVisualiser.RebuildVisualisation();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    SetViewSubtitle(
                        genomeEmbeddingPathUiPanel.transform,
                        batchGenomeMdsFitnessPathVisualiser.RunLabelText);
                }

                trajectorySceneCamera?.ResetDistanceLimits();
                ConfigureEmbeddingSceneCamera(
                    batchGenomeMdsFitnessPathVisualiser.OrbitFocus,
                    batchGenomeMdsFitnessPathVisualiser.ContentExtent,
                    limitVerticalOrbit: false);
                ConfigureBatchGenomeMdsFitnessPathUi();
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        private void ConfigureOption10DualPaneViewUi()
        {
            Option10DualPaneMdsAttractorViewController option10Controller = ResolveOption10DualPaneViewController();
            if (option10DualPaneUiPanel == null || option10Controller == null)
            {
                return;
            }

            option10DualPaneViewUi = Option10DualPaneViewUiFactory.EnsureOnPanel(
                option10DualPaneUiPanel.transform,
                option10Controller,
                this);
            Option10DualPaneViewUiFactory.SetVisible(option10DualPaneViewUi, true);
            option10DualPaneViewUi?.Explorer?.GoToFirstEpisode();
            option10DualPaneViewUi?.Explorer?.SyncAttractorReadout();
            option10DualPaneViewUi?.LeftZoomSlider?.SyncFromCamera();
            option10DualPaneViewUi?.RightZoomSlider?.SyncFromCamera();
        }

        private void HideOption10DualPaneViewUi()
        {
            Option10DualPaneViewUiFactory.SetVisible(option10DualPaneViewUi, false);
            option10DualPaneViewUi = null;
            SetViewActive(option10DualPaneUiPanel, false);

            if (option10DualPaneMdsAttractorViewController != null)
            {
                option10DualPaneMdsAttractorViewController.SetViewActive(false, mainCamera);
            }
        }

        private Option10DualPaneMdsAttractorViewController ResolveOption10DualPaneViewController()
        {
            if (option10DualPaneMdsAttractorViewController != null)
            {
                return option10DualPaneMdsAttractorViewController;
            }

            if (option10DualPaneMdsAttractorViewRoot != null)
            {
                option10DualPaneMdsAttractorViewController =
                    option10DualPaneMdsAttractorViewRoot.GetComponent<Option10DualPaneMdsAttractorViewController>();
                if (option10DualPaneMdsAttractorViewController != null)
                {
                    return option10DualPaneMdsAttractorViewController;
                }
            }

            Option10DualPaneMdsAttractorViewController[] controllers =
                FindObjectsByType<Option10DualPaneMdsAttractorViewController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (controllers.Length > 0)
            {
                option10DualPaneMdsAttractorViewController = controllers[0];
                option10DualPaneMdsAttractorViewRoot = option10DualPaneMdsAttractorViewController.gameObject;
                return option10DualPaneMdsAttractorViewController;
            }

            GameObject root = new GameObject("Option10DualPaneMdsAttractorViewRoot");
            option10DualPaneMdsAttractorViewController =
                root.AddComponent<Option10DualPaneMdsAttractorViewController>();
            option10DualPaneMdsAttractorViewRoot = root;
            return option10DualPaneMdsAttractorViewController;
        }

        public void ShowGenomeMdsFitnessPathView()
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, false);
            SetViewActive(genomeMdsFitnessPathViewRoot, true);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, false);
            HideOption10DualPaneViewUi();
            HideBooleanAttractorLandscapeUi();

            const string title =
                "Accepted genome path (X/Z = MDS components 1/2, Y = total fitness)";

            if (genomeEmbeddingPathUiPanel != null)
            {
                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                ShowGenomeMdsPathLegends(genomeEmbeddingPathUiPanel.transform);
            }

            if (genomeMdsFitnessPathVisualiser != null)
            {
                genomeMdsFitnessPathVisualiser.RebuildVisualisation();
                ConfigureEmbeddingSceneCamera(genomeMdsFitnessPathVisualiser, limitVerticalOrbit: false);
                ConfigureGenomeMdsFitnessPathUi();
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
                GenomePath.GenomeMdsFitnessPathUiFactory.SetVisible(genomeMdsFitnessPathUi, false);
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        private void ConfigureGenomeMdsFitnessPathUi()
        {
            if (genomeEmbeddingPathUiPanel == null || genomeMdsFitnessPathVisualiser == null)
            {
                return;
            }

            Trajectory.TrajectoryZoomSlider zoomSlider =
                genomeEmbeddingPathUiPanel.GetComponentInChildren<Trajectory.TrajectoryZoomSlider>(true);

            if (trajectorySceneCamera != null)
            {
                float extent = genomeMdsFitnessPathVisualiser.ContentExtent;
                trajectorySceneCamera.SetDistanceLimits(
                    genomeMdsFitnessPathVisualiser.MinOrbitDistance,
                    Mathf.Max(extent * 1.6f, 14f));
            }

            genomeMdsFitnessPathUi = GenomePath.GenomeMdsFitnessPathUiFactory.EnsureOnPanel(
                genomeEmbeddingPathUiPanel.transform,
                genomeMdsFitnessPathVisualiser,
                trajectorySceneCamera,
                zoomSlider,
                subtitle =>
                {
                    if (genomeEmbeddingPathUiPanel != null)
                    {
                        SetViewSubtitle(genomeEmbeddingPathUiPanel.transform, subtitle);
                    }
                });
            GenomePath.GenomeMdsFitnessPathUiFactory.SetVisible(genomeMdsFitnessPathUi, true);
            Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);

            if (genomeMdsFitnessPathUi?.Explorer != null)
            {
                genomeMdsFitnessPathUi.Explorer.SetRecommendedFocusDistance(
                    genomeMdsFitnessPathVisualiser.RecommendedFocusDistance);
                genomeMdsFitnessPathUi.Explorer.SyncRunNavigationDisplay();
            }
        }

        private void HideGenomeMdsFitnessPathUi()
        {
            GenomePath.GenomeMdsFitnessPathUiFactory.SetVisible(genomeMdsFitnessPathUi, false);
            HideBatchGenomeMdsFitnessPathUi();
            HideSharedGenomeMdsPathViewControls();
            HideBatchGenomeMdsPathViewControls();

            if (genomeEmbeddingPathUiPanel != null)
            {
                Transform infoPanel = genomeEmbeddingPathUiPanel.transform.Find("PathInfoPanel");
                if (infoPanel != null)
                {
                    infoPanel.gameObject.SetActive(false);
                }
            }

            trajectorySceneCamera?.ResetDistanceLimits();
        }

        private void ConfigureBatchGenomeMdsFitnessPathUi()
        {
            if (genomeEmbeddingPathUiPanel == null || batchGenomeMdsFitnessPathVisualiser == null)
            {
                GenomePath.BatchGenomeMdsFitnessPathUiFactory.SetVisible(batchGenomeMdsFitnessPathUi, false);
                return;
            }

            batchGenomeMdsFitnessPathUi = GenomePath.BatchGenomeMdsFitnessPathUiFactory.EnsureOnPanel(
                genomeEmbeddingPathUiPanel.transform,
                batchGenomeMdsFitnessPathVisualiser);
            GenomePath.BatchGenomeMdsFitnessPathUiFactory.SetVisible(batchGenomeMdsFitnessPathUi, true);
            Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
        }

        private void HideBatchGenomeMdsFitnessPathUi()
        {
            GenomePath.BatchGenomeMdsFitnessPathUiFactory.SetVisible(batchGenomeMdsFitnessPathUi, false);

            if (genomeEmbeddingPathUiPanel != null)
            {
                GenomePath.BatchGenomeMdsFitnessPathLegend.SetMoveClassificationVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    false);
                GenomePath.BatchGenomeMdsFitnessPathLegend.SetDriftMovesVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    false);
                GenomePath.BatchGenomeMdsFitnessPathLegend.SetDriftMovesByCategoryVisible(
                    genomeEmbeddingPathUiPanel.transform,
                    false);
            }
        }

        private void ShowGenomeEmbeddingPathView(
            GameObject viewRoot,
            GenomePath.GenomeMdsPathVisualiser visualiser,
            string title,
            bool limitVerticalOrbit)
        {
            ActivateEmbeddingPathUiPanel();

            SetViewActive(genomeMdsPathViewRoot, viewRoot == genomeMdsPathViewRoot);
            SetViewActive(genomeMdsFitnessPathViewRoot, false);
            SetViewActive(booleanAttractorLandscapeViewRoot, false);
            SetViewActive(sharedGenomeMdsPathViewRoot, false);
            SetBatchGenomeViewRootsActive(false, false);
            HideOption10DualPaneViewUi();
            HideGenomeMdsFitnessPathUi();
            HideBooleanAttractorLandscapeUi();

            if (genomeEmbeddingPathUiPanel != null)
            {
                SetViewTitle(genomeEmbeddingPathUiPanel.transform, title);
                Trajectory.TrajectoryViewUiFactory.DisableRootRaycasts(genomeEmbeddingPathUiPanel);
                ShowGenomeMdsPathLegends(genomeEmbeddingPathUiPanel.transform);
            }

            if (visualiser != null)
            {
                visualiser.RebuildVisualisation();
                if (genomeEmbeddingPathUiPanel != null)
                {
                    SetViewSubtitle(genomeEmbeddingPathUiPanel.transform, visualiser.RunLabelText);
                }

                trajectorySceneCamera?.ResetDistanceLimits();
                ConfigureEmbeddingSceneCamera(visualiser, limitVerticalOrbit);
            }
            else
            {
                ConfigureSceneCamera(null, 0f);
            }

            Trajectory.TrajectoryLightingSetup.Apply();
            SyncZoomSlider(genomeEmbeddingPathUiPanel);
        }

        private void ConfigureEmbeddingSceneCamera(
            GenomePath.GenomeMdsPathVisualiser visualiser,
            bool limitVerticalOrbit)
        {
            ConfigureEmbeddingSceneCamera(visualiser.OrbitFocus, visualiser.ContentExtent, limitVerticalOrbit);
        }

        private void ConfigureEmbeddingSceneCamera(
            GenomePath.GenomeMdsFitnessPathVisualiser visualiser,
            bool limitVerticalOrbit)
        {
            ConfigureEmbeddingSceneCamera(visualiser.OrbitFocus, visualiser.ContentExtent, limitVerticalOrbit);
        }

        private void ConfigureEmbeddingSceneCamera(
            Transform focus,
            float contentExtent,
            bool limitVerticalOrbit)
        {
            if (trajectorySceneCamera == null)
            {
                return;
            }

            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }

            trajectorySceneCamera.enabled = true;
            trajectorySceneCamera.SetVerticalOrbitLimited(limitVerticalOrbit);
            trajectorySceneCamera.SetFocus(focus);
            trajectorySceneCamera.SetLookAtHeight(0f);
            trajectorySceneCamera.SetPitchDegrees(22f);
            trajectorySceneCamera.SetDistance(Mathf.Max(contentExtent * 1.45f, 14f));
            trajectorySceneCamera.PlaceCameraFixed();
        }

        private void ConfigureSceneCamera(Transform focus, float lookAtHeight)
        {
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
            }

            if (trajectorySceneCamera == null)
            {
                return;
            }

            trajectorySceneCamera.enabled = true;
            if (focus != null)
            {
                trajectorySceneCamera.SetFocus(focus);
            }

            trajectorySceneCamera.SetLookAtHeight(lookAtHeight);
            trajectorySceneCamera.PlaceCameraFixed();
        }

        private void SetBatchGenomeViewRootsActive(
            bool mdsPathActive,
            bool mdsFitnessPathActive)
        {
            SetViewActive(batchGenomeMdsPathViewRoot, mdsPathActive);
            SetViewActive(batchGenomeMdsFitnessPathViewRoot, mdsFitnessPathActive);
        }

        private static void SetViewActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SyncZoomSlider(GameObject activePanel)
        {
            if (activePanel == null)
            {
                return;
            }

            Trajectory.TrajectoryZoomSlider zoomSlider =
                activePanel.GetComponentInChildren<Trajectory.TrajectoryZoomSlider>(true);
            if (zoomSlider != null)
            {
                zoomSlider.SyncFromCamera();
            }
        }

        private static void SetViewTitle(Transform panelRoot, string titleText)
        {
            Transform titleTransform = panelRoot.Find("ViewTitle");
            if (titleTransform == null || !titleTransform.TryGetComponent(out Text title))
            {
                return;
            }

            title.text = titleText;
            title.alignment = TextAnchor.UpperCenter;
            title.fontSize = 24;

            RectTransform rectTransform = title.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -36f);
            rectTransform.sizeDelta = new Vector2(1200f, 56f);
        }

        private static void SetViewSubtitle(Transform panelRoot, string subtitleText)
        {
            Transform subtitleTransform = panelRoot.Find("ViewSubtitle");
            if (subtitleTransform == null)
            {
                GameObject subtitleObject = new GameObject(
                    "ViewSubtitle",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                subtitleObject.transform.SetParent(panelRoot, false);

                Text subtitle = subtitleObject.GetComponent<Text>();
                subtitle.alignment = TextAnchor.UpperCenter;
                subtitle.fontSize = 18;
                subtitle.fontStyle = FontStyle.Normal;
                subtitle.color = new Color(0.82f, 0.84f, 0.88f, 1f);
                subtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                subtitle.raycastTarget = false;

                RectTransform rectTransform = subtitleObject.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 1f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(0f, -96f);
                rectTransform.sizeDelta = new Vector2(1200f, 28f);

                subtitleTransform = subtitleObject.transform;
            }

            if (!subtitleTransform.TryGetComponent(out Text subtitleTextComponent))
            {
                return;
            }

            subtitleTextComponent.text = subtitleText ?? string.Empty;
            subtitleTextComponent.gameObject.SetActive(!string.IsNullOrWhiteSpace(subtitleText));
        }
    }
}
