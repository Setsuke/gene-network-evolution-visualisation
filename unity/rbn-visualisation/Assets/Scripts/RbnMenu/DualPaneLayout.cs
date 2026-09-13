using UnityEngine;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Normalized screen layout for Option 9 dual-pane viewports, legends below figures, and zoom anchors.
    /// </summary>
    public static class DualPaneLayout
    {
        public const float OuterMarginX = 0.04f;
        public const float OuterMarginTop = 0.09f;
        public const float PaneGap = 0.03f;
        /// <summary>
        /// Bottom shelf (tabs + legend/readout). Kept compact so figure viewports stay tall.
        /// </summary>
        public const float LegendBelowFraction = 0.36f;
        public const float GapBelowFrame = 0.004f;

        /// <summary>
        /// Shared with <see cref="DualPaneViewUiFactory"/> so pane columns extend down to the controls.
        /// </summary>
        public const float ControlsBottomPaddingPx = 12f;
        public const float ControlsStackHeightPx = 156f;
        public const float ColumnBottomClearancePx = 4f;

        private static float OuterMarginBottom =>
            (ControlsBottomPaddingPx + ControlsStackHeightPx + ColumnBottomClearancePx) / Screen.height;

        public static Rect LeftPaneViewport => BuildPaneFigureViewport(isLeftPane: true);
        public static Rect RightPaneViewport => BuildPaneFigureViewport(isLeftPane: false);
        public static Rect LeftPaneLegendBelow => BuildPaneLegendBelow(isLeftPane: true);
        public static Rect RightPaneLegendBelow => BuildPaneLegendBelow(isLeftPane: false);
        public static Rect RightPaneLegendSlot => BuildRightPaneLegendSlot();
        public static Rect RightPaneReadoutSlot => BuildRightPaneReadoutSlot();
        public static Rect LeftPaneLegendSlot => BuildLeftPaneLegendSlot();
        public static Rect LeftPaneFactorScoreSlot => BuildLeftPaneFactorScoreSlot();

        public static Vector2 LeftPaneTitleAnchor => GetPaneTitleAnchor(isLeftPane: true);
        public static Vector2 RightPaneTitleAnchor => GetPaneTitleAnchor(isLeftPane: false);
        public static Rect LeftPaneZoomRow => BuildPaneZoomRow(isLeftPane: true);
        public static Rect RightPaneZoomRow => BuildPaneZoomRow(isLeftPane: false);

        public static void ApplyNormalizedRect(RectTransform rectTransform, Rect normalizedRect)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(normalizedRect.x, normalizedRect.y);
            rectTransform.anchorMax = new Vector2(
                normalizedRect.x + normalizedRect.width,
                normalizedRect.y + normalizedRect.height);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Rect BuildPaneColumn(bool isLeftPane)
        {
            float paneWidth = (1f - OuterMarginX * 2f - PaneGap) * 0.5f;
            float x = isLeftPane
                ? OuterMarginX
                : OuterMarginX + paneWidth + PaneGap;
            return new Rect(x, OuterMarginBottom, paneWidth, 1f - OuterMarginTop - OuterMarginBottom);
        }

        private static Rect BuildPaneLegendBelow(bool isLeftPane)
        {
            Rect column = BuildPaneColumn(isLeftPane);
            float legendHeight = column.height * LegendBelowFraction;
            return new Rect(column.x, column.y, column.width, legendHeight);
        }

        private static Rect BuildPaneFigureViewport(bool isLeftPane)
        {
            Rect column = BuildPaneColumn(isLeftPane);
            float legendHeight = column.height * LegendBelowFraction;
            float gap = GapBelowFrame;
            return new Rect(
                column.x,
                column.y + legendHeight + gap,
                column.width,
                column.height - legendHeight - gap);
        }

        private static Rect BuildPaneZoomRow(bool isLeftPane)
        {
            Rect figure = BuildPaneFigureViewport(isLeftPane);
            float rowHeight = 0.028f;
            return new Rect(figure.x + 0.008f, figure.y + 0.008f, figure.width - 0.016f, rowHeight);
        }

        private static Rect BuildRightPaneLegendSlot()
        {
            Rect shelf = BuildPaneLegendBelow(isLeftPane: false);
            const float inset = 0.01f;
            const float gap = 0.014f;
            float slotWidth = (shelf.width - inset * 2f - gap) * 0.32f;
            return new Rect(shelf.x + inset, shelf.y + 0.006f, slotWidth, shelf.height - 0.012f);
        }

        private static Rect BuildRightPaneReadoutSlot()
        {
            Rect shelf = BuildPaneLegendBelow(isLeftPane: false);
            const float inset = 0.01f;
            const float gap = 0.014f;
            float legendWidth = (shelf.width - inset * 2f - gap) * 0.32f;
            float readoutX = shelf.x + inset + legendWidth + gap;
            float readoutWidth = shelf.width - inset - legendWidth - gap - inset;
            return new Rect(readoutX, shelf.y + 0.006f, readoutWidth, shelf.height - 0.012f);
        }

        private static Rect BuildLeftPaneLegendSlot()
        {
            Rect shelf = BuildPaneLegendBelow(isLeftPane: true);
            const float inset = 0.01f;
            const float gap = 0.014f;
            float legendWidth = (shelf.width - inset * 2f - gap) * 0.4f;
            return new Rect(shelf.x + inset, shelf.y + 0.006f, legendWidth, shelf.height - 0.012f);
        }

        private static Rect BuildLeftPaneFactorScoreSlot()
        {
            Rect shelf = BuildPaneLegendBelow(isLeftPane: true);
            const float inset = 0.01f;
            const float gap = 0.014f;
            float legendWidth = (shelf.width - inset * 2f - gap) * 0.4f;
            float factorX = shelf.x + inset + legendWidth + gap;
            float factorWidth = shelf.width - inset - legendWidth - gap - inset;
            return new Rect(factorX, shelf.y + 0.006f, factorWidth, shelf.height - 0.012f);
        }

        private static Vector2 GetPaneTitleAnchor(bool isLeftPane)
        {
            Rect pane = BuildPaneColumn(isLeftPane);
            return new Vector2(pane.x + pane.width * 0.5f, 1f - OuterMarginTop * 0.35f);
        }
    }
}
