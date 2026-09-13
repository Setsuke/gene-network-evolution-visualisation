using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    internal static class SharedGenomeHammingUtility
    {
        [Serializable]
        private sealed class GenomeSidecarData
        {
            public string[] genomes;
        }

        public static int HammingDistance(string left, string right)
        {
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            {
                return 0;
            }

            int length = Math.Min(left.Length, right.Length);
            int distance = 0;
            for (int index = 0; index < length; index++)
            {
                if (left[index] != right[index])
                {
                    distance++;
                }
            }

            distance += Math.Abs(left.Length - right.Length);
            return distance;
        }

        public static bool TryLoadGenomeSidecar(string pathJsonFileName, out string[] genomes)
        {
            genomes = null;
            if (string.IsNullOrWhiteSpace(pathJsonFileName))
            {
                return false;
            }

            string sidecarFileName = $"{Path.GetFileNameWithoutExtension(pathJsonFileName)}_genomes.json";
            string streamingPath = Path.Combine(Application.streamingAssetsPath, sidecarFileName);
            if (!File.Exists(streamingPath))
            {
#if UNITY_EDITOR
                string editorPath = $"Assets/StreamingAssets/{sidecarFileName}";
                TextAsset editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(editorPath);
                if (editorAsset != null)
                {
                    return TryParseGenomeSidecar(editorAsset.text, out genomes);
                }
#endif
                return false;
            }

            return TryParseGenomeSidecar(File.ReadAllText(streamingPath), out genomes);
        }

        public static bool TryCollectGenomeIndicesAtOrAboveHamming(
            string pathJsonFileName,
            int initialGenomeIndex,
            int threshold,
            out HashSet<int> genomeIndices)
        {
            genomeIndices = new HashSet<int>();
            if (!TryLoadGenomeSidecar(pathJsonFileName, out string[] genomes))
            {
                return false;
            }

            if (initialGenomeIndex < 0 || initialGenomeIndex >= genomes.Length)
            {
                return false;
            }

            string initialGenome = genomes[initialGenomeIndex];
            for (int index = 0; index < genomes.Length; index++)
            {
                if (index == initialGenomeIndex)
                {
                    continue;
                }

                if (HammingDistance(initialGenome, genomes[index]) >= threshold)
                {
                    genomeIndices.Add(index);
                }
            }

            return true;
        }

        public static bool TryCollectGenomeIndicesAtOrAboveHammingFromPoints(
            GenomePathBatchData batch,
            int threshold,
            out HashSet<int> genomeIndices)
        {
            genomeIndices = new HashSet<int>();
            if (batch?.runs == null)
            {
                return false;
            }

            bool foundHammingFromInitial = false;
            Dictionary<int, int> distanceByGenomeIndex = new Dictionary<int, int>();

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    GenomePathPointData point = points[pointIndex];
                    if (point == null)
                    {
                        continue;
                    }

                    if (point.hammingFromInitial > 0)
                    {
                        foundHammingFromInitial = true;
                    }

                    if (!distanceByGenomeIndex.TryGetValue(point.genomeIndex, out int existing)
                        || point.hammingFromInitial > existing)
                    {
                        distanceByGenomeIndex[point.genomeIndex] = point.hammingFromInitial;
                    }
                }
            }

            if (!foundHammingFromInitial)
            {
                genomeIndices.Clear();
                return false;
            }

            foreach (KeyValuePair<int, int> entry in distanceByGenomeIndex)
            {
                if (entry.Key == batch.initialGenomeIndex)
                {
                    continue;
                }

                if (entry.Value >= threshold)
                {
                    genomeIndices.Add(entry.Key);
                }
            }

            return genomeIndices.Count > 0;
        }

        private static bool TryParseGenomeSidecar(string jsonText, out string[] genomes)
        {
            genomes = null;
            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return false;
            }

            GenomeSidecarData payload = JsonUtility.FromJson<GenomeSidecarData>(jsonText);
            if (payload?.genomes == null || payload.genomes.Length == 0)
            {
                return false;
            }

            genomes = payload.genomes;
            return true;
        }
    }
}
