using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public sealed class GenomeMdsFitnessPathUiHandles
    {
        public GameObject Root;
        public GenomeMdsFitnessPathExplorer Explorer;
        public GenomeMdsFitnessPathInfoPanel InfoPanel;
    }

    public static class GenomeMdsFitnessPathUiFactory
    {
        private const string ControlsRootName = "GenomeMdsFitnessPathControls";
        private const string InfoPanelName = "PathInfoPanel";

        private const float TopRightMargin = 24f;
        private const float PanelWidth = 360f;
        private const float StepNavBarHeight = 44f;
        private const float RunNavBarHeight = 36f;
        private const float NavBarHeight = StepNavBarHeight + RunNavBarHeight;
        private const float StackGap = 8f;
        private const float TextSectionHeight = GenomeMdsFitnessPathInfoPanel.TextSectionHeight;
        private const float SquareSize = GenomeMdsFitnessPathFactorCircles.DefaultSquareSize;
        private const float SquareSpacing = GenomeMdsFitnessPathFactorCircles.DefaultSquareSpacing;

        private static readonly Color PanelBackground = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        private static readonly Color ButtonBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color InputBackground = new Color(0.12f, 0.14f, 0.18f, 1f);

        public static GenomeMdsFitnessPathUiHandles EnsureOnPanel(
            Transform panelRoot,
            GenomeMdsFitnessPathVisualiser visualiser,
            TrajectorySceneCamera sceneCamera,
            TrajectoryZoomSlider zoomSlider = null,
            System.Action<string> onActiveRunChanged = null)
        {
            if (panelRoot == null)
            {
                return null;
            }

            GenomeMdsFitnessPathInfoPanel infoPanel = EnsureInfoPanel(panelRoot, visualiser);
            GameObject controlsRoot = EnsureNavControlsRoot(panelRoot);
            GenomeMdsFitnessPathExplorer explorer = EnsureExplorer(controlsRoot);
            Text runValue = EnsureRunNavRow(controlsRoot.transform, explorer);
            Transform stepNavRow = EnsureStepNavRow(controlsRoot.transform);
            InputField stepInput = EnsureNavBar(stepNavRow, explorer);

            explorer.Configure(
                visualiser,
                sceneCamera,
                infoPanel,
                stepInput,
                zoomSlider,
                runValue,
                onActiveRunChanged);

            return new GenomeMdsFitnessPathUiHandles
            {
                Root = controlsRoot,
                Explorer = explorer,
                InfoPanel = infoPanel,
            };
        }

        public static void SetVisible(GenomeMdsFitnessPathUiHandles handles, bool visible)
        {
            if (handles?.Root != null)
            {
                handles.Root.SetActive(visible);
            }

            if (handles?.InfoPanel != null)
            {
                handles.InfoPanel.SetVisible(visible);
            }

            if (!visible)
            {
                handles?.Explorer?.ResetExplorer();
            }
        }

        private static GenomeMdsFitnessPathExplorer EnsureExplorer(GameObject controlsRoot)
        {
            GenomeMdsFitnessPathExplorer explorer =
                controlsRoot.GetComponent<GenomeMdsFitnessPathExplorer>();
            if (explorer == null)
            {
                explorer = controlsRoot.AddComponent<GenomeMdsFitnessPathExplorer>();
            }

            return explorer;
        }

        private static GameObject EnsureNavControlsRoot(Transform panelRoot)
        {
            Transform existingRoot = panelRoot.Find(ControlsRootName);
            GameObject controlsRoot;
            if (existingRoot != null)
            {
                controlsRoot = existingRoot.gameObject;
                RemoveLegacyBottomControls(controlsRoot.transform);
            }
            else
            {
                controlsRoot = CreatePanel(
                    ControlsRootName,
                    panelRoot,
                    PanelBackground,
                    anchorMin: new Vector2(1f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    pivot: new Vector2(1f, 1f),
                    anchoredPosition: new Vector2(-TopRightMargin, -TopRightMargin),
                    sizeDelta: new Vector2(PanelWidth, NavBarHeight));
            }

            ApplyNavControlsLayout(controlsRoot.GetComponent<RectTransform>());
            EnsureVerticalLayout(controlsRoot);
            EnsureHorizontalLayout(controlsRoot);
            return controlsRoot;
        }

        private static void EnsureVerticalLayout(GameObject controlsRoot)
        {
            VerticalLayoutGroup layout = controlsRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = controlsRoot.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static Transform EnsureStepNavRow(Transform controlsRoot)
        {
            Transform stepNavRow = controlsRoot.Find("StepNavRow");
            if (stepNavRow == null)
            {
                GameObject rowObject = new GameObject(
                    "StepNavRow",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement));
                rowObject.transform.SetParent(controlsRoot, false);
                stepNavRow = rowObject.transform;
            }

            LayoutElement rowLayout = stepNavRow.GetComponent<LayoutElement>()
                ?? stepNavRow.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = StepNavBarHeight;
            rowLayout.minHeight = StepNavBarHeight;

            HorizontalLayoutGroup layout = stepNavRow.GetComponent<HorizontalLayoutGroup>()
                ?? stepNavRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            return stepNavRow;
        }

        private static Text EnsureRunNavRow(Transform controlsRoot, GenomeMdsFitnessPathExplorer explorer)
        {
            Transform runNavRow = controlsRoot.Find("RunNavRow");
            if (runNavRow == null)
            {
                GameObject rowObject = new GameObject(
                    "RunNavRow",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement));
                rowObject.transform.SetParent(controlsRoot, false);
                runNavRow = rowObject.transform;
            }

            DestroyIfExists(runNavRow, "RunInputField");
            DestroyIfExists(runNavRow, "PreviousRunButton");
            DestroyIfExists(runNavRow, "NextRunButton");

            LayoutElement rowLayout = runNavRow.GetComponent<LayoutElement>()
                ?? runNavRow.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = RunNavBarHeight;
            rowLayout.minHeight = RunNavBarHeight;

            HorizontalLayoutGroup layout = runNavRow.GetComponent<HorizontalLayoutGroup>()
                ?? runNavRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 0, 4);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            CreateRunNavLabel(runNavRow, "RunLabel", "Run");
            Button firstRunButton = EnsureNavButton(runNavRow, "FirstRunButton", "<<");
            Button previousRunButton = EnsureNavButton(runNavRow, "PreviousRunButton", "<");
            Text runValue = EnsureRunValueLabel(runNavRow);
            Button nextRunButton = EnsureNavButton(runNavRow, "NextRunButton", ">");
            Button lastRunButton = EnsureNavButton(runNavRow, "LastRunButton", ">>");

            firstRunButton.onClick.RemoveAllListeners();
            previousRunButton.onClick.RemoveAllListeners();
            nextRunButton.onClick.RemoveAllListeners();
            lastRunButton.onClick.RemoveAllListeners();
            firstRunButton.onClick.AddListener(explorer.GoToFirstRun);
            previousRunButton.onClick.AddListener(explorer.GoToPreviousRun);
            nextRunButton.onClick.AddListener(explorer.GoToNextRun);
            lastRunButton.onClick.AddListener(explorer.GoToLastRun);

            runNavRow.SetAsFirstSibling();
            return runValue;
        }

        private static void CreateRunNavLabel(Transform parent, string name, string text)
        {
            Transform existing = parent.Find(name);
            GameObject labelObject;
            if (existing != null)
            {
                labelObject = existing.gameObject;
            }
            else
            {
                labelObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text),
                    typeof(LayoutElement));
                labelObject.transform.SetParent(parent, false);
            }

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.82f, 0.86f, 0.92f, 1f);

            LayoutElement layout = labelObject.GetComponent<LayoutElement>()
                ?? labelObject.AddComponent<LayoutElement>();
            layout.minWidth = 34f;
            layout.preferredWidth = 34f;
            layout.preferredHeight = 32f;
        }

        private static Text EnsureRunValueLabel(Transform parent)
        {
            Transform existing = parent.Find("RunValueLabel");
            if (existing != null && existing.TryGetComponent(out Text existingLabel))
            {
                StyleRunValueLabel(existing.gameObject, existingLabel);
                return existingLabel;
            }

            GameObject valueObject = new GameObject(
                "RunValueLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            valueObject.transform.SetParent(parent, false);

            Image background = valueObject.GetComponent<Image>();
            background.color = InputBackground;
            background.raycastTarget = false;

            LayoutElement layout = valueObject.AddComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredWidth = 88f;
            layout.flexibleWidth = 1f;
            layout.preferredHeight = 32f;

            GameObject textObject = new GameObject(
                "ValueText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(valueObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text value = textObject.GetComponent<Text>();
            StyleRunValueLabel(valueObject, value);
            return value;
        }

        private static void StyleRunValueLabel(GameObject valueObject, Text value)
        {
            value.text = "1/1";
            value.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            value.fontSize = 18;
            value.fontStyle = FontStyle.Bold;
            value.color = Color.white;
            value.alignment = TextAnchor.MiddleCenter;
            value.raycastTarget = false;
        }

        private static void ApplyNavControlsLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-TopRightMargin, -TopRightMargin);
            rectTransform.sizeDelta = new Vector2(PanelWidth, NavBarHeight);
            rectTransform.SetAsFirstSibling();
        }

        private static void EnsureHorizontalLayout(GameObject controlsRoot)
        {
            HorizontalLayoutGroup layout = controlsRoot.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(layout);
                return;
            }
