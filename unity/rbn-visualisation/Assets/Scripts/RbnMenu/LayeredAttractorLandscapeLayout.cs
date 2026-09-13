using UnityEngine;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Normalized layout for Option 11: 3D figure inside a framed viewport above the controls.
    /// </summary>
    public static class LayeredAttractorLandscapeLayout
    {
        public const string FigureFrameName = "LayeredAttractorFigureFrame";

        public const float OuterMarginX = 0.05f;
        public const float OuterMarginTop = 0.17f;
        public const float ControlsBottomPaddingPx = 72f;
        public const float ControlsStackHeightPx = 196f;
        public const float ZoomRowHeightPx = 48f;
        public const float MinimumBottomChromeFraction = 0.28f;

        public static float BottomChromeFraction
        {
            get
            {
                float computed = (ControlsBottomPaddingPx + ControlsStackHeightPx + ZoomRowHeightPx)
                    / Mathf.Max(Screen.height, 1f);
                return Mathf.Max(computed, MinimumBottomChromeFraction);
            }
        }

        private static float OuterMarginBottom => BottomChromeFraction;

        public static Rect FigureViewport => new Rect(
            OuterMarginX,
            OuterMarginBottom,
            1f - OuterMarginX * 2f,
            1f - OuterMarginTop - OuterMarginBottom);

        public static void ApplyTopRightOverlayLayout(
            RectTransform panelRect,
            Rect figureViewport,
            float rightMargin = 16f,
            float topMargin = 12f)
        {
            if (panelRect == null)
            {
                return;
            }

            float anchorX = figureViewport.x + figureViewport.width;
            float anchorY = figureViewport.y + figureViewport.height;
            panelRect.anchorMin = new Vector2(anchorX, anchorY);
            panelRect.anchorMax = new Vector2(anchorX, anchorY);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-rightMargin, -topMargin);
        }
    }
}
