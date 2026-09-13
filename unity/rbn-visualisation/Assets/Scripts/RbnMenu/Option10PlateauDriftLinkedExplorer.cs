using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Option 10: navigates plateau episodes and steps within each segment; right pane follows the same step range.
    /// </summary>
    public class Option10PlateauDriftLinkedExplorer : MonoBehaviour
    {
        [SerializeField] private PlateauDriftPathVisualiser plateauDriftVisualiser;
        [SerializeField] private BooleanAttractorLandscapeVisualiser attractorVisualiser;
        [SerializeField] private TrajectorySceneCamera leftSceneCamera;
        [SerializeField] private TrajectorySceneCamera rightSceneCamera;
        [SerializeField] private Text infoLabel;
        [SerializeField] private Text runValueLabel;
        [SerializeField] private Text episodeValueLabel;
        [SerializeField] private Text stepValueLabel;
        [SerializeField] private Button contextPathToggleButton;
        [SerializeField] private Option10DualPaneMdsAttractorViewController viewController;
        [SerializeField] private float highlightScaleMultiplier = 1.35f;

        private int currentEpisodeIndex;
        private int currentAbsoluteStepIndex;
        private bool isActive;
        private bool leftCameraInitialized;
        private bool rightCameraInitialized;

        public PlateauEpisode ActiveEpisode =>
            plateauDriftVisualiser?.ActiveEpisode;

        public void Configure(
            PlateauDriftPathVisualiser plateauVisualiser,
            BooleanAttractorLandscapeVisualiser landscapeVisualiser,
            TrajectorySceneCamera leftCamera,
            TrajectorySceneCamera rightCamera,
            Text infoText)
        {
            plateauDriftVisualiser = plateauVisualiser;
            attractorVisualiser = landscapeVisualiser;
            leftSceneCamera = leftCamera;
            rightSceneCamera = rightCamera;
            infoLabel = infoText;
        }

        public void ConfigureNavValueLabels(Text runValue, Text episodeValue, Text stepValue)
        {
            runValueLabel = runValue;
            episodeValueLabel = episodeValue;
            stepValueLabel = stepValue;
        }

        public void ConfigureViewController(Option10DualPaneMdsAttractorViewController controller)
        {
            viewController = controller;
        }

        public void ConfigureContextPathToggle(Button toggleButton)
        {
            contextPathToggleButton = toggleButton;
            BindContextPathToggle();
        }

        public void ResetExplorer()
        {
            isActive = false;
            leftCameraInitialized = false;
            rightCameraInitialized = false;
            currentEpisodeIndex = 0;
            currentAbsoluteStepIndex = 0;
            plateauDriftVisualiser?.ClearStepHighlight();
            attractorVisualiser?.SetHighlightMode(BooleanAttractorLandscapeHighlightMode.GlobalAttractors);
            SyncContextPathToggleLabel();
            SyncPanels();
            SyncNavValueLabels();
        }

        public void GoToPreviousRun()
        {
            if (GetRunCount() <= 1)
            {
                return;
            }

            SelectRun(GetActiveRunIndex() - 1);
        }

        public void GoToNextRun()
        {
            if (GetRunCount() <= 1)
            {
                return;
            }

            SelectRun(GetActiveRunIndex() + 1);
        }

        public void GoToFirstRun()
        {
            if (GetRunCount() <= 0)
            {
                return;
            }

            SelectRun(0);
        }

        public void GoToLastRun()
        {
            if (GetRunCount() <= 0)
            {
                return;
            }

            SelectRun(GetRunCount() - 1);
        }

        public void GoToDisplayRun(int displayRun)
        {
            if (GetRunCount() <= 0)
            {
                return;
            }

            displayRun = Mathf.Clamp(displayRun, 1, GetRunCount());
            SelectRun(displayRun - 1);
        }

        public void SelectRun(int runIndex)
        {
            if (viewController == null || GetRunCount() <= 0)
            {
                return;
            }

            runIndex = Mathf.Clamp(runIndex, 0, GetRunCount() - 1);
            viewController.SetActiveRun(runIndex);
            currentEpisodeIndex = 0;
            leftCameraInitialized = false;
            rightCameraInitialized = false;

            if (plateauDriftVisualiser?.PlateauEpisodes == null
                || plateauDriftVisualiser.PlateauEpisodes.Length == 0)
            {
                isActive = false;
                plateauDriftVisualiser?.ClearStepHighlight();
                attractorVisualiser?.ShowStep(0);
                SyncPanels();
                SyncNavValueLabels();
                return;
            }

            GoToFirstEpisode();
            SyncNavValueLabels();
        }

        public void GoToFirstEpisode()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            currentEpisodeIndex = 0;
            plateauDriftVisualiser.ShowEpisode(currentEpisodeIndex);
            currentAbsoluteStepIndex = plateauDriftVisualiser.ActiveEpisode.StartStep;
            FocusCurrentStep();
        }

        public void GoToPreviousEpisode()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            if (currentEpisodeIndex <= 0)
            {
                return;
            }

            currentEpisodeIndex--;
            plateauDriftVisualiser.ShowEpisode(currentEpisodeIndex);
            currentAbsoluteStepIndex = plateauDriftVisualiser.ActiveEpisode.StartStep;
            FocusCurrentStep();
        }

        public void GoToNextEpisode()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            if (currentEpisodeIndex >= plateauDriftVisualiser.PlateauEpisodes.Length - 1)
            {
                return;
            }

            currentEpisodeIndex++;
            plateauDriftVisualiser.ShowEpisode(currentEpisodeIndex);
            currentAbsoluteStepIndex = plateauDriftVisualiser.ActiveEpisode.StartStep;
            FocusCurrentStep();
        }

        public void GoToLastEpisode()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            int lastEpisodeIndex = plateauDriftVisualiser.PlateauEpisodes.Length - 1;
            if (currentEpisodeIndex >= lastEpisodeIndex)
            {
                return;
            }

            currentEpisodeIndex = lastEpisodeIndex;
            plateauDriftVisualiser.ShowEpisode(currentEpisodeIndex);
            currentAbsoluteStepIndex = plateauDriftVisualiser.ActiveEpisode.StartStep;
            FocusCurrentStep();
        }

        public void GoToFirstStep()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            currentAbsoluteStepIndex = ActiveEpisode.StartStep;
            FocusCurrentStep();
        }

        public void GoToPreviousStep()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            if (!isActive)
            {
                GoToFirstStep();
                return;
            }

            if (currentAbsoluteStepIndex <= ActiveEpisode.StartStep)
            {
                return;
            }

            currentAbsoluteStepIndex--;
            FocusCurrentStep();
        }

        public void GoToNextStep()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            if (!isActive)
            {
                GoToFirstStep();
                return;
            }

            if (currentAbsoluteStepIndex >= ActiveEpisode.ExitStep)
            {
                return;
            }

            currentAbsoluteStepIndex++;
            FocusCurrentStep();
        }

        public void GoToLastStep()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            currentAbsoluteStepIndex = ActiveEpisode.ExitStep;
            FocusCurrentStep();
        }

        public void GoToDisplayStep(int displayStep)
        {
            if (!TryEnsureReady())
            {
                return;
            }

            int minDisplay = ActiveEpisode.StartStep + 1;
            int maxDisplay = ActiveEpisode.ExitStep + 1;
            displayStep = Mathf.Clamp(displayStep, minDisplay, maxDisplay);
            currentAbsoluteStepIndex = displayStep - 1;
            FocusCurrentStep();
        }

        public void ToggleContextPathEdges()
        {
            if (attractorVisualiser == null)
            {
                return;
            }

            attractorVisualiser.CycleHighlightMode();
            SyncContextPathToggleLabel();
            SyncAttractorReadout();
        }

        public void SyncAttractorReadout()
        {
            Transform panelRoot = transform.parent;
            if (panelRoot == null || attractorVisualiser == null)
            {
                return;
            }

            DualPaneRightLegendShelfTabs tabs = DualPaneRightLegendShelfTabs.EnsureOnPanel(panelRoot);
            DualPaneRightShelfTab activeTab = tabs != null
                ? tabs.ActiveTab
                : DualPaneRightShelfTab.Legend;

            BooleanAttractorLandscapeHighlightMode highlightMode = attractorVisualiser.HighlightMode;
            tabs?.SyncSecondaryTabLabel(highlightMode);
            BooleanAttractorLandscapeLegend.LayoutDualPaneRightShelf(
                panelRoot,
                activeTab,
                highlightMode);

            DualPaneViewUiFactory.BringShelfTabBarsToFront(panelRoot);

            if (!attractorVisualiser.HasActiveRun || activeTab != DualPaneRightShelfTab.Attractors)
            {
                return;
            }

            switch (highlightMode)
            {
                case BooleanAttractorLandscapeHighlightMode.ContextPath:
                    BooleanAttractorLandscapeContextPathPanel.UpdateFromVisualiser(panelRoot, attractorVisualiser);
                    break;
                case BooleanAttractorLandscapeHighlightMode.ContextAttractors:
                    BooleanAttractorLandscapeContextAttractorReadoutPanel.UpdateFromVisualiser(
                        panelRoot,
                        attractorVisualiser);
                    break;
                default:
                    BooleanAttractorLandscapeAttractorReadoutPanel.UpdateFromVisualiser(panelRoot, attractorVisualiser);
                    break;
            }
        }

        public void SyncPanels()
        {
            Transform panelRoot = transform.parent;
            if (panelRoot == null)
            {
                return;
            }

            PlateauEpisode episode = ActiveEpisode;
            GenomePathPointData currentPoint = ResolveCurrentGenomePoint();
            PlateauDriftMetricsPanel.UpdateFromEpisode(
                panelRoot,
                episode,
                currentAbsoluteStepIndex,
                attractorVisualiser,
                currentPoint,
                GetActiveRunIndex(),
                GetRunCount());
            PlateauDriftAttractorSummaryPanel.UpdateFromEpisode(panelRoot, episode, attractorVisualiser);
            Option10LeftLegendShelfTabs.RefreshShelfLayout(panelRoot);
        }

        private GenomePathPointData ResolveCurrentGenomePoint()
        {
            GenomePathRunData genomeRun = plateauDriftVisualiser?.LoadedGenomeRun;
            if (genomeRun?.points == null
                || currentAbsoluteStepIndex < 0
                || currentAbsoluteStepIndex >= genomeRun.points.Length)
            {
                return null;
            }

            return genomeRun.points[currentAbsoluteStepIndex];
        }

        private void Update()
        {
            if (!isActive || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                GoToNextStep();
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                GoToPreviousStep();
            }
            else if (Keyboard.current.pageDownKey.wasPressedThisFrame)
            {
                GoToNextEpisode();
            }
            else if (Keyboard.current.pageUpKey.wasPressedThisFrame)
            {
                GoToPreviousEpisode();
            }
            else if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
            {
                GoToPreviousRun();
            }
            else if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {
                GoToNextRun();
            }
        }

        private void FocusCurrentStep()
        {
            if (!TryEnsureReady())
            {
                return;
            }

            PlateauEpisode episode = ActiveEpisode;
            currentAbsoluteStepIndex = Mathf.Clamp(
                currentAbsoluteStepIndex,
                episode.StartStep,
                episode.ExitStep);
            isActive = true;

            plateauDriftVisualiser.HighlightAbsoluteStep(currentAbsoluteStepIndex, highlightScaleMultiplier);
            attractorVisualiser.ShowStep(currentAbsoluteStepIndex);
            ReapplyRenderIsolation();

            ConfigureLeftCamera();
            ConfigureRightCamera();
            SyncAttractorReadout();
            SyncPanels();
            SyncNavValueLabels();
        }

        private void ConfigureLeftCamera()
        {
            if (leftSceneCamera == null || plateauDriftVisualiser == null)
            {
                return;
            }

            leftSceneCamera.SetFocus(plateauDriftVisualiser.OrbitFocus);
            leftSceneCamera.SetLookAtHeight(0f);
            leftSceneCamera.SetVerticalOrbitLimited(true);
            leftSceneCamera.SetPitchDegrees(22f);
            leftSceneCamera.SetDistanceLimits(
                plateauDriftVisualiser.MinOrbitDistance,
                DualPaneMdsAttractorViewController.GetMaxCameraDistance(plateauDriftVisualiser.ContentExtent));

            if (!leftCameraInitialized)
            {
                leftSceneCamera.SetDistance(
                    DualPaneMdsAttractorViewController.GetDefaultCameraDistance(
                        plateauDriftVisualiser.ContentExtent));
                leftCameraInitialized = true;
            }

            leftSceneCamera.PlaceCameraFixed();
        }

        private void ConfigureRightCamera()
        {
            if (rightSceneCamera == null || attractorVisualiser == null)
            {
                return;
            }

            rightSceneCamera.SetFocus(attractorVisualiser.OrbitFocus);
            rightSceneCamera.SetLookAtHeight(0f);
            rightSceneCamera.SetVerticalOrbitLimited(false);
            rightSceneCamera.SetDistanceLimits(
                attractorVisualiser.MinOrbitDistance,
                DualPaneMdsAttractorViewController.GetMaxCameraDistance(attractorVisualiser.ContentExtent));

            if (!rightCameraInitialized)
            {
                rightSceneCamera.SetDistance(
                    DualPaneMdsAttractorViewController.GetDefaultCameraDistance(
                        attractorVisualiser.ContentExtent));
                rightCameraInitialized = true;
            }

            rightSceneCamera.PlaceCameraFixed();
        }

        private void ReapplyRenderIsolation()
        {
            Camera leftCam = leftSceneCamera != null ? leftSceneCamera.GetComponent<Camera>() : null;
            Camera rightCam = rightSceneCamera != null ? rightSceneCamera.GetComponent<Camera>() : null;
            DualPaneRenderIsolation.Apply(
                plateauDriftVisualiser != null ? plateauDriftVisualiser.transform : null,
                attractorVisualiser != null ? attractorVisualiser.transform : null,
                leftCam,
                rightCam);
        }

        private bool TryEnsureReady()
        {
            if (plateauDriftVisualiser == null || attractorVisualiser == null)
            {
                return false;
            }

            if (!plateauDriftVisualiser.HasActiveRun || !attractorVisualiser.HasActiveRun)
            {
                return false;
            }

            if (plateauDriftVisualiser.PlateauEpisodes == null
                || plateauDriftVisualiser.PlateauEpisodes.Length == 0
                || plateauDriftVisualiser.ActiveEpisode == null)
            {
                return false;
            }

            return true;
        }

        public void SyncNavValueLabels()
        {
            if (runValueLabel != null)
            {
                int runCount = Mathf.Max(1, GetRunCount());
                int activeRun = GetRunCount() > 0
                    ? Mathf.Clamp(GetActiveRunIndex() + 1, 1, runCount)
                    : 0;
                runValueLabel.text = $"{activeRun}/{runCount}";
            }

            if (episodeValueLabel != null)
            {
                int episodeCount = Mathf.Max(1, GetEpisodeCount());
                if (GetEpisodeCount() <= 0 || ActiveEpisode == null)
                {
                    episodeValueLabel.text = $"0/{episodeCount}";
                }
                else
                {
                    int activeEpisode = Mathf.Clamp(currentEpisodeIndex + 1, 1, episodeCount);
                    episodeValueLabel.text = $"{activeEpisode}/{episodeCount}";
                }
            }

            if (stepValueLabel != null)
            {
                if (ActiveEpisode == null)
                {
                    stepValueLabel.text = "0/0";
                    return;
                }

                int totalSteps = ActiveEpisode.ExitStep - ActiveEpisode.StartStep + 1;
                totalSteps = Mathf.Max(1, totalSteps);
                int activeStep = currentAbsoluteStepIndex - ActiveEpisode.StartStep + 1;
                activeStep = Mathf.Clamp(activeStep, 1, totalSteps);
                stepValueLabel.text = $"{activeStep}/{totalSteps}";
            }
        }

        private void SyncContextPathToggleLabel()
        {
            if (contextPathToggleButton == null)
            {
                return;
            }

            Text label = contextPathToggleButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            BooleanAttractorLandscapeHighlightMode highlightMode = attractorVisualiser != null
                ? attractorVisualiser.HighlightMode
                : BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
            label.text = highlightMode switch
            {
                BooleanAttractorLandscapeHighlightMode.ContextPath => "Show context attractors",
                BooleanAttractorLandscapeHighlightMode.ContextAttractors => "Show attractor cycles",
                _ => "Show context path",
            };
        }

        private void BindContextPathToggle()
        {
            if (contextPathToggleButton == null)
            {
                return;
            }

            contextPathToggleButton.onClick.RemoveAllListeners();
            contextPathToggleButton.onClick.AddListener(ToggleContextPathEdges);
            SyncContextPathToggleLabel();
        }

        private int GetRunCount()
        {
            if (viewController != null)
            {
                return viewController.RunCount;
            }

            int genomeRuns = plateauDriftVisualiser != null ? plateauDriftVisualiser.RunCount : 0;
            int attractorRuns = attractorVisualiser != null ? attractorVisualiser.RunCount : 0;
            if (genomeRuns <= 0)
            {
                return attractorRuns;
            }

            if (attractorRuns <= 0)
            {
                return genomeRuns;
            }

            return Mathf.Min(genomeRuns, attractorRuns);
        }

        private int GetActiveRunIndex()
        {
            if (viewController != null)
            {
                return viewController.ActiveRunIndex;
            }

            if (attractorVisualiser != null)
            {
                return attractorVisualiser.ActiveRunIndex;
            }

            return plateauDriftVisualiser != null ? plateauDriftVisualiser.ActiveRunIndex : 0;
        }

        private int GetEpisodeCount()
        {
            return plateauDriftVisualiser?.PlateauEpisodes?.Length ?? 0;
        }
    }
}
