using System;

namespace RbnVisualisation.GenomePath
{
    [Serializable]
    public sealed class PlateauEpisode
    {
        public int EpisodeIndex;
        public int StartStep;
        public int EndStep;
        public int ExitStep;
        public float PlateauFitness;
        public int NeutralMoveCount;
        public int HammingDriftDuringPlateau;
        public int HammingDriftAtExit;
        public int PathChangesDuringPlateau;
        public int CycleChangesDuringPlateau;
        public int FactorScoreChangesDuringPlateau;
        public string ExitType;
        public int[] StepIndices;
        public float[] FitnessTimeline;
        public int[] HammingFromStartTimeline;

        public int SegmentStepCount => ExitStep - StartStep + 1;

        public int GetRelativeStepIndex(int absoluteStepIndex) => absoluteStepIndex - StartStep;
    }
}
