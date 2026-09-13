using System.Globalization;

namespace RbnVisualisation.GenomePath
{
    public static class ContextFitnessUtility
    {
        public const int GenesPerContext = 5;

        public static bool TryGetContextFitness(
            GenomePathPointData point,
            out float context1,
            out float context2)
        {
            context1 = 0f;
            context2 = 0f;
            if (point?.contextFitness != null && point.contextFitness.Length >= 2)
            {
                context1 = point.contextFitness[0];
                context2 = point.contextFitness[1];
                return true;
            }

            return TryGetFromFactorScores(point?.factorScores, out context1, out context2);
        }

        public static bool TryGetFromFactorScores(
            float[] factorScores,
            out float context1,
            out float context2)
        {
            context1 = 0f;
            context2 = 0f;
            if (factorScores == null || factorScores.Length < GenesPerContext * 2)
            {
                return false;
            }

            context1 = Product(factorScores, 0, GenesPerContext);
            context2 = Product(factorScores, GenesPerContext, GenesPerContext * 2);
            return true;
        }

        public static string FormatPair(float context1, float context2)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "c1={0:F3} × c2={1:F3} = {2:F4}",
                context1,
                context2,
                context1 * context2);
        }

        public static string FormatPairLine(float context1, float context2)
        {
            return $"Context fitness: {FormatPair(context1, context2)}";
        }

        private static float Product(float[] values, int start, int end)
        {
            float product = 1f;
            for (int index = start; index < end; index++)
            {
                product *= values[index];
            }

            return product;
        }
    }
}
