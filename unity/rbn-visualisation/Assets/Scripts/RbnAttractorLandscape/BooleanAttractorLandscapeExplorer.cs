using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Step navigation for the fixed Boolean state attractor landscape.
    /// Node positions never change; only transition edges and highlights update.
    /// </summary>
    public class BooleanAttractorLandscapeExplorer : MonoBehaviour
    {
        [SerializeField] private BooleanAttractorLandscapeVisualiser visualiser;
        [SerializeField] private TrajectorySceneCamera sceneCamera;
        [SerializeField] private Text infoLabel;
        [SerializeField] private InputField stepInputField;
        [SerializeField] private InputField runInputField;
        [SerializeField] private TrajectoryZoomSlider zoomSlider;
        [SerializeField] private Button contextPathToggleButton;
        [SerializeField] private bool enableRunNavigation;

        private int currentStepIndex;
        private bool isActive;
        private System.Action<string> onActiveRunChanged;

        public void Configure(
            BooleanAttractorLandscapeVisualiser landscapeVisualiser,
            TrajectorySceneCamera camera,
            Text infoText,
            InputField stepInput,
            TrajectoryZoomSlider zoomControl = null,
            InputField runInput = null,
            System.Action<string> activeRunChanged = null,
            bool runNavigation = false)
        {
            visualiser = landscapeVisualiser;
            sceneCamera = camera;
            infoLabel = infoText;
            stepInputField = stepInput;
            zoomSlider = zoomControl;
            onActiveRunChanged = activeRunChanged;
            enableRunNavigation = runNavigation;
            BindStepInput(stepInput);
            BindRunInput(runInput);
            BindContextPathToggle(contextPathToggleButton);
            BooleanAttractorLandscapeContextPathPanel.EnsureOnPanel(transform.parent);
            BooleanAttractorLandscapeContextAttractorReadoutPanel.EnsureOnPanel(transform.parent);
            BooleanAttractorLandscapeAttractorReadoutPanel.EnsureOnPanel(transform.parent);
            SyncContextPathDisplay();
        }

        public void ConfigureContextPathToggle(Button toggleButton)
        {
            contextPathToggleButton = toggleButton;
            BindContextPathToggle(toggleButton);
        }

        public void ConfigureRunInput(InputField runInput)
        {
            BindRunInput(runInput);
            SyncRunInputField(force: true);
        }

        public void SetActiveRunChangedHandler(System.Action<string> activeRunChanged)
        {
            onActiveRunChanged = activeRunChanged;
        }

        public void ResetExplorer()
        {
            isActive = false;
            currentStepIndex = 0;
            visualiser?.SetHighlightMode(BooleanAttractorLandscapeHighlightMode.GlobalAttractors);
            SyncContextPathToggleLabel();
            SyncContextPathDisplay();
            SyncStepInputField(force: true);
            SyncRunInputField(force: true);
            UpdateInfoLabel();
        }

        public void GoToPreviousRun()
        {
            if (!enableRunNavigation || GetRunCount() <= 1)
            {
                return;
            }

            SelectRun(GetActiveRunIndex() - 1);
        }

        public void GoToNextRun()
        {
            if (!enableRunNavigation || GetRunCount() <= 1)
            {
                return;
            }

            SelectRun(GetActiveRunIndex() + 1);
        }

        public void GoToDisplayRun(int displayRun)
        {
            if (!enableRunNavigation || GetRunCount() <= 0)
            {
                return;
            }

            displayRun = Mathf.Clamp(displayRun, 1, GetRunCount());
            SelectRun(displayRun - 1);
        }

        public void SelectRun(int runIndex)
        {
            if (!enableRunNavigation || visualiser == null || GetRunCount() <= 0)
            {
                return;
            }

            runIndex = Mathf.Clamp(runIndex, 0, GetRunCount() - 1);
            visualiser.SetActiveRun(runIndex);
            currentStepIndex = 0;
            FocusCurrentStep();
            SyncRunInputField(force: true);
            NotifyActiveRunChanged();
        }

        public void ToggleContextPathEdges()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.CycleHighlightMode();
            SyncContextPathToggleLabel();
            SyncContextPathDisplay();
        }

        public void SyncContextPathDisplay()
        {
            SyncLegendVisibility();
            SyncTopRightReadout();
        }

        private void SyncTopRightReadout()
        {
            Transform panelRoot = transform.parent;
            if (panelRoot == null || visualiser == null)
            {
                return;
            }

            BooleanAttractorLandscapeHighlightMode highlightMode = visualiser.HighlightMode;
            BooleanAttractorLandscapeContextPathPanel.SetVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.ContextPath);
            BooleanAttractorLandscapeContextAttractorReadoutPanel.SetVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.ContextAttractors);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.GlobalAttractors);

            if (!visualiser.HasActiveRun)
            {
                return;
            }

            switch (highlightMode)
            {
                case BooleanAttractorLandscapeHighlightMode.ContextPath:
                    BooleanAttractorLandscapeContextPathPanel.UpdateFromVisualiser(panelRoot, visualiser);
                    break;
                case BooleanAttractorLandscapeHighlightMode.ContextAttractors:
                    BooleanAttractorLandscapeContextAttractorReadoutPanel.UpdateFromVisualiser(panelRoot, visualiser);
                    break;
                default:
                    BooleanAttractorLandscapeAttractorReadoutPanel.UpdateFromVisualiser(panelRoot, visualiser);
                    break;
            }
        }

        public void SyncLegendVisibility()
        {
            if (visualiser == null)
            {
                return;
            }

            Transform panelRoot = transform.parent;
            BooleanAttractorLandscapeHighlightMode highlightMode = visualiser.HighlightMode;
            BooleanAttractorLandscapeLegend.SetAttractorCyclesLegendVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.GlobalAttractors);
            BooleanAttractorLandscapeLegend.SetContextPathLegendVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.ContextPath);
            BooleanAttractorLandscapeLegend.SetContextAttractorsLegendVisible(
                panelRoot,
                highlightMode == BooleanAttractorLandscapeHighlightMode.ContextAttractors);
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

            int lastIndex = visualiser.ActiveRun.steps.Length - 1;
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

            currentStepIndex = visualiser.ActiveRun.steps.Length - 1;
            FocusCurrentStep();
        }

        public void GoToDisplayStep(int displayStep)
        {
            if (!TryEnsureActiveRun())
            {
                return;
            }

            int lastDisplayStep = visualiser.ActiveRun.steps.Length;
            displayStep = Mathf.Clamp(displayStep, 1, lastDisplayStep);
            currentStepIndex = displayStep - 1;
            FocusCurrentStep();
        }

        private void FocusCurrentStep()
        {
            visualiser.ShowStep(currentStepIndex);
            isActive = true;
            SyncStepInputField(force: true);
            UpdateInfoLabel();

            if (sceneCamera != null && visualiser != null)
            {
                sceneCamera.SetFocus(visualiser.OrbitFocus);
                sceneCamera.SetLookAtHeight(0f);
            }

            zoomSlider?.SyncFromCamera();
            SyncTopRightReadout();
        }

        private bool TryEnsureActiveRun()
        {
            return visualiser != null && visualiser.HasActiveRun;
        }

        private void BindContextPathToggle(Button toggleButton)
        {
            if (toggleButton == null)
            {
                return;
            }

            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(ToggleContextPathEdges);
            SyncContextPathToggleLabel();
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

            BooleanAttractorLandscapeHighlightMode highlightMode = visualiser != null
                ? visualiser.HighlightMode
                : BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
            label.text = highlightMode switch
            {
                BooleanAttractorLandscapeHighlightMode.ContextPath => "Show context attractors",
                BooleanAttractorLandscapeHighlightMode.ContextAttractors => "Show attractor cycles",
                _ => "Show context path",
            };
        }

        private void BindStepInput(InputField stepInput)
        {
            if (stepInput == null)
            {
                return;
            }

            stepInput.onEndEdit.RemoveAllListeners();
            stepInput.onEndEdit.AddListener(OnStepInputSubmitted);
        }

        private void BindRunInput(InputField runInput)
        {
            runInputField = runInput;
            if (runInputField == null)
            {
                return;
            }

            runInputField.onEndEdit.RemoveAllListeners();
            runInputField.onEndEdit.AddListener(OnRunInputSubmitted);
        }

        private void OnRunInputSubmitted(string text)
        {
            if (!enableRunNavigation)
            {
                return;
            }

            if (!int.TryParse(text, out int displayRun))
            {
                SyncRunInputField(force: true);
                return;
            }

            GoToDisplayRun(displayRun);
        }

        private void SyncRunInputField(bool force)
        {
            if (!enableRunNavigation || runInputField == null || visualiser == null || GetRunCount() <= 0)
            {
                return;
            }

            if (!force && runInputField.isFocused)
            {
                return;
            }

            runInputField.text = (GetActiveRunIndex() + 1).ToString();
        }

        private int GetRunCount() => visualiser != null ? visualiser.RunCount : 0;

        private int GetActiveRunIndex() => visualiser != null ? visualiser.ActiveRunIndex : 0;

        private void NotifyActiveRunChanged()
        {
            if (onActiveRunChanged == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            onActiveRunChanged.Invoke(BuildActiveRunSubtitle());
        }

        private string BuildActiveRunSubtitle()
        {
            if (visualiser == null || !visualiser.HasActiveRun)
            {
                return string.Empty;
            }

            if (GetRunCount() > 1)
            {
                return $"Run {GetActiveRunIndex() + 1}/{GetRunCount()}: {visualiser.ActiveRun.sourceLabel}";
            }

            return visualiser.ActiveRun.sourceLabel;
        }

        private void OnStepInputSubmitted(string text)
        {
            if (!int.TryParse(text, out int displayStep))
            {
                SyncStepInputField(force: true);
                return;
            }

            GoToDisplayStep(displayStep);
        }

        private void SyncStepInputField(bool force)
        {
            if (stepInputField == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            if (!force && stepInputField.isFocused)
            {
                return;
            }

            stepInputField.text = (currentStepIndex + 1).ToString();
        }

        private void UpdateInfoLabel()
        {
            if (infoLabel == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            BooleanAttractorLandscapeStepData step = visualiser.ActiveStep;
            infoLabel.text =
                $"{visualiser.ActiveRun.sourceLabel}  |  step {currentStepIndex + 1}/" +
                $"{visualiser.ActiveRun.steps.Length}  |  fitness {step.fitness:F4}  |  " +
                $"{step.changeType}";
        }
    }
}
