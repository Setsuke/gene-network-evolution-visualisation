using System;

namespace RbnVisualisation.AttractorLandscape
{
    /// <summary>
    /// Fixed classical MDS layout of the 5-bit Boolean state space (32 nodes).
    /// This is not the 160-bit genome embedding used by Options 2-4.
    /// </summary>
    [Serializable]
    public class BooleanStateLayoutData
    {
        public int schemaVersion;
        public string space;
        public string description;
        public int geneCount;
        public int stateCount;
        public string metric;
        public string method;
        public int dimensions;
        public BooleanStateLayoutNodeData[] nodes;
    }

    [Serializable]
    public class BooleanStateLayoutNodeData
    {
        public int index;
        public string state;
        public string label;
        public float x;
        public float y;
        public float z;
    }
}
