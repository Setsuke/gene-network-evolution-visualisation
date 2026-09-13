using System.Collections.Generic;
using System.Text;
using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Readout of per-context attractor cycles when "Show context attractors" is active.
    /// </summary>
    public class BooleanAttractorLandscapeContextAttractorReadoutPanel : MonoBehaviour
    {
        public const string PanelName = "ContextAttractorReadoutPanel";
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

        public static BooleanAttractorLandscapeContextAttractorReadoutPanel EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return null;
            }

            Transform existing = panelRoot.Find(PanelName);
            if (existing != null)
            {
                return existing.GetComponent<BooleanAttractorLandscapeContextAttractorReadoutPanel>();
            }

            GameObject panelObject = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BooleanAttractorLandscapeContextAttractorReadoutPanel));
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
                "ContextAttractorText",
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

            BooleanAttractorLandscapeContextAttractorReadoutPanel panel =
                panelObject.GetComponent<BooleanAttractorLandscapeContextAttractorReadoutPanel>();
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

            BooleanAttractorLandscapeContextAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
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

            BooleanAttractorLandscapeContextAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
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

            BooleanAttractorLandscapeContextAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
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

        public static void UpdateFromVisualiser(
            Transform panelRoot,
            BooleanAttractorLandscapeVisualiser visualiser)
        {
            if (panelRoot == null || visualiser == null || !visualiser.HasActiveRun)
            {
                return;
            }

            BooleanAttractorLandscapeContextAttractorReadoutPanel panel = EnsureOnPanel(panelRoot);
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

            string content = BooleanAttractorContextAttractorFormatter.FormatStep(
                step,
                visualiser.ContextDefinitions,
                visualiser.ResolveStateLabel,
                visualiser.GetContextAttractorColor);
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

    internal static class BooleanAttractorContextAttractorFormatter
    {
        public static string FormatStep(
            BooleanAttractorLandscapeStepData step,
            BooleanAttractorContextData[] contexts,
            System.Func<int, string> resolveStateLabel,
            System.Func<int, Color> resolveContextColor)
        {
            if (step?.contextPaths == null || step.contextPaths.Length == 0)
            {
                return "No context attractors for this step.";
            }

            HashSet<int> targetIndices = BooleanAttractorTargetTextStyle.CollectTargetIndices(contexts);
            var builder = new StringBuilder();
            for (int pathIndex = 0; pathIndex < step.contextPaths.Length; pathIndex++)
            {
                BooleanAttractorContextPathData contextPath = step.contextPaths[pathIndex];
                if (contextPath == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                string label = ResolveContextLabel(contextPath, contexts, pathIndex);
                Color contextColor = resolveContextColor != null
                    ? resolveContextColor(pathIndex)
                    : Color.white;
                builder.Append("<color=");
                builder.Append(ColorUtility.ToHtmlStringRGB(contextColor));
                builder.Append('>');
                builder.Append(label);
                builder.AppendLine(":</color>");
                builder.Append(FormatCycleStates(contextPath, resolveStateLabel, targetIndices));
            }

            return builder.ToString().TrimEnd();
        }

        private static string ResolveContextLabel(
            BooleanAttractorContextPathData contextPath,
            BooleanAttractorContextData[] contexts,
            int pathIndex)
        {
            if (contexts != null)
            {
                for (int index = 0; index < contexts.Length; index++)
                {
                    BooleanAttractorContextData context = contexts[index];
                    if (context == null)
                    {
                        continue;
                    }

                    if (context.initialIndex == contextPath.initialIndex
                        && context.targetIndex == contextPath.targetIndex)
                    {
                        return string.IsNullOrWhiteSpace(context.label)
                            ? $"Context {index + 1}"
                            : context.label;
                    }
                }
            }

            return $"Context {pathIndex + 1}";
        }

        private static string FormatCycleStates(
            BooleanAttractorContextPathData contextPath,
            System.Func<int, string> resolveStateLabel,
            HashSet<int> targetIndices)
        {
            if (contextPath.cycleIndices == null || contextPath.cycleIndices.Length == 0)
            {
                if (contextPath.pathIndices != null && contextPath.pathIndices.Length > 0)
                {
                    int lastIndex = contextPath.pathIndices[contextPath.pathIndices.Length - 1];
                    return BooleanAttractorTargetTextStyle.FormatStateLabel(
                        lastIndex,
                        resolveStateLabel(lastIndex),
                        targetIndices);
                }

                return "No cycle reached.";
            }

            if (contextPath.cycleIndices.Length == 1)
            {
                int stateIndex = contextPath.cycleIndices[0];
                return BooleanAttractorTargetTextStyle.FormatStateLabel(
                    stateIndex,
                    resolveStateLabel(stateIndex),
                    targetIndices);
            }

            var builder = new StringBuilder();
            for (int index = 0; index < contextPath.cycleIndices.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(" <-> ");
                }

                int stateIndex = contextPath.cycleIndices[index];
                builder.Append(BooleanAttractorTargetTextStyle.FormatStateLabel(
                    stateIndex,
                    resolveStateLabel(stateIndex),
                    targetIndices));
            }

            return builder.ToString();
        }
    }
}
