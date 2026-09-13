using System;

namespace RbnVisualisation.GenomePath
{
    [Serializable]
    public class GenomePathContextData
    {
        public string label;
        public string target;
    }

    [Serializable]
    public class GenomeReferencePointData
    {
        public int genomeIndex;
        public int sourceGenomeIndex;
        public int hammingDistance;
        public string role;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class GenomePathBatchData
    {
        public int schemaVersion;
        public string method;
        public string metric;
        public int dimensions;
        public int runCount;
        public int genomeCount;
        public int geneCount;
        public int contextCount;
        public bool sharedInitialGenome;
        public bool solutionsOnly;
        public int initialGenomeIndex;
        public int referencePointCount;
        public GenomePathContextData[] contexts;
        public GenomePathRunData[] runs;
        public GenomeReferencePointData[] referencePoints;
    }

    [Serializable]
    public class GenomePathRunData
    {
        public string sourceLabel;
        public float finalFitness;
        public int acceptedCount;
        public GenomePathPointData[] points;
    }

    [Serializable]
    public class GenomePathPointData
    {
        public int step;
        public string genomeId;
        public int genomeIndex;
        public float fitness;
        public float x;
        public float y;
        public float z;
        public string changeType;
        public float[] factorScores;
        public float[] contextFitness;
        public int hammingFromPrevious;
        public int hammingFromInitial;
    }
}
