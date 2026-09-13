using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public static class PlateauDriftLeftShelfLayout
    {
        private const float ColumnGap = 0.014f;
        private const float EdgeInset = 0.01f;
        private const float SummaryWidthFraction = 0.4f;

        public static void ApplyDetailsTab(Transform panelRoot, Rect contentShelf)
        {
            if (panelRoot == null)
            {
                return;
            }

            Rect summarySlot = BuildSummarySlot(contentShelf);
            Rect metricsSlot = BuildMetricsSlot(contentShelf);

            ApplyPanelSlot(panelRoot, "PlateauDriftAttractorSummaryPanel", summarySlot);
            ApplyPanelSlot(panelRoot, "PlateauDriftMetricsPanel", metricsSlot);
            SetDetailsVisible(panelRoot, true);
        }

        public static void SetDetailsVisible(Transform panelRoot, bool visible)
        {
            SetPanelVisible(panelRoot, "PlateauDriftAttractorSummaryPanel", visible);
            SetPanelVisible(panelRoot, "PlateauDriftMetricsPanel", visible);
        }

        private static Rect BuildSummarySlot(Rect contentShelf)
        {
            float summaryWidth = (contentShelf.width - EdgeInset * 2f - ColumnGap) * SummaryWidthFraction;
            return new Rect(
                contentShelf.x + EdgeInset,
                contentShelf.y + 0.006f,
                summaryWidth,
                contentShelf.height - 0.012f);
        }

        private static Rect BuildMetricsSlot(Rect contentShelf)
        {
            float summaryWidth = (contentShelf.width - EdgeInset * 2f - ColumnGap) * SummaryWidthFraction;
            float metricsX = contentShelf.x + EdgeInset + summaryWidth + ColumnGap;
            float metricsWidth = contentShelf.width - EdgeInset - summaryWidth - ColumnGap - EdgeInset;
            return new Rect(
                metricsX,
                contentShelf.y + 0.006f,
                metricsWidth,
                contentShelf.height - 0.012f);
        }

        private static void ApplyPanelSlot(Transform panelRoot, string panelName, Rect slot)
        {
            Transform panelTransform = panelRoot.Find(panelName);
            if (panelTransform == null || !panelTransform.TryGetComponent(out RectTransform panelRect))
            {
                return;
            }

            DualPaneLayout.ApplyNormalizedRect(panelRect, slot);
            if (panelTransform.TryGetComponent(out Image background))
            {
                background.color = Color.clear;
                background.raycastTarget = false;
            }
        }

        private static void SetPanelVisible(Transform panelRoot, string panelName, bool visible)
        {
            Transform panelTransform = panelRoot.Find(panelName);
            if (panelTransform != null)
            {
                panelTransform.gameObject.SetActive(visible);
            }
        }
    }
}