#endif
            Object.Destroy(layout);
        }

        private static InputField EnsureNavBar(Transform navRoot, GenomeMdsFitnessPathExplorer explorer)
        {
            Button firstButton = EnsureNavButton(navRoot, "FirstStepButton", "<<");
            Button previousButton = EnsureNavButton(navRoot, "PreviousStepButton", "<");
            InputField stepInput = EnsureStepInputField(navRoot);
            Button nextButton = EnsureNavButton(navRoot, "NextStepButton", ">");
            Button lastButton = EnsureNavButton(navRoot, "LastStepButton", ">>");

            firstButton.onClick.RemoveAllListeners();
            previousButton.onClick.RemoveAllListeners();
            nextButton.onClick.RemoveAllListeners();
            lastButton.onClick.RemoveAllListeners();

            firstButton.onClick.AddListener(explorer.GoToFirstStep);
            previousButton.onClick.AddListener(explorer.GoToPreviousStep);
            nextButton.onClick.AddListener(explorer.GoToNextStep);
            lastButton.onClick.AddListener(explorer.GoToLastStep);

            return stepInput;
        }

        private static void RemoveLegacyBottomControls(Transform controlsRoot)
        {
            DestroyIfExists(controlsRoot, "ExploreButton");
            MigrateLegacyNavButtons(controlsRoot);
        }

        private static void MigrateLegacyNavButtons(Transform controlsRoot)
        {
            Transform stepNavRow = controlsRoot.Find("StepNavRow");
            if (stepNavRow == null)
            {
                return;
            }

            string[] buttonNames =
            {
                "FirstStepButton",
                "PreviousStepButton",
                "StepInputField",
                "NextStepButton",
                "LastStepButton",
            };

            for (int index = 0; index < buttonNames.Length; index++)
            {
                Transform child = controlsRoot.Find(buttonNames[index]);
                if (child != null)
                {
                    child.SetParent(stepNavRow, false);
                }
            }
        }

        private static void DestroyIfExists(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                Object.Destroy(child.gameObject);
            }
        }

        private static Button EnsureNavButton(Transform parent, string name, string label)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                StyleNavButton(existing.gameObject, label);
                return existingButton;
            }

            Button button = CreateNavButton(name, parent, label);
            return button;
        }

        private static Button CreateNavButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            StyleNavButton(buttonObject, label);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.minWidth = 44f;
            layout.preferredWidth = 44f;
            layout.preferredHeight = 36f;

            return buttonObject.GetComponent<Button>();
        }

        private static void StyleNavButton(GameObject buttonObject, string label)
        {
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonBackground;
        }

        private static InputField EnsureStepInputField(Transform parent)
        {
            Transform existing = parent.Find("StepInputField");
            if (existing != null && existing.TryGetComponent(out InputField existingInput))
            {
                StyleStepInput(existing.gameObject, existingInput);
                return existingInput;
            }

            return CreateStepInputField(parent);
        }

        private static InputField CreateStepInputField(Transform parent)
        {
            GameObject inputObject = DefaultControls.CreateInputField(new DefaultControls.Resources());
            inputObject.name = "StepInputField";
            inputObject.transform.SetParent(parent, false);

            InputField input = inputObject.GetComponent<InputField>();
            StyleStepInput(inputObject, input);

            LayoutElement layout = inputObject.AddComponent<LayoutElement>();
            layout.minWidth = 72f;
            layout.preferredWidth = 96f;
            layout.flexibleWidth = 1f;
            layout.preferredHeight = 36f;

            return input;
        }

        private static void StyleStepInput(GameObject inputObject, InputField input)
        {
            input.text = "1";
            input.contentType = InputField.ContentType.IntegerNumber;
            input.lineType = InputField.LineType.SingleLine;

            if (input.textComponent != null)
            {
                input.textComponent.fontSize = 18;
                input.textComponent.color = Color.white;
                input.textComponent.alignment = TextAnchor.MiddleCenter;
            }

            if (input.placeholder is Text placeholder)
            {
                placeholder.text = "1";
                placeholder.fontSize = 18;
                placeholder.color = new Color(0.65f, 0.68f, 0.72f, 1f);
                placeholder.alignment = TextAnchor.MiddleCenter;
            }

            Image image = inputObject.GetComponent<Image>();
            image.color = InputBackground;
        }

        private static float ComputeInfoPanelHeight(int contextCount)
        {
            if (contextCount <= 0)
            {
                return TextSectionHeight + 24f;
            }

            return TextSectionHeight
                + GenomeMdsFitnessPathFactorCircles.GetRequiredHeight(
                    contextCount,
                    SquareSize,
                    SquareSpacing);
        }

        private static float ComputeInfoPanelWidth(int geneCount)
        {
            float requiredWidth = GenomeMdsFitnessPathFactorCircles.GetRequiredWidth(
                geneCount,
                SquareSize,
                SquareSpacing);
            return Mathf.Max(PanelWidth, requiredWidth);
        }

        private static GenomeMdsFitnessPathInfoPanel EnsureInfoPanel(
            Transform panelRoot,
            GenomeMdsFitnessPathVisualiser visualiser)
        {
            int geneCount = visualiser != null ? visualiser.GeneCount : 0;
            int contextCount = visualiser != null ? visualiser.ContextCount : 0;
            string[] contextTargets = visualiser != null ? visualiser.ContextTargets : null;
            float panelHeight = ComputeInfoPanelHeight(contextCount);
            float panelWidth = ComputeInfoPanelWidth(geneCount);

            Transform existingPanel = panelRoot.Find(InfoPanelName);
            if (existingPanel == null)
            {
                Transform controlsRoot = panelRoot.Find(ControlsRootName);
                if (controlsRoot != null)
                {
                    existingPanel = controlsRoot.Find(InfoPanelName);
                }
            }

            if (existingPanel != null)
            {
                ApplyInfoPanelLayout(existingPanel.GetComponent<RectTransform>(), panelRoot, panelHeight, panelWidth);
                GenomeMdsFitnessPathInfoPanel existing =
                    existingPanel.GetComponent<GenomeMdsFitnessPathInfoPanel>();
                if (existing != null)
                {
                    existing.EnsureWired();
                    ApplyInfoPanelLabelLayout(existingPanel, contextCount);
                    existing.ConfigureFactorLayout(geneCount, contextCount, contextTargets);
                    ApplyFactorSectionLayout(existingPanel);
                    return existing;
                }
            }

            return CreateInfoPanel(panelRoot, geneCount, contextCount, panelHeight, panelWidth, contextTargets);
        }

        private static void ApplyFactorSectionLayout(Transform panelTransform)
        {
            Transform circlesRoot = panelTransform.Find("FactorCircles");
            if (circlesRoot == null || !circlesRoot.TryGetComponent(out RectTransform circlesRect))
            {
                return;
            }

            GenomeMdsFitnessPathInfoPanel.LayoutFactorSection(circlesRect);
        }

        private static void ApplyInfoPanelLayout(
            RectTransform rectTransform,
            Transform panelRoot,
            float panelHeight,
            float panelWidth)
        {
            if (rectTransform == null || panelRoot == null)
            {
                return;
            }

            if (rectTransform.parent != panelRoot)
            {
                rectTransform.SetParent(panelRoot, false);
            }

            float infoTopOffset = TopRightMargin + NavBarHeight + StackGap;
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-TopRightMargin, -infoTopOffset);
            rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);
            rectTransform.SetAsLastSibling();
        }

        private static void ApplyInfoPanelLabelLayout(Transform panelTransform, int contextCount)
        {
            ApplyLabelRect(
                panelTransform.Find("AcceptedGenomeText"),
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -40f),
                offsetMax: new Vector2(-16f, -16f));

            ApplyLabelRect(
                panelTransform.Find("FitnessText"),
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(16f, -68f),
                offsetMax: new Vector2(-16f, -44f));

            Transform fitnessLabel = panelTransform.Find("FitnessText");
            Transform acceptedLabel = panelTransform.Find("AcceptedGenomeText");
            if (acceptedLabel != null)
            {
                acceptedLabel.SetAsLastSibling();
            }

            if (fitnessLabel != null)
            {
                fitnessLabel.SetAsLastSibling();
            }
        }

        private static void ApplyLabelRect(
            Transform labelTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (labelTransform == null || !labelTransform.TryGetComponent(out RectTransform rectTransform))
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static GenomeMdsFitnessPathInfoPanel CreateInfoPanel(
            Transform panelRoot,
            int geneCount,
            int contextCount,
            float panelHeight,
            float panelWidth,
            string[] contextTargets)
        {
            float infoTopOffset = TopRightMargin + NavBarHeight + StackGap;
            GameObject panel = CreatePanel(
                InfoPanelName,
                panelRoot,
                PanelBackground,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                pivot: new Vector2(1f, 1f),
                anchoredPosition: new Vector2(-TopRightMargin, -infoTopOffset),
                sizeDelta: new Vector2(panelWidth, panelHeight));

            Text acceptedText = CreateLabel(
                "AcceptedGenomeText",
                panel.transform,
                "Accepted genome: — / —",
                18,
                FontStyle.Normal,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(16f, -40f),
                new Vector2(-16f, -16f),
                TextAnchor.UpperLeft);

            Text fitnessText = CreateLabel(
                "FitnessText",
                panel.transform,
                "Fitness: —",
                18,
                FontStyle.Normal,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(16f, -68f),
                new Vector2(-16f, -44f),
                TextAnchor.UpperLeft);

            GameObject circlesRoot = new GameObject(
                "FactorCircles",
                typeof(RectTransform),
                typeof(GenomeMdsFitnessPathFactorCircles));
            circlesRoot.transform.SetParent(panel.transform, false);
            ApplyFactorSectionLayout(panel.transform);

            GenomeMdsFitnessPathFactorCircles factorCircles =
                circlesRoot.GetComponent<GenomeMdsFitnessPathFactorCircles>();

            GenomeMdsFitnessPathInfoPanel infoPanel = panel.AddComponent<GenomeMdsFitnessPathInfoPanel>();
            infoPanel.Initialize(panel, fitnessText, acceptedText, factorCircles);
            infoPanel.ConfigureFactorLayout(geneCount, contextCount, contextTargets);
            infoPanel.SetVisible(false);
            panel.transform.SetAsLastSibling();
            return infoPanel;
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject panel = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            return panel;
        }

        private static Text CreateLabel(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = new Color(0.9f, 0.92f, 0.96f, 1f);
            text.raycastTarget = false;
            return text;
        }
    }
}
