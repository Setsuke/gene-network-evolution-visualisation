using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public sealed class BatchGenomeMdsFitnessPathUiHandles
    {
        public GameObject Root;
        public BatchGenomeMdsFitnessPathViewControls Controls;
    }

    public static class BatchGenomeMdsFitnessPathUiFactory
    {
        private const string ControlsRootName = "BatchGenomeMdsFitnessPathControls";
        private const float TopRightMargin = 24f;
        private const float PanelWidth = 300f;
        private const float ButtonHeight = 36f;
        private const float PanelPadding = 8f;
        private static readonly Color PanelBackground = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        private static readonly Color ButtonBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color ActiveButtonBackground = new Color(0.24f, 0.52f, 0.88f, 1f);

        public static BatchGenomeMdsFitnessPathUiHandles EnsureOnPanel(
            Transform panelRoot,
            BatchGenomeMdsFitnessPathVisualiser visualiser)
        {
            if (panelRoot == null)
            {
                return null;
            }

            GameObject controlsRoot = EnsureControlsRoot(panelRoot);
            BatchGenomeMdsFitnessPathViewControls controls = EnsureControlsComponent(controlsRoot);
            Button moveTypesButton = EnsureActionButton(
                controlsRoot.transform,
                "ToggleMoveClassificationButton",
                "Show move types");
            Button neutralOnlyButton = EnsureActionButton(
                controlsRoot.transform,
                "ShowNeutralMovesButton",
                "Show only neutral moves");
            Button improvementOnlyButton = EnsureActionButton(
                controlsRoot.transform,
                "ShowImprovementMovesButton",
                "Show only improvement moves");
            Button driftMovesButton = EnsureActionButton(
                controlsRoot.transform,
                "ShowDriftMovesButton",
                "Show drift moves");
            Button driftMovesByCategoryButton = EnsureActionButton(
                controlsRoot.transform,
                "ShowDriftMovesByCategoryButton",
                "Show drift moves by category");

            controls.Configure(
                visualiser,
                panelRoot,
                moveTypesButton,
                moveTypesButton.GetComponentInChildren<Text>(true),
                neutralOnlyButton,
                improvementOnlyButton,
                driftMovesButton,
                driftMovesByCategoryButton);

            return new BatchGenomeMdsFitnessPathUiHandles
            {
                Root = controlsRoot,
                Controls = controls,
            };
        }

        public static void SetVisible(BatchGenomeMdsFitnessPathUiHandles handles, bool visible)
        {
            if (handles?.Root != null)
            {
                handles.Root.SetActive(visible);
            }

            if (!visible)
            {
                handles?.Controls?.ResetViewMode();
            }
        }

        private static GameObject EnsureControlsRoot(Transform panelRoot)
        {
            Transform existingRoot = panelRoot.Find(ControlsRootName);
            GameObject controlsRoot;
            if (existingRoot != null)
            {
                controlsRoot = existingRoot.gameObject;
                RemoveLegacyHorizontalLayout(controlsRoot);
            }
            else
            {
                controlsRoot = new GameObject(
                    ControlsRootName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                controlsRoot.transform.SetParent(panelRoot, false);
            }

            Image background = controlsRoot.GetComponent<Image>();
            background.color = PanelBackground;
            background.raycastTarget = true;

            EnsureVerticalLayout(controlsRoot);
            ApplyControlsLayout(controlsRoot.GetComponent<RectTransform>());
            return controlsRoot;
        }

        private static void RemoveLegacyHorizontalLayout(GameObject controlsRoot)
        {
            HorizontalLayoutGroup horizontalLayout = controlsRoot.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                Object.Destroy(horizontalLayout);
            }

            Transform legacyButton = controlsRoot.transform.Find("ToggleMoveClassificationButton");
            if (legacyButton != null)
            {
                RectTransform legacyRect = legacyButton.GetComponent<RectTransform>();
                if (legacyRect != null)
                {
                    legacyRect.anchorMin = new Vector2(0f, 1f);
                    legacyRect.anchorMax = new Vector2(1f, 1f);
                    legacyRect.pivot = new Vector2(0.5f, 1f);
                    legacyRect.offsetMin = Vector2.zero;
                    legacyRect.offsetMax = Vector2.zero;
                }
            }
        }

        private static void EnsureVerticalLayout(GameObject controlsRoot)
        {
            VerticalLayoutGroup layout = controlsRoot.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = controlsRoot.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(
                (int)PanelPadding,
                (int)PanelPadding,
                (int)PanelPadding,
                (int)PanelPadding);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ApplyControlsLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return;
            }

            const int buttonCount = 5;
            float panelHeight = PanelPadding * 2f + buttonCount * ButtonHeight + (buttonCount - 1) * 6f;
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-TopRightMargin, -TopRightMargin);
            rectTransform.sizeDelta = new Vector2(PanelWidth, panelHeight);
            rectTransform.SetAsFirstSibling();
        }

        private static BatchGenomeMdsFitnessPathViewControls EnsureControlsComponent(GameObject controlsRoot)
        {
            BatchGenomeMdsFitnessPathViewControls controls =
                controlsRoot.GetComponent<BatchGenomeMdsFitnessPathViewControls>();
            if (controls == null)
            {
                controls = controlsRoot.AddComponent<BatchGenomeMdsFitnessPathViewControls>();
            }

            return controls;
        }

        private static Button EnsureActionButton(Transform parent, string name, string label)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing.TryGetComponent(out Button existingButton))
            {
                StyleActionButton(existing.gameObject, label, false);
                EnsureButtonLayout(existing.gameObject);
                return existingButton;
            }

            Button button = CreateActionButton(name, parent, label);
            return button;
        }

        private static Button CreateActionButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);
            StyleActionButton(buttonObject, label, false);
            EnsureButtonLayout(buttonObject);
            return buttonObject.GetComponent<Button>();
        }

        private static void EnsureButtonLayout(GameObject buttonObject)
        {
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = buttonObject.AddComponent<LayoutElement>();
            }

            layout.minHeight = ButtonHeight;
            layout.preferredHeight = ButtonHeight;
        }

        internal static void StyleActionButton(GameObject buttonObject, string label, bool active)
        {
            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;

            Image image = buttonObject.GetComponent<Image>();
            image.color = active ? ActiveButtonBackground : ButtonBackground;
        }
    }

    public class BatchGenomeMdsFitnessPathViewControls : MonoBehaviour
    {
        private BatchGenomeMdsFitnessPathVisualiser visualiser;
        private Transform panelRoot;
        private Button moveTypesButton;
        private Text moveTypesLabel;
        private Button neutralOnlyButton;
        private Button improvementOnlyButton;
        private Button driftMovesButton;
        private Button driftMovesByCategoryButton;

        public void Configure(
            BatchGenomeMdsFitnessPathVisualiser targetVisualiser,
            Transform legendPanelRoot,
            Button moveClassificationButton,
            Text moveClassificationLabel,
            Button neutralButton,
            Button improvementButton,
            Button driftButton,
            Button driftByCategoryButton)
        {
            visualiser = targetVisualiser;
            panelRoot = legendPanelRoot;
            moveTypesButton = moveClassificationButton;
            moveTypesLabel = moveClassificationLabel;
            neutralOnlyButton = neutralButton;
            improvementOnlyButton = improvementButton;
            driftMovesButton = driftButton;
            driftMovesByCategoryButton = driftByCategoryButton;

            if (moveTypesButton != null)
            {
                moveTypesButton.onClick.RemoveAllListeners();
                moveTypesButton.onClick.AddListener(ToggleMoveClassification);
            }

            if (neutralOnlyButton != null)
            {
                neutralOnlyButton.onClick.RemoveAllListeners();
                neutralOnlyButton.onClick.AddListener(ToggleNeutralOnlyFilter);
            }

            if (improvementOnlyButton != null)
            {
                improvementOnlyButton.onClick.RemoveAllListeners();
                improvementOnlyButton.onClick.AddListener(ToggleImprovementOnlyFilter);
            }

            if (driftMovesButton != null)
            {
                driftMovesButton.onClick.RemoveAllListeners();
                driftMovesButton.onClick.AddListener(ToggleDriftMovesOnly);
            }

            if (driftMovesByCategoryButton != null)
            {
                driftMovesByCategoryButton.onClick.RemoveAllListeners();
                driftMovesByCategoryButton.onClick.AddListener(ToggleDriftMovesByCategory);
            }

            ResetViewMode();
        }

        public void ResetViewMode()
        {
            if (visualiser != null)
            {
                visualiser.SetShowMoveClassification(false);
                visualiser.SetDriftHighlightMode(BatchDriftHighlightMode.Off);
                visualiser.SetMoveVisibilityFilter(BatchPathMoveVisibilityFilter.All);
            }

            SyncUi();
        }

        private void ToggleMoveClassification()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.SetShowMoveClassification(!visualiser.ShowMoveClassification);
            SyncUi();
        }

        private void ToggleNeutralOnlyFilter()
        {
            if (visualiser == null)
            {
                return;
            }

            BatchPathMoveVisibilityFilter nextFilter =
                visualiser.MoveVisibilityFilter == BatchPathMoveVisibilityFilter.NeutralOnly
                    ? BatchPathMoveVisibilityFilter.All
                    : BatchPathMoveVisibilityFilter.NeutralOnly;
            visualiser.SetMoveVisibilityFilter(nextFilter);
            SyncUi();
        }

        private void ToggleImprovementOnlyFilter()
        {
            if (visualiser == null)
            {
                return;
            }

            BatchPathMoveVisibilityFilter nextFilter =
                visualiser.MoveVisibilityFilter == BatchPathMoveVisibilityFilter.ImprovementOnly
                    ? BatchPathMoveVisibilityFilter.All
                    : BatchPathMoveVisibilityFilter.ImprovementOnly;
            visualiser.SetMoveVisibilityFilter(nextFilter);
            SyncUi();
        }

        private void ToggleDriftMovesOnly()
        {
            if (visualiser == null)
            {
                return;
            }

            bool enable = visualiser.DriftHighlightMode != BatchDriftHighlightMode.UnifiedYellow;
            visualiser.SetDriftHighlightMode(
                enable ? BatchDriftHighlightMode.UnifiedYellow : BatchDriftHighlightMode.Off);
            SyncUi();
        }

        private void ToggleDriftMovesByCategory()
        {
            if (visualiser == null)
            {
                return;
            }

            bool enable = visualiser.DriftHighlightMode != BatchDriftHighlightMode.ByCategory;
            visualiser.SetDriftHighlightMode(
                enable ? BatchDriftHighlightMode.ByCategory : BatchDriftHighlightMode.Off);
            SyncUi();
        }

        private void SyncUi()
        {
            bool moveClassificationMode = visualiser != null && visualiser.ShowMoveClassification;
            bool driftMovesMode = visualiser != null && visualiser.ShowDriftMovesOnly;
            bool driftByCategoryMode = visualiser != null && visualiser.ShowDriftMovesByCategory;
            BatchPathMoveVisibilityFilter filter = visualiser != null
                ? visualiser.MoveVisibilityFilter
                : BatchPathMoveVisibilityFilter.All;

            if (moveTypesLabel != null)
            {
                moveTypesLabel.text = moveClassificationMode ? "Show fitness colors" : "Show move types";
            }

            if (moveTypesButton != null)
            {
                BatchGenomeMdsFitnessPathUiFactory.StyleActionButton(
                    moveTypesButton.gameObject,
                    moveClassificationMode ? "Show fitness colors" : "Show move types",
                    moveClassificationMode);
            }

            if (neutralOnlyButton != null)
            {
                BatchGenomeMdsFitnessPathUiFactory.StyleActionButton(
                    neutralOnlyButton.gameObject,
                    filter == BatchPathMoveVisibilityFilter.NeutralOnly
                        ? "Show all moves"
                        : "Show only neutral moves",
                    filter == BatchPathMoveVisibilityFilter.NeutralOnly);
            }

            if (improvementOnlyButton != null)
            {
                BatchGenomeMdsFitnessPathUiFactory.StyleActionButton(
                    improvementOnlyButton.gameObject,
                    filter == BatchPathMoveVisibilityFilter.ImprovementOnly
                        ? "Show all moves"
                        : "Show only improvement moves",
                    filter == BatchPathMoveVisibilityFilter.ImprovementOnly);
            }

            if (driftMovesButton != null)
            {
                BatchGenomeMdsFitnessPathUiFactory.StyleActionButton(
                    driftMovesButton.gameObject,
                    driftMovesMode ? "Show all paths" : "Show drift moves",
                    driftMovesMode);
            }

            if (driftMovesByCategoryButton != null)
            {
                BatchGenomeMdsFitnessPathUiFactory.StyleActionButton(
                    driftMovesByCategoryButton.gameObject,
                    driftByCategoryMode ? "Show all paths" : "Show drift moves by category",
                    driftByCategoryMode);
            }

            if (panelRoot == null)
            {
                return;
            }

            bool driftLegendMode = driftMovesMode || driftByCategoryMode;
            BatchGenomeMdsFitnessPathLegend.SetVisible(panelRoot, !moveClassificationMode && !driftLegendMode);
            BatchGenomeMdsFitnessPathLegend.SetMoveClassificationVisible(panelRoot, moveClassificationMode);
            BatchGenomeMdsFitnessPathLegend.SetDriftMovesVisible(panelRoot, driftMovesMode);
            BatchGenomeMdsFitnessPathLegend.SetDriftMovesByCategoryVisible(panelRoot, driftByCategoryMode);
        }
    }
}
