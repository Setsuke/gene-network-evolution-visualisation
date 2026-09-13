using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Factor-score grid in a separate box below the three-column colour legend (Option 9 left pane).
    /// </summary>
    public class GenomeMdsPathFactorScorePanel : MonoBehaviour
    {
        public const string PanelName = "MdsPathFactorScorePanel";

        [SerializeField] private GenomeMdsFitnessPathFactorCircles factorCircles;

        public static GenomeMdsPathFactorScorePanel EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return null;
            }

            Transform existing = panelRoot.Find(PanelName);
            if (existing != null)
            {
                GenomeMdsPathFactorScorePanel panel =
                    existing.GetComponent<GenomeMdsPathFactorScorePanel>();
                if (panel != null)
                {
                    panel.EnsureWired();
                    return panel;
                }
            }

            GameObject panelObject = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(GenomeMdsPathFactorScorePanel));
            panelObject.transform.SetParent(panelRoot, false);

            Image background = panelObject.GetComponent<Image>();
            background.raycastTarget = false;
            DualPaneLegendShelfStyle.SetFlat(panelObject.transform);

            GameObject circlesRoot = new GameObject(
                "FactorCircles",
                typeof(RectTransform),
                typeof(GenomeMdsFitnessPathFactorCircles));
            circlesRoot.transform.SetParent(panelObject.transform, false);

            RectTransform circlesRect = circlesRoot.GetComponent<RectTransform>();
            float pad = DualPaneLegendShelfStyle.FactorScoreBoxPaddingPx;
            circlesRect.anchorMin = Vector2.zero;
            circlesRect.anchorMax = Vector2.one;
            circlesRect.offsetMin = new Vector2(pad, pad);
            circlesRect.offsetMax = new Vector2(-pad, -pad);

            GenomeMdsPathFactorScorePanel panelComponent =
                panelObject.GetComponent<GenomeMdsPathFactorScorePanel>();
            panelComponent.factorCircles = circlesRoot.GetComponent<GenomeMdsFitnessPathFactorCircles>();
            panelComponent.gameObject.SetActive(false);
            return panelComponent;
        }

        public static void PositionInFactorScoreBox(Transform factorPanelTransform, Rect boxNorm)
        {
            if (factorPanelTransform == null
                || !factorPanelTransform.TryGetComponent(out RectTransform panelRect))
            {
                return;
            }

            DualPaneLayout.ApplyNormalizedRect(panelRect, boxNorm);
            DualPaneLegendShelfStyle.SetFlat(factorPanelTransform);

            Transform circlesRoot = factorPanelTransform.Find("FactorCircles");
            if (circlesRoot != null && circlesRoot.TryGetComponent(out RectTransform circlesRect))
            {
                float pad = DualPaneLegendShelfStyle.FactorScoreBoxPaddingPx;
                circlesRect.offsetMin = new Vector2(pad, pad);
                circlesRect.offsetMax = new Vector2(-pad, -pad);
            }
        }

        public static void ConfigureFromVisualiser(Transform panelRoot, GenomeMdsPathVisualiser visualiser)
        {
            if (panelRoot == null)
            {
                return;
            }

            GenomeMdsPathFactorScorePanel panel = EnsureOnPanel(panelRoot);
            if (panel == null)
            {
                return;
            }

            int geneCount = visualiser != null ? visualiser.GeneCount : 0;
            int contextCount = visualiser != null ? visualiser.ContextCount : 0;
            string[] contextTargets = visualiser != null ? visualiser.ContextTargets : null;
            panel.ConfigureLayout(geneCount, contextCount, contextTargets);

            if (visualiser != null && visualiser.HasActiveRun)
            {
                int stepIndex = visualiser.HighlightedStepIndex >= 0
                    ? visualiser.HighlightedStepIndex
                    : 0;
                UpdateFromVisualiser(panelRoot, visualiser, stepIndex);
            }
        }

        public static void UpdateFromVisualiser(
            Transform panelRoot,
            GenomeMdsPathVisualiser visualiser,
            int stepIndex)
        {
            if (panelRoot == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            GenomeMdsPathFactorScorePanel panel = EnsureOnPanel(panelRoot);
            if (panel == null || panel.factorCircles == null || !panel.factorCircles.IsConfigured)
            {
                return;
            }

            if (!visualiser.TryGetStepData(stepIndex, out _, out float[] factorScores))
            {
                panel.factorCircles.UpdateScores(null);
                return;
            }

            panel.factorCircles.UpdateScores(factorScores);
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform panelTransform = panelRoot.Find(PanelName);
            if (panelTransform != null)
            {
                panelTransform.gameObject.SetActive(visible);
            }
        }

        private void EnsureWired()
        {
            if (factorCircles == null)
            {
                Transform circlesRoot = transform.Find("FactorCircles");
                if (circlesRoot != null)
                {
                    factorCircles = circlesRoot.GetComponent<GenomeMdsFitnessPathFactorCircles>();
                }
            }
        }

        private void ConfigureLayout(int geneCount, int contextCount, string[] contextTargets)
        {
            EnsureWired();
            if (factorCircles == null)
            {
                return;
            }

            if (geneCount <= 0 || contextCount <= 0)
            {
                factorCircles.gameObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            if (factorCircles.IsConfigured
                && factorCircles.GeneCount == geneCount
                && factorCircles.ContextCount == contextCount
                && ContextTargetsMatch(factorCircles.ContextTargets, contextTargets))
            {
                factorCircles.gameObject.SetActive(true);
                return;
            }

            factorCircles.ConfigureCellLayout(
                GenomeMdsFitnessPathFactorCircles.DefaultSquareSize,
                GenomeMdsFitnessPathFactorCircles.DefaultSquareSpacing);
            factorCircles.Build(geneCount, contextCount, contextTargets);
            factorCircles.gameObject.SetActive(true);
            DualPaneLegendShelfStyle.ApplyFactorScorePanelTypography(transform);
        }

        private static bool ContextTargetsMatch(string[] left, string[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
