using RbnVisualisation.AttractorLandscape;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    public readonly struct GenomePathChangeTypeStyle
    {
        public GenomePathChangeTypeStyle(string changeType, string label, Color color)
        {
            ChangeType = changeType;
            Label = label;
            Color = color;
        }

        public string ChangeType { get; }
        public string Label { get; }
        public Color Color { get; }
    }

    public static class GenomePathChangeTypeStyles
    {
        public const string OnlyIrrelevantTransitionsChanged = "only_irrelevant_transitions_changed";
        public const string PathChangedCycleUnchanged = "path_transitions_changed_attractor_cycle_unchanged";
        public const string CycleChangedFactorScoresUnchanged = "attractor_cycle_changed_factor_scores_unchanged";

        private static readonly Color IrrelevantTransitionColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        private static readonly Color PathChangingNeutralColor = new Color(0.13f, 0.77f, 0.37f, 1f);
        private static readonly Color AttractorChangingNeutralColor = new Color(0.08f, 0.45f, 1f, 1f);
        private static readonly Color FactorChangingNeutralColor = new Color(0.98f, 0.55f, 0.12f, 1f);
        private static readonly Color DirectImprovementColor = new Color(0.78f, 0.80f, 0.84f, 1f);
        private static readonly Color CompensatoryImprovementColor = new Color(0.42f, 0.44f, 0.48f, 1f);

        public static readonly GenomePathChangeTypeStyle[] FactorNeutralSubcategoryLegendEntries =
        {
            new(OnlyIrrelevantTransitionsChanged, "Irrelevant-transition change", IrrelevantTransitionColor),
            new(PathChangedCycleUnchanged, "Path-changing neutral", PathChangingNeutralColor),
            new(CycleChangedFactorScoresUnchanged, "Attractor-changing neutral", AttractorChangingNeutralColor),
        };

        public static readonly GenomePathChangeTypeStyle[] LegendEntries =
        {
            new("factor_neutral", "Factor neutral", new Color(0.52f, 0.68f, 0.88f, 1f)),
            new("factor_changing_neutral", "Factor-changing neutral", FactorChangingNeutralColor),
            new("direct_improvement", "Direct improvement", DirectImprovementColor),
            new("compensatory_improvement", "Compensatory improvement", CompensatoryImprovementColor),
        };

        public static readonly GenomePathChangeTypeStyle[] Option10LegendEntries =
        {
            FactorNeutralSubcategoryLegendEntries[0],
            FactorNeutralSubcategoryLegendEntries[1],
            FactorNeutralSubcategoryLegendEntries[2],
            LegendEntries[1],
            LegendEntries[2],
            LegendEntries[3],
        };

        public static Color GetColor(string changeType, Color fallback)
        {
            return GetColor(changeType, fallback, attractorRun: null, previousStep: -1, currentStep: -1);
        }

        public static Color GetColor(
            string changeType,
            Color fallback,
            BooleanAttractorLandscapeRunData attractorRun,
            int previousStep,
            int currentStep)
        {
            string resolvedChangeType = ResolveSegmentChangeType(
                changeType,
                attractorRun,
                previousStep,
                currentStep);
            return GetColorForResolvedChangeType(resolvedChangeType, fallback);
        }

        public static string ResolveSegmentChangeType(
            string changeType,
            BooleanAttractorLandscapeRunData attractorRun,
            int previousStep,
            int currentStep)
        {
            string normalizedChangeType = NormalizeChangeType(changeType);
            if (IsFactorNeutralSubcategory(normalizedChangeType))
            {
                return normalizedChangeType;
            }

            if (normalizedChangeType != "factor_neutral")
            {
                return normalizedChangeType;
            }

            if (attractorRun != null && previousStep >= 0 && currentStep > previousStep)
            {
                return PlateauEpisodeAnalyzer.ClassifyFactorNeutralMove(
                    attractorRun,
                    previousStep,
                    currentStep);
            }

            return normalizedChangeType;
        }

        private static Color GetColorForResolvedChangeType(string resolvedChangeType, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(resolvedChangeType))
            {
                return fallback;
            }

            for (int i = 0; i < FactorNeutralSubcategoryLegendEntries.Length; i++)
            {
                if (FactorNeutralSubcategoryLegendEntries[i].ChangeType == resolvedChangeType)
                {
                    return FactorNeutralSubcategoryLegendEntries[i].Color;
                }
            }

            for (int i = 0; i < LegendEntries.Length; i++)
            {
                if (LegendEntries[i].ChangeType == resolvedChangeType)
                {
                    return LegendEntries[i].Color;
                }
            }

            return fallback;
        }

        public static string NormalizeChangeType(string changeType)
        {
            if (string.IsNullOrWhiteSpace(changeType))
            {
                return string.Empty;
            }

            if (changeType == "partial_improvement"
                || changeType == "pure_improvement"
                || changeType == "partial_global_improvement")
            {
                return "direct_improvement";
            }

            return changeType;
        }

        public static bool IsFactorNeutralSubcategory(string changeType)
        {
            string normalizedChangeType = NormalizeChangeType(changeType);
            return normalizedChangeType == OnlyIrrelevantTransitionsChanged
                   || normalizedChangeType == PathChangedCycleUnchanged
                   || normalizedChangeType == CycleChangedFactorScoresUnchanged;
        }

        public static bool IsNeutralMove(string changeType)
        {
            string normalizedChangeType = NormalizeChangeType(changeType);
            return normalizedChangeType == "factor_neutral"
                   || normalizedChangeType == "factor_changing_neutral"
                   || IsFactorNeutralSubcategory(normalizedChangeType);
        }

        public static bool IsImprovementMove(string changeType)
        {
            string normalizedChangeType = NormalizeChangeType(changeType);
            return normalizedChangeType == "direct_improvement"
                   || normalizedChangeType == "compensatory_improvement";
        }

        public enum PlateauDriftCategoryBucket
        {
            NeutralMove,
            PathChangedAttractorUnchanged,
            AttractorChanged,
        }

        private static readonly Color DriftNeutralMoveColor = IrrelevantTransitionColor;
        private static readonly Color DriftPathChangedColor = PathChangingNeutralColor;
        private static readonly Color DriftAttractorChangedColor = AttractorChangingNeutralColor;

        public static PlateauDriftCategoryBucket GetDriftCategoryBucket(string changeType)
        {
            string normalizedChangeType = NormalizeChangeType(changeType);
            if (normalizedChangeType == PathChangedCycleUnchanged)
            {
                return PlateauDriftCategoryBucket.PathChangedAttractorUnchanged;
            }

            if (normalizedChangeType == CycleChangedFactorScoresUnchanged
                || normalizedChangeType == "factor_changing_neutral")
            {
                return PlateauDriftCategoryBucket.AttractorChanged;
            }

            return PlateauDriftCategoryBucket.NeutralMove;
        }

        public static Color GetDriftCategoryColor(string changeType)
        {
            return GetDriftCategoryColor(GetDriftCategoryBucket(changeType));
        }

        public static Color GetDriftCategoryColor(PlateauDriftCategoryBucket bucket)
        {
            switch (bucket)
            {
                case PlateauDriftCategoryBucket.PathChangedAttractorUnchanged:
                    return DriftPathChangedColor;
                case PlateauDriftCategoryBucket.AttractorChanged:
                    return DriftAttractorChangedColor;
                default:
                    return DriftNeutralMoveColor;
            }
        }

        public static string GetDriftCategoryLabel(PlateauDriftCategoryBucket bucket)
        {
            switch (bucket)
            {
                case PlateauDriftCategoryBucket.PathChangedAttractorUnchanged:
                    return "Path-changing neutral";
                case PlateauDriftCategoryBucket.AttractorChanged:
                    return "Attractor-changing neutral";
                default:
                    return "Irrelevant-transition change";
            }
        }
    }
}
