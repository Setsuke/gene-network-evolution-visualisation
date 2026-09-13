using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Option 7 controls on the genome embedding UI panel.
    /// </summary>
    public class BatchGenomeMdsPathViewControls : MonoBehaviour
    {
        private const string GreyNodesToggleButtonName = "HideGreyNodesToggleButton";
        private const string PathConnectionsToggleButtonName = "HidePathConnectionsToggleButton";
        private const string AxesToggleButtonName = "HideAxesToggleButton";
        private const string DriftCategoriesToggleButtonName = "BatchShowDriftCategoriesToggleButton";
        private const string DriftingGenomesToggleButtonName = "BatchShowDriftingGenomesToggleButton";
        private static readonly Color ButtonBackground = new Color(0.16f, 0.34f, 0.62f, 1f);
        private static readonly Color ActiveButtonBackground = new Color(0.24f, 0.52f, 0.88f, 1f);

        [SerializeField] private Button greyNodesToggleButton;
        [SerializeField] private Button pathConnectionsToggleButton;
        [SerializeField] private Button axesToggleButton;
        [SerializeField] private Button driftCategoriesToggleButton;
        [SerializeField] private Button driftingGenomesToggleButton;
        private BatchGenomeMdsPathVisualiser visualiser;
        private Transform legendPanelRoot;

        public static void EnsureOnPanel(GameObject panelRoot, BatchGenomeMdsPathVisualiser targetVisualiser)
        {
            if (panelRoot == null || targetVisualiser == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out BatchGenomeMdsPathViewControls controls))
            {
                controls = panelRoot.AddComponent<BatchGenomeMdsPathViewControls>();
            }

            controls.EnsureBuilt();
            controls.BindVisualiser(panelRoot.transform, targetVisualiser);
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            SetButtonVisible(panelRoot, GreyNodesToggleButtonName, visible);
            SetButtonVisible(panelRoot, PathConnectionsToggleButtonName, visible);
            SetButtonVisible(panelRoot, AxesToggleButtonName, visible);
            SetButtonVisible(panelRoot, DriftCategoriesToggleButtonName, visible);
            SetButtonVisible(panelRoot, DriftingGenomesToggleButtonName, visible);

            if (!visible && panelRoot.TryGetComponent(out BatchGenomeMdsPathViewControls controls))
            {
                controls.ResetViewMode();
            }
        }

        public void ResetViewMode()
        {
            visualiser?.SetShowDriftCategories(false);
            visualiser?.SetShowDriftingGenomes(false);
            SyncToggleLabels();
        }

        private static void SetButtonVisible(Transform panelRoot, string buttonName, bool visible)
        {
            Transform toggleTransform = panelRoot.Find(buttonName);
            if (toggleTransform != null)
            {
                toggleTransform.gameObject.SetActive(visible);
            }
        }

        private void EnsureBuilt()
        {
            greyNodesToggleButton ??= FindToggleButton(GreyNodesToggleButtonName);
            pathConnectionsToggleButton ??= FindToggleButton(PathConnectionsToggleButtonName);
            axesToggleButton ??= FindToggleButton(AxesToggleButtonName);
            driftCategoriesToggleButton ??= FindToggleButton(DriftCategoriesToggleButtonName);
            driftingGenomesToggleButton ??= FindToggleButton(DriftingGenomesToggleButtonName);

            if (greyNodesToggleButton == null)
            {
                greyNodesToggleButton = CreateToggleButton(
                    transform,
                    GreyNodesToggleButtonName,
                    "Hide grey nodes",
                    new Vector2(24f, 72f));
            }

            if (pathConnectionsToggleButton == null)
            {
                pathConnectionsToggleButton = CreateToggleButton(
                    transform,
                    PathConnectionsToggleButtonName,
                    "Hide connections",
                    new Vector2(24f, 112f));
            }

            if (axesToggleButton == null)
            {
                axesToggleButton = CreateToggleButton(
                    transform,
                    AxesToggleButtonName,
                    "Hide axes",
                    new Vector2(24f, 152f));
            }

            if (driftCategoriesToggleButton == null)
            {
                driftCategoriesToggleButton = CreateToggleButton(
                    transform,
                    DriftCategoriesToggleButtonName,
                    "Show drift categories",
                    new Vector2(24f, 192f));
            }

            if (driftingGenomesToggleButton == null)
            {
                driftingGenomesToggleButton = CreateToggleButton(
                    transform,
                    DriftingGenomesToggleButtonName,
                    "Show drifting genomes",
                    new Vector2(24f, 232f));
            }

            TrajectoryViewUiFactory.BringInteractiveControlsToFront(greyNodesToggleButton.gameObject);
            TrajectoryViewUiFactory.BringInteractiveControlsToFront(pathConnectionsToggleButton.gameObject);
            TrajectoryViewUiFactory.BringInteractiveControlsToFront(axesToggleButton.gameObject);
            TrajectoryViewUiFactory.BringInteractiveControlsToFront(driftCategoriesToggleButton.gameObject);
            TrajectoryViewUiFactory.BringInteractiveControlsToFront(driftingGenomesToggleButton.gameObject);
        }

        private Button FindToggleButton(string buttonName)
        {
            Transform existing = transform.Find(buttonName);
            return existing != null ? existing.GetComponent<Button>() : null;
        }

        private void BindVisualiser(Transform panelRootTransform, BatchGenomeMdsPathVisualiser targetVisualiser)
        {
            visualiser = targetVisualiser;
            legendPanelRoot = panelRootTransform;

            if (greyNodesToggleButton != null)
            {
                greyNodesToggleButton.onClick.RemoveAllListeners();
                greyNodesToggleButton.onClick.AddListener(ToggleGreyNodesVisible);
            }

            if (pathConnectionsToggleButton != null)
            {
                pathConnectionsToggleButton.onClick.RemoveAllListeners();
                pathConnectionsToggleButton.onClick.AddListener(TogglePathConnectionsVisible);
            }

            if (axesToggleButton != null)
            {
                axesToggleButton.onClick.RemoveAllListeners();
                axesToggleButton.onClick.AddListener(ToggleAxesVisible);
            }

            if (driftCategoriesToggleButton != null)
            {
                driftCategoriesToggleButton.onClick.RemoveAllListeners();
                driftCategoriesToggleButton.onClick.AddListener(ToggleDriftCategoriesVisible);
            }

            if (driftingGenomesToggleButton != null)
            {
                driftingGenomesToggleButton.onClick.RemoveAllListeners();
                driftingGenomesToggleButton.onClick.AddListener(ToggleDriftingGenomesVisible);
            }

            ResetViewMode();
        }

        private void ToggleGreyNodesVisible()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.ToggleIntermediateNodesVisible();
            SyncToggleLabels();
        }

        private void TogglePathConnectionsVisible()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.TogglePathConnectionsVisible();
            SyncToggleLabels();
        }

        private void ToggleAxesVisible()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.ToggleAxesVisible();
            SyncToggleLabels();
        }

        private void ToggleDriftCategoriesVisible()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.ToggleDriftCategories();
            SyncToggleLabels();
        }

        private void ToggleDriftingGenomesVisible()
        {
            if (visualiser == null)
            {
                return;
            }

            visualiser.ToggleDriftingGenomes();
            SyncToggleLabels();
        }

        private void SyncToggleLabels()
        {
            SyncGreyNodesLabel();
            SyncPathConnectionsLabel();
            SyncAxesLabel();
            SyncDriftCategoriesLabel();
            SyncDriftingGenomesLabel();
            SyncLegends();
        }

        private void SyncGreyNodesLabel()
        {
            if (greyNodesToggleButton == null)
            {
                return;
            }

            Text label = greyNodesToggleButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            bool visible = visualiser == null || visualiser.IntermediateNodesVisible;
            label.text = visible ? "Hide grey nodes" : "Show grey nodes";
        }

        private void SyncPathConnectionsLabel()
        {
            if (pathConnectionsToggleButton == null)
            {
                return;
            }

            Text label = pathConnectionsToggleButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            bool visible = visualiser == null || visualiser.PathConnectionsVisible;
            label.text = visible ? "Hide connections" : "Show connections";
        }

        private void SyncAxesLabel()
        {
            if (axesToggleButton == null)
            {
                return;
            }

            Text label = axesToggleButton.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            bool visible = visualiser == null || visualiser.AxesVisible;
            label.text = visible ? "Hide axes" : "Show axes";
        }

        private void SyncDriftCategoriesLabel()
        {
            if (driftCategoriesToggleButton == null)
            {
                return;
            }

            Text label = driftCategoriesToggleButton.GetComponentInChildren<Text>();
            Image image = driftCategoriesToggleButton.GetComponent<Image>();
            if (label == null)
            {
                return;
            }

            bool active = visualiser != null && visualiser.ShowDriftCategories;
            label.text = active ? "Show all connections" : "Show drift categories";
            if (image != null)
            {
                image.color = active ? ActiveButtonBackground : ButtonBackground;
            }
        }

        private void SyncDriftingGenomesLabel()
        {
            if (driftingGenomesToggleButton == null)
            {
                return;
            }

            Text label = driftingGenomesToggleButton.GetComponentInChildren<Text>();
            Image image = driftingGenomesToggleButton.GetComponent<Image>();
            if (label == null)
            {
                return;
            }

            bool active = visualiser != null && visualiser.ShowDriftingGenomes;
            label.text = active ? "Show default genomes" : "Show drifting genomes";
            if (image != null)
            {
                image.color = active ? ActiveButtonBackground : ButtonBackground;
            }
        }

        private void SyncLegends()
        {
            if (legendPanelRoot == null)
            {
                return;
            }

            bool driftCategoriesMode = visualiser != null && visualiser.ShowDriftCategories;
            bool driftingGenomesMode = visualiser != null && visualiser.ShowDriftingGenomes;
            SharedGenomeMdsPathLegend.SetBatchVisible(
                legendPanelRoot,
                !driftCategoriesMode && !driftingGenomesMode);
            SharedGenomeMdsPathLegend.SetBatchDriftCategoriesVisible(legendPanelRoot, driftCategoriesMode);
            SharedGenomeMdsPathLegend.SetBatchDriftingGenomesVisible(legendPanelRoot, driftingGenomesMode);
        }

        private static Button CreateToggleButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = name;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(220f, 36f);

            Text labelText = buttonObject.GetComponentInChildren<Text>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonBackground;

            return buttonObject.GetComponent<Button>();
        }
    }
}
