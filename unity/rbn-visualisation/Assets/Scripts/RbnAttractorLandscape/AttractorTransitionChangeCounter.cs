namespace RbnVisualisation.AttractorLandscape
{
    public static class AttractorTransitionChangeCounter
    {
        public static int CountChangedTransitions(
            BooleanAttractorLandscapeStepData previousStep,
            BooleanAttractorLandscapeStepData currentStep)
        {
            if (previousStep?.edgeTo == null || currentStep?.edgeTo == null)
            {
                return 0;
            }

            int[] previousEdges = previousStep.edgeTo;
            int[] currentEdges = currentStep.edgeTo;
            int compareLength = System.Math.Min(previousEdges.Length, currentEdges.Length);
            int changed = 0;

            for (int sourceIndex = 0; sourceIndex < compareLength; sourceIndex++)
            {
                if (previousEdges[sourceIndex] != currentEdges[sourceIndex])
                {
                    changed++;
                }
            }

            if (previousEdges.Length != currentEdges.Length)
            {
                changed += System.Math.Abs(previousEdges.Length - currentEdges.Length);
            }

            return changed;
        }

        public static int CountChangedTransitionsAtStep(
            BooleanAttractorLandscapeRunData run,
            int stepIndex)
        {
            if (run?.steps == null || stepIndex <= 0 || stepIndex >= run.steps.Length)
            {
                return 0;
            }

            return CountChangedTransitions(run.steps[stepIndex - 1], run.steps[stepIndex]);
        }
    }
}
