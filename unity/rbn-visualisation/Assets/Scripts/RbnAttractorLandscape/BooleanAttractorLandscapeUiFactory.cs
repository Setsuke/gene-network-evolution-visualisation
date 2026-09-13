using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    public sealed class BooleanAttractorLandscapeUiHandles
    {
        public BooleanAttractorLandscapeExplorer Explorer;
        public GameObject Root;
    }

    /// <summary>
    /// UI controls for Option 5: fixed Boolean state attractor landscape.
    /// </summary>
    public static class BooleanAttractorLandscapeUiFactory
    {
        private const float ControlsBottomOffset = 72f;

        public static BooleanAttractorLandscapeUiHandles EnsureOnPanel(
            Transform panelRoot,
            BooleanAttractorLandscapeVisualiser visualiser,
            TrajectorySceneCamera sceneCamera,
            TrajectoryZoomSlider zoomSlider = null,
            bool enableRunNavigation = false,
            string controlsRootName = "BooleanAttractorLandscapeControls",
            System.Action<string> onActiveRunChanged = null)
        {
            Transform existing = panelRoot.Find(controlsRootName);
            GameObject controlsRoot;
            BooleanAttractorLandscapeExplorer explorer;

            if (existing != null)
            {
                controlsRoot = existing.gameObject;
                explorer = controlsRoot.GetComponent<BooleanAttractorLandscapeExplorer>();
                ApplyControlsLayout(controlsRoot.GetComponent<RectTransform>(), enableRunNavigation);
                BindNavigationButtons(controlsRoot.transform, explorer);
            }
            else
            {
                controlsRoot = new GameObject(
                    controlsRootName,
                    typeof(RectTransform),
                    typeof(BooleanAttractorLandscapeExplorer));
                controlsRoot.transform.SetParent(panelRoot, false);
                explorer = controlsRoot.GetComponent<BooleanAttractorLandscapeExplorer>();
                ApplyControlsLayout(controlsRoot.GetComponent<RectTransform>(), enableRunNavigation);

                Text infoLabel = CreateText(
                    "InfoLabel",
                    controlsRoot.transform,
                    string.Empty,
                    16,
                    TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, enableRunNavigation ? 68f : 36f),
                    new Vector2(980f, 28f));

                InputField runInput = null;
                if (enableRunNavigation)
                {
                    runInput = CreateRunNavRow(controlsRoot.transform, explorer);
                }

                InputField stepInput = CreateStepInput(controlsRoot.transform, enableRunNavigation);
                CreateNavButton("FirstStepButton", controlsRoot.transform, "<<", new Vector2(-300f, 0f));
                CreateNavButton("PreviousStepButton", controlsRoot.transform, "<", new Vector2(-220f, 0f));
                CreateNavButton("NextStepButton", controlsRoot.transform, ">", new Vector2(220f, 0f));
                CreateNavButton("LastStepButton", controlsRoot.transform, ">>", new Vector2(300f, 0f));
                BindNavigationButtons(controlsRoot.transform, explorer);

                explorer.Configure(
                    visualiser,
                    sceneCamera,
                    infoLabel,
                    stepInput,
                    zoomSlider,
                    runInput,
                    onActiveRunChanged,
                    enableRunNavigation);
            }

            Button contextPathToggle = EnsureContextPathToggleButton(controlsRoot.transform);

            if (explorer != null)
            {
                explorer.Configure(
                    visualiser,
                    sceneCamera,
                    FindText(controlsRoot.transform, "InfoLabel"),
                    FindInput(controlsRoot.transform, "StepInputField"),
                    zoomSlider,
                    FindInput(controlsRoot.transform, "RunInputField"),
                    onActiveRunChanged,
                    enableRunNavigation);
                explorer.ConfigureContextPathToggle(contextPathToggle);
                if (enableRunNavigation)
                {
                    explorer.ConfigureRunInput(FindInput(controlsRoot.transform, "RunInputField"));
                }
            }

            TrajectoryViewUiFactory.BringInteractiveControlsToFront(controlsRoot);
            BooleanAttractorLandscapeContextPathPanel.EnsureOnPanel(panelRoot);
            BooleanAttractorLandscapeContextAttractorReadoutPanel.EnsureOnPanel(panelRoot);
            BooleanAttractorLandscapeAttractorReadoutPanel.EnsureOnPanel(panelRoot);

            return new BooleanAttractorLandscapeUiHandles
            {
                Explorer = explorer,
                Root = controlsRoot,
            };
        }

        private static void ApplyControlsLayout(RectTransform controlsRect, bool enableRunNavigation)
        {
            if (controlsRect == null)
            {
                return;
            }

            controlsRect.anchorMin = new Vector2(0.5f, 0f);
            controlsRect.anchorMax = new Vector2(0.5f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.anchoredPosition = new Vector2(0f, ControlsBottomOffset);
            controlsRect.sizeDelta = new Vector2(enableRunNavigation ? 1180f : 1050f, enableRunNavigation ? 108f : 72f);
        }

        private static InputField CreateRunNavRow(
            Transform controlsRoot,
            BooleanAttractorLandscapeExplorer explorer)
        {
            GameObject runNavRow = new GameObject("RunNavRow", typeof(RectTransform));
            runNavRow.transform.SetParent(controlsRoot, false);
            RectTransform rowRect = runNavRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, 34f);
            rowRect.sizeDelta = new Vector2(420f, 28f);

            CreateNavButton("PreviousRunButton", runNavRow.transform, "[", new Vector2(-150f, 0f));
            CreateNavButton("NextRunButton", runNavRow.transform, "]", new Vector2(150f, 0f));
            InputField runInput = CreateRunInputField(runNavRow.transform);

            BindNavButton(runNavRow.transform, "PreviousRunButton", explorer.GoToPreviousRun);
            BindNavButton(runNavRow.transform, "NextRunButton", explorer.GoToNextRun);
            return runInput;
        }

        private static InputField CreateRunInputField(Transform parent)
        {
            GameObject inputObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputObject.name = "RunInputField";
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(72f, 28f);
            InputField inputField = inputObject.GetComponent<InputField>();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = "1";
            return inputField;
        }

        private static InputField CreateStepInput(Transform parent, bool enableRunNavigation)
        {
            InputField stepInput = CreateStepInput(parent);
            if (enableRunNavigation)
            {
                RectTransform rect = stepInput.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, -8f);
            }

            return stepInput;
        }

        private static void BindNavigationButtons(Transform controlsRoot, BooleanAttractorLandscapeExplorer explorer)
        {
            if (controlsRoot == null || explorer == null)
            {
                return;
            }

            BindNavButton(controlsRoot, "FirstStepButton", explorer.GoToFirstStep);
            BindNavButton(controlsRoot, "PreviousStepButton", explorer.GoToPreviousStep);
            BindNavButton(controlsRoot, "NextStepButton", explorer.GoToNextStep);
            BindNavButton(controlsRoot, "LastStepButton", explorer.GoToLastStep);
        }

        private static void BindNavButton(Transform controlsRoot, string buttonName, UnityEngine.Events.UnityAction action)
        {
            Transform buttonTransform = controlsRoot.Find(buttonName);
            if (buttonTransform == null || !buttonTransform.TryGetComponent(out Button button))
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.interactable = true;
        }

        public static void RefreshInteraction(BooleanAttractorLandscapeUiHandles handles)
        {
            if (handles?.Root == null)
            {
                return;
            }

            TrajectoryViewUiFactory.BringInteractiveControlsToFront(handles.Root);
        }

        public static void SetVisible(BooleanAttractorLandscapeUiHandles handles, bool visible)
        {
            if (handles?.Root != null)
            {
                handles.Root.SetActive(visible);
            }

            if (visible)
            {
                RefreshInteraction(handles);
            }
        }

        private static Button EnsureContextPathToggleButton(Transform controlsRoot)
        {
            Transform existing = controlsRoot.Find("ShowContextPathButton");
            Button toggleButton;
            if (existing != null)
            {
                toggleButton = existing.GetComponent<Button>();
            }
            else
            {
                toggleButton = CreateNavButton(
                    "ShowContextPathButton",
                    controlsRoot,
                    "Show context path",
                    new Vector2(420f, 0f));
                RectTransform rect = toggleButton.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(190f, 36f);
            }

            return toggleButton;
        }

        private static InputField CreateStepInput(Transform parent)
        {
            GameObject inputObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputObject.name = "StepInputField";
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(120f, 36f);
            InputField inputField = inputObject.GetComponent<InputField>();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = "1";
            return inputField;
        }

        private static Button CreateNavButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchoredPosition)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(56f, 36f);
            Text text = buttonObject.GetComponentInChildren<Text>();
            text.text = label;
            text.fontSize = 18;
            return buttonObject.GetComponent<Button>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static Text FindText(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static InputField FindInput(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            return child != null ? child.GetComponent<InputField>() : null;
        }
    }
}
