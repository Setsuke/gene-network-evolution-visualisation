using System;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Per-accepted-genome transition dynamics over the fixed Boolean state layout.
    /// Genome changes update edges and highlights only; node positions stay fixed.
    /// </summary>
    [Serializable]
    public class BooleanAttractorLandscapeBatchData
    {
        public int schemaVersion;
        public string layoutFile;
        public string space;
        public string description;
        public int geneCount;
        public int stateCount;
        public BooleanAttractorContextData[] contexts;
        public int runCount;
        public BooleanAttractorLandscapeRunData[] runs;
    }

    [Serializable]
    public class BooleanAttractorContextData
    {
        public string label;
        public string initialState;
        public string targetState;
        public int initialIndex;
        public int targetIndex;
    }

    [Serializable]
    public class BooleanAttractorLandscapeRunData
    {
        public string sourceLabel;
        public float finalFitness;
        public int acceptedCount;
        public BooleanAttractorLandscapeStepData[] steps;
    }

    [Serializable]
    public class BooleanAttractorLandscapeStepData
    {
        public int step;
        public int generation;
        public float fitness;
        public string changeType;
        public int[] edgeTo;
        public BooleanAttractorContextPathData[] contextPaths;
        public BooleanAttractorEntryData[] attractors;
    }

    [Serializable]
    public class BooleanAttractorContextPathData
    {
        public int initialIndex;
        public int targetIndex;
        public string initialState;
        public string targetState;
        public int[] pathIndices;
        public int[] cycleIndices;
        public float fitness;
    }

    [Serializable]
    public class BooleanAttractorEntryData
    {
        public string type;
        public int[] stateIndices;
    }
}
