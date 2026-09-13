using System.Collections.Generic;
using System.Text;
using RbnVisualisation.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Top-right readout of context paths for Option 5 when "Show context path" is active.
    /// </summary>
    public class BooleanAttractorLandscapeContextPathPanel : MonoBehaviour
    {
        public const string PanelName = "ContextPathReadoutPanel";
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

        public static BooleanAttractorLandscapeContextPathPanel EnsureOnPanel(Transform panelRoot)
        {
            if (panelRoot == null)
            {
                return null;
            }

            Transform existing = panelRoot.Find(PanelName);
            if (existing != null)
            {
                return existing.GetComponent<BooleanAttractorLandscapeContextPathPanel>();
            }

            GameObject panelObject = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(BooleanAttractorLandscapeContextPathPanel));
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
                "ContextPathText",
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

            BooleanAttractorLandscapeContextPathPanel panel =
                panelObject.GetComponent<BooleanAttractorLandscapeContextPathPanel>();
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

            BooleanAttractorLandscapeContextPathPanel panel = EnsureOnPanel(panelRoot);
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

            BooleanAttractorLandscapeContextPathPanel panel = EnsureOnPanel(panelRoot);
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

            BooleanAttractorLandscapeContextPathPanel panel = EnsureOnPanel(panelRoot);
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

            BooleanAttractorLandscapeContextPathPanel panel = EnsureOnPanel(panelRoot);
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

            string content = BooleanAttractorContextPathFormatter.FormatStep(
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

    internal static class BooleanAttractorContextPathFormatter
    {
        public static string FormatStep(
            BooleanAttractorLandscapeStepData step,
            BooleanAttractorContextData[] contexts,
            System.Func<int, string> resolveStateLabel)
        {
            if (step?.contextPaths == null || step.contextPaths.Length == 0)
            {
                return "No context paths for this step.";
            }

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
                string initialState = ResolveState(contextPath.initialState, contextPath.initialIndex, resolveStateLabel);
                string targetState = ResolveState(contextPath.targetState, contextPath.targetIndex, resolveStateLabel);
                builder.Append("<color=#FBD12E>");
                builder.Append(label);
                builder.Append(" (");
                builder.Append(initialState);
                builder.Append("->");
                builder.Append(BooleanAttractorTargetTextStyle.ColorizeTarget(targetState));
                builder.Append("):</color>");
                builder.AppendLine();
                builder.Append(FormatPathStates(contextPath, resolveStateLabel));
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

        private static string ResolveState(
            string explicitState,
            int stateIndex,
            System.Func<int, string> resolveStateLabel)
        {
            if (!string.IsNullOrWhiteSpace(explicitState))
            {
                return explicitState;
            }

            return resolveStateLabel != null
                ? resolveStateLabel(stateIndex)
                : stateIndex.ToString();
        }

        private static string FormatPathStates(
            BooleanAttractorContextPathData contextPath,
            System.Func<int, string> resolveStateLabel)
        {
            if (contextPath.pathIndices != null && contextPath.pathIndices.Length > 0)
            {
                var builder = new StringBuilder();
                for (int index = 0; index < contextPath.pathIndices.Length; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(" -> ");
                    }

                    builder.Append(BooleanAttractorTargetTextStyle.FormatStateLabel(
                        contextPath.pathIndices[index],
                        resolveStateLabel(contextPath.pathIndices[index]),
                        new HashSet<int> { contextPath.targetIndex }));
                }

                return builder.ToString();
            }

            return BooleanAttractorTargetTextStyle.FormatStateLabel(
                       contextPath.initialIndex,
                       ResolveState(contextPath.initialState, contextPath.initialIndex, resolveStateLabel),
                       null)
                   + " -> "
                   + BooleanAttractorTargetTextStyle.FormatStateLabel(
                       contextPath.targetIndex,
                       ResolveState(contextPath.targetState, contextPath.targetIndex, resolveStateLabel),
                       new HashSet<int> { contextPath.targetIndex });
        }
    }
}
