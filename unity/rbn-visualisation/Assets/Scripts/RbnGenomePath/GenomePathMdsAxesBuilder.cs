using System.Collections.Generic;
using RbnVisualisation.Trajectory;
using UnityEngine;
using UnityEngine.Rendering;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Draws labelled X/Y/Z axes through the MDS embedding origin (both directions).
    /// Uses thin 3D cylinders so axis length stays stable at every camera distance.
    /// </summary>
    internal static class GenomePathMdsAxesBuilder
    {
        private static readonly Color XAxisColor = new Color(0.92f, 0.22f, 0.22f, 1f);
        private static readonly Color YAxisColor = new Color(0.95f, 0.92f, 0.12f, 1f);
        private static readonly Color ZAxisColor = new Color(0.23f, 0.51f, 0.96f, 1f);
        private const float XPositiveAxisLengthFactor = 1.85f;

        public static void Build(
            Transform parent,
            Vector3 worldCenter,
            float axisHalfLength,
            float lineWidth,
            bool showLabels,
            ICollection<GameObject> spawnedObjects,
            float xPositiveAxisLengthFactor = XPositiveAxisLengthFactor)
        {
            if (parent == null || spawnedObjects == null || axisHalfLength <= 0.0001f)
            {
                return;
            }

            GameObject axesRoot = new GameObject("MdsAxes");
            axesRoot.transform.SetParent(parent, false);
            axesRoot.transform.localPosition = parent.InverseTransformPoint(worldCenter);
            axesRoot.transform.localRotation = Quaternion.identity;
            spawnedObjects.Add(axesRoot);

            float diameter = Mathf.Max(lineWidth, 0.024f);

            CreateAxis(
                axesRoot.transform,
                worldCenter,
                Vector3.right,
                axisHalfLength,
                axisHalfLength * xPositiveAxisLengthFactor,
                diameter,
                XAxisColor,
                "X",
                labelOnPositiveSide: xPositiveAxisLengthFactor <= 1.001f,
                showLabels,
                spawnedObjects);
            CreateAxis(
                axesRoot.transform,
                worldCenter,
                Vector3.up,
                axisHalfLength,
                axisHalfLength,
                diameter,
                YAxisColor,
                "Y",
                labelOnPositiveSide: true,
                showLabels,
                spawnedObjects);
            CreateAxis(
                axesRoot.transform,
                worldCenter,
                Vector3.forward,
                axisHalfLength,
                axisHalfLength,
                diameter,
                ZAxisColor,
                "Z",
                labelOnPositiveSide: false,
                showLabels,
                spawnedObjects);
        }

        private static Material CreateAxisMaterial(Color color)
        {
            Material material = TrajectoryRenderUtility.CreateOverlayLineMaterial(color);
            material.renderQueue = (int)RenderQueue.Overlay;
            return material;
        }

        private static void CreateAxis(
            Transform parent,
            Vector3 worldCenter,
            Vector3 direction,
            float negativeHalfLength,
            float positiveHalfLength,
            float diameter,
            Color color,
            string label,
            bool labelOnPositiveSide,
            bool showLabel,
            ICollection<GameObject> spawnedObjects)
        {
            Vector3 unit = direction.normalized;
            Vector3 negative = worldCenter - unit * negativeHalfLength;
            Vector3 positive = worldCenter + unit * positiveHalfLength;

            Material material = CreateAxisMaterial(color);

            CreateAxisTube(parent, negative, positive, diameter, material, $"Axis_{label}", spawnedObjects);

            if (showLabel)
            {
                Vector3 labelTip = labelOnPositiveSide ? positive : negative;
                Vector3 labelDirection = labelOnPositiveSide ? unit : -unit;
                CreateAxisLabel(parent, labelTip, labelDirection, label, color, spawnedObjects);
            }
        }

        private static void CreateAxisTube(
            Transform parent,
            Vector3 start,
            Vector3 end,
            float diameter,
            Material material,
            string objectName,
            ICollection<GameObject> spawnedObjects)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.0001f)
            {
                return;
            }

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            segment.name = objectName;
            segment.layer = parent.gameObject.layer;
            segment.transform.SetParent(parent, false);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            segment.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            spawnedObjects.Add(segment);

            Renderer renderer = segment.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Collider collider = segment.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private static void CreateAxisLabel(
            Transform parent,
            Vector3 worldTip,
            Vector3 direction,
            string label,
            Color color,
            ICollection<GameObject> spawnedObjects)
        {
            GameObject labelObject = new GameObject($"AxisLabel_{label}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = worldTip + direction * 0.12f;
            spawnedObjects.Add(labelObject);

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.06f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            labelObject.AddComponent<BillboardLabel>();
        }
    }
}
