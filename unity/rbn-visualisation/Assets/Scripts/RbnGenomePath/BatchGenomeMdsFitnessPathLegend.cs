using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Bottom-right legend for batch MDS + fitness height (MDS 2D - Fitness menu option).
    /// </summary>
    public class BatchGenomeMdsFitnessPathLegend : MonoBehaviour
    {
        public const string PanelName = "BatchGenomeMdsFitnessLegend";
        public const string MoveClassificationPanelName = "BatchGenomeMdsFitnessMoveLegend";
        public const string DriftMovesPanelName = "BatchGenomeMdsFitnessDriftLegend";
        public const string DriftMovesByCategoryPanelName = "BatchGenomeMdsFitnessDriftByCategoryLegend";

        private const float RowHeight = 28f;
        private const float SwatchSize = 16f;
        private const float PanelWidth = 430f;
        private const float MoveLegendWidth = 340f;
        private const float BottomMargin = 24f;
        private const float RightMargin = 24f;
        private const float NoteHeight = 88f;
        private const float MoveLegendNoteHeight = 56f;

        private struct LegendEntry
        {
            public Color Color;
            public string Label;
        }

        public static void EnsureOnPanel(GameObject panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out BatchGenomeMdsFitnessPathLegend legend))
            {
                legend = panelRoot.AddComponent<BatchGenomeMdsFitnessPathLegend>();
            }

            legend.EnsureBuilt();
            legend.EnsureMoveClassificationLegendBuilt();
            legend.EnsureDriftMovesLegendBuilt();
            legend.EnsureDriftMovesByCategoryLegendBuilt();
        }

        public static void SetDriftMovesByCategoryVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(DriftMovesByCategoryPanelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
        }

        public static void SetDriftMovesVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(DriftMovesPanelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
        }

        public static void SetMoveClassificationVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform legendTransform = panelRoot.Find(MoveClassificationPanelName);
            if (legendTransform != null)
            {
                legendTransform.gameObject.SetActive(visible);
            }
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

        private void EnsureBuilt()
        {
            Transform existingLegend = transform.Find(PanelName);
            if (existingLegend != null)
            {
                Destroy(existingLegend.gameObject);
            }

            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Run initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Other accepted genomes" },
                new LegendEntry
                {
                    Color = new Color(0.13f, 0.77f, 0.37f, 1f),
                    Label = "Fitness 0.25",
                },
                new LegendEntry
                {
                    Color = new Color(0.23f, 0.51f, 0.96f, 1f),
                    Label = "Fitness 0.5",
                },
                new LegendEntry
                {
                    Color = new Color(0.98f, 0.85f, 0.18f, 1f),
                    Label = "Fitness 0.296",
                },
            };

            BuildLegendPanel(entries);
        }

        private void BuildLegendPanel(LegendEntry[] entries)
        {
            const string legendTitle = "MDS + Fitness";
            float titleHeight = 30f;
            float padding = 12f;
            float panelHeight = padding * 2f + titleHeight + entries.Length * RowHeight;

            GameObject legendRoot = CreateUiObject(
                PanelName,
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
            legendRect.sizeDelta = new Vector2(PanelWidth, panelHeight);
            legendRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);

            CreateText(
                "LegendTitle",
                legendRoot.transform,
                legendTitle,
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -padding),
                new Vector2(-padding, titleHeight),
                new Color(0.92f, 0.94f, 0.98f, 1f));

            for (int index = 0; index < entries.Length; index++)
            {
                float yOffset = padding + titleHeight + index * RowHeight;
                CreateLegendRow(legendRoot.transform, entries[index], padding, yOffset);
            }
        }

        private void EnsureDriftMovesLegendBuilt()
        {
            Transform existingLegend = transform.Find(DriftMovesPanelName);
            if (existingLegend != null)
            {
                return;
            }

            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Run initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Other accepted genomes" },
                new LegendEntry { Color = new Color(0.98f, 0.92f, 0.15f, 1f), Label = "Plateau drift moves (no exit)" },
                new LegendEntry { Color = new Color(0.62f, 0.64f, 0.68f, 1f), Label = "Other moves" },
            };

            BuildDriftMovesLegendPanel(entries);
        }

        private void BuildDriftMovesLegendPanel(LegendEntry[] entries)
        {
            const string legendTitle = "Drift moves";
            float titleHeight = 30f;
            float padding = 12f;
            float panelHeight = padding * 2f + titleHeight + entries.Length * RowHeight + MoveLegendNoteHeight;

            GameObject legendRoot = CreateUiObject(
                DriftMovesPanelName,
                transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            legendRoot.SetActive(false);

            Image background = legendRoot.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            background.raycastTarget = false;

            RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
            legendRect.anchorMin = new Vector2(1f, 0f);
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.sizeDelta = new Vector2(MoveLegendWidth, panelHeight);
            legendRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);

            CreateText(
                "LegendTitle",
                legendRoot.transform,
                legendTitle,
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -padding),
                new Vector2(-padding, titleHeight),
                new Color(0.92f, 0.94f, 0.98f, 1f));

            for (int index = 0; index < entries.Length; index++)
            {
                float yOffset = padding + titleHeight + index * RowHeight;
                CreateLegendRow(legendRoot.transform, entries[index], padding, yOffset);
            }

            CreateText(
                "DriftMovesNote",
                legendRoot.transform,
                "Full paths stay visible. Only neutral plateau drift is highlighted in yellow; exit steps stay grey.",
                13,
                FontStyle.Italic,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -(padding + titleHeight + entries.Length * RowHeight + 6f)),
                new Vector2(-padding, MoveLegendNoteHeight),
                new Color(0.72f, 0.76f, 0.84f, 1f));
        }

        private void EnsureDriftMovesByCategoryLegendBuilt()
        {
            Transform existingLegend = transform.Find(DriftMovesByCategoryPanelName);
            if (existingLegend != null)
            {
                Destroy(existingLegend.gameObject);
            }

            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Run initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Other accepted genomes" },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.NeutralMove),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.NeutralMove),
                },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.PathChangedAttractorUnchanged),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.PathChangedAttractorUnchanged),
                },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.AttractorChanged),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.AttractorChanged),
                },
                new LegendEntry { Color = new Color(0.62f, 0.64f, 0.68f, 1f), Label = "Other moves" },
            };

            BuildDriftMovesByCategoryLegendPanel(entries);
        }

        private void BuildDriftMovesByCategoryLegendPanel(LegendEntry[] entries)
        {
            const string legendTitle = "Drift moves by category";
            float titleHeight = 30f;
            float padding = 12f;
            float panelHeight = padding * 2f + titleHeight + entries.Length * RowHeight;

            GameObject legendRoot = CreateUiObject(
                DriftMovesByCategoryPanelName,
                transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            legendRoot.SetActive(false);

            Image background = legendRoot.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            background.raycastTarget = false;

            RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
            legendRect.anchorMin = new Vector2(1f, 0f);
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.sizeDelta = new Vector2(PanelWidth, panelHeight);
            legendRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);

            CreateText(
                "LegendTitle",
                legendRoot.transform,
                legendTitle,
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -padding),
                new Vector2(-padding, titleHeight),
                new Color(0.92f, 0.94f, 0.98f, 1f));

            for (int index = 0; index < entries.Length; index++)
            {
                float yOffset = padding + titleHeight + index * RowHeight;
                CreateLegendRow(legendRoot.transform, entries[index], padding, yOffset);
            }
        }

        private void EnsureMoveClassificationLegendBuilt()
        {
            Transform existingLegend = transform.Find(MoveClassificationPanelName);
            if (existingLegend != null)
            {
                return;
            }

            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Run initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Plateau fitness 0.25 / 0.5 / 0.296" },
            };

            for (int index = 0; index < GenomePathChangeTypeStyles.LegendEntries.Length; index++)
            {
                GenomePathChangeTypeStyle style = GenomePathChangeTypeStyles.LegendEntries[index];
                entries = AppendEntry(
                    entries,
                    new LegendEntry { Color = style.Color, Label = style.Label });
            }

            BuildMoveClassificationLegendPanel(entries);
        }

        private static LegendEntry[] AppendEntry(LegendEntry[] entries, LegendEntry entry)
        {
            LegendEntry[] expanded = new LegendEntry[entries.Length + 1];
            for (int index = 0; index < entries.Length; index++)
            {
                expanded[index] = entries[index];
            }

            expanded[entries.Length] = entry;
            return expanded;
        }

        private void BuildMoveClassificationLegendPanel(LegendEntry[] entries)
        {
            const string legendTitle = "Move classification";
            float titleHeight = 30f;
            float padding = 12f;
            float panelHeight = padding * 2f + titleHeight + entries.Length * RowHeight + MoveLegendNoteHeight;

            GameObject legendRoot = CreateUiObject(
                MoveClassificationPanelName,
                transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            legendRoot.SetActive(false);

            Image background = legendRoot.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 0.92f);
            background.raycastTarget = false;

            RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
            legendRect.anchorMin = new Vector2(1f, 0f);
            legendRect.anchorMax = new Vector2(1f, 0f);
            legendRect.pivot = new Vector2(1f, 0f);
            legendRect.sizeDelta = new Vector2(MoveLegendWidth, panelHeight);
            legendRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);

            CreateText(
                "LegendTitle",
                legendRoot.transform,
                legendTitle,
                18,
                FontStyle.Bold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -padding),
                new Vector2(-padding, titleHeight),
                new Color(0.92f, 0.94f, 0.98f, 1f));

            for (int index = 0; index < entries.Length; index++)
            {
                float yOffset = padding + titleHeight + index * RowHeight;
                CreateLegendRow(legendRoot.transform, entries[index], padding, yOffset);
            }

            CreateText(
                "MoveClassificationNote",
                legendRoot.transform,
                "Segments and non-plateau nodes use Option 3 move colours. " +
                "Nodes at fitness 0.25, 0.5 and 0.296 stay grey.",
                13,
                FontStyle.Italic,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(padding, -(padding + titleHeight + entries.Length * RowHeight + 6f)),
                new Vector2(-padding, MoveLegendNoteHeight),
                new Color(0.72f, 0.76f, 0.84f, 1f));
        }

        private static void CreateLegendRow(Transform parent, LegendEntry entry, float leftPadding, float topOffset)
        {
            GameObject row = CreateUiObject($"LegendRow_{entry.Label}", parent, typeof(RectTransform));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(leftPadding, -topOffset);
            rowRect.sizeDelta = new Vector2(-leftPadding * 2f, RowHeight);

            GameObject swatch = CreateUiObject(
                "Swatch",
                row.transform,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Image swatchImage = swatch.GetComponent<Image>();
            swatchImage.color = entry.Color;
            swatchImage.raycastTarget = false;
            swatchImage.sprite = GenomePathUiCircleSprite.Get();
            swatchImage.type = Image.Type.Simple;
            swatchImage.preserveAspect = true;

            RectTransform swatchRect = swatch.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            swatchRect.pivot = new Vector2(0.5f, 0.5f);
            swatchRect.anchoredPosition = new Vector2(SwatchSize * 0.5f, 0f);
            swatchRect.sizeDelta = new Vector2(SwatchSize, SwatchSize);

            CreateText(
                "Label",
                row.transform,
                entry.Label,
                14,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(SwatchSize + 10f, 0f),
                new Vector2(0f, 0f),
                new Color(0.86f, 0.88f, 0.92f, 1f));
        }

        private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
        {
            GameObject uiObject = new GameObject(name, components);
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void CreateText(
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
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = anchorMin;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }
    }
}
