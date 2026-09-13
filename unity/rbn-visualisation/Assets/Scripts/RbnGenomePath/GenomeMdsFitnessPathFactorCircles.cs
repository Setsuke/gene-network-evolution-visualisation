using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    public class GenomeMdsFitnessPathFactorCircles : MonoBehaviour
    {
        private static readonly Color ZeroScoreColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        private static readonly Color PartialScoreColor = new Color(0.98f, 0.82f, 0.08f, 1f);
        private static readonly Color PerfectScoreColor = new Color(0.13f, 0.77f, 0.37f, 1f);
        private static readonly Color InfoPanelTextColor = new Color(0.9f, 0.92f, 0.96f, 1f);
        private static readonly Color ScoreTextOnDarkBackground = Color.white;
        private static readonly Color ScoreTextOnYellowBackground = new Color(0.12f, 0.12f, 0.14f, 1f);
        private const float PerfectScoreThreshold = 0.999f;

        public const float DefaultSquareSize = 48f;
        public const float DefaultSquareSpacing = 8f;

        private const int InfoPanelFontSize = DualPaneLegendShelfStyle.TitleFontSize;
        private const int ScoreFontSize = DualPaneLegendShelfStyle.CellFontSize;
        private const float TitleHeight = 24f;
        private const float ContextLabelHeight = 24f;
        private const float RowGap = 4f;
        private const float BlockSpacing = 10f;
        private const float SectionPaddingTop = 10f;
        private const float SectionPaddingBottom = 12f;
        private const float HorizontalPadding = 16f;

        [SerializeField] private int geneCount;
        [SerializeField] private int contextCount;
        [SerializeField] private float squareSize = DefaultSquareSize;
        [SerializeField] private float squareSpacing = DefaultSquareSpacing;
        [SerializeField] private string[] contextTargets;

        private ScoreCell[][] scoreCells;

        public bool IsConfigured => geneCount > 0 && contextCount > 0 && scoreCells != null;
        public int GeneCount => geneCount;
        public int ContextCount => contextCount;
        public string[] ContextTargets => contextTargets;

        public void ConfigureCellLayout(float cellSize, float cellSpacing)
        {
            squareSize = cellSize;
            squareSpacing = cellSpacing;
        }

        public void Build(int genesPerContext, int contexts, string[] contextTargets)
        {
            geneCount = genesPerContext;
            contextCount = contexts;
            this.contextTargets = contextTargets;

            if (rectTransform == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            ClearChildren();
            scoreCells = new ScoreCell[contextCount][];

            float cursorY = -SectionPaddingTop;
            CreateStaticLabel(
                "FactorScoresTitle",
                "Target factor scores:",
                InfoPanelFontSize,
                FontStyle.Normal,
                InfoPanelTextColor,
                ref cursorY,
                TitleHeight,
                TextAnchor.UpperLeft);

            for (int contextIndex = 0; contextIndex < contextCount; contextIndex++)
            {
                string target = ResolveContextTarget(contextTargets, contextIndex, genesPerContext);
                string contextLine = $"Context {contextIndex + 1}  |  Target: {target}";
                CreateStaticLabel(
                    $"ContextLabel_{contextIndex + 1}",
                    contextLine,
                    InfoPanelFontSize,
                    FontStyle.Normal,
                    InfoPanelTextColor,
                    ref cursorY,
                    ContextLabelHeight,
                    TextAnchor.UpperLeft);

                cursorY -= RowGap;
                scoreCells[contextIndex] = CreateScoreRow(contextIndex, ref cursorY);
                cursorY -= BlockSpacing;
            }
        }

        public void UpdateScores(float[] factorScores)
        {
            if (!IsConfigured)
            {
                return;
            }

            for (int contextIndex = 0; contextIndex < contextCount; contextIndex++)
            {
                for (int geneIndex = 0; geneIndex < geneCount; geneIndex++)
                {
                    int factorIndex = contextIndex * geneCount + geneIndex;
                    float score = factorScores != null
                        && factorIndex < factorScores.Length
                        ? factorScores[factorIndex]
                        : 0f;

                    Color backgroundColor = ScoreToColor(score);
                    scoreCells[contextIndex][geneIndex].Background.color = backgroundColor;
                    scoreCells[contextIndex][geneIndex].Label.text = FormatScore(score);
                    scoreCells[contextIndex][geneIndex].Label.color = ScoreTextColor(score);
                }
            }
        }

        public static float GetRequiredHeight(int contextCount, float squareSize, float rowSpacing)
        {
            if (contextCount <= 0)
            {
                return 0f;
            }

            float blockHeight = ContextLabelHeight + RowGap + squareSize;
            return SectionPaddingTop
                + TitleHeight
                + BlockSpacing
                + contextCount * blockHeight
                + (contextCount - 1) * BlockSpacing
                + SectionPaddingBottom;
        }

        public static float GetRequiredWidth(int geneCount, float squareSize, float squareSpacing)
        {
            if (geneCount <= 0)
            {
                return 360f;
            }

            return HorizontalPadding * 2f
                + geneCount * squareSize
                + (geneCount - 1) * squareSpacing;
        }

        public float GetMeasuredWidth()
        {
            if (!IsConfigured)
            {
                return 0f;
            }

            float gridWidth = GetRequiredWidth(geneCount, squareSize, squareSpacing);
            float maxLabelWidth = 0f;
            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (text.gameObject.name == "ScoreLabel")
                {
                    continue;
                }

                maxLabelWidth = Mathf.Max(maxLabelWidth, text.preferredWidth);
            }

            return Mathf.Max(gridWidth, maxLabelWidth + HorizontalPadding * 2f);
        }

        public float GetMeasuredHeight()
        {
            return GetRequiredHeight(contextCount, squareSize, squareSpacing);
        }

        private RectTransform rectTransform => transform as RectTransform;

        private ScoreCell[] CreateScoreRow(int contextIndex, ref float cursorY)
        {
            ScoreCell[] rowCells = new ScoreCell[geneCount];
            float rowTop = cursorY;
            float rowCenterY = rowTop - squareSize * 0.5f;

            for (int geneIndex = 0; geneIndex < geneCount; geneIndex++)
            {
                GameObject cellObject = new GameObject(
                    $"FactorScoreCell_C{contextIndex + 1}_G{geneIndex}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                cellObject.transform.SetParent(transform, false);

                RectTransform cellRect = cellObject.GetComponent<RectTransform>();
                cellRect.anchorMin = new Vector2(0f, 1f);
                cellRect.anchorMax = new Vector2(0f, 1f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(squareSize, squareSize);
                cellRect.anchoredPosition = new Vector2(
                    HorizontalPadding + squareSize * 0.5f + geneIndex * (squareSize + squareSpacing),
                    rowCenterY);

                Image background = cellObject.GetComponent<Image>();
                background.sprite = GenomePathUiCircleSprite.GetSquare();
                background.type = Image.Type.Simple;
                background.color = ZeroScoreColor;
                background.raycastTarget = false;

                Text label = CreateScoreLabel(cellObject.transform);
                rowCells[geneIndex] = new ScoreCell
                {
                    Background = background,
                    Label = label,
                };
            }

            cursorY -= squareSize;
            return rowCells;
        }

        private static Text CreateScoreLabel(Transform parent)
        {
            GameObject labelObject = new GameObject(
                "ScoreLabel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            labelObject.transform.SetParent(parent, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelObject.GetComponent<Text>();
            label.text = "0.00";
            DualPaneLegendShelfStyle.ApplyFactorScoreCell(label);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = ScoreTextOnDarkBackground;
            label.raycastTarget = false;
            return label;
        }

        private void CreateStaticLabel(
            string name,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Color color,
            ref float cursorY,
            float height,
            TextAnchor alignment)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(transform, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(HorizontalPadding, cursorY - height);
            rect.offsetMax = new Vector2(-HorizontalPadding, cursorY);

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;

            if (name == "FactorScoresTitle")
            {
                DualPaneLegendShelfStyle.ApplyFactorScoreTitle(text);
            }
            else if (name.StartsWith("ContextLabel_"))
            {
                DualPaneLegendShelfStyle.ApplyFactorScoreLabel(text);
            }
            else
            {
                text.font = DualPaneLegendShelfStyle.ShelfFont;
                text.fontSize = fontSize;
                text.fontStyle = fontStyle;
            }

            cursorY -= height;
        }

        private static string FormatScore(float score)
        {
            return Mathf.Clamp01(score).ToString("0.00");
        }

        private static Color ScoreToColor(float score)
        {
            score = Mathf.Clamp01(score);
            if (score <= 0f)
            {
                return ZeroScoreColor;
            }

            if (score >= PerfectScoreThreshold)
            {
                return PerfectScoreColor;
            }

            return PartialScoreColor;
        }

        private static Color ScoreTextColor(float score)
        {
            score = Mathf.Clamp01(score);
            if (score > 0f && score < PerfectScoreThreshold)
            {
                return ScoreTextOnYellowBackground;
            }

            return ScoreTextOnDarkBackground;
        }

        private static string ResolveContextTarget(string[] contextTargets, int contextIndex, int genesPerContext)
        {
            if (contextTargets != null
                && contextIndex < contextTargets.Length
                && !string.IsNullOrEmpty(contextTargets[contextIndex]))
            {
                return contextTargets[contextIndex];
            }

            int placeholderLength = genesPerContext > 0 ? genesPerContext : 5;
            return new string('?', placeholderLength);
        }

        private void ClearChildren()
        {
            for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(transform.GetChild(childIndex).gameObject);
            }
        }

        private sealed class ScoreCell
        {
            public Image Background;
            public Text Label;
        }
    }
}
