using RbnVisualisation.GenomePath;
using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Bottom-right legends for Option 5: attractor-cycles view and context-path view.
    /// </summary>
    public class BooleanAttractorLandscapeLegend : MonoBehaviour
    {
        private const string AttractorCyclesLegendName = "AttractorCyclesLegend";
        private const string ContextPathLegendName = "ContextPathLegend";
        private const string ContextAttractorsLegendName = "ContextAttractorsLegend";

        private static readonly Color PointAttractorColor = new Color(0.28f, 0.52f, 0.98f, 1f);
        private static readonly Color LimitCycleNodeColor = new Color(0.98f, 0.58f, 0.12f, 1f);
        private static readonly Color LimitCycleEdgeColor = new Color(0.18f, 0.82f, 0.38f, 0.95f);
        private static readonly Color ContextAttractor1Color = new Color(0.18f, 0.77f, 0.71f, 1f);
        private static readonly Color ContextAttractor2Color = new Color(0.91f, 0.28f, 0.56f, 1f);
        private static readonly Color ContextAttractor1EdgeColor = new Color(0.18f, 0.77f, 0.71f, 0.95f);
        private static readonly Color ContextAttractor2EdgeColor = new Color(0.91f, 0.28f, 0.56f, 0.95f);
        private static readonly Color InitialNodeColor = new Color(0.2f, 0.78f, 0.95f, 1f);
        private static readonly Color TargetNodeColor = new Color(0.92f, 0.22f, 0.22f, 1f);
        private static readonly Color PathNodeColor = new Color(0.98f, 0.82f, 0.18f, 1f);
        private static readonly Color PathEdgeColor = new Color(0.98f, 0.82f, 0.18f, 0.95f);
        private static readonly Color StateTransitionEdgeColor = new Color(0.72f, 0.74f, 0.78f, 0.95f);

        [SerializeField] private float rowHeight = 28f;
        [SerializeField] private float swatchSize = 16f;
        [SerializeField] private float lineSwatchWidth = 36f;
        [SerializeField] private float lineSwatchHeight = 4f;
        [SerializeField] private float panelWidth = 340f;
        [SerializeField] private float bottomMargin = 24f;
        [SerializeField] private float rightMargin = 24f;

        private bool built;

        public static void EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeLegend legend = panelRoot.GetComponent<BooleanAttractorLandscapeLegend>();
            if (legend == null)
            {
                legend = panelRoot.gameObject.AddComponent<BooleanAttractorLandscapeLegend>();
            }

            legend.EnsureBuilt();
        }

        public static void SetAttractorCyclesLegendVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, AttractorCyclesLegendName, visible);
        }

        public static void SetContextPathLegendVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, ContextPathLegendName, visible);
        }

        public static void SetContextAttractorsLegendVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, ContextAttractorsLegendName, visible);
        }

        public static void ApplyPaneLayout(Transform panelRoot, Rect paneViewport, float rightMargin = 24f, float bottomMargin = 24f)
        {
            ApplyLegendPanelLayout(panelRoot, AttractorCyclesLegendName, paneViewport, rightMargin, bottomMargin, alignRight: true);
            ApplyLegendPanelLayout(panelRoot, ContextPathLegendName, paneViewport, rightMargin, bottomMargin, alignRight: true);
            ApplyLegendPanelLayout(panelRoot, ContextAttractorsLegendName, paneViewport, rightMargin, bottomMargin, alignRight: true);
        }

        public static void ApplyFigureViewportLegendLayout(
            Transform panelRoot,
            Rect figureViewport,
            float horizontalMargin = 16f,
            float bottomMargin = 16f)
        {
            ApplyFigureViewportLegendPanelLayout(
                panelRoot,
                AttractorCyclesLegendName,
                figureViewport,
                horizontalMargin,
                bottomMargin);
            ApplyFigureViewportLegendPanelLayout(
                panelRoot,
                ContextPathLegendName,
                figureViewport,
                horizontalMargin,
                bottomMargin);
            ApplyFigureViewportLegendPanelLayout(
                panelRoot,
                ContextAttractorsLegendName,
                figureViewport,
                horizontalMargin,
                bottomMargin);
        }

        public static void ApplyDefaultOverlayLayout(Transform panelRoot, float rightMargin = 24f, float bottomMargin = 24f)
        {
            ApplyDefaultLegendPanelLayout(panelRoot, AttractorCyclesLegendName, rightMargin, bottomMargin);
            ApplyDefaultLegendPanelLayout(panelRoot, ContextPathLegendName, rightMargin, bottomMargin);
            ApplyDefaultLegendPanelLayout(panelRoot, ContextAttractorsLegendName, rightMargin, bottomMargin);
        }

        public static void ApplyLegendBandLayout(
            Transform panelRoot,
            Rect legendBand,
            bool alignRight = false,
            float horizontalMargin = 12f,
            float bottomMargin = 8f,
            bool flatShelf = false)
        {
            ApplyLegendPanelLayout(
                panelRoot,
                AttractorCyclesLegendName,
                legendBand,
                horizontalMargin,
                bottomMargin,
                alignRight,
                flatShelf);
            ApplyLegendPanelLayout(
                panelRoot,
                ContextPathLegendName,
                legendBand,
                horizontalMargin,
                bottomMargin,
                alignRight,
                flatShelf);
            ApplyLegendPanelLayout(
                panelRoot,
                ContextAttractorsLegendName,
                legendBand,
                horizontalMargin,
                bottomMargin,
                alignRight,
                flatShelf);
        }

        public static void LayoutDualPaneRightShelf(
            Transform panelRoot,
            DualPaneRightShelfTab activeTab,
            BooleanAttractorLandscapeHighlightMode highlightMode)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform attractorLegend = panelRoot.Find(AttractorCyclesLegendName);
            Transform contextLegend = panelRoot.Find(ContextPathLegendName);
            Transform contextAttractorLegend = panelRoot.Find(ContextAttractorsLegendName);
            Transform attractorReadout = panelRoot.Find(BooleanAttractorLandscapeAttractorReadoutPanel.PanelName);
            Transform contextReadout = panelRoot.Find(BooleanAttractorLandscapeContextPathPanel.PanelName);
            Transform contextAttractorReadout = panelRoot.Find(
                BooleanAttractorLandscapeContextAttractorReadoutPanel.PanelName);

            SetLegendPanelVisible(panelRoot, AttractorCyclesLegendName, false);
            SetLegendPanelVisible(panelRoot, ContextPathLegendName, false);
            SetLegendPanelVisible(panelRoot, ContextAttractorsLegendName, false);
            BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, false);
            BooleanAttractorLandscapeContextAttractorReadoutPanel.SetVisible(panelRoot, false);

            Rect contentShelf = DualPaneRightLegendShelfTabs.GetTabContentRect(DualPaneLayout.RightPaneLegendBelow);

            if (activeTab == DualPaneRightShelfTab.Legend)
            {
                Transform activeLegend = highlightMode switch
                {
                    BooleanAttractorLandscapeHighlightMode.ContextPath => contextLegend,
                    BooleanAttractorLandscapeHighlightMode.ContextAttractors => contextAttractorLegend,
                    _ => attractorLegend,
                };
                if (activeLegend != null)
                {
                    DualPaneLegendShelfStyle.ApplyLegendPanelTypography(activeLegend);
                    PositionLegendInShelfBox(activeLegend, contentShelf);
                    activeLegend.gameObject.SetActive(true);
                    activeLegend.SetAsLastSibling();
                }
            }
            else if (activeTab == DualPaneRightShelfTab.Attractors)
            {
                switch (highlightMode)
                {
                    case BooleanAttractorLandscapeHighlightMode.ContextPath:
                        BooleanAttractorLandscapeContextPathPanel.ApplyLegendBelowLayout(panelRoot, contentShelf);
                        BooleanAttractorLandscapeContextPathPanel.SetVisible(panelRoot, true);
                        if (contextReadout != null)
                        {
                            contextReadout.SetAsLastSibling();
                        }

                        break;

                    case BooleanAttractorLandscapeHighlightMode.ContextAttractors:
                        BooleanAttractorLandscapeContextAttractorReadoutPanel.ApplyLegendBelowLayout(
                            panelRoot,
                            contentShelf);
                        BooleanAttractorLandscapeContextAttractorReadoutPanel.SetVisible(panelRoot, true);
                        if (contextAttractorReadout != null)
                        {
                            contextAttractorReadout.SetAsLastSibling();
                        }

                        break;

                    default:
                        BooleanAttractorLandscapeAttractorReadoutPanel.ApplyLegendBelowLayout(panelRoot, contentShelf);
                        BooleanAttractorLandscapeAttractorReadoutPanel.SetVisible(panelRoot, true);
                        if (attractorReadout != null)
                        {
                            attractorReadout.SetAsLastSibling();
                        }

                        break;
                }
            }
        }

        private static void PositionLegendInShelfBox(Transform legendTransform, Rect contentShelf)
        {
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Canvas canvas = legendTransform.GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;

            float panelWidth = MeasureLegendPanelWidth(legendTransform);
            float panelHeight = MeasureLegendPanelHeight(legendTransform);
            float contentLeftCanvas = contentShelf.x * Screen.width / scaleFactor;
            float anchorXNorm = contentLeftCanvas * scaleFactor / Screen.width;
            float topAnchorY = contentShelf.y + contentShelf.height;

            legendRect.anchorMin = new Vector2(anchorXNorm, topAnchorY);
            legendRect.anchorMax = new Vector2(anchorXNorm, topAnchorY);
            legendRect.pivot = new Vector2(0f, 1f);
            legendRect.anchoredPosition = Vector2.zero;
            legendRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            DualPaneLegendShelfStyle.SetFlat(legendTransform);
        }

        private static float MeasureLegendPanelHeight(Transform legendTransform)
        {
            if (legendTransform != null
                && legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return legendRect.sizeDelta.y;
            }

            return 0f;
        }

        private static float MeasureLegendPanelWidth(Transform legendTransform)
        {
            if (legendTransform == null)
            {
                return 0f;
            }

            const float padding = 12f;
            const float swatchColumn = 46f;
            float maxTextWidth = 0f;
            Text[] texts = legendTransform.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.gameObject.name != "Label" && text.gameObject.name != "LegendTitle")
                {
                    continue;
                }

                maxTextWidth = Mathf.Max(maxTextWidth, text.preferredWidth);
            }

            return padding + swatchColumn + maxTextWidth + padding;
        }

        private static void ApplyLegendPanelLayout(
            Transform panelRoot,
            string panelName,
            Rect paneViewport,
            float horizontalMargin,
            float bottomMargin,
            bool alignRight,
            bool flatShelf = false)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(panelName);
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            float anchorX = alignRight
                ? paneViewport.x + paneViewport.width
                : paneViewport.x;
            float anchorY = paneViewport.y + paneViewport.height;
            legendRect.anchorMin = new Vector2(anchorX, anchorY);
            legendRect.anchorMax = new Vector2(anchorX, anchorY);
            legendRect.pivot = new Vector2(alignRight ? 1f : 0f, 1f);
            legendRect.anchoredPosition = new Vector2(
                alignRight ? -horizontalMargin : horizontalMargin,
                -bottomMargin);

            float slotWidthPixels = paneViewport.width * Screen.width;
            float maxWidth = Mathf.Max(140f, slotWidthPixels - horizontalMargin * 2f);
            legendRect.sizeDelta = new Vector2(
                Mathf.Min(340f, maxWidth),
                legendRect.sizeDelta.y);

            if (flatShelf)
            {
                RbnVisualisation.Menu.DualPaneLegendShelfStyle.SetFlat(legendTransform);
                FitPanelToSlot(legendRect, paneViewport);
            }
        }

        private static void ApplyFigureViewportLegendPanelLayout(
            Transform panelRoot,
            string panelName,
            Rect figureViewport,
            float horizontalMargin,
            float bottomMargin)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(panelName);
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            float anchorX = figureViewport.x + figureViewport.width;
            float anchorY = figureViewport.y;
            legendRect.anchorMin = new Vector2(anchorX, anchorY);
            legendRect.anchorMax = new Vector2(anchorX, anchorY);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.anchoredPosition = new Vector2(-horizontalMargin, bottomMargin);

            float slotWidthPixels = figureViewport.width * Screen.width;
            float maxWidth = Mathf.Max(140f, slotWidthPixels - horizontalMargin * 2f);
            legendRect.sizeDelta = new Vector2(
                Mathf.Min(340f, maxWidth),
                legendRect.sizeDelta.y);
        }

        private static void ApplyDefaultLegendPanelLayout(
            Transform panelRoot,
            string panelName,
            float rightMargin,
            float bottomMargin)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(panelName);
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            legendRect.anchorMin = new Vector2(1f, 0f);
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.anchoredPosition = new Vector2(-rightMargin, bottomMargin);
        }

        private static void FitPanelToSlot(RectTransform legendRect, Rect slot)
        {
            float maxHeight = slot.height * Screen.height - 12f;
            if (legendRect.sizeDelta.y > maxHeight)
            {
                legendRect.sizeDelta = new Vector2(legendRect.sizeDelta.x, maxHeight);
            }
        }

        private static void SetLegendPanelVisible(Transform panelRoot, string panelName, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(panelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
        }

        private void EnsureBuilt()
        {
            if (built
                && LegendHasRow(AttractorCyclesLegendName, "State transitions")
                && LegendHasRow(ContextPathLegendName, "State transitions")
                && LegendHasRow(ContextAttractorsLegendName, "State transitions"))
            {
                return;
            }

            Transform legacyLegend = transform.Find("AttractorLandscapeLegend");
            if (legacyLegend != null)
            {
                Destroy(legacyLegend.gameObject);
            }

            EnsureLegendPanel(
                AttractorCyclesLegendName,
                "Attractor landscape",
                new[]
                {
                    Circle("Point attractor", PointAttractorColor),
                    Circle("Limit cycle", LimitCycleNodeColor),
                    Line("Limit cycle path", LimitCycleEdgeColor),
                    Line("State transitions", StateTransitionEdgeColor),
                },
                visible: true);

            EnsureLegendPanel(
                ContextPathLegendName,
                "Context path",
                new[]
                {
                    Circle("Initial state", InitialNodeColor),
                    Circle("Target state", TargetNodeColor),
                    Circle("Context path state", PathNodeColor),
                    Line("Context path", PathEdgeColor),
                    Line("State transitions", StateTransitionEdgeColor),
                },
                visible: false);

            EnsureLegendPanel(
                ContextAttractorsLegendName,
                "Context attractors",
                new[]
                {
                    Circle("Context 1 cycle", ContextAttractor1Color),
                    Line("Context 1 cycle path", ContextAttractor1EdgeColor),
                    Circle("Context 2 cycle", ContextAttractor2Color),
                    Line("Context 2 cycle path", ContextAttractor2EdgeColor),
                    Line("State transitions", StateTransitionEdgeColor),
                },
                visible: false);

            built = true;
        }

        private bool LegendHasRow(string panelName, string rowLabel)
        {
            Transform legend = transform.Find(panelName);
            return legend != null && legend.Find($"LegendRow_{rowLabel}") != null;
        }

        private void EnsureLegendPanel(
            string panelName,
            string title,
            LegendRowSpec[] entries,
            bool visible)
        {
            Transform existing = transform.Find(panelName);
            if (existing != null)
            {
                if (LegendHasRow(panelName, "State transitions"))
                {
                    existing.gameObject.SetActive(visible);
                    return;
                }

                Destroy(existing.gameObject);
            }

            BuildLegendPanel(panelName, title, entries, visible);
        }

        private LegendRowSpec Circle(string label, Color color)
        {
            return new LegendRowSpec
            {
                Kind = LegendSwatchKind.Circle,
                Color = color,
                Label = label,
            };
        }

        private LegendRowSpec Line(string label, Color color)
        {
            return new LegendRowSpec
            {
                Kind = LegendSwatchKind.Line,
                Color = color,
                Label = label,
            };
        }

        private void BuildLegendPanel(
            string panelName,
            string title,
            LegendRowSpec[] entries,
            bool visible)
        {
            float titleHeight = 30f;
            float padding = 12f;
            float panelHeight = padding * 2f + titleHeight + entries.Length * rowHeight;

            GameObject legendRoot = CreateUiObject(
                panelName,
                transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Image background = legendRoot.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            background.raycastTarget = false;

            RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
            legendRect.anchorMin = new Vector2(1f, 0f);
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.anchoredPosition = new Vector2(-rightMargin, bottomMargin);
            legendRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            CreateText(
                "LegendTitle",
                legendRoot.transform,
                title,
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -padding),
                new Vector2(-padding, titleHeight),
                new Color(0.92f, 0.94f, 0.98f, 1f));

            for (int i = 0; i < entries.Length; i++)
            {
                float yOffset = padding + titleHeight + i * rowHeight;
                CreateLegendRow(legendRoot.transform, entries[i], padding, yOffset);
            }

            legendRoot.SetActive(visible);
        }

        private enum LegendSwatchKind
        {
            Circle,
            Line,
        }

        private struct LegendRowSpec
        {
            public LegendSwatchKind Kind;
            public Color Color;
            public string Label;
        }

        private void CreateLegendRow(Transform parent, LegendRowSpec entry, float leftPadding, float topOffset)
        {
            GameObject row = CreateUiObject($"LegendRow_{entry.Label}", parent, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(leftPadding, -topOffset);
            rowRect.sizeDelta = new Vector2(-leftPadding * 2f, rowHeight);

            GameObject swatch = CreateUiObject(
                "Swatch",
                row.transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Image swatchImage = swatch.GetComponent<Image>();
            swatchImage.color = entry.Color;
            swatchImage.raycastTarget = false;

            RectTransform swatchRect = swatch.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            swatchRect.pivot = new Vector2(0f, 0.5f);

            if (entry.Kind == LegendSwatchKind.Circle)
            {
                swatchImage.sprite = GenomePathUiCircleSprite.Get();
                swatchImage.type = Image.Type.Simple;
                swatchImage.preserveAspect = true;
                swatchRect.anchoredPosition = new Vector2(0f, 0f);
                swatchRect.sizeDelta = new Vector2(swatchSize, swatchSize);
            }
            else
            {
                swatchRect.anchoredPosition = new Vector2(0f, 0f);
                swatchRect.sizeDelta = new Vector2(lineSwatchWidth, lineSwatchHeight);
            }

            CreateText(
                "Label",
                row.transform,
                entry.Label,
                16,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(
                    entry.Kind == LegendSwatchKind.Circle ? swatchSize + 10f : lineSwatchWidth + 10f,
                    0f),
                new Vector2(0f, 0f),
                new Color(0.86f, 0.88f, 0.92f, 1f));
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject uiObject = new GameObject(name, components);
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color)
        {
            GameObject textObject = CreateUiObject(
                name,
                parent,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMin;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            return text;
        }
    }
}
