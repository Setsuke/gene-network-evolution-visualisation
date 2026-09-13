using System;
using System.Collections.Generic;
using RbnVisualisation.AttractorLandscape;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    public static class PlateauEpisodeAnalyzer
    {
        private const float FitnessEpsilon = 1e-5f;

        public static PlateauEpisode[] AnalyzeRun(
            GenomePathRunData genomeRun,
            BooleanAttractorLandscapeRunData attractorRun)
        {
            if (genomeRun?.points == null || genomeRun.points.Length < 2)
            {
                return Array.Empty<PlateauEpisode>();
            }

            List<PlateauEpisode> episodes = new List<PlateauEpisode>();
            GenomePathPointData[] points = genomeRun.points;
            int searchStep = 0;

            while (searchStep < points.Length - 1)
            {
                float plateauFitness = points[searchStep].fitness;
                int endStep = searchStep;
                while (endStep + 1 < points.Length
                       && Mathf.Abs(points[endStep + 1].fitness - plateauFitness) <= FitnessEpsilon)
                {
                    endStep++;
                }

                int exitStep = endStep + 1;
                bool hasNeutralMoves = endStep > searchStep;
                bool hasExit = exitStep < points.Length
                               && points[exitStep].fitness > plateauFitness + FitnessEpsilon;

                if (hasNeutralMoves && hasExit)
                {
                    episodes.Add(BuildEpisode(
                        episodes.Count,
                        searchStep,
                        endStep,
                        exitStep,
                        plateauFitness,
                        points,
                        attractorRun));
                }

                searchStep = hasExit ? exitStep : endStep + 1;
            }

            return episodes.ToArray();
        }

        private static PlateauEpisode BuildEpisode(
            int episodeIndex,
            int startStep,
            int endStep,
            int exitStep,
            float plateauFitness,
            GenomePathPointData[] points,
            BooleanAttractorLandscapeRunData attractorRun)
        {
            int segmentLength = exitStep - startStep + 1;
            int[] stepIndices = new int[segmentLength];
            float[] fitnessTimeline = new float[segmentLength];
            int[] hammingFromStartTimeline = new int[segmentLength];

            int cumulativeHamming = 0;
            for (int offset = 0; offset < segmentLength; offset++)
            {
                int absoluteStep = startStep + offset;
                stepIndices[offset] = absoluteStep;
                fitnessTimeline[offset] = points[absoluteStep].fitness;
                if (offset > 0)
                {
                    cumulativeHamming += GetHammingFromPrevious(points, absoluteStep);
                }

                hammingFromStartTimeline[offset] = cumulativeHamming;
            }

            int hammingDuringPlateau = 0;
            for (int step = startStep + 1; step <= endStep; step++)
            {
                hammingDuringPlateau += GetHammingFromPrevious(points, step);
            }

            int hammingAtExit = cumulativeHamming;

            return new PlateauEpisode
            {
                EpisodeIndex = episodeIndex,
                StartStep = startStep,
                EndStep = endStep,
                ExitStep = exitStep,
                PlateauFitness = plateauFitness,
                NeutralMoveCount = endStep - startStep,
                HammingDriftDuringPlateau = hammingDuringPlateau,
                HammingDriftAtExit = hammingAtExit,
                PathChangesDuringPlateau = CountPathChanges(attractorRun, startStep, endStep),
                CycleChangesDuringPlateau = CountCycleChanges(attractorRun, startStep, endStep),
                FactorScoreChangesDuringPlateau = CountFactorScoreChanges(points, startStep, endStep),
                ExitType = ResolveExitType(points, exitStep),
                StepIndices = stepIndices,
                FitnessTimeline = fitnessTimeline,
                HammingFromStartTimeline = hammingFromStartTimeline,
            };
        }

        private static int GetHammingFromPrevious(GenomePathPointData[] points, int stepIndex)
        {
            if (stepIndex <= 0 || stepIndex >= points.Length)
            {
                return 0;
            }

            int hamming = points[stepIndex].hammingFromPrevious;
            if (hamming > 0)
            {
                return hamming;
            }

            return EstimateHammingFromMds(points[stepIndex - 1], points[stepIndex]);
        }

        private static int EstimateHammingFromMds(GenomePathPointData previous, GenomePathPointData current)
        {
            if (previous.genomeIndex != current.genomeIndex)
            {
                float dx = current.x - previous.x;
                float dy = current.y - previous.y;
                float dz = current.z - previous.z;
                return Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy + dz * dz)));
            }

            return 0;
        }

        public static string ClassifyFactorNeutralMove(
            BooleanAttractorLandscapeRunData attractorRun,
            int previousStep,
            int currentStep)
        {
            if (attractorRun?.steps == null)
            {
                return GenomePathChangeTypeStyles.OnlyIrrelevantTransitionsChanged;
            }

            if (ContextPathsChanged(
                    attractorRun.steps,
                    previousStep,
                    currentStep,
                    comparePaths: false,
                    compareCycles: true))
            {
                return GenomePathChangeTypeStyles.CycleChangedFactorScoresUnchanged;
            }

            if (ContextPathsChanged(
                    attractorRun.steps,
                    previousStep,
                    currentStep,
                    comparePaths: true,
                    compareCycles: false))
            {
                return GenomePathChangeTypeStyles.PathChangedCycleUnchanged;
            }

            return GenomePathChangeTypeStyles.OnlyIrrelevantTransitionsChanged;
        }

        private static string ResolveExitType(GenomePathPointData[] points, int exitStep)
        {
            if (exitStep >= points.Length - 1)
            {
                return "solution";
            }

            string changeType = points[exitStep].changeType ?? string.Empty;
            if (changeType == "partial_improvement"
                || changeType == "pure_improvement"
                || changeType == "partial_global_improvement")
            {
                return "direct improvement";
            }

            if (changeType == "compensatory_improvement")
            {
                return "compensatory improvement";
            }

            if (changeType == "direct_improvement")
            {
                return "direct improvement";
            }

            return string.IsNullOrWhiteSpace(changeType) ? "unknown" : changeType.Replace('_', ' ');
        }

        private static int CountPathChanges(
            BooleanAttractorLandscapeRunData attractorRun,
            int startStep,
            int endStep)
        {
            if (attractorRun?.steps == null)
            {
                return 0;
            }

            int changes = 0;
            for (int step = startStep + 1; step <= endStep; step++)
            {
                if (ContextPathsChanged(attractorRun.steps, step - 1, step, comparePaths: true, compareCycles: false))
                {
                    changes++;
                }
            }

            return changes;
        }

        private static int CountCycleChanges(
            BooleanAttractorLandscapeRunData attractorRun,
            int startStep,
            int endStep)
        {
            if (attractorRun?.steps == null)
            {
                return 0;
            }

            int changes = 0;
            for (int step = startStep + 1; step <= endStep; step++)
            {
                if (ContextPathsChanged(attractorRun.steps, step - 1, step, comparePaths: false, compareCycles: true))
                {
                    changes++;
                }
            }

            return changes;
        }

        private static bool ContextPathsChanged(
            BooleanAttractorLandscapeStepData[] steps,
            int previousStep,
            int currentStep,
            bool comparePaths,
            bool compareCycles)
        {
            if (previousStep < 0 || currentStep >= steps.Length || previousStep >= steps.Length)
            {
                return false;
            }

            BooleanAttractorContextPathData[] previousPaths = steps[previousStep].contextPaths;
            BooleanAttractorContextPathData[] currentPaths = steps[currentStep].contextPaths;
            if (previousPaths == null || currentPaths == null)
            {
                return false;
            }

            int count = Mathf.Min(previousPaths.Length, currentPaths.Length);
            for (int index = 0; index < count; index++)
            {
                if (comparePaths
                    && !IntArraysEqual(previousPaths[index].pathIndices, currentPaths[index].pathIndices))
                {
                    return true;
                }

                if (compareCycles
                    && !AttractorCycleComparison.CyclesEqual(
                        previousPaths[index].cycleIndices,
                        currentPaths[index].cycleIndices))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountFactorScoreChanges(
            GenomePathPointData[] points,
            int startStep,
            int endStep)
        {
            int changes = 0;
            for (int step = startStep + 1; step <= endStep; step++)
            {
                if (!FloatArraysEqual(points[step - 1].factorScores, points[step].factorScores))
                {
                    changes++;
                }
            }

            return changes;
        }

        private static bool IntArraysEqual(int[] left, int[] right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FloatArraysEqual(float[] left, float[] right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                if (Mathf.Abs(left[index] - right[index]) > FitnessEpsilon)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
