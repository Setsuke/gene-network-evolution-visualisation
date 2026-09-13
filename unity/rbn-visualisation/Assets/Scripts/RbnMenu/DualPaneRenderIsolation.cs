using UnityEngine;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Keeps each dual-pane half visible to only its camera via dedicated layers.
    /// </summary>
    public static class DualPaneRenderIsolation
    {
        public const string LeftLayerName = "DualPaneLeft";
        public const string RightLayerName = "DualPaneRight";
        private const int FallbackLeftLayerIndex = 8;
        private const int FallbackRightLayerIndex = 9;

        public static void Apply(
            Transform leftContentRoot,
            Transform rightContentRoot,
            Camera leftCamera,
            Camera rightCamera)
        {
            int leftLayer = ResolveLayer(LeftLayerName, FallbackLeftLayerIndex);
            int rightLayer = ResolveLayer(RightLayerName, FallbackRightLayerIndex);
            if (leftLayer == rightLayer)
            {
                Debug.LogError(
                    "DualPaneRenderIsolation: left and right pane layers must differ. " +
                    "Check Project Settings > Tags and Layers.");
                return;
            }

            if (leftContentRoot != null)
            {
                ApplyLayerRecursively(leftContentRoot, leftLayer);
            }

            if (rightContentRoot != null)
            {
                ApplyLayerRecursively(rightContentRoot, rightLayer);
            }

            if (leftCamera != null)
            {
                leftCamera.cullingMask = 1 << leftLayer;
            }

            if (rightCamera != null)
            {
                rightCamera.cullingMask = 1 << rightLayer;
            }
        }

        public static void ApplyLayerRecursively(Transform root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                ApplyLayerRecursively(root.GetChild(childIndex), layer);
            }
        }

        private static int ResolveLayer(string layerName, int fallbackIndex)
        {
            int namedLayer = LayerMask.NameToLayer(layerName);
            if (namedLayer >= 0)
            {
                return namedLayer;
            }

            Debug.LogWarning(
                $"DualPaneRenderIsolation: layer '{layerName}' was not found; using index {fallbackIndex}.");
            return fallbackIndex;
        }
    }
}
