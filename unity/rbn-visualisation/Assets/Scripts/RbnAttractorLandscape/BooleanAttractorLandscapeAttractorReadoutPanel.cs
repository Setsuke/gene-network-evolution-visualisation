using System.Collections.Generic;
using System.Text;
using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Top-right readout of point attractors and limit cycles for Option 5
    /// when "Show attractor cycles" is active.
    /// </summary>
    public class BooleanAttractorLandscapeAttractorReadoutPanel : MonoBehaviour
    {
        public const string PanelName = "AttractorReadoutPanel";
        private const float TopMargin = 120f;
        private const float RightMargin = 24f;
        private const float PanelWidth = 520f;
        private const float MinPanelHeight = 80f;
        private const float LineHeight = 22f;
        private const float Padding = 14f;

        private static readonly Color PanelBackground = new Color(0.07f, 0.08f, 0.11f, 0.92f);
        private static readonly Color BodyTextColor = new Color(0.9f, 0.92f, 0.96f, 1f);

        [SerializeField] private Text bodyText;
        [SerializeField] private bool useLegendShelfLayout;

        public static BooleanAttractorLandscapeAttractorReadoutPanel EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return null;
            }

            Transform existing = panelRoot.Find(PanelName);
            if (existing != null)
            {
                return existing.GetComponent<BooleanAttractorLandscapeAttractorReadoutPanel>();
            }

            GameObject panelObject = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BooleanAttractorLandscapeAttractorReadoutPanel));
            panelObject.transform.SetParent(panelRoot, false);

            Image background = panelObject.GetComponent<Image>();
            background.color = PanelBackground;
            background.raycastTarget = false;

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            panelRect.sizeDelta = new Vector2(PanelWidth, MinPanelHeight);

            GameObject textObject = new GameObject(
                "AttractorReadoutText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(panelObject.transform, false);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = BodyTextColor;
            text.raycastTarget = false;
            text.supportRichText = true;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(Padding, Padding);
            textRect.offsetMax = new Vector2(-Padding, -Padding);

            BooleanAttractorLandscapeAttractorReadoutPanel panel =
                panelObject.GetComponent<BooleanAttractorLandscapeAttractorReadoutPanel>();
            panel.bodyText = text;
            panel.gameObject.SetActive(false);
            return panel;
        }

        public static void SetVisible(Transform panelRoot, bool visible)
        {
            if (panelRoot == null)
            {
                return;
            }

            Transform panelTransform = panelRoot.Find(PanelName);
            if (panelTransform != null)
            {
                panelTransform.gameObject.SetActive(visible);
            }
        }

        public static void ApplyLegendBelowLayout(Transform panelRoot, Rect readoutSlot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
            if (panel == null || !panel.TryGetComponent(out RectTransform panelRect))
            {
                return;
            }

            panel.useLegendShelfLayout = true;
            DualPaneLayout.ApplyNormalizedRect(panelRect, readoutSlot);
            DualPaneLegendShelfStyle.SetFlat(panelRect);
            if (panel.bodyText != null)
            {
                DualPaneLegendShelfStyle.ApplyReadoutBody(panel.bodyText);
                panel.bodyText.alignment = TextAnchor.UpperLeft;
                DualPaneLegendShelfStyle.ApplyShelfTextPadding(panel.bodyText);
            }
        }

        public static void ApplyFigureViewportTopRightLayout(Transform panelRoot, Rect figureViewport)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
            if (panel == null || !panel.TryGetComponent(out RectTransform panelRect))
            {
                return;
            }

            panel.useLegendShelfLayout = false;
            LayeredAttractorLandscapeLayout.ApplyTopRightOverlayLayout(panelRect, figureViewport);
            panelRect.sizeDelta = new Vector2(PanelWidth, MinPanelHeight);
            DualPaneLegendShelfStyle.RestoreOverlayPanel(panelRect);
            if (panel.bodyText != null)
            {
                panel.bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                panel.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                panel.bodyText.fontSize = 16;
                RectTransform textRect = panel.bodyText.rectTransform;
                textRect.offsetMin = new Vector2(Padding, Padding);
                textRect.offsetMax = new Vector2(-Padding, -Padding);
            }
        }

        public static void ApplyTopRightLayout(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return;
            }

            BooleanAttractorLandscapeAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
            if (panel == null || !panel.TryGetComponent(out RectTransform panelRect))
            {
                return;
            }

            panel.useLegendShelfLayout = false;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-RightMargin, -TopMargin);
            panelRect.sizeDelta = new Vector2(PanelWidth, MinPanelHeight);
            RbnVisualisation.Menu.DualPaneLegendShelfStyle.RestoreOverlayPanel(panelRect);
            if (panel.bodyText != null)
            {
                panel.bodyText.verticalOverflow = VerticalWrapMode.Overflow;
                panel.bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                panel.bodyText.fontSize = 16;
                RectTransform textRect = panel.bodyText.rectTransform;
                textRect.offsetMin = new Vector2(Padding, Padding);
                textRect.offsetMax = new Vector2(-Padding, -Padding);
            }
        }

        public static void UpdateFromVisualiser(
            Transform panelRoot,
            BooleanAttractorLandscapeVisualiser visualiser)
        {
            if (panelRoot == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            BooleanAttractorLandscapeAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
            if (panel == null)
            {
                return;
            }

            panel.RenderStep(visualiser, visualiser.ActiveStep);
        }

        private void RenderStep(
            BooleanAttractorLandscapeVisualiser visualiser,
            BooleanAttractorLandscapeStepData step)
        {
            if (bodyText == null)
            {
                return;
            }

            string content = BooleanAttractorReadoutFormatter.FormatStep(
                step,
                visualiser.ContextDefinitions,
                visualiser.ResolveStateLabel);
            bodyText.text = content;
            ResizeToContent(content);
        }

        private void ResizeToContent(string content)
        {
            if (useLegendShelfLayout || !TryGetComponent(out RectTransform panelRect) || bodyText == null)
            {
                return;
            }

            int lineCount = string.IsNullOrEmpty(content) ? 1 : content.Split('\n').Length;
            float height = Padding * 2f + lineCount * LineHeight;
            panelRect.sizeDelta = new Vector2(PanelWidth, Mathf.Max(MinPanelHeight, height));
        }
    }

    internal static class BooleanAttractorReadoutFormatter
    {
        public static string FormatStep(
            BooleanAttractorLandscapeStepData step,
            BooleanAttractorContextData[] contexts,
            System.Func<int, string> resolveStateLabel)
        {
            if (step?.attractors == null || step.attractors.Length == 0)
            {
                return "No attractors for this step.";
            }

            HashSet<int> targetIndices = BooleanAttractorTargetTextStyle.CollectTargetIndices(contexts);
            var pointStates = new System.Collections.Generic.List<string>();
            var limitCycles = new System.Collections.Generic.List<string>();

            foreach (BooleanAttractorEntryData attractor in step.attractors)
            {
                if (attractor?.stateIndices == null || attractor.stateIndices.Length == 0)
                {
                    continue;
                }

                if (attractor.type == "point")
                {
                    pointStates.Add(FormatPointAttractor(attractor, resolveStateLabel, targetIndices));
                }
                else if (attractor.type == "limit_cycle")
                {
                    limitCycles.Add(FormatLimitCycle(attractor, resolveStateLabel, targetIndices));
                }
            }

            var builder = new StringBuilder();
            AppendPointAttractorsSection(builder, pointStates);
            if (builder.Length > 0 && limitCycles.Count > 0)
            {
                builder.AppendLine();
            }

            AppendLimitCyclesSection(builder, limitCycles);
            return builder.ToString().TrimEnd();
        }

        private static void AppendPointAttractorsSection(StringBuilder builder, System.Collections.Generic.List<string> pointStates)
        {
            builder.Append("<color=#4790FA>Point attractors (");
            builder.Append(pointStates.Count);
            builder.AppendLine("):</color>");

            if (pointStates.Count == 0)
            {
                return;
            }

            for (int index = 0; index < pointStates.Count; index++)
            {
                builder.AppendLine(pointStates[index]);
            }
        }

        private static void AppendLimitCyclesSection(StringBuilder builder, System.Collections.Generic.List<string> limitCycles)
        {
            builder.Append("<color=#FA9420>Limit cycles (");
            builder.Append(limitCycles.Count);
            builder.AppendLine("):</color>");

            if (limitCycles.Count == 0)
            {
                return;
            }

            for (int index = 0; index < limitCycles.Count; index++)
            {
                builder.AppendLine(limitCycles[index]);
            }
        }

        private static string FormatPointAttractor(
            BooleanAttractorEntryData attractor,
            System.Func<int, string> resolveStateLabel,
            HashSet<int> targetIndices)
        {
            int stateIndex = attractor.stateIndices[0];
            string label = resolveStateLabel(stateIndex);
            return BooleanAttractorTargetTextStyle.FormatStateLabel(stateIndex, label, targetIndices);
        }

        private static string FormatLimitCycle(
            BooleanAttractorEntryData attractor,
            System.Func<int, string> resolveStateLabel,
            HashSet<int> targetIndices)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < attractor.stateIndices.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(" -> ");
                }

                int stateIndex = attractor.stateIndices[index];
                string label = resolveStateLabel(stateIndex);
                builder.Append(BooleanAttractorTargetTextStyle.FormatStateLabel(
                    stateIndex,
                    label,
                    targetIndices));
            }

            return builder.ToString();
        }
    }
}
