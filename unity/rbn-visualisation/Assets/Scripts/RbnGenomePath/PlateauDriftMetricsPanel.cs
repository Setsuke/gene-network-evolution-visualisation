using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public static class PlateauDriftMetricsPanel
    {
        private static readonly Color TextColor = new Color(0.88f, 0.91f, 0.96f, 1f);

        public static Text EnsureOnPanel(Transform panelRoot)
        {
            Transform existing = panelRoot.Find("PlateauDriftMetricsPanel");
            GameObject panelObject;
            if (existing != null)
            {
                panelObject = existing.gameObject;
            }
            else
            {
                panelObject = new GameObject(
                    "PlateauDriftMetricsPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                panelObject.transform.SetParent(panelRoot, false);
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                Menu.DualPaneLayout.ApplyNormalizedRect(
                    panelRect,
                    Menu.DualPaneLayout.LeftPaneFactorScoreSlot);

                GameObject textObject = new GameObject(
                    "Body",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(panelObject.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(12f, 10f);
                textRect.offsetMax = new Vector2(-12f, -10f);
            }

            ApplyTransparentBackground(panelObject);

            Text body = panelObject.GetComponentInChildren<Text>(true);
            if (body == null)
            {
                GameObject textObject = new GameObject(
                    "Body",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(panelObject.transform, false);
                body = textObject.GetComponent<Text>();
            }

            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 14;
            body.alignment = TextAnchor.UpperLeft;
            body.color = TextColor;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.supportRichText = false;
            body.raycastTarget = false;
            return body;
        }

        public static void UpdateFromEpisode(
            Transform panelRoot,
            PlateauEpisode episode,
            int currentAbsoluteStep,
            AttractorLandscape.BooleanAttractorLandscapeVisualiser attractorVisualiser,
            GenomePathPointData currentPoint = null,
            int activeRunIndex = -1,
            int runCount = -1)
        {
            Text body = EnsureOnPanel(panelRoot);
            if (episode == null)
            {
                body.text = "No plateau episodes detected.";
                return;
            }

            int transitionsChanged = 0;
            if (attractorVisualiser != null && attractorVisualiser.HasActiveRun)
            {
                transitionsChanged = AttractorLandscape.AttractorTransitionChangeCounter.CountChangedTransitionsAtStep(
                    attractorVisualiser.ActiveRun,
                    currentAbsoluteStep);
            }

            string contextFitnessLine = "Context fitness: —";
            if (currentPoint != null
                && ContextFitnessUtility.TryGetContextFitness(currentPoint, out float context1, out float context2))
            {
                contextFitnessLine = ContextFitnessUtility.FormatPairLine(context1, context2);
            }

            int displayRunIndex = activeRunIndex >= 0
                ? activeRunIndex
                : attractorVisualiser != null
                    ? attractorVisualiser.ActiveRunIndex
                    : 0;
            int displayRunCount = runCount > 0
                ? runCount
                : attractorVisualiser != null
                    ? attractorVisualiser.RunCount
                    : 0;

            body.text =
                $"Run {displayRunIndex + 1}/{displayRunCount}\n" +
                $"Plateau episode {episode.EpisodeIndex + 1}\n" +
                $"Steps {episode.StartStep + 1}–{episode.EndStep + 1} drift, exit step {episode.ExitStep + 1}\n" +
                $"Current step {currentAbsoluteStep + 1}\n" +
                $"Transitions changed at step: {transitionsChanged}\n\n" +
                $"Plateau fitness: {episode.PlateauFitness:F4}\n" +
                $"{contextFitnessLine}\n" +
                $"Neutral accepted moves: {episode.NeutralMoveCount}\n" +
                $"Exit type: {episode.ExitType}";
        }

        private static void ApplyTransparentBackground(GameObject panelObject)
        {
            if (panelObject != null
                && panelObject.TryGetComponent(out Image background))
            {
                background.color = Color.clear;
                background.raycastTarget = false;
            }
        }
    }
}
