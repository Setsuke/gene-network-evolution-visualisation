using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    public sealed class GenomeMdsFitnessPathRunState
    {
        public string RunLabel = string.Empty;
        public GameObject PathRoot;
        public int TotalAcceptedGenomes;
        public Vector3[] WorldPositions = System.Array.Empty<Vector3>();
        public float[] FitnessValues = System.Array.Empty<float>();
        public GameObject[] JointObjects = System.Array.Empty<GameObject>();
        public Vector3[] JointBaseScales = System.Array.Empty<Vector3>();
        public Material[] JointBaseMaterials = System.Array.Empty<Material>();
        public float[][] FactorScoresPerStep = System.Array.Empty<float[]>();

        public int PointCount => WorldPositions?.Length ?? 0;
    }
}
