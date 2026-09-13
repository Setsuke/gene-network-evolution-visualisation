using System;
using System.Collections.Generic;
using UnityEngine;

namespace RbnVisualisation.AttractorLandscape
{
    public struct HammingLayerWallDescriptor
    {
        public int HammingWeight;
        public Vector3 Center;
        public float Width;
        public float Height;
        public float DepthZ;
    }

    public static class BooleanHammingLayerLayout
    {
        public static int HammingWeight(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                return 0;
            }

            int weight = 0;
            for (int index = 0; index < state.Length; index++)
            {
                if (state[index] == '1')
                {
                    weight++;
                }
            }

            return weight;
        }

        /// <summary>
        /// Places each Hamming-weight layer on a vertical wall in 3D.
        /// 00000 sits on the first wall (low/front); 11111 on the last wall (high/back).
        /// States within a layer are arranged on the wall face (X horizontal, Y vertical).
        /// Walls are separated along +Z.
        /// </summary>
        public static void AssignLayeredWallPositions(
            int stateCount,
            int geneCount,
            Func<int, string> resolveStateLabel,
            float wallDepthSpacing,
            float verticalTierSpacing,
            float nodeSpacing,
            float wallPadding,
            Vector3[] positions,
            List<HammingLayerWallDescriptor> wallDescriptors)
        {
            if (positions == null || positions.Length != stateCount)
            {
                throw new ArgumentException("positions array must match stateCount.");
            }

            wallDescriptors?.Clear();

            var layers = new List<int>[geneCount + 1];
            for (int weight = 0; weight <= geneCount; weight++)
            {
                layers[weight] = new List<int>();
            }

            for (int index = 0; index < stateCount; index++)
            {
                int weight = HammingWeight(resolveStateLabel(index));
                weight = Mathf.Clamp(weight, 0, geneCount);
                layers[weight].Add(index);
            }

            for (int weight = 0; weight <= geneCount; weight++)
            {
                List<int> layer = layers[weight];
                layer.Sort((left, right) => string.CompareOrdinal(
                    resolveStateLabel(left),
                    resolveStateLabel(right)));

                float depthZ = weight * wallDepthSpacing;
                float tierBaseY = weight * verticalTierSpacing;
                int count = layer.Count;

                float layerMinX = float.PositiveInfinity;
                float layerMaxX = float.NegativeInfinity;
                float layerMinY = float.PositiveInfinity;
                float layerMaxY = float.NegativeInfinity;

                if (TryGetCustomRowCounts(count, out int[] rowCounts))
                {
                    PlaceLayerStatesInRows(
                        layer,
                        rowCounts,
                        tierBaseY,
                        depthZ,
                        nodeSpacing,
                        positions,
                        ref layerMinX,
                        ref layerMaxX,
                        ref layerMinY,
                        ref layerMaxY);
                }
                else
                {
                    ComputeGridSize(count, out int columnCount, out int rowCount);

                    float gridWidth = columnCount > 1 ? (columnCount - 1) * nodeSpacing : 0f;
                    float gridHeight = rowCount > 1 ? (rowCount - 1) * nodeSpacing : 0f;
                    float startX = -gridWidth * 0.5f;
                    float startY = tierBaseY - gridHeight * 0.5f;

                    for (int layerIndex = 0; layerIndex < count; layerIndex++)
                    {
                        int stateIndex = layer[layerIndex];
                        int column = layerIndex % columnCount;
                        int row = layerIndex / columnCount;
                        float x = startX + column * nodeSpacing;
                        float y = startY + row * nodeSpacing;
                        Vector3 position = new Vector3(x, y, depthZ);
                        positions[stateIndex] = position;

                        layerMinX = Mathf.Min(layerMinX, x);
                        layerMaxX = Mathf.Max(layerMaxX, x);
                        layerMinY = Mathf.Min(layerMinY, y);
                        layerMaxY = Mathf.Max(layerMaxY, y);
                    }
                }

                if (count == 0)
                {
                    continue;
                }

                float wallWidth = Mathf.Max(layerMaxX - layerMinX, nodeSpacing) + wallPadding * 2f;
                float wallHeight = Mathf.Max(layerMaxY - layerMinY, nodeSpacing) + wallPadding * 2f;
                float centerX = count == 1 ? 0f : (layerMinX + layerMaxX) * 0.5f;
                float centerY = count == 1 ? tierBaseY : (layerMinY + layerMaxY) * 0.5f;

                wallDescriptors?.Add(new HammingLayerWallDescriptor
                {
                    HammingWeight = weight,
                    Center = new Vector3(centerX, centerY, depthZ),
                    Width = wallWidth,
                    Height = wallHeight,
                    DepthZ = depthZ,
                });
            }
        }

        public static void ComputeContentBounds(
            Vector3[] positions,
            out Vector3 contentCenter,
            out float contentExtent)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            if (positions == null || positions.Length == 0)
            {
                contentCenter = Vector3.zero;
                contentExtent = 1f;
                return;
            }

            for (int index = 0; index < positions.Length; index++)
            {
                min = Vector3.Min(min, positions[index]);
                max = Vector3.Max(max, positions[index]);
            }

            contentCenter = (min + max) * 0.5f;
            Vector3 size = max - min;
            contentExtent = Mathf.Max(size.x, size.y, size.z, 1f);
        }

        private static bool TryGetCustomRowCounts(int count, out int[] rowCounts)
        {
            switch (count)
            {
            case 5:
                rowCounts = new[] { 2, 3 };
                return true;
            case 10:
                rowCounts = new[] { 3, 4, 3 };
                return true;
            default:
                rowCounts = null;
                return false;
            }
        }

        private static void PlaceLayerStatesInRows(
            List<int> layer,
            int[] rowCounts,
            float tierBaseY,
            float depthZ,
            float nodeSpacing,
            Vector3[] positions,
            ref float layerMinX,
            ref float layerMaxX,
            ref float layerMinY,
            ref float layerMaxY)
        {
            int rowCount = rowCounts.Length;
            float gridHeight = rowCount > 1 ? (rowCount - 1) * nodeSpacing : 0f;
            float startY = tierBaseY - gridHeight * 0.5f;
            int stateOrdinal = 0;

            for (int row = 0; row < rowCount; row++)
            {
                int itemsInRow = rowCounts[row];
                float rowWidth = itemsInRow > 1 ? (itemsInRow - 1) * nodeSpacing : 0f;
                float rowStartX = -rowWidth * 0.5f;
                float y = startY + row * nodeSpacing;

                for (int column = 0; column < itemsInRow; column++)
                {
                    int stateIndex = layer[stateOrdinal++];
                    float x = rowStartX + column * nodeSpacing;
                    Vector3 position = new Vector3(x, y, depthZ);
                    positions[stateIndex] = position;

                    layerMinX = Mathf.Min(layerMinX, x);
                    layerMaxX = Mathf.Max(layerMaxX, x);
                    layerMinY = Mathf.Min(layerMinY, y);
                    layerMaxY = Mathf.Max(layerMaxY, y);
                }
            }
        }

        private static void ComputeGridSize(int count, out int columnCount, out int rowCount)
        {
            if (count <= 1)
            {
                columnCount = 1;
                rowCount = 1;
                return;
            }

            columnCount = Mathf.CeilToInt(Mathf.Sqrt(count));
            rowCount = Mathf.CeilToInt(count / (float)columnCount);
        }
    }
}
