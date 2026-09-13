using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public static class PlateauDriftAttractorSummaryPanel
    {
        private static readonly Color TextColor = new Color(0.88f, 0.91f, 0.96f, 1f);

        public static Text EnsureOnPanel(Transform panelRoot)
        {
            Transform existing = panelRoot.Find("PlateauDriftAttractorSummaryPanel");
            GameObject panelObject;
            if (existing != null)
            {
                panelObject = existing.gameObject;
            }
            else
            {
                panelObject = new GameObject(
                    "PlateauDriftAttractorSummaryPanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                panelObject.transform.SetParent(panelRoot, false);
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                Menu.DualPaneLayout.ApplyNormalizedRect(
                    panelRect,
                    Menu.DualPaneLayout.LeftPaneLegendSlot);

                GameObject textObject = new GameObject(
                    "Body",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(panelObject.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(10f, 8f);
                textRect.offsetMax = new Vector2(-10f, -8f);
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
            body.fontSize = 13;
            body.alignment = TextAnchor.UpperLeft;
            body.color = TextColor;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.supportRichText = true;
            body.raycastTarget = false;
            return body;
        }

        public static void UpdateFromEpisode(
            Transform panelRoot,
            PlateauEpisode episode,
            AttractorLandscape.BooleanAttractorLandscapeVisualiser attractorVisualiser)
        {
            Text body = EnsureOnPanel(panelRoot);
            if (episode == null)
            {
                body.text = "No plateau episode selected.";
                return;
            }

            if (attractorVisualiser == null || !attractorVisualiser.HasActiveRun)
            {
                body.text = "Attractor data not loaded.";
                return;
            }

            body.text = AttractorLandscape.PlateauDriftAttractorSummaryFormatter.FormatSummary(
                episode,
                attractorVisualiser.ActiveRun,
                attractorVisualiser.ResolveStateLabel);
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
