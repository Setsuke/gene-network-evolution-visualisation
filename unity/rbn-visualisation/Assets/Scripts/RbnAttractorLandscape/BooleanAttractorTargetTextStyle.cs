using System.Collections.Generic;

namespace RbnVisualisation.AttractorLandscape
{
    internal static class BooleanAttractorTargetTextStyle
    {
        internal const string TargetColorHex = "#EB3838";

        internal static string ColorizeTarget(string text)
        {
            return $"<color={TargetColorHex}>{text}</color>";
        }

        internal static HashSet<int> CollectTargetIndices(BooleanAttractorContextData[] contexts)
        {
            var targetIndices = new HashSet<int>();
            if (contexts == null)
            {
                return targetIndices;
            }

            for (int index = 0; index < contexts.Length; index++)
            {
                BooleanAttractorContextData context = contexts[index];
                if (context != null)
                {
                    targetIndices.Add(context.targetIndex);
                }
            }

            return targetIndices;
        }

        internal static string FormatStateLabel(
            int stateIndex,
            string label,
            HashSet<int> targetIndices)
        {
            string text = string.IsNullOrWhiteSpace(label) ? stateIndex.ToString() : label;
            return targetIndices != null && targetIndices.Contains(stateIndex)
                ? ColorizeTarget(text)
                : text;
        }
    }
}
