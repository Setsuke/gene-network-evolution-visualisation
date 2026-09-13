using System.Collections.Generic;

namespace RbnVisualisation.AttractorLandscape
{
    public static class AttractorCycleComparison
    {
        public static bool CyclesEqual(int[] left, int[] right)
        {
            if (left == null && right == null)
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            if (left.Length <= 1)
            {
                return left.Length == 0 || left[0] == right[0];
            }

            return CanonicalCycleKey(left) == CanonicalCycleKey(right);
        }

        public static string CanonicalCycleKey(int[] cycleIndices)
        {
            if (cycleIndices == null || cycleIndices.Length == 0)
            {
                return string.Empty;
            }

            if (cycleIndices.Length == 1)
            {
                return cycleIndices[0].ToString();
            }

            string best = null;
            for (int offset = 0; offset < cycleIndices.Length; offset++)
            {
                var parts = new List<string>(cycleIndices.Length);
                for (int index = 0; index < cycleIndices.Length; index++)
                {
                    parts.Add(cycleIndices[(offset + index) % cycleIndices.Length].ToString());
                }

                string candidate = string.Join(",", parts);
                if (best == null || string.CompareOrdinal(candidate, best) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }
    }
}
