using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    public sealed class Option10DualPaneViewUiHandles
    {
        public GameObject Root;
        public Option10PlateauDriftLinkedExplorer Explorer;
        public TrajectoryZoomSlider LeftZoomSlider;
        public TrajectoryZoomSlider RightZoomSlider;
    }

    public static class Option10DualPaneViewUiFactory
    {
        private const float NavRowHeight = 36f;
        private const float NavRowWidth = 344f;
        private const float NavRowLabelWidth = 64f;
        private const float NavRowValueWidth = 88f;
        private const float NavRowButtonWidth = 36f;
        private const float NavRowWideButtonWidth = 44f;
        private const float StepNavBarHeightPx = 186f;
        private const float StepNavBarBottomInsetPx = 6f;
        private const float ContextToggleWidth = 220f;
        private static readonly Color ButtonBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color ValueBackground = new Color(0.12f, 0.20f, 0.34f, 1f);
        private static readonly Color LabelColor = new Color(0.82f, 0.86f, 0.92f, 1f);
        private static readonly Color ValueTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);

        public static Option10DualPaneViewUiHandles EnsureOnPanel(
            Transform panelRoot,
            Option10DualPaneMdsAttractorViewController viewController,
            AppMenuController menuController)
        {
            if (panelRoot == null || viewController == null)
            {
                return null;
            }

            DualPaneViewUiHandles baseHandles = DualPaneViewUiFactory.EnsureOnPanel(
                panelRoot,
                viewController,
                menuController);
            if (baseHandles == null)
            {
                return null;
            }

            CustomizeLeftPane(panelRoot);
            ResizeControlsPanelForOption10(panelRoot);
            PlateauDriftMetricsPanel.EnsureOnPanel(panelRoot);
            PlateauDriftAttractorSummaryPanel.EnsureOnPanel(panelRoot);
            RemoveTimelineChart(panelRoot);

            Option10LeftLegendShelfTabs leftShelfTabs = Option10LeftLegendShelfTabs.EnsureOnPanel(panelRoot);
            if (leftShelfTabs != null)
            {
                leftShelfTabs.Bind(panelRoot.gameObject);
            }

            GenomeMdsPathLegend.SetVisible(panelRoot, false);
            Option10LeftLegendShelfTabs.RefreshShelfLayout(panelRoot);

            Option10PlateauDriftLinkedExplorer explorer = EnsurePlateauDriftExplorer(
                panelRoot,
                viewController);

            DualPaneViewUiFactory.SyncRightPaneShelfLayout(panelRoot, viewController.AttractorVisualiser);

            return new Option10DualPaneViewUiHandles
            {
                Root = panelRoot.gameObject,
                Explorer = explorer,
                LeftZoomSlider = baseHandles.LeftZoomSlider,
                RightZoomSlider = baseHandles.RightZoomSlider,
            };
        }

        public static void SetVisible(Option10DualPaneViewUiHandles handles, bool visible)
        {
            if (handles?.Root != null)
            {
                handles.Root.SetActive(visible);
                if (visible)
                {
                    DualPaneViewUiFactory.BringShelfTabBarsToFront(handles.Root.transform);
                }
            }

            if (!visible)
            {
                handles?.Explorer?.ResetExplorer();
            }
        }

        private static void RemoveLegacyLeftShelfTabs(Transform panelRoot)
        {
            Transform legacyTabs = panelRoot.Find(DualPaneLeftLegendShelfTabs.RootName);
            if (legacyTabs == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(legacyTabs.gameObject);
                return;
            }
#endif
            Object.Destroy(legacyTabs.gameObject);
        }

        private static void ResizeControlsPanelForOption10(Transform panelRoot)
        {
            Transform controlsRoot = panelRoot.Find("DualPaneControls");
            if (controlsRoot == null)
            {
                return;
            }

            const float panelPadding = 12f;
            float controlsPanelHeight = StepNavBarHeightPx + panelPadding;
            RectTransform rectTransform = controlsRoot.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(NavRowWidth + 40f, controlsPanelHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        private static void HideControlInfoLabel(Text infoLabel)
        {
            if (infoLabel == null)
            {
                return;
            }

            infoLabel.text = string.Empty;
            infoLabel.gameObject.SetActive(false);
        }

        private static void CustomizeLeftPane(Transform panelRoot)
        {
            SetTitleText(panelRoot, "LeftPaneTitle", "Plateau Drift View");
            SetTitleText(
                panelRoot,
                "ViewTitle",
                "Plateau drift: local MDS segment + linked attractor step");
            RemoveLegacyLeftShelfTabs(panelRoot);
            Option10LeftLegendShelfTabs.SetVisible(panelRoot, true);
            GenomeMdsPathLegend.SetVisible(panelRoot, false);
            Transform factorPanel = panelRoot.Find("FactorScorePanel");
            if (factorPanel != null)
            {
                factorPanel.gameObject.SetActive(false);
            }
        }

        private static Option10PlateauDriftLinkedExplorer EnsurePlateauDriftExplorer(
            Transform panelRoot,
            Option10DualPaneMdsAttractorViewController viewController)
        {
            Transform controlsRoot = panelRoot.Find("DualPaneControls");
            if (controlsRoot == null)
            {
                return null;
            }

            Option10PlateauDriftLinkedExplorer explorer =
                controlsRoot.GetComponent<Option10PlateauDriftLinkedExplorer>();
            if (explorer == null)
            {
                explorer = controlsRoot.gameObject.AddComponent<Option10PlateauDriftLinkedExplorer>();
            }

            Text infoLabel = controlsRoot.Find("InfoLabel")?.GetComponent<Text>();
            Transform navBar = controlsRoot.Find("StepNavBar");
            Button contextToggle = navBar?.Find("ContextPathToggleButton")?.GetComponent<Button>();
            Transform buttonsRow = navBar?.Find("StepButtonsRow");

            explorer.Configure(
                viewController.PlateauDriftVisualiser,
                viewController.AttractorVisualiser,
                viewController.LeftSceneCamera,
                viewController.RightSceneCamera,
                infoLabel);
            explorer.ConfigureContextPathToggle(contextToggle);
            explorer.ConfigureViewController(viewController);
            HideControlInfoLabel(infoLabel);

            if (navBar != null)
            {
                ConfigurePlateauDriftNavBar(navBar, explorer);
            }

            HideStepButtonsRow(buttonsRow);

            explorer.SyncNavValueLabels();
            if (controlsRoot != null)
            {
                TrajectoryViewUiFactory.BringInteractiveControlsToFront(controlsRoot.gameObject);
            }

            return explorer;
        }

        private static void HideStepButtonsRow(Transform buttonsRow)
        {
            if (buttonsRow == null)
            {
                return;
            }

            buttonsRow.gameObject.SetActive(false);
        }

        private static void ConfigurePlateauDriftNavBar(
            Transform navBar,
            Option10PlateauDriftLinkedExplorer explorer)
        {
            RemoveLegacyNavInputs(navBar);
            CleanupPlateauDriftNavRows(navBar);
            ApplyOption10StepNavBarLayout(navBar);

            Text runValue = BuildFixedNavRow(
                navBar,
                "RunNavRow",
                "Run",
                rowIndex: 0,
                explorer.GoToFirstRun,
                explorer.GoToPreviousRun,
                explorer.GoToNextRun,
                explorer.GoToLastRun);
            Text episodeValue = BuildFixedNavRow(
                navBar,
                "EpisodeNavRow",
                "Episode",
                rowIndex: 1,
                explorer.GoToFirstEpisode,
                explorer.GoToPreviousEpisode,
                explorer.GoToNextEpisode,
                explorer.GoToLastEpisode);
            Text stepValue = BuildFixedNavRow(
                navBar,
                "StepNavRow",
                "Step",
                rowIndex: 2,
                explorer.GoToFirstStep,
                explorer.GoToPreviousStep,
                explorer.GoToNextStep,
                explorer.GoToLastStep);
            explorer.ConfigureNavValueLabels(runValue, episodeValue, stepValue);
            LayoutContextPathToggle(navBar);
        }

        private static void CleanupPlateauDriftNavRows(Transform navBar)
        {
            DestroyChildIfPresent(navBar, "RunNavRow");
            DestroyChildIfPresent(navBar, "EpisodeNavRow");
            DestroyChildIfPresent(navBar, "StepNavRow");
            DestroyChildIfPresent(navBar, "RunNavRowValue");
            DestroyChildIfPresent(navBar, "EpisodeNavRowValue");
            DestroyChildIfPresent(navBar, "StepNavRowValue");

            RemoveLayoutGroup<VerticalLayoutGroup>(navBar);
            RemoveLayoutGroup<HorizontalLayoutGroup>(navBar);
        }

        private static void RemoveLayoutGroup<T>(Transform target) where T : Component
        {
            if (target == null)
            {
                return;
            }

            T layout = target.GetComponent<T>();
            if (layout != null)
            {
                Object.Destroy(layout);
            }
        }

        private static void RemoveLegacyNavInputs(Transform navBar)
        {
            DestroyChildIfPresent(navBar, "StepInputField");
            DestroyChildIfPresent(navBar, "RunInputField");
        }

        private static void ApplyOption10StepNavBarLayout(Transform navBar)
        {
            RectTransform rectTransform = navBar.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, StepNavBarBottomInsetPx);
            rectTransform.sizeDelta = new Vector2(NavRowWidth, StepNavBarHeightPx);
        }

        private static Text BuildFixedNavRow(
            Transform navBar,
            string rowName,
            string labelText,
            int rowIndex,
            UnityEngine.Events.UnityAction onFirst,
            UnityEngine.Events.UnityAction onPrevious,
            UnityEngine.Events.UnityAction onNext,
            UnityEngine.Events.UnityAction onLast)
        {
            GameObject rowObject = new GameObject(rowName, typeof(RectTransform));
            rowObject.transform.SetParent(navBar, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 1f);
            rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -(4f + rowIndex * (NavRowHeight + 6f)));
            rowRect.sizeDelta = new Vector2(NavRowWidth, NavRowHeight);

            float leftEdge = 0f;
            CreateFixedNavLabel(rowObject.transform, $"{rowName}Label", labelText, leftEdge, NavRowLabelWidth);
            float x = leftEdge + NavRowLabelWidth + 4f;
            x = CreateFixedNavButton(rowObject.transform, "FirstButton", "<<", x, NavRowWideButtonWidth, onFirst);
            x += NavRowWideButtonWidth + 4f;
            x = CreateFixedNavButton(rowObject.transform, "PreviousButton", "<", x, NavRowButtonWidth, onPrevious);
            x += NavRowButtonWidth + 4f;
            Text valueLabel = CreateFixedNavValue(rowObject.transform, $"{rowName}Value", x);
            x += NavRowValueWidth + 4f;
            x = CreateFixedNavButton(rowObject.transform, "NextButton", ">", x, NavRowButtonWidth, onNext);
            x += NavRowButtonWidth + 4f;
            CreateFixedNavButton(rowObject.transform, "LastButton", ">>", x, NavRowWideButtonWidth, onLast);
            return valueLabel;
        }

        private static void CreateFixedNavLabel(
            Transform parent,
            string name,
            string text,
            float x,
            float width)
        {
            GameObject labelObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(parent, false);
            ConfigureLeftAnchoredRect(labelObject.GetComponent<RectTransform>(), x, width, NavRowHeight);

            Text label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = LabelColor;
            label.raycastTarget = false;
        }

        private static Text CreateFixedNavValue(Transform parent, string name, float x)
        {
            GameObject valueObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            valueObject.transform.SetParent(parent, false);
            ConfigureLeftAnchoredRect(valueObject.GetComponent<RectTransform>(), x, NavRowValueWidth, NavRowHeight);

            Image background = valueObject.GetComponent<Image>();
            background.color = ValueBackground;
            background.raycastTarget = false;

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
            value.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            value.text = "1/1";
            value.fontSize = 18;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleCenter;
            value.color = ValueTextColor;
            value.raycastTarget = false;
            return value;
        }

        private static float CreateFixedNavButton(
            Transform parent,
            string name,
            string label,
            float x,
            float width,
            UnityEngine.Events.UnityAction onClick)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            ConfigureLeftAnchoredRect(buttonObject.GetComponent<RectTransform>(), x, width, NavRowHeight);

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonBackground;
            image.raycastTarget = true;

            Text buttonLabel = buttonObject.GetComponentInChildren<Text>();
            if (buttonLabel != null)
            {
                buttonLabel.text = label;
                buttonLabel.fontSize = 16;
                buttonLabel.fontStyle = FontStyle.Bold;
                buttonLabel.alignment = TextAnchor.MiddleCenter;
                buttonLabel.color = Color.white;
                buttonLabel.raycastTarget = false;
            }

            Button button = buttonObject.GetComponent<Button>();
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            return x;
        }

        private static void ConfigureLeftAnchoredRect(
            RectTransform rectTransform,
            float x,
            float width,
            float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(x, 0f);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void LayoutContextPathToggle(Transform navBar)
        {
            Transform toggle = navBar.Find("ContextPathToggleButton");
            if (toggle == null)
            {
                return;
            }

            RemoveLayoutGroup<LayoutElement>(toggle);
            RectTransform rectTransform = toggle.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 4f);
            rectTransform.sizeDelta = new Vector2(ContextToggleWidth, NavRowHeight);
            toggle.SetAsLastSibling();
        }

        private static void DestroyChildIfPresent(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(child.gameObject);
                return;
            }
#endif
            Object.Destroy(child.gameObject);
        }

        private static void SetTitleText(Transform panelRoot, string objectName, string text)
        {
            Transform titleTransform = panelRoot.Find(objectName);
            if (titleTransform != null && titleTransform.TryGetComponent(out Text title))
            {
                title.text = text;
            }
        }

        private static void RemoveTimelineChart(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform existing = panelRoot.Find("PlateauDriftTimelineChart");
            if (existing == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(existing.gameObject);
                return;
            }
#endif
            Object.Destroy(existing.gameObject);
        }
    }
}
