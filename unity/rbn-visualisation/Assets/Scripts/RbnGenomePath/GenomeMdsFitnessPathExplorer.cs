using System;
using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public class GenomeMdsFitnessPathExplorer : MonoBehaviour
    {
        [SerializeField] private GenomeMdsFitnessPathVisualiser visualiser;
        [SerializeField] private TrajectorySceneCamera sceneCamera;
        [SerializeField] private GenomeMdsFitnessPathInfoPanel infoPanel;
        [SerializeField] private InputField stepInputField;
        [SerializeField] private Text runValueLabel;
        [SerializeField] private TrajectoryZoomSlider zoomSlider;
        [SerializeField] private float highlightScaleMultiplier = 1.35f;

        private int currentStepIndex;
        private bool isActive;
        private bool exploreCameraInitialized;
        private float recommendedFocusDistance = 2.5f;
        private Action<string> onActiveRunChanged;

        public bool IsActive => isActive;

        public void Configure(
            GenomeMdsFitnessPathVisualiser pathVisualiser,
            TrajectorySceneCamera camera,
            GenomeMdsFitnessPathInfoPanel panel,
            InputField stepInput,
            TrajectoryZoomSlider zoomControl = null,
            Text runValue = null,
            Action<string> activeRunChanged = null)
        {
            visualiser = pathVisualiser;
            sceneCamera = camera;
            infoPanel = panel;
            zoomSlider = zoomControl;
            runValueLabel = runValue;
            onActiveRunChanged = activeRunChanged;
            BindStepInput(stepInput);
            RefreshFactorLayout();
            SyncRunNavigationDisplay();
        }

        public void ConfigureRunValueLabel(Text runValue)
        {
            runValueLabel = runValue;
            SyncRunNavigationDisplay();
        }

        public void SetActiveRunChangedHandler(Action<string> activeRunChanged)
        {
            onActiveRunChanged = activeRunChanged;
        }

        private void RefreshFactorLayout()
        {
            if (visualiser == null || infoPanel == null)
            {
                return;
            }

            if (visualiser.GeneCount <= 0 || visualiser.ContextCount <= 0)
            {
                return;
            }

            infoPanel.ConfigureFactorLayout(
                visualiser.GeneCount,
                visualiser.ContextCount,
                visualiser.ContextTargets);
        }

        public void ResetExplorer()
        {
            isActive = false;
            exploreCameraInitialized = false;
            currentStepIndex = 0;
            visualiser?.ClearStepHighlight();
            infoPanel?.Clear();
            infoPanel?.SetVisible(false);
            SyncStepInputField(force: true);
            SyncRunNavigationDisplay();
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
            if (visualiser == null || GetRunCount() <= 0)
            {
                return;
            }

            runIndex = Mathf.Clamp(runIndex, 0, GetRunCount() - 1);
            visualiser.SetActiveRun(runIndex);
            exploreCameraInitialized = false;
            currentStepIndex = 0;
            isActive = false;
            visualiser.ClearStepHighlight();
            SyncRunNavigationDisplay();

            if (visualiser.HasActiveRun)
            {
                GoToFirstStep();
            }
            else
            {
                infoPanel?.Clear();
                infoPanel?.SetVisible(false);
                SyncStepInputField(force: true);
            }
        }

        public void SetRecommendedFocusDistance(float distance)
        {
            recommendedFocusDistance = Mathf.Max(visualiser?.MinOrbitDistance ?? 0.5f, distance);
        }

        public void GoToFirstStep()
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            currentStepIndex = 0;
            FocusCurrentStep();
        }

        public void GoToPreviousStep()
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            if (!isActive)
            {
                GoToFirstStep();
                return;
            }

            if (currentStepIndex <= 0)
            {
                return;
            }

            currentStepIndex--;
            FocusCurrentStep();
        }

        public void GoToNextStep()
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            if (!isActive)
            {
                GoToFirstStep();
                return;
            }

            int lastIndex = visualiser.ActiveRun.PointCount - 1;
            if (currentStepIndex >= lastIndex)
            {
                return;
            }

            currentStepIndex++;
            FocusCurrentStep();
        }

        public void GoToLastStep()
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            currentStepIndex = visualiser.ActiveRun.PointCount - 1;
            FocusCurrentStep();
        }

        public void GoToDisplayStep(int displayStep)
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            int lastDisplayStep = GetLastDisplayStep();
            displayStep = Mathf.Clamp(displayStep, 1, lastDisplayStep);
            currentStepIndex = displayStep - 1;
            FocusCurrentStep();
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
        }

        private void BindStepInput(InputField stepInput)
        {
            if (stepInputField != null)
            {
                stepInputField.onEndEdit.RemoveListener(OnStepInputSubmitted);
            }

            stepInputField = stepInput;
            if (stepInputField != null)
            {
                stepInputField.contentType = InputField.ContentType.IntegerNumber;
                stepInputField.lineType = InputField.LineType.SingleLine;
                stepInputField.onEndEdit.AddListener(OnStepInputSubmitted);
            }
        }

        private void OnStepInputSubmitted(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                SyncStepInputField(force: true);
                return;
            }

            if (!int.TryParse(rawValue.Trim(), out int displayStep))
            {
                SyncStepInputField(force: true);
                return;
            }

            GoToDisplayStep(displayStep);
        }

        private bool TryEnsureActiveRun()
        {
            return visualiser != null && visualiser.HasActiveRun;
        }

        private int GetLastDisplayStep()
        {
            GenomeMdsFitnessPathRunState run = visualiser.ActiveRun;
            if (run == null)
            {
                return 1;
            }

            return Mathf.Max(run.TotalAcceptedGenomes, run.PointCount, 1);
        }

        private void FocusCurrentStep()
        {
            GenomeMdsFitnessPathRunState run = visualiser.ActiveRun;
            if (run == null || run.PointCount == 0)
            {
                return;
            }

            bool firstFocus = !exploreCameraInitialized;
            isActive = true;
            currentStepIndex = Mathf.Clamp(currentStepIndex, 0, run.PointCount - 1);
            Vector3 worldPosition = run.WorldPositions[currentStepIndex];
            float fitness = run.FitnessValues[currentStepIndex];

            visualiser.HighlightStep(currentStepIndex, highlightScaleMultiplier);
            visualiser.OrbitFocus.position = worldPosition;

            if (sceneCamera != null)
            {
                sceneCamera.SetFocus(visualiser.OrbitFocus);
                sceneCamera.SetLookAtHeight(0f);

                if (firstFocus)
                {
                    sceneCamera.SetDistance(recommendedFocusDistance);
                    exploreCameraInitialized = true;
                    zoomSlider?.SyncFromCamera();
                }

                sceneCamera.PlaceCameraFixed();
            }

            float[] factorScores = null;
            if (run.FactorScoresPerStep != null && currentStepIndex < run.FactorScoresPerStep.Length)
            {
                factorScores = run.FactorScoresPerStep[currentStepIndex];
            }

            infoPanel?.ShowStep(
                currentStepIndex,
                run.TotalAcceptedGenomes,
                fitness,
                factorScores);
            SyncStepInputField(force: false);
        }

        private void SyncStepInputField(bool force)
        {
            if (stepInputField == null)
            {
                return;
            }

            if (!force && stepInputField.isFocused)
            {
                return;
            }

            stepInputField.text = (currentStepIndex + 1).ToString();
        }

        public void SyncRunNavigationDisplay()
        {
            SyncRunValueLabel();
            onActiveRunChanged?.Invoke(BuildActiveRunSubtitle());
        }

        private void SyncRunValueLabel()
        {
            if (runValueLabel == null)
            {
                return;
            }

            int runCount = GetRunCount();
            if (runCount <= 0)
            {
                runValueLabel.text = "0/0";
                return;
            }

            int activeRun = Mathf.Clamp(GetActiveRunIndex() + 1, 1, runCount);
            runValueLabel.text = $"{activeRun}/{runCount}";
        }

        private int GetRunCount()
        {
            return visualiser != null ? visualiser.RunCount : 0;
        }

        private int GetActiveRunIndex()
        {
            return visualiser != null ? visualiser.ActiveRunIndex : 0;
        }

        private string BuildActiveRunSubtitle()
        {
            if (visualiser == null || visualiser.RunCount <= 0)
            {
                return string.Empty;
            }

            return $"Run {visualiser.ActiveRunIndex + 1}/{visualiser.RunCount}";
        }
    }
}
