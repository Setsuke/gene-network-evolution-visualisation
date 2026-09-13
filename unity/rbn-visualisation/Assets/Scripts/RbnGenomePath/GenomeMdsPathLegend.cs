using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Legends for factor-score and accepted-move colours in the genome path view.
    /// </summary>
    public class GenomeMdsPathLegend : MonoBehaviour
    {
        public enum LegendHorizontalAnchor
        {
            Right,
            Left,
        }

        private const string FactorScoreLegendName = "FactorScoreLegend";
        private const string ChangeTypeLegendName = "ChangeTypeLegend";
        private const string GenomeRoleLegendName = "GenomeRoleLegend";

        private static readonly Color FactorScoreZeroColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        private static readonly Color FactorScorePartialColor = new Color(0.98f, 0.82f, 0.08f, 1f);
        private static readonly Color FactorScorePerfectColor = new Color(0.13f, 0.77f, 0.37f, 1f);
        private static readonly Color InitialGenomeColor = Color.white;
        private static readonly Color SolutionGenomeColor = new Color(0.86f, 0.15f, 0.15f, 1f);

        [SerializeField] private float rowHeight = 28f;
        [SerializeField] private float swatchSize = 16f;
        [SerializeField] private float lineSwatchWidth = 36f;
        [SerializeField] private float lineSwatchHeight = 4f;
        [SerializeField] private float panelWidth = 340f;
        [SerializeField] private float bottomMargin = 24f;
        [SerializeField] private float rightMargin = 24f;
        [SerializeField] private float leftMargin = 24f;
        [SerializeField] private float legendGap = 8f;
        [SerializeField] private LegendHorizontalAnchor horizontalAnchor = LegendHorizontalAnchor.Right;
        [SerializeField] private Rect screenAnchorRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private float dualPanePanelWidth = 280f;
        [SerializeField] private bool dualPaneCompactText;

        private struct LegendEntry
        {
            public Color Color;
            public string Label;
        }

        private enum LegendSwatchKind
        {
            Square,
            Circle,
            Line,
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        public static void EnsureOnPanel(GameObject panelRoot)
        {
            EnsureOnPanel(panelRoot, LegendHorizontalAnchor.Right);
        }

        public static void EnsureOnPanel(GameObject panelRoot, LegendHorizontalAnchor anchor)
        {
            EnsureOnPanel(panelRoot, anchor, new Rect(0f, 0f, 1f, 1f));
        }

        public static void EnsureOnPanel(GameObject panelRoot, LegendHorizontalAnchor anchor, Rect screenAnchorRect)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out GenomeMdsPathLegend legend))
            {
                legend = panelRoot.AddComponent<GenomeMdsPathLegend>();
            }

            legend.horizontalAnchor = anchor;
            legend.screenAnchorRect = screenAnchorRect;
            legend.EnsureBuilt();
        }

        public static void ApplyLegendBelowFigure(GameObject panelRoot, Rect legendBelow)
        {
            EnsureOnPanel(panelRoot, LegendHorizontalAnchor.Left, legendBelow);
            if (!panelRoot.TryGetComponent(out GenomeMdsPathLegend legend))
            {
                return;
            }

            legend.rowHeight = 20f;
            legend.panelWidth = legend.dualPanePanelWidth;
            legend.dualPaneCompactText = true;
            legend.EnsureBuilt();
            legend.ApplyFlatShelfStyle();
        }

        public static void LayoutDualPaneLeftShelf(
            GameObject panelRoot,
            GenomeMdsPathVisualiser visualiser,
            DualPaneLeftShelfTab activeTab = DualPaneLeftShelfTab.Legend)
        {
            if (panelRoot == null || !panelRoot.TryGetComponent(out GenomeMdsPathLegend legend))
            {
                return;
            }

            legend.LayoutDualPaneLeftShelfColumns(visualiser, activeTab);
        }

        private void LayoutDualPaneLeftShelfColumns(
            GenomeMdsPathVisualiser visualiser,
            DualPaneLeftShelfTab activeTab)
        {
            Transform genomeLegend = transform.Find(GenomeRoleLegendName);
            Transform moveLegend = transform.Find(ChangeTypeLegendName);
            Transform factorScorePanel = transform.Find(GenomeMdsPathFactorScorePanel.PanelName);

            ApplyDualPaneLegendTextLayout(genomeLegend);
            ApplyDualPaneLegendTextLayout(moveLegend);

            Canvas.ForceUpdateCanvases();
            float scaleFactor = GetCanvasScaleFactor();

            Rect contentShelf = DualPaneLeftLegendShelfTabs.GetTabContentRect(DualPaneLayout.LeftPaneLegendBelow);
            float gapCanvas = DualPaneLegendShelfStyle.ColumnGapPx / scaleFactor;
            float shelfWidthCanvas = contentShelf.width * Screen.width / scaleFactor;
            float topAnchorY = contentShelf.y + contentShelf.height;

            SetColorLegendColumnVisible(genomeLegend, false);
            SetColorLegendColumnVisible(moveLegend, false);
            SetLegendPanelVisible(transform, FactorScoreLegendName, false);

            if (activeTab == DualPaneLeftShelfTab.Legend)
            {
                float genomeWidth = genomeLegend != null ? MeasureLegendPanelWidth(genomeLegend) : 0f;
                float moveWidth = moveLegend != null ? MeasureLegendPanelWidth(moveLegend) : 0f;

                float[] legendWidths = { genomeWidth, moveWidth };
                Transform[] legendColumns = { genomeLegend, moveLegend };
                float legendTotalWidth = Sum(legendWidths) + gapCanvas;

                if (legendTotalWidth > shelfWidthCanvas && legendTotalWidth > 0f)
                {
                    float shrink = shelfWidthCanvas / legendTotalWidth;
                    for (int i = 0; i < legendWidths.Length; i++)
                    {
                        legendWidths[i] *= shrink;
                    }

                    legendTotalWidth = shelfWidthCanvas;
                }

                float contentLeftCanvas = contentShelf.x * Screen.width / scaleFactor;
                float legendStartCanvas = contentLeftCanvas + (shelfWidthCanvas - legendTotalWidth) * 0.5f;
                float cursorCanvas = legendStartCanvas;
                for (int i = 0; i < legendColumns.Length; i++)
                {
                    if (legendColumns[i] == null)
                    {
                        continue;
                    }

                    SetColorLegendColumnVisible(legendColumns[i], true);
                    float anchorXNorm = cursorCanvas * scaleFactor / Screen.width;
                    float columnHeight = MeasureLegendPanelHeight(legendColumns[i]);
                    PositionLegendColumn(
                        legendColumns[i],
                        anchorXNorm,
                        topAnchorY,
                        legendWidths[i],
                        columnHeight);
                    cursorCanvas += legendWidths[i] + gapCanvas;
                }
            }

            if (factorScorePanel != null)
            {
                factorScorePanel.gameObject.SetActive(false);
            }
        }

        private static void SetLegendPanelVisible(Transform panelRoot, string panelName, bool visible)
        {
            Transform legendTransform = panelRoot.Find(panelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
        }

        private static void SetLegendPanelVisible(GameObject panelRoot, string panelName, bool visible)
        {
            SetLegendPanelVisible(panelRoot.transform, panelName, visible);
        }

        private static void SetColorLegendColumnVisible(Transform legendTransform, bool visible)
        {
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
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

        private static float Sum(float[] values)
        {
            float total = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                total += values[i];
            }

            return total;
        }

        private float GetCanvasScaleFactor()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        }

        private void PositionLegendColumn(
            Transform legendTransform,
            float anchorXNorm,
            float anchorYNorm,
            float widthCanvas,
            float heightCanvas)
        {
            if (!legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            legendRect.anchorMin = new Vector2(anchorXNorm, anchorYNorm);
            legendRect.anchorMax = new Vector2(anchorXNorm, anchorYNorm);
            legendRect.pivot = new Vector2(0f, 1f);
            legendRect.anchoredPosition = Vector2.zero;
            legendRect.sizeDelta = new Vector2(
                Mathf.Max(80f, widthCanvas),
                heightCanvas);
        }

        private float MeasureLegendPanelWidth(Transform legendTransform)
        {
            const float padding = 12f;
            float swatchColumn = legendTransform.name == ChangeTypeLegendName
                ? lineSwatchWidth + 10f
                : swatchSize + 10f;
            float maxTextWidth = 0f;

            Text[] texts = legendTransform.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.font == null)
                {
                    continue;
                }

                if (text.gameObject.name != "Label" && text.gameObject.name != "LegendTitle")
                {
                    continue;
                }

                if (dualPaneCompactText && text.gameObject.name == "Label")
                {
                    text.fontSize = DualPaneLegendShelfStyle.BodyFontSize;
                }

                maxTextWidth = Mathf.Max(maxTextWidth, text.preferredWidth);
            }

            return padding + swatchColumn + maxTextWidth + padding;
        }

        private void ApplyFlatShelfStyle()
        {
            SetPanelFlat(GenomeRoleLegendName);
            SetPanelFlat(FactorScoreLegendName);
            SetPanelFlat(ChangeTypeLegendName);
        }

        private void SetPanelFlat(string panelName)
        {
            Transform legendTransform = transform.Find(panelName);
            if (legendTransform != null)
            {
                DualPaneLegendShelfStyle.SetFlat(legendTransform);
            }
        }

        private void ApplyDualPaneLegendTextLayout(Transform legendTransform)
        {
            if (!dualPaneCompactText || legendTransform == null)
            {
                return;
            }

            DualPaneLegendShelfStyle.ApplyLegendPanelTypography(legendTransform);
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!visible)
            {
                SetLegendPanelVisible(panelRoot, GenomeRoleLegendName, false);
                SetLegendPanelVisible(panelRoot, FactorScoreLegendName, false);
                SetLegendPanelVisible(panelRoot, ChangeTypeLegendName, false);
                GenomeMdsPathFactorScorePanel.SetVisible(panelRoot, false);
                return;
            }

            if (panelRoot.Find(DualPaneLeftLegendShelfTabs.RootName) != null
                || panelRoot.Find(Option10LeftLegendShelfTabs.RootName) != null)
            {
                return;
            }

            SetLegendPanelVisible(panelRoot, GenomeRoleLegendName, true);
            SetLegendPanelVisible(panelRoot, FactorScoreLegendName, true);
            SetLegendPanelVisible(panelRoot, ChangeTypeLegendName, true);
        }

        private void EnsureBuilt()
        {
            Transform factorLegend = transform.Find(FactorScoreLegendName);
            if (factorLegend == null)
            {
                BuildFactorScoreLegend();
                factorLegend = transform.Find(FactorScoreLegendName);
            }

            float factorHeight = factorLegend != null
                ? factorLegend.GetComponent<RectTransform>().sizeDelta.y
                : 0f;
            float moveLegendBottomOffset = bottomMargin + factorHeight + legendGap;

            int expectedMoveEntries = GenomePathChangeTypeStyles.Option10LegendEntries.Length;
            Transform moveLegend = transform.Find(ChangeTypeLegendName);
            if (NeedsLegendRebuild(moveLegend, expectedMoveEntries))
            {
                if (moveLegend != null)
                {
                    Destroy(moveLegend.gameObject);
                }

                BuildMoveTypeLegend(moveLegendBottomOffset);
                moveLegend = transform.Find(ChangeTypeLegendName);
            }
            else if (moveLegend != null)
            {
                EnsureLineLegendSwatches(moveLegend);
                PositionLegendPanel(moveLegend, moveLegendBottomOffset);
            }

            float moveHeight = moveLegend != null
                ? moveLegend.GetComponent<RectTransform>().sizeDelta.y
                : 0f;
            float genomeLegendBottomOffset = moveLegendBottomOffset + moveHeight + legendGap;

            const int expectedGenomeEntries = 2;
            Transform genomeLegend = transform.Find(GenomeRoleLegendName);
            if (NeedsLegendRebuild(genomeLegend, expectedGenomeEntries))
            {
                if (genomeLegend != null)
                {
                    Destroy(genomeLegend.gameObject);
                }

                BuildGenomeRoleLegend(genomeLegendBottomOffset);
                genomeLegend = transform.Find(GenomeRoleLegendName);
            }
            else if (genomeLegend != null)
            {
                EnsureCircularLegendSwatches(genomeLegend);
                PositionLegendPanel(genomeLegend, genomeLegendBottomOffset);
            }

            if (factorLegend != null)
            {
                PositionLegendPanel(factorLegend, bottomMargin);
            }

            ApplyOption10PanelGuard();
        }

        private void ApplyOption10PanelGuard()
        {
            if (transform.Find(Option10LeftLegendShelfTabs.RootName) != null)
            {
                SetVisible(transform, false);
            }
        }

        private static bool NeedsLegendRebuild(Transform legendPanel, int expectedRowCount)
        {
            if (legendPanel == null)
            {
                return true;
            }

            return CountLegendRows(legendPanel) != expectedRowCount;
        }

        private static int CountLegendRows(Transform legendPanel)
        {
            if (legendPanel == null)
            {
                return -1;
            }

            int rowCount = 0;
            for (int index = 0; index < legendPanel.childCount; index++)
            {
                Transform child = legendPanel.GetChild(index);
                if (child.name.StartsWith("LegendRow_"))
                {
                    rowCount++;
                }
            }

            return rowCount;
        }

        private void BuildGenomeRoleLegend(float bottomOffset)
        {
            LegendEntry[] entries =
            {
                new LegendEntry { Color = InitialGenomeColor, Label = "Initial genome" },
                new LegendEntry { Color = SolutionGenomeColor, Label = "Solution genome" },
            };

            CreateLegendPanel(
                GenomeRoleLegendName,
                "Genome",
                entries,
                bottomOffset,
                LegendSwatchKind.Circle);
        }

        private void BuildFactorScoreLegend()
        {
            LegendEntry[] entries =
            {
                new LegendEntry { Color = FactorScoreZeroColor, Label = "Factor score 0" },
                new LegendEntry
                {
                    Color = FactorScorePartialColor,
                    Label = "Factor score between 0 and 1",
                },
                new LegendEntry { Color = FactorScorePerfectColor, Label = "Factor score 1" },
            };

            CreateLegendPanel(
                FactorScoreLegendName,
                "Factor score",
                entries,
                bottomMargin,
                LegendSwatchKind.Square);
        }

        private void BuildMoveTypeLegend(float bottomOffset)
        {
            GenomePathChangeTypeStyle[] entries = GenomePathChangeTypeStyles.Option10LegendEntries;
            var moveEntries = new LegendEntry[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                moveEntries[i] = new LegendEntry
                {
                    Color = entries[i].Color,
                    Label = entries[i].Label,
                };
            }

            CreateLegendPanel(
                ChangeTypeLegendName,
                "Move type",
                moveEntries,
                bottomOffset,
                LegendSwatchKind.Line);
        }

        private void CreateLegendPanel(
            string panelName,
            string title,
            LegendEntry[] entries,
            float bottomOffset,
            LegendSwatchKind swatchKind = LegendSwatchKind.Square)
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
            legendRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            PositionLegendPanel(legendRoot.transform, bottomOffset);

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
                CreateLegendRow(legendRoot.transform, entries[i], padding, yOffset, swatchKind);
            }
        }

        private void PositionLegendPanel(Transform legendTransform, float bottomOffset)
        {
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            if (horizontalAnchor == LegendHorizontalAnchor.Left)
            {
                legendRect.anchorMin = new Vector2(screenAnchorRect.x, screenAnchorRect.y);
                legendRect.anchorMax = new Vector2(screenAnchorRect.x, screenAnchorRect.y);
                legendRect.pivot = new Vector2(0f, 0f);
                legendRect.anchoredPosition = new Vector2(leftMargin, bottomOffset);
                return;
            }

            float rightAnchorX = screenAnchorRect.x + screenAnchorRect.width;
            float bottomAnchorY = screenAnchorRect.y;
            legendRect.anchorMin = new Vector2(rightAnchorX, bottomAnchorY);
            legendRect.anchorMax = new Vector2(rightAnchorX, bottomAnchorY);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.anchoredPosition = new Vector2(-rightMargin, bottomOffset);
        }

        private void CreateLegendRow(
            Transform parent,
            LegendEntry entry,
            float leftPadding,
            float topOffset,
            LegendSwatchKind swatchKind)
        {
            GameObject row = CreateUiObject(
                $"LegendRow_{entry.Label}",
                parent,
                typeof(RectTransform));

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

            float labelOffset;
            if (swatchKind == LegendSwatchKind.Line)
            {
                swatchImage.sprite = null;
                swatchRect.pivot = new Vector2(0f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(0f, 0f);
                swatchRect.sizeDelta = new Vector2(lineSwatchWidth, lineSwatchHeight);
                labelOffset = lineSwatchWidth + 10f;
            }
            else if (swatchKind == LegendSwatchKind.Circle)
            {
                swatchImage.sprite = GenomePathUiCircleSprite.Get();
                swatchImage.type = Image.Type.Simple;
                swatchImage.preserveAspect = true;
                swatchRect.pivot = new Vector2(0.5f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(swatchSize * 0.5f, 0f);
                swatchRect.sizeDelta = new Vector2(swatchSize, swatchSize);
                labelOffset = swatchSize + 10f;
            }
            else
            {
                swatchRect.pivot = new Vector2(0.5f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(swatchSize * 0.5f, 0f);
                swatchRect.sizeDelta = new Vector2(swatchSize, swatchSize);
                labelOffset = swatchSize + 10f;
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
                new Vector2(labelOffset, 0f),
                new Vector2(0f, 0f),
                new Color(0.86f, 0.88f, 0.92f, 1f));
        }

        private void EnsureLineLegendSwatches(Transform legendRoot)
        {
            if (legendRoot == null)
            {
                return;
            }

            Image[] images = legendRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image.gameObject.name != "Swatch")
                {
                    continue;
                }

                image.sprite = null;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;

                if (!image.TryGetComponent(out RectTransform swatchRect))
                {
                    continue;
                }

                swatchRect.pivot = new Vector2(0f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(0f, 0f);
                swatchRect.sizeDelta = new Vector2(lineSwatchWidth, lineSwatchHeight);
            }
        }

        private static void EnsureCircularLegendSwatches(Transform legendRoot)
        {
            if (legendRoot == null)
            {
                return;
            }

            Sprite circleSprite = GenomePathUiCircleSprite.Get();
            Image[] images = legendRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image.gameObject.name != "Swatch")
                {
                    continue;
                }

                image.sprite = circleSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;

                if (!image.TryGetComponent(out RectTransform swatchRect))
                {
                    continue;
                }

                swatchRect.pivot = new Vector2(0.5f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(
                    swatchRect.sizeDelta.x * 0.5f,
                    swatchRect.anchoredPosition.y);
            }
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
