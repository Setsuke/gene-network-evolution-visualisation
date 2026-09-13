using System;
using System.Collections.Generic;
using System.Text;
using RbnVisualisation.GenomePath;

namespace RbnVisualisation.AttractorLandscape
{
    public static class PlateauDriftAttractorSummaryFormatter
    {
        public static string FormatSummary(
            PlateauEpisode episode,
            BooleanAttractorLandscapeRunData run,
            Func<int, string> resolveStateLabel)
        {
            if (episode == null || run?.steps == null)
            {
                return "No attractor drift summary available.";
            }

            if (episode.StartStep < 0
                || episode.EndStep >= run.steps.Length
                || episode.ExitStep >= run.steps.Length)
            {
                return "Attractor drift summary: step range out of bounds.";
            }

            BooleanAttractorLandscapeStepData driftStart = run.steps[episode.StartStep];
            BooleanAttractorLandscapeStepData driftEnd = run.steps[episode.EndStep];
            BooleanAttractorLandscapeStepData exitStep = run.steps[episode.ExitStep];

            var builder = new StringBuilder();
            AppendChangesSection(
                builder,
                driftStart,
                driftEnd,
                "Changes during drift",
                resolveStateLabel);
            builder.AppendLine();
            AppendChangesSection(
                builder,
                driftStart,
                exitStep,
                "Changes from drift start to exit",
                resolveStateLabel);

            return builder.ToString().TrimEnd();
        }

        private static void AppendChangesSection(
            StringBuilder builder,
            BooleanAttractorLandscapeStepData startStep,
            BooleanAttractorLandscapeStepData endStep,
            string changesLabel,
            Func<int, string> resolveStateLabel)
        {
            List<string> startPoints = ExtractPointAttractors(startStep, resolveStateLabel);
            List<string> endPoints = ExtractPointAttractors(endStep, resolveStateLabel);
            List<LabeledCycle> startCycles = ExtractLimitCycleEntries(startStep, resolveStateLabel);
            List<LabeledCycle> endCycles = ExtractLimitCycleEntries(endStep, resolveStateLabel);

            builder.AppendLine($"<color=#9AA8BC>{changesLabel}:</color>");
            AppendPointChanges(builder, startPoints, endPoints);
            AppendLimitCycleChanges(builder, startCycles, endCycles);
        }

        private sealed class LabeledCycle
        {
            public string Display;
            public string CanonicalKey;
        }

        private static void AppendPointChanges(
            StringBuilder builder,
            List<string> startPoints,
            List<string> endPoints)
        {
            DiffSets(startPoints, endPoints, out _, out List<string> removed, out List<string> added);
            AppendAggregateChangeLine(builder, "Point attractors", removed.Count, added.Count);
        }

        private static void AppendLimitCycleChanges(
            StringBuilder builder,
            List<LabeledCycle> startCycles,
            List<LabeledCycle> endCycles)
        {
            DiffCycleSets(
                startCycles,
                endCycles,
                out _,
                out List<string> removed,
                out List<string> added);
            AppendAggregateChangeLine(builder, "Limit cycles", removed.Count, added.Count);
        }

        private static void AppendAggregateChangeLine(
            StringBuilder builder,
            string label,
            int removedCount,
            int addedCount)
        {
            builder.Append("  ");
            builder.Append(label);
            builder.Append(": ");

            if (removedCount == 0 && addedCount == 0)
            {
                builder.AppendLine("no change");
                return;
            }

            if (removedCount == 1 && addedCount == 1)
            {
                builder.AppendLine("1 changed");
                return;
            }

            var parts = new List<string>();
            if (removedCount > 0)
            {
                parts.Add($"{removedCount} removed");
            }

            if (addedCount > 0)
            {
                parts.Add($"{addedCount} added");
            }

            builder.AppendLine(string.Join(", ", parts));
        }

        private static void DiffCycleSets(
            List<LabeledCycle> start,
            List<LabeledCycle> end,
            out List<string> unchanged,
            out List<string> removed,
            out List<string> added)
        {
            unchanged = new List<string>();
            removed = new List<string>();
            added = new List<string>();

            var startBuckets = BucketCyclesByCanonicalKey(start);
            var endBuckets = BucketCyclesByCanonicalKey(end);
            var allKeys = new HashSet<string>(startBuckets.Keys);
            allKeys.UnionWith(endBuckets.Keys);

            foreach (string canonicalKey in allKeys)
            {
                startBuckets.TryGetValue(canonicalKey, out List<LabeledCycle> startMatches);
                endBuckets.TryGetValue(canonicalKey, out List<LabeledCycle> endMatches);
                startMatches ??= new List<LabeledCycle>();
                endMatches ??= new List<LabeledCycle>();

                int shared = Math.Min(startMatches.Count, endMatches.Count);
                for (int index = 0; index < shared; index++)
                {
                    unchanged.Add(startMatches[index].Display);
                }

                for (int index = shared; index < startMatches.Count; index++)
                {
                    removed.Add(startMatches[index].Display);
                }

                for (int index = shared; index < endMatches.Count; index++)
                {
                    added.Add(endMatches[index].Display);
                }
            }
        }

