using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Option 10 left-pane legend: factor-neutral subcategories plus exit move types.
    /// </summary>
    public static class PlateauDriftPathLegend
    {
        public const string PanelName = "PlateauDriftPathLegend";

        private const float RowHeight = 22f;
        private const float LineSwatchWidth = 36f;
        private const float LineSwatchHeight = 4f;
        private const float CircleSwatchSize = 16f;
        private const float PanelWidth = 360f;
        private const float Padding = 12f;
        private static readonly Color CurrentGenomeColor = Color.cyan;

        private enum LegendSwatchKind
        {
            Line,
            Circle,
        }

        private struct LegendEntry
        {
            public Color Color;
            public string Label;
            public LegendSwatchKind SwatchKind;
        }

        public static void EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform existingLegend = panelRoot.Find(PanelName);
            if (existingLegend != null)
            {
                if (TryRefreshLegendContent(existingLegend))
                {
                    return;
                }

                Object.DestroyImmediate(existingLegend.gameObject);
            }

            BuildLegendPanel(panelRoot);
        }

        public static void LayoutInShelf(Transform panelRoot, Rect contentShelf)
        {
            if (panelRoot == null)
            {
                return;
            }

            EnsureOnPanel(panelRoot);
            Transform legendTransform = panelRoot.Find(PanelName);
            if (legendTransform == null || !legendTransform.TryGetComponent(out RectTransform legendRect))
            {
                return;
            }

            Rect slot = DualPaneLegendShelfStyle.InsetSlot(contentShelf);
            DualPaneLayout.ApplyNormalizedRect(legendRect, slot);
            DualPaneLegendShelfStyle.SetFlat(legendTransform);
            DualPaneLegendShelfStyle.ApplyLegendPanelTypography(legendTransform);
            legendTransform.gameObject.SetActive(true);
            legendTransform.SetAsLastSibling();
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(PanelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
        }

        private static LegendEntry[] BuildLegendEntries()
        {
            GenomePathChangeTypeStyle[] styles = GenomePathChangeTypeStyles.Option10LegendEntries;
            var entries = new LegendEntry[styles.Length + 1];
            for (int index = 0; index < styles.Length; index++)
            {
                entries[index] = new LegendEntry
                {
                    Color = styles[index].Color,
                    Label = styles[index].Label,
                    SwatchKind = LegendSwatchKind.Line,
                };
            }

            entries[styles.Length] = new LegendEntry
            {
                Color = CurrentGenomeColor,
                Label = "Current genome",
                SwatchKind = LegendSwatchKind.Circle,
            };

            return entries;
        }

        private static bool TryRefreshLegendContent(Transform legendRoot)
        {
            LegendEntry[] entries = BuildLegendEntries();
            if (legendRoot.childCount != entries.Length)
            {
                return false;
            }

            for (int index = 0; index < entries.Length; index++)
            {
                Transform row = legendRoot.GetChild(index);
                Transform swatch = row.Find("Swatch");
                Transform label = row.Find("Label");
                if (swatch == null || label == null || !swatch.TryGetComponent(out Image swatchImage))
                {
                    return false;
                }

                if (!label.TryGetComponent(out Text labelText))
                {
                    return false;
                }

                ApplySwatchStyle(swatchImage, swatch.GetComponent<RectTransform>(), entries[index].SwatchKind);
                swatchImage.color = entries[index].Color;
                labelText.text = entries[index].Label;
            }

            return true;
        }

        private static void BuildLegendPanel(Transform panelRoot)
        {
            LegendEntry[] entries = BuildLegendEntries();
            float panelHeight = Padding * 2f + entries.Length * RowHeight;
            GameObject legendRoot = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            legendRoot.transform.SetParent(panelRoot, false);

            Image background = legendRoot.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            background.raycastTarget = false;

            RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
            legendRect.sizeDelta = new Vector2(PanelWidth, panelHeight);

            for (int index = 0; index < entries.Length; index++)
            {
                float yOffset = Padding + index * RowHeight;
                CreateLegendRow(legendRoot.transform, entries[index], yOffset);
            }
        }

        private static void CreateLegendRow(Transform parent, LegendEntry entry, float yOffset)
        {
            GameObject rowObject = new GameObject(
                "Row",
                typeof(RectTransform));
            rowObject.transform.SetParent(parent, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.offsetMin = new Vector2(Padding, -(yOffset + RowHeight));
            rowRect.offsetMax = new Vector2(-Padding, -yOffset);

            GameObject swatchObject = new GameObject(
                "Swatch",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            swatchObject.transform.SetParent(rowObject.transform, false);
            Image swatchImage = swatchObject.GetComponent<Image>();
            swatchImage.color = entry.Color;
            swatchImage.raycastTarget = false;

            RectTransform swatchRect = swatchObject.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            ApplySwatchStyle(swatchImage, swatchRect, entry.SwatchKind);

            float labelOffset = entry.SwatchKind == LegendSwatchKind.Line
                ? LineSwatchWidth + 10f
                : CircleSwatchSize + 10f;

            CreateText(
                rowObject.transform,
                "Label",
                entry.Label,
                14,
                FontStyle.Normal,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(labelOffset, 0f),
                new Vector2(0f, 0f));
        }

        private static void ApplySwatchStyle(
            Image swatchImage,
            RectTransform swatchRect,
            LegendSwatchKind swatchKind)
        {
            if (swatchKind == LegendSwatchKind.Circle)
            {
                swatchImage.sprite = GenomePathUiCircleSprite.Get();
                swatchImage.type = Image.Type.Simple;
                swatchImage.preserveAspect = true;
                swatchRect.pivot = new Vector2(0.5f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(CircleSwatchSize * 0.5f, 0f);
                swatchRect.sizeDelta = new Vector2(CircleSwatchSize, CircleSwatchSize);
                return;
            }

            swatchImage.sprite = null;
            swatchImage.type = Image.Type.Simple;
            swatchImage.preserveAspect = false;
            swatchRect.pivot = new Vector2(0f, 0.5f);
            swatchRect.anchoredPosition = Vector2.zero;
            swatchRect.sizeDelta = new Vector2(LineSwatchWidth, LineSwatchHeight);
        }

        private static void CreateText(
            Transform parent,
            string name,
            string content,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = new Color(0.88f, 0.91f, 0.96f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }
    }
}
