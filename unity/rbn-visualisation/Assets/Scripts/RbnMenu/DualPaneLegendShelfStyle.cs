using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Menu
{
    internal static class DualPaneLegendShelfStyle
    {
        internal static readonly Color ShelfBackground = new Color(0.05f, 0.06f, 0.08f, 0.94f);
        private static readonly Color OverlayPanelBackground = new Color(0.07f, 0.08f, 0.11f, 0.92f);

        internal const float SlotEdgeMarginPx = 8f;
        internal const float ColumnGapPx = 18f;
        internal const float ShelfRowGapPx = 12f;
        internal const float ContentPaddingPx = 12f;
        internal const float FactorScoreBoxPaddingPx = 12f;

        internal const int TabFontSize = 15;
        internal const int TitleFontSize = 15;
        internal const int BodyFontSize = 14;
        internal const int CellFontSize = 13;

        internal static readonly Color TabTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        internal static readonly Color TitleTextColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        internal static readonly Color BodyTextColor = new Color(0.86f, 0.88f, 0.92f, 1f);
        internal static readonly Color ReadoutTextColor = new Color(0.9f, 0.92f, 0.96f, 1f);

        private static Font cachedShelfFont;

        internal static Font ShelfFont
        {
            get
            {
                if (cachedShelfFont == null)
                {
                    cachedShelfFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return cachedShelfFont;
            }
        }

        internal static void ApplyTabLabel(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = ShelfFont;
            text.fontSize = TabFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = TabTextColor;
            text.alignment = TextAnchor.MiddleCenter;
        }

        internal static void ApplyLegendTitle(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = ShelfFont;
            text.fontSize = TitleFontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = TitleTextColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        internal static void ApplyLegendBody(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = ShelfFont;
            text.fontSize = BodyFontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = BodyTextColor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        internal static void ApplyReadoutBody(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = ShelfFont;
            text.fontSize = BodyFontSize;
            text.fontStyle = FontStyle.Normal;
            text.color = ReadoutTextColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        internal static void ApplyFactorScoreTitle(Text text)
        {
            ApplyLegendTitle(text);
        }

        internal static void ApplyFactorScoreLabel(Text text)
        {
            ApplyLegendBody(text);
        }

        internal static void ApplyFactorScoreCell(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.font = ShelfFont;
            text.fontSize = CellFontSize;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        internal static void ApplyLegendPanelTypography(Transform legendRoot)
        {
            if (legendRoot == null)
            {
                return;
            }

            Text[] labels = legendRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = labels[i];
                if (label.gameObject.name == "LegendTitle")
                {
                    ApplyLegendTitle(label);
                }
                else if (label.gameObject.name == "Label")
                {
                    ApplyLegendBody(label);
                }
            }
        }

        internal static void ApplyFactorScorePanelTypography(Transform factorPanelRoot)
        {
            if (factorPanelRoot == null)
            {
                return;
            }

            Text[] labels = factorPanelRoot.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = labels[i];
                if (label.gameObject.name == "FactorScoresTitle")
                {
                    ApplyFactorScoreTitle(label);
                }
                else if (label.gameObject.name.StartsWith("ContextLabel_"))
                {
                    ApplyFactorScoreLabel(label);
                }
                else if (label.gameObject.name == "ScoreLabel")
                {
                    ApplyFactorScoreCell(label);
                }
            }
        }

        internal static Rect InsetShelf(Rect shelf)
        {
            float marginX = SlotEdgeMarginPx / Screen.width;
            float marginY = SlotEdgeMarginPx / Screen.height;
            return new Rect(
                shelf.x + marginX,
                shelf.y + marginY,
                Mathf.Max(0f, shelf.width - marginX * 2f),
                Mathf.Max(0f, shelf.height - marginY * 2f));
        }

        internal static Rect InsetSlot(Rect slot)
        {
            return InsetShelf(slot);
        }

        internal static void ApplyShelfTextPadding(Text bodyText)
        {
            if (bodyText == null)
            {
                return;
            }

            RectTransform textRect = bodyText.rectTransform;
            textRect.offsetMin = new Vector2(ContentPaddingPx, ContentPaddingPx);
            textRect.offsetMax = new Vector2(-ContentPaddingPx, -ContentPaddingPx);
        }

        internal static void SetFlat(Transform panelTransform)
        {
            if (panelTransform != null && panelTransform.TryGetComponent(out Image image))
            {
                image.color = Color.clear;
            }
        }

        internal static void RestoreOverlayPanel(Transform panelTransform)
        {
            if (panelTransform != null && panelTransform.TryGetComponent(out Image image))
            {
                image.color = OverlayPanelBackground;
            }
        }
    }
}