        private static Dictionary<string, List<LabeledCycle>> BucketCyclesByCanonicalKey(
            List<LabeledCycle> cycles)
        {
            var buckets = new Dictionary<string, List<LabeledCycle>>();
            for (int index = 0; index < cycles.Count; index++)
            {
                LabeledCycle cycle = cycles[index];
                if (!buckets.TryGetValue(cycle.CanonicalKey, out List<LabeledCycle> bucket))
                {
                    bucket = new List<LabeledCycle>();
                    buckets[cycle.CanonicalKey] = bucket;
                }

                bucket.Add(cycle);
            }

            return buckets;
        }

        private static void DiffSets(
            List<string> start,
            List<string> end,
            out List<string> unchanged,
            out List<string> removed,
            out List<string> added)
        {
            unchanged = new List<string>();
            removed = new List<string>();
            added = new List<string>();

            var startCounts = CountLabels(start);
            var endCounts = CountLabels(end);
            var allLabels = new HashSet<string>(startCounts.Keys);
            allLabels.UnionWith(endCounts.Keys);

            foreach (string label in allLabels)
            {
                startCounts.TryGetValue(label, out int startCount);
                endCounts.TryGetValue(label, out int endCount);
                int shared = Math.Min(startCount, endCount);
                for (int index = 0; index < shared; index++)
                {
                    unchanged.Add(label);
                }

                for (int index = shared; index < startCount; index++)
                {
                    removed.Add(label);
                }

                for (int index = shared; index < endCount; index++)
                {
                    added.Add(label);
                }
            }
        }

        private static Dictionary<string, int> CountLabels(List<string> labels)
        {
            var counts = new Dictionary<string, int>();
            for (int index = 0; index < labels.Count; index++)
            {
                string label = labels[index];
                counts.TryGetValue(label, out int count);
                counts[label] = count + 1;
            }

            return counts;
        }

        private static List<string> ExtractPointAttractors(
            BooleanAttractorLandscapeStepData step,
            Func<int, string> resolveStateLabel)
        {
            var points = new List<string>();
            if (step?.attractors == null)
            {
                return points;
            }

            for (int index = 0; index < step.attractors.Length; index++)
            {
                BooleanAttractorEntryData attractor = step.attractors[index];
                if (attractor?.type != "point"
                    || attractor.stateIndices == null
                    || attractor.stateIndices.Length == 0)
                {
                    continue;
                }

                points.Add(resolveStateLabel(attractor.stateIndices[0]));
            }

            points.Sort(StringComparer.Ordinal);
            return points;
        }

        private static List<LabeledCycle> ExtractLimitCycleEntries(
            BooleanAttractorLandscapeStepData step,
            Func<int, string> resolveStateLabel)
        {
            var cycles = new List<LabeledCycle>();
            if (step?.attractors == null)
            {
                return cycles;
            }

            for (int index = 0; index < step.attractors.Length; index++)
            {
                BooleanAttractorEntryData attractor = step.attractors[index];
                if (attractor?.type != "limit_cycle"
                    || attractor.stateIndices == null
                    || attractor.stateIndices.Length == 0)
                {
                    continue;
                }

                cycles.Add(new LabeledCycle
                {
                    Display = FormatLimitCycle(attractor, resolveStateLabel),
                    CanonicalKey = CanonicalizeLimitCycle(attractor, resolveStateLabel),
                });
            }

            cycles.Sort((left, right) => string.CompareOrdinal(left.CanonicalKey, right.CanonicalKey));
            return cycles;
        }

        private static string CanonicalizeLimitCycle(
            BooleanAttractorEntryData attractor,
            Func<int, string> resolveStateLabel)
        {
            var states = new List<string>(attractor.stateIndices.Length);
            for (int index = 0; index < attractor.stateIndices.Length; index++)
            {
                states.Add(resolveStateLabel(attractor.stateIndices[index]));
            }

            return CanonicalizeCycleStates(states);
        }

        private static string CanonicalizeCycleStates(List<string> states)
        {
            if (states.Count == 0)
            {
                return string.Empty;
            }

            if (states.Count == 1)
            {
                return states[0];
            }

            string best = null;
            for (int offset = 0; offset < states.Count; offset++)
            {
                var rotated = new StringBuilder();
                for (int index = 0; index < states.Count; index++)
                {
                    if (index > 0)
                    {
                        rotated.Append(" -> ");
                    }

                    rotated.Append(states[(offset + index) % states.Count]);
                }

                string candidate = rotated.ToString();
                if (best == null || string.CompareOrdinal(candidate, best) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static string FormatLimitCycle(
            BooleanAttractorEntryData attractor,
            Func<int, string> resolveStateLabel)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < attractor.stateIndices.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(" -> ");
                }

                builder.Append(resolveStateLabel(attractor.stateIndices[index]));
            }

            return builder.ToString();
        }
    }
}
