using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Bottom-right legend for the shared-initial-genome MDS view.
    /// </summary>
    public class SharedGenomeMdsPathLegend : MonoBehaviour
    {
        private const string LegendPanelName = "SharedGenomeMdsLegend";
        private const string BatchLegendPanelName = "BatchGenomeMdsLegend";
        private const string DriftCategoriesLegendPanelName = "SharedGenomeMdsDriftCategoriesLegend";
        private const string DriftingGenomesLegendPanelName = "SharedGenomeMdsDriftingGenomesLegend";
        private const string BatchDriftCategoriesLegendPanelName = "BatchGenomeMdsDriftCategoriesLegend";
        private const string BatchDriftingGenomesLegendPanelName = "BatchGenomeMdsDriftingGenomesLegend";
        private const string DriftCategoriesFitnessImprovementLabel = "Fitness Improvement";
        private static readonly Color DriftingGenomeLegendColor = new Color(0.98f, 0.55f, 0.12f, 1f);

        [SerializeField] private float rowHeight = 28f;
        [SerializeField] private float swatchSize = 16f;
        [SerializeField] private float lineSwatchWidth = 36f;
        [SerializeField] private float lineSwatchHeight = 4f;
        [SerializeField] private float panelWidth = 340f;
        [SerializeField] private float bottomMargin = 24f;
        [SerializeField] private float rightMargin = 24f;

        private struct LegendEntry
        {
            public Color Color;
            public string Label;
            public LegendSwatchKind SwatchKind;
        }

        private enum LegendSwatchKind
        {
            Circle,
            Line,
        }

        public static void EnsureOnPanel(GameObject panelRoot, string legendTitle = "Shared genome MDS")
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out SharedGenomeMdsPathLegend legend))
            {
                legend = panelRoot.AddComponent<SharedGenomeMdsPathLegend>();
            }

            legend.EnsureBuilt(legendTitle);
            legend.EnsureDriftCategoriesLegendBuilt();
            legend.EnsureDriftingGenomesLegendBuilt();
        }

        public static void SetDriftingGenomesVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, DriftingGenomesLegendPanelName, visible);
        }

        public static void SetDriftCategoriesVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, DriftCategoriesLegendPanelName, visible);
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, LegendPanelName, visible);
        }

        public static void SetBatchVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, BatchLegendPanelName, visible);
        }

        public static void SetBatchDriftCategoriesVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, BatchDriftCategoriesLegendPanelName, visible);
        }

        public static void SetBatchDriftingGenomesVisible(Transform panelRoot, bool visible)
        {
            SetLegendPanelVisible(panelRoot, BatchDriftingGenomesLegendPanelName, visible);
        }

        public static void EnsureBatchOnPanel(GameObject panelRoot, string legendTitle = "Batch genome MDS")
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out SharedGenomeMdsPathLegend legend))
            {
                legend = panelRoot.AddComponent<SharedGenomeMdsPathLegend>();
            }

            legend.EnsureBuiltBatch(legendTitle);
            legend.EnsureBatchDriftCategoriesLegendBuilt();
            legend.EnsureBatchDriftingGenomesLegendBuilt();
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

        public static void EnsureSolutionsOnlyOnPanel(GameObject panelRoot, string legendTitle = "Shared genome solutions MDS")
        {
            if (panelRoot == null)
            {
                return;
            }

            if (!panelRoot.TryGetComponent(out SharedGenomeMdsPathLegend legend))
            {
                legend = panelRoot.AddComponent<SharedGenomeMdsPathLegend>();
            }

            legend.EnsureBuiltSolutionsOnly(legendTitle);
        }

        private void EnsureBuiltSolutionsOnly(string legendTitle)
        {
            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Shared initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.15f, 0.45f, 0.92f, 1f), Label = "Initial Hamming 160" },
                new LegendEntry { Color = new Color(0.58f, 0.78f, 0.98f, 0.72f), Label = "Initial Hamming 159" },
                new LegendEntry { Color = new Color(0.98f, 0.45f, 0.09f, 1f), Label = "Solution Hamming 160 antipode" },
            };

            if (TryUpdateExistingLegend(legendTitle, entries.Length))
            {
                return;
            }

            BuildLegendPanel(legendTitle, entries);
        }

        private void EnsureBuilt(string legendTitle)
        {
            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Shared initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.13f, 0.77f, 0.37f, 1f), Label = "Fitness 0.25" },
                new LegendEntry { Color = new Color(0.23f, 0.51f, 0.96f, 1f), Label = "Fitness 0.5" },
                new LegendEntry { Color = new Color(0.98f, 0.85f, 0.18f, 1f), Label = "Fitness 0.296" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Other accepted genomes" },
            };

            if (TryUpdateExistingLegend(legendTitle, entries.Length))
            {
                return;
            }

            BuildLegendPanel(LegendPanelName, legendTitle, entries);
        }

        private void EnsureDriftCategoriesLegendBuilt()
        {
            Transform existingLegend = transform.Find(DriftCategoriesLegendPanelName);
            if (existingLegend != null)
            {
                if (UsesMixedSwatchDriftCategoriesLegend(existingLegend, lineSwatchWidth, lineSwatchHeight))
                {
                    UpdateLegendRowLabel(existingLegend, "Other connections", DriftCategoriesFitnessImprovementLabel);
                    return;
                }

                DestroyImmediate(existingLegend.gameObject);
            }

            LegendEntry[] entries =
            {
                new LegendEntry
                {
                    Color = Color.white,
                    Label = "Shared initial genome",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.86f, 0.15f, 0.15f, 1f),
                    Label = "Final solution",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.72f, 0.74f, 0.78f, 1f),
                    Label = "Other accepted genomes",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.NeutralMove),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.NeutralMove),
                    SwatchKind = LegendSwatchKind.Line,
                },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.PathChangedAttractorUnchanged),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.PathChangedAttractorUnchanged),
                    SwatchKind = LegendSwatchKind.Line,
                },
                new LegendEntry
                {
                    Color = GenomePathChangeTypeStyles.GetDriftCategoryColor(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.AttractorChanged),
                    Label = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                        GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.AttractorChanged),
                    SwatchKind = LegendSwatchKind.Line,
                },
                new LegendEntry
                {
                    Color = new Color(0.62f, 0.64f, 0.68f, 1f),
                    Label = DriftCategoriesFitnessImprovementLabel,
                    SwatchKind = LegendSwatchKind.Line,
                },
            };

            BuildLegendPanel(
                DriftCategoriesLegendPanelName,
                "Plateau drift categories",
                entries);

            Transform driftLegend = transform.Find(DriftCategoriesLegendPanelName);
            if (driftLegend != null)
            {
                driftLegend.gameObject.SetActive(false);
            }
        }

        private void EnsureDriftingGenomesLegendBuilt()
        {
            Transform existingLegend = transform.Find(DriftingGenomesLegendPanelName);
            if (existingLegend != null)
            {
                return;
            }

            LegendEntry[] entries =
            {
                new LegendEntry
                {
                    Color = Color.white,
                    Label = "Shared initial genome",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.86f, 0.15f, 0.15f, 1f),
                    Label = "Final solution",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = DriftingGenomeLegendColor,
                    Label = "Hamming >= 60 from initial",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.72f, 0.74f, 0.78f, 1f),
                    Label = "Other accepted genomes",
                    SwatchKind = LegendSwatchKind.Circle,
                },
            };

            BuildLegendPanel(
                DriftingGenomesLegendPanelName,
                "Genomes in shared initial genome evolution",
                entries);

            Transform driftingLegend = transform.Find(DriftingGenomesLegendPanelName);
            if (driftingLegend != null)
            {
                driftingLegend.gameObject.SetActive(false);
            }
        }

        private void EnsureBuiltBatch(string legendTitle)
        {
            LegendEntry[] entries =
            {
                new LegendEntry { Color = Color.white, Label = "Run initial genome" },
                new LegendEntry { Color = new Color(0.86f, 0.15f, 0.15f, 1f), Label = "Final solution" },
                new LegendEntry { Color = new Color(0.13f, 0.77f, 0.37f, 1f), Label = "Fitness 0.25" },
                new LegendEntry { Color = new Color(0.23f, 0.51f, 0.96f, 1f), Label = "Fitness 0.5" },
                new LegendEntry { Color = new Color(0.98f, 0.85f, 0.18f, 1f), Label = "Fitness 0.296" },
                new LegendEntry { Color = new Color(0.72f, 0.74f, 0.78f, 1f), Label = "Other accepted genomes" },
            };

            if (TryUpdateExistingLegend(BatchLegendPanelName, legendTitle, entries.Length))
            {
                return;
            }

            BuildLegendPanel(BatchLegendPanelName, legendTitle, entries);
        }

        private void EnsureBatchDriftCategoriesLegendBuilt()
        {
            Transform existingLegend = transform.Find(BatchDriftCategoriesLegendPanelName);
            if (existingLegend != null)
            {
                return;
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
                new LegendEntry { Color = new Color(0.62f, 0.64f, 0.68f, 1f), Label = "Other connections" },
            };

            BuildLegendPanel(
                BatchDriftCategoriesLegendPanelName,
                "Plateau drift categories",
                entries);

            Transform driftLegend = transform.Find(BatchDriftCategoriesLegendPanelName);
            if (driftLegend != null)
            {
                driftLegend.gameObject.SetActive(false);
            }
        }

        private void EnsureBatchDriftingGenomesLegendBuilt()
        {
            Transform existingLegend = transform.Find(BatchDriftingGenomesLegendPanelName);
            if (existingLegend != null)
            {
                return;
            }

            LegendEntry[] entries =
            {
                new LegendEntry
                {
                    Color = Color.white,
                    Label = "Run initial genome",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.86f, 0.15f, 0.15f, 1f),
                    Label = "Final solution",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = DriftingGenomeLegendColor,
                    Label = "Drifting genomes",
                    SwatchKind = LegendSwatchKind.Circle,
                },
                new LegendEntry
                {
                    Color = new Color(0.72f, 0.74f, 0.78f, 1f),
                    Label = "Other accepted genomes",
                    SwatchKind = LegendSwatchKind.Circle,
                },
            };

            BuildLegendPanel(
                BatchDriftingGenomesLegendPanelName,
                "Plateau drifting genomes",
                entries);

            Transform driftingLegend = transform.Find(BatchDriftingGenomesLegendPanelName);
            if (driftingLegend != null)
            {
                driftingLegend.gameObject.SetActive(false);
            }
        }

        private bool TryUpdateExistingLegend(string legendTitle, int expectedEntryCount)
        {
            return TryUpdateExistingLegend(LegendPanelName, legendTitle, expectedEntryCount);
        }

        private bool TryUpdateExistingLegend(string panelName, string legendTitle, int expectedEntryCount)
        {
            Transform existingLegend = transform.Find(panelName);
            if (existingLegend == null)
            {
                return false;
            }

            Transform titleTransform = existingLegend.Find("LegendTitle");
            if (titleTransform == null
                || !titleTransform.TryGetComponent(out RectTransform titleRect)
                || !UsesTopAnchoredTitleLayout(titleRect)
                || CountLegendRows(existingLegend) != expectedEntryCount)
            {
                DestroyImmediate(existingLegend.gameObject);
                return false;
            }

            if (titleTransform.TryGetComponent(out Text title))
            {
                title.text = legendTitle;
            }

            return true;
        }

        private static int CountLegendRows(Transform legendRoot)
        {
            int rowCount = 0;
            for (int childIndex = 0; childIndex < legendRoot.childCount; childIndex++)
            {
                Transform child = legendRoot.GetChild(childIndex);
                if (child.name.StartsWith("LegendRow_"))
                {
                    rowCount++;
                }
            }

            return rowCount;
        }

        private static bool UsesTopAnchoredTitleLayout(RectTransform titleRect)
        {
            return titleRect.anchorMin.y >= 0.99f && titleRect.anchorMax.y >= 0.99f;
        }

        private static void UpdateLegendRowLabel(Transform legendRoot, string previousLabel, string newLabel)
        {
            Transform row = legendRoot.Find($"LegendRow_{previousLabel}");
            if (row == null)
            {
                return;
            }

            row.name = $"LegendRow_{newLabel}";
            Transform labelTransform = row.Find("Label");
            if (labelTransform != null && labelTransform.TryGetComponent(out Text label))
            {
                label.text = newLabel;
            }
        }

        private static bool UsesMixedSwatchDriftCategoriesLegend(
            Transform legendRoot,
            float expectedLineWidth,
            float expectedLineHeight)
        {
            string neutralMoveLabel = GenomePathChangeTypeStyles.GetDriftCategoryLabel(
                GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.NeutralMove);
            Transform neutralMoveRow = legendRoot.Find($"LegendRow_{neutralMoveLabel}");
            if (neutralMoveRow == null)
            {
                return false;
            }

            Transform swatch = neutralMoveRow.Find("Swatch");
            if (swatch == null || !swatch.TryGetComponent(out RectTransform swatchRect))
            {
                return false;
            }

            return swatchRect.sizeDelta.y <= expectedLineHeight + 1f
                   && swatchRect.sizeDelta.x >= expectedLineWidth - 1f;
        }

        private void BuildLegendPanel(string legendTitle, LegendEntry[] entries)
        {
            BuildLegendPanel(LegendPanelName, legendTitle, entries);
        }

        private void BuildLegendPanel(string panelName, string legendTitle, LegendEntry[] entries)
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
            legendRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            legendRect.anchoredPosition = new Vector2(-rightMargin, bottomMargin);

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

            for (int i = 0; i < entries.Length; i++)
            {
                float yOffset = padding + titleHeight + i * rowHeight;
                CreateLegendRow(legendRoot.transform, entries[i], padding, yOffset);
            }
        }

        private void CreateLegendRow(Transform parent, LegendEntry entry, float leftPadding, float topOffset)
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

            float labelOffset;
            if (entry.SwatchKind == LegendSwatchKind.Line)
            {
                swatchImage.sprite = null;
                swatchRect.pivot = new Vector2(0f, 0.5f);
                swatchRect.anchoredPosition = new Vector2(0f, 0f);
                swatchRect.sizeDelta = new Vector2(lineSwatchWidth, lineSwatchHeight);
                labelOffset = lineSwatchWidth + 10f;
            }
            else
            {
                swatchImage.sprite = GenomePathUiCircleSprite.Get();
                swatchImage.type = Image.Type.Simple;
                swatchImage.preserveAspect = true;
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
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
