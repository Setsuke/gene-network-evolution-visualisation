using System.Collections.Generic;
using System.IO;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.AttractorLandscape
{
    public enum BooleanLandscapeLayoutMode
    {
        MdsEmbedding,
        HammingLayers,
    }

    /// <summary>
    /// Fixed-layout attractor landscape for the 5-bit Boolean state space.
    /// Node positions come from Hamming-distance classical MDS and never move.
    /// Selecting a different accepted genome only updates transition edges and highlights.
    /// This is different from the genome-space MDS visualisation of accepted 160-bit genomes.
    /// </summary>
    public class BooleanAttractorLandscapeVisualiser : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string layoutFileName = "boolean_state_mds_layout.json";
        [SerializeField] private string dynamicsFileName = "unity_boolean_attractor_landscape.json";
        [SerializeField] private TextAsset layoutOverride;
        [SerializeField] private TextAsset dynamicsOverride;

        [Header("Layout")]
        [SerializeField] private BooleanLandscapeLayoutMode layoutMode = BooleanLandscapeLayoutMode.MdsEmbedding;
        [SerializeField] private float targetExtent = 16f;
        [SerializeField] private float layerSpacing = 1.5f;
        [SerializeField] private float layerVerticalTierSpacing = 1.35f;
        [SerializeField] private float layerNodeSpacing = 1.85f;
        [SerializeField] private float layerWallPadding = 0.9f;
        [SerializeField] private float nodeDiameter = 0.22f;
        [SerializeField] private bool showStateLabels;
        [SerializeField] private float labelCharacterSize = 0.08f;
        [SerializeField] private Color labelColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        [SerializeField] private float labelVerticalOffset = 0.28f;
        [SerializeField] private float highlightedNodeDiameter = 0.34f;
        [SerializeField] private float initialTargetNodeDiameter = 0.4f;

        [Header("Edges")]
        [SerializeField] private float transitionEdgeWidth = 0.012f;
        [SerializeField] private float pathEdgeWidth = 0.05f;
        [SerializeField] private float cycleEdgeWidth = 0.045f;

        [Header("Colours")]
        [SerializeField] private Color baseNodeColor = new Color(0.72f, 0.74f, 0.78f, 1f);
        [SerializeField] private Color pathNodeColor = new Color(0.98f, 0.82f, 0.18f, 1f);
        [SerializeField] private Color cycleNodeColor = new Color(0.18f, 0.82f, 0.38f, 1f);
        [SerializeField] private Color initialNodeColor = new Color(0.2f, 0.78f, 0.95f, 1f);
        [SerializeField] private Color targetNodeColor = new Color(0.92f, 0.22f, 0.22f, 1f);
        [SerializeField] private Color attractorPointColor = new Color(0.28f, 0.52f, 0.98f, 1f);
        [SerializeField] private Color limitCycleNodeColor = new Color(0.98f, 0.58f, 0.12f, 1f);
        [SerializeField] private Color transitionEdgeColor = new Color(0.82f, 0.84f, 0.88f, 0.18f);
        [SerializeField] private Color pathEdgeColor = new Color(0.98f, 0.82f, 0.18f, 0.95f);
        [SerializeField] private Color cycleEdgeColor = new Color(0.18f, 0.82f, 0.38f, 0.95f);
        [SerializeField] private Color[] contextAttractorColors =
        {
            new Color(0.18f, 0.77f, 0.71f, 1f),
            new Color(0.91f, 0.28f, 0.56f, 1f),
            new Color(0.68f, 0.45f, 0.98f, 1f),
            new Color(0.98f, 0.62f, 0.18f, 1f),
        };

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly List<Renderer> nodeRenderers = new List<Renderer>();
        private readonly List<LineRenderer> transitionLines = new List<LineRenderer>();
        private readonly List<LineRenderer> pathLines = new List<LineRenderer>();
        private readonly List<LineRenderer> cycleLines = new List<LineRenderer>();

        private BooleanStateLayoutData layoutData;
        private BooleanAttractorLandscapeBatchData batchData;
        private string[] stateLabelsByIndex = System.Array.Empty<string>();
        private Vector3[] nodePositions = System.Array.Empty<Vector3>();
        private Material transitionEdgeMaterial;
        private Material pathEdgeMaterial;
        private Material cycleEdgeMaterial;
        private Material baseNodeMaterial;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private int activeRunIndex;
        private int activeStepIndex = -1;
        private BooleanAttractorLandscapeHighlightMode highlightMode =
            BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
        private GameObject[] stateLabelObjects = System.Array.Empty<GameObject>();

        private bool LabelOnlyAttractorNodes =>
            layoutMode == BooleanLandscapeLayoutMode.HammingLayers && showStateLabels;

        public BooleanAttractorLandscapeHighlightMode HighlightMode => highlightMode;
        public bool ShowContextPathEdges =>
            highlightMode == BooleanAttractorLandscapeHighlightMode.ContextPath;
        public bool ShowContextAttractors =>
            highlightMode == BooleanAttractorLandscapeHighlightMode.ContextAttractors;

        public Transform OrbitFocus
        {
            get
            {
                EnsureOrbitFocus();
                return orbitFocus;
            }
        }

        public Vector3 ContentCenter => contentCenter;
        public float ContentExtent => contentExtent;
        public float MinOrbitDistance => Mathf.Max(nodeDiameter * 4f, 0.5f);
        public float RecommendedFocusDistance =>
            layoutMode == BooleanLandscapeLayoutMode.HammingLayers
                ? Mathf.Max(contentExtent * 0.85f, highlightedNodeDiameter * 10f, 2f)
                : Mathf.Max(targetExtent * 0.55f, highlightedNodeDiameter * 10f, 2f);
        public string RunLabelText { get; private set; } = string.Empty;
        public BooleanAttractorLandscapeRunData ActiveRun { get; private set; }
        public BooleanAttractorLandscapeStepData ActiveStep { get; private set; }
        public bool HasActiveRun => ActiveRun != null && ActiveRun.steps != null && ActiveRun.steps.Length > 0;
        public int ActiveRunIndex => activeRunIndex;
        public int ActiveStepIndex => activeStepIndex;
        public int RunCount => batchData?.runs?.Length ?? 0;
        public BooleanAttractorContextData[] ContextDefinitions => batchData?.contexts;
        public BooleanLandscapeLayoutMode LayoutMode => layoutMode;

        public string ResolveStateLabel(int index)
        {
            if (stateLabelsByIndex != null && index >= 0 && index < stateLabelsByIndex.Length)
            {
                string label = stateLabelsByIndex[index];
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return label;
                }
            }

            return index.ToString();
        }

        private void Start()
        {
            if (FindFirstObjectByType<Menu.AppMenuController>() != null)
            {
                return;
            }

            RebuildVisualisation();
        }

        [ContextMenu("Rebuild Visualisation")]
        public void RebuildVisualisation()
        {
            TrajectoryLightingSetup.Apply();
            ClearSpawnedObjects();

            string dynamicsJson = LoadJsonText(dynamicsFileName, dynamicsOverride);
            if (string.IsNullOrWhiteSpace(dynamicsJson))
            {
                Debug.LogError(
                    "BooleanAttractorLandscapeVisualiser: missing dynamics JSON. " +
                    $"Expected StreamingAssets/{dynamicsFileName}.");
                return;
            }

            batchData = JsonUtility.FromJson<BooleanAttractorLandscapeBatchData>(dynamicsJson);
            if (batchData?.runs == null || batchData.runs.Length == 0)
            {
                Debug.LogError("BooleanAttractorLandscapeVisualiser: dynamics JSON contains no runs.");
                return;
            }

            layoutData = null;
            if (layoutMode == BooleanLandscapeLayoutMode.MdsEmbedding)
            {
                string layoutJson = LoadJsonText(layoutFileName, layoutOverride);
                if (string.IsNullOrWhiteSpace(layoutJson))
                {
                    Debug.LogError(
                        "BooleanAttractorLandscapeVisualiser: missing layout JSON. " +
                        $"Expected StreamingAssets/{layoutFileName}.");
                    return;
                }

                layoutData = JsonUtility.FromJson<BooleanStateLayoutData>(layoutJson);
                if (layoutData?.nodes == null || layoutData.nodes.Length == 0)
                {
                    Debug.LogError("BooleanAttractorLandscapeVisualiser: layout JSON contains no nodes.");
                    return;
                }
            }

            EnsureMaterials();
            if (layoutMode == BooleanLandscapeLayoutMode.HammingLayers)
            {
                BuildHammingLayerNodeLayout();
            }
            else
            {
                BuildFixedNodeLayout();
            }
            BuildTransitionLinePool();
            RunLabelText = BuildRunLabelText();
            activeRunIndex = 0;
            highlightMode = BooleanAttractorLandscapeHighlightMode.GlobalAttractors;
            ShowStep(0);
        }

        public void SetHighlightMode(BooleanAttractorLandscapeHighlightMode mode)
        {
            highlightMode = mode;
            if (ActiveStep != null)
            {
                UpdateHighlights(ActiveStep);
            }
        }

        public void CycleHighlightMode()
        {
            highlightMode = highlightMode switch
            {
                BooleanAttractorLandscapeHighlightMode.GlobalAttractors =>
                    BooleanAttractorLandscapeHighlightMode.ContextPath,
                BooleanAttractorLandscapeHighlightMode.ContextPath =>
                    BooleanAttractorLandscapeHighlightMode.ContextAttractors,
                _ => BooleanAttractorLandscapeHighlightMode.GlobalAttractors,
            };

            if (ActiveStep != null)
            {
                UpdateHighlights(ActiveStep);
            }
        }

        public void SetShowContextPathEdges(bool enabled)
        {
            SetHighlightMode(enabled
                ? BooleanAttractorLandscapeHighlightMode.ContextPath
                : BooleanAttractorLandscapeHighlightMode.GlobalAttractors);
        }

        public Color GetContextAttractorColor(int contextIndex)
        {
            if (contextAttractorColors == null || contextAttractorColors.Length == 0)
            {
                return cycleNodeColor;
            }

            return contextAttractorColors[
                Mathf.Abs(contextIndex) % contextAttractorColors.Length];
        }

        public void SetActiveRun(int runIndex)
        {
            if (batchData?.runs == null || batchData.runs.Length == 0)
            {
                return;
            }

            activeRunIndex = Mathf.Clamp(runIndex, 0, batchData.runs.Length - 1);
            ShowStep(0);
        }

        public void ShowStep(int stepIndex)
        {
            if (batchData?.runs == null || batchData.runs.Length == 0)
            {
                return;
            }

            ActiveRun = batchData.runs[Mathf.Clamp(activeRunIndex, 0, batchData.runs.Length - 1)];
            if (ActiveRun.steps == null || ActiveRun.steps.Length == 0)
            {
                return;
            }

            activeStepIndex = Mathf.Clamp(stepIndex, 0, ActiveRun.steps.Length - 1);
            ActiveStep = ActiveRun.steps[activeStepIndex];
            UpdateTransitionEdges(ActiveStep.edgeTo);
            UpdateHighlights(ActiveStep);
        }

        private string LoadJsonText(string fileName, TextAsset jsonOverrideAsset)
        {
            if (jsonOverrideAsset != null)
            {
                return jsonOverrideAsset.text;
            }

            string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
            if (File.Exists(streamingPath))
            {
                return File.ReadAllText(streamingPath);
            }

#if UNITY_EDITOR
            TextAsset editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"Assets/StreamingAssets/{fileName}");
            if (editorAsset != null)
            {
                return editorAsset.text;
            }
#endif

            return null;
        }

        private void EnsureMaterials()
        {
            transitionEdgeMaterial = TrajectoryRenderUtility.CreateLineMaterial(transitionEdgeColor);
            pathEdgeMaterial = TrajectoryRenderUtility.CreateLineMaterial(pathEdgeColor);
            cycleEdgeMaterial = TrajectoryRenderUtility.CreateLineMaterial(cycleEdgeColor);
            baseNodeMaterial = TrajectoryRenderUtility.CreateMarkerMaterial(baseNodeColor);
        }

        public void ConfigureLayeredPresentation(bool layeredLayout, bool stateLabels)
        {
            layoutMode = layeredLayout
                ? BooleanLandscapeLayoutMode.HammingLayers
                : BooleanLandscapeLayoutMode.MdsEmbedding;
            showStateLabels = stateLabels;
        }

        private void BuildHammingLayerNodeLayout()
        {
            int geneCount = batchData?.geneCount > 0 ? batchData.geneCount : 5;
            int stateCount = batchData?.stateCount > 0 ? batchData.stateCount : 1 << geneCount;
            nodePositions = new Vector3[stateCount];
            nodeRenderers.Clear();
            stateLabelsByIndex = new string[stateCount];

            for (int index = 0; index < stateCount; index++)
            {
                stateLabelsByIndex[index] = ConvertIndexToStateLabel(index, geneCount);
            }

            BooleanHammingLayerLayout.AssignLayeredWallPositions(
                stateCount,
                geneCount,
                ResolveStateLabel,
                layerSpacing,
                layerVerticalTierSpacing,
                layerNodeSpacing,
                layerWallPadding,
                nodePositions,
                wallDescriptors: null);

            BooleanHammingLayerLayout.ComputeContentBounds(
                nodePositions,
                out contentCenter,
                out contentExtent);

            stateLabelObjects = new GameObject[stateCount];

            for (int index = 0; index < stateCount; index++)
            {
                CreateNodeObject(index, stateLabelsByIndex[index], nodePositions[index]);
            }

            EnsureOrbitFocus();
            orbitFocus.localPosition = contentCenter;
        }

        private static string ConvertIndexToStateLabel(int index, int geneCount)
        {
            string binary = System.Convert.ToString(index, 2);
            if (binary.Length < geneCount)
            {
                binary = binary.PadLeft(geneCount, '0');
            }

            return binary;
        }

        private void BuildFixedNodeLayout()
        {
            nodePositions = new Vector3[layoutData.nodes.Length];
            nodeRenderers.Clear();
            stateLabelsByIndex = new string[layoutData.nodes.Length];

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < layoutData.nodes.Length; index++)
            {
                BooleanStateLayoutNodeData node = layoutData.nodes[index];
                int nodeIndex = node.index >= 0 && node.index < stateLabelsByIndex.Length
                    ? node.index
                    : index;
                stateLabelsByIndex[nodeIndex] = node.state;
                Vector3 raw = new Vector3(node.x, node.y, node.z);
                min = Vector3.Min(min, raw);
                max = Vector3.Max(max, raw);
            }

            Vector3 center = (min + max) * 0.5f;
            float extent = Mathf.Max(max.x - min.x, max.y - min.y, max.z - min.z);
            float scale = extent > 0f ? targetExtent / extent : 1f;
            contentCenter = Vector3.zero;
            contentExtent = targetExtent;

            for (int index = 0; index < layoutData.nodes.Length; index++)
            {
                BooleanStateLayoutNodeData node = layoutData.nodes[index];
                int nodeIndex = node.index >= 0 && node.index < nodePositions.Length ? node.index : index;
                Vector3 position = (new Vector3(node.x, node.y, node.z) - center) * scale;
                nodePositions[nodeIndex] = position;
                CreateNodeObject(nodeIndex, node.state, position);
            }

            EnsureOrbitFocus();
            orbitFocus.localPosition = contentCenter;
        }

        private void CreateNodeObject(int nodeIndex, string stateLabel, Vector3 position)
        {
            GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObject.name = $"BooleanState_{stateLabel}";
            nodeObject.layer = gameObject.layer;
            nodeObject.transform.SetParent(transform, false);
            nodeObject.transform.localPosition = position;
            nodeObject.transform.localScale = Vector3.one * nodeDiameter;
            Destroy(nodeObject.GetComponent<Collider>());

            Renderer renderer = nodeObject.GetComponent<Renderer>();
            TrajectoryRenderUtility.ConfigureLitRenderer(renderer, baseNodeMaterial);
            while (nodeRenderers.Count <= nodeIndex)
            {
                nodeRenderers.Add(null);
            }

            nodeRenderers[nodeIndex] = renderer;
            spawnedObjects.Add(nodeObject);

            if (showStateLabels && !LabelOnlyAttractorNodes && !string.IsNullOrWhiteSpace(stateLabel))
            {
                CreateStateLabel(stateLabel, position);
            }
        }

        private GameObject CreateStateLabelObject(string stateLabel, Vector3 nodePosition)
        {
            GameObject labelObject = new GameObject($"Label_{stateLabel}");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = nodePosition + Vector3.up * labelVerticalOffset;
            spawnedObjects.Add(labelObject);

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = stateLabel;
            textMesh.fontSize = LabelOnlyAttractorNodes ? 48 : 64;
            textMesh.characterSize = LabelOnlyAttractorNodes
                ? labelCharacterSize * 0.65f
                : labelCharacterSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = labelColor;
            labelObject.AddComponent<BillboardLabel>();
            return labelObject;
        }

        private void CreateStateLabel(string stateLabel, Vector3 nodePosition)
        {
            CreateStateLabelObject(stateLabel, nodePosition);
        }

        private void UpdateAttractorStateLabels(
            HashSet<int> attractorPointNodes,
            HashSet<int> limitCycleNodes)
        {
            if (!LabelOnlyAttractorNodes || stateLabelObjects == null)
            {
                return;
            }

            for (int nodeIndex = 0; nodeIndex < stateLabelObjects.Length; nodeIndex++)
            {
                bool shouldShow = attractorPointNodes.Contains(nodeIndex)
                    || limitCycleNodes.Contains(nodeIndex);
                if (shouldShow)
                {
                    if (stateLabelObjects[nodeIndex] == null)
                    {
                        string label = ResolveStateLabel(nodeIndex);
                        stateLabelObjects[nodeIndex] = CreateStateLabelObject(
                            label,
                            nodePositions[nodeIndex]);
                    }
                    else
                    {
                        stateLabelObjects[nodeIndex].SetActive(true);
                    }
                }
                else if (stateLabelObjects[nodeIndex] != null)
                {
                    stateLabelObjects[nodeIndex].SetActive(false);
                }
            }
        }

        private void BuildTransitionLinePool()
        {
            transitionLines.Clear();
            for (int sourceIndex = 0; sourceIndex < nodePositions.Length; sourceIndex++)
            {
                LineRenderer line = CreateEdgeLine(
                    $"Transition_{sourceIndex}",
                    transitionEdgeMaterial,
                    transitionEdgeWidth);
                transitionLines.Add(line);
            }
        }

        private void UpdateTransitionEdges(int[] edgeTo)
        {
            if (edgeTo == null || edgeTo.Length != nodePositions.Length)
            {
                return;
            }

            for (int sourceIndex = 0; sourceIndex < edgeTo.Length; sourceIndex++)
            {
                int targetIndex = edgeTo[sourceIndex];
                LineRenderer line = transitionLines[sourceIndex];
                line.startColor = transitionEdgeColor;
                line.endColor = transitionEdgeColor;
                line.startWidth = transitionEdgeWidth;
                line.endWidth = transitionEdgeWidth;
                line.positionCount = 2;
                line.SetPosition(0, nodePositions[sourceIndex]);
                line.SetPosition(1, nodePositions[targetIndex]);
                line.gameObject.SetActive(true);
            }
        }

        private void UpdateHighlights(BooleanAttractorLandscapeStepData step)
        {
            ResetNodeVisuals();
            ClearDynamicLines(pathLines);
            ClearDynamicLines(cycleLines);

            HashSet<int> pathNodes = new HashSet<int>();
            HashSet<int> cycleNodes = new HashSet<int>();
            HashSet<int> initialNodes = new HashSet<int>();
            HashSet<int> targetNodes = new HashSet<int>();
            HashSet<int> attractorPointNodes = new HashSet<int>();
            HashSet<int> limitCycleNodes = new HashSet<int>();

            Dictionary<int, Color> contextAttractorNodeColors = null;
            if (highlightMode == BooleanAttractorLandscapeHighlightMode.ContextAttractors)
            {
                contextAttractorNodeColors = new Dictionary<int, Color>();
            }

            if (step.contextPaths != null)
            {
                for (int pathIndex = 0; pathIndex < step.contextPaths.Length; pathIndex++)
                {
                    BooleanAttractorContextPathData contextPath = step.contextPaths[pathIndex];
                    initialNodes.Add(contextPath.initialIndex);
                    targetNodes.Add(contextPath.targetIndex);
                    AddIndices(pathNodes, contextPath.pathIndices);
                    AddIndices(cycleNodes, contextPath.cycleIndices);

                    if (highlightMode == BooleanAttractorLandscapeHighlightMode.ContextPath)
                    {
                        DrawPathSegments(
                            contextPath.pathIndices,
                            pathLines,
                            pathEdgeMaterial,
                            pathEdgeWidth);
                    }
                    else if (highlightMode == BooleanAttractorLandscapeHighlightMode.ContextAttractors)
                    {
                        Color contextColor = GetContextAttractorColor(pathIndex);
                        Color contextEdgeColor = new Color(
                            contextColor.r,
                            contextColor.g,
                            contextColor.b,
                            0.95f);
                        AddContextAttractorNodeColors(
                            contextAttractorNodeColors,
                            contextPath.cycleIndices,
                            contextColor);
                        DrawCycleSegments(
                            contextPath.cycleIndices,
                            cycleLines,
                            cycleEdgeMaterial,
                            cycleEdgeWidth,
                            contextEdgeColor);
                    }
                }
            }

            if (step.attractors != null
                && highlightMode == BooleanAttractorLandscapeHighlightMode.GlobalAttractors)
            {
                foreach (BooleanAttractorEntryData attractor in step.attractors)
                {
                    if (attractor.stateIndices == null)
                    {
                        continue;
                    }

                    if (attractor.type == "point")
                    {
                        AddIndices(attractorPointNodes, attractor.stateIndices);
                    }
                    else if (attractor.type == "limit_cycle")
                    {
                        AddIndices(limitCycleNodes, attractor.stateIndices);
                        DrawCycleSegments(
                            attractor.stateIndices,
                            cycleLines,
                            cycleEdgeMaterial,
                            cycleEdgeWidth);
                    }
                }
            }

            for (int nodeIndex = 0; nodeIndex < nodeRenderers.Count; nodeIndex++)
            {
                float diameter = nodeDiameter;
                Color color = baseNodeColor;

                switch (highlightMode)
                {
                    case BooleanAttractorLandscapeHighlightMode.ContextPath:
                        if (pathNodes.Contains(nodeIndex))
                        {
                            color = pathNodeColor;
                            diameter = highlightedNodeDiameter;
                        }

                        if (initialNodes.Contains(nodeIndex))
                        {
                            color = initialNodeColor;
                            diameter = initialTargetNodeDiameter;
                        }

                        if (targetNodes.Contains(nodeIndex))
                        {
                            color = targetNodeColor;
                            diameter = initialTargetNodeDiameter;
                        }

                        break;

                    case BooleanAttractorLandscapeHighlightMode.ContextAttractors:
                        if (contextAttractorNodeColors != null
                            && contextAttractorNodeColors.TryGetValue(nodeIndex, out Color contextColor))
                        {
                            color = contextColor;
                            diameter = highlightedNodeDiameter;
                        }

                        break;

                    default:
                        if (attractorPointNodes.Contains(nodeIndex))
                        {
                            color = attractorPointColor;
                            diameter = highlightedNodeDiameter;
                        }

                        if (limitCycleNodes.Contains(nodeIndex))
                        {
                            color = limitCycleNodeColor;
                            diameter = highlightedNodeDiameter;
                        }

                        break;
                }

                ApplyNodeVisual(nodeRenderers[nodeIndex], color, diameter);
            }

            UpdateAttractorStateLabels(attractorPointNodes, limitCycleNodes);
        }

        private void ResetNodeVisuals()
        {
            for (int nodeIndex = 0; nodeIndex < nodeRenderers.Count; nodeIndex++)
            {
                ApplyNodeVisual(nodeRenderers[nodeIndex], baseNodeColor, nodeDiameter);
            }
        }

        private static void ApplyNodeVisual(Renderer renderer, Color color, float diameter)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.localScale = Vector3.one * diameter;
            Material material = TrajectoryRenderUtility.CreateMarkerMaterial(color);
            TrajectoryRenderUtility.ConfigureLitRenderer(renderer, material);
        }

        private void DrawPathSegments(
            int[] pathIndices,
            List<LineRenderer> linePool,
            Material material,
            float width)
        {
            if (pathIndices == null || pathIndices.Length < 2)
            {
                return;
            }

            for (int step = 0; step < pathIndices.Length - 1; step++)
            {
                LineRenderer line = CreateEdgeLine($"Path_{step}_{linePool.Count}", material, width);
                line.startColor = pathEdgeColor;
                line.endColor = pathEdgeColor;
                line.startWidth = width;
                line.endWidth = width;
                line.positionCount = 2;
                line.SetPosition(0, nodePositions[pathIndices[step]]);
                line.SetPosition(1, nodePositions[pathIndices[step + 1]]);
                linePool.Add(line);
            }
        }

        private static void AddContextAttractorNodeColors(
            Dictionary<int, Color> nodeColors,
            int[] cycleIndices,
            Color color)
        {
            if (nodeColors == null || cycleIndices == null)
            {
                return;
            }

            for (int index = 0; index < cycleIndices.Length; index++)
            {
                nodeColors[cycleIndices[index]] = color;
            }
        }

        private void DrawCycleSegments(
            int[] cycleIndices,
            List<LineRenderer> linePool,
            Material material,
            float width,
            Color? edgeColorOverride = null)
        {
            if (cycleIndices == null || cycleIndices.Length == 0)
            {
                return;
            }

            if (cycleIndices.Length == 1)
            {
                return;
            }

            Color edgeColor = edgeColorOverride ?? cycleEdgeColor;
            for (int step = 0; step < cycleIndices.Length; step++)
            {
                int fromIndex = cycleIndices[step];
                int toIndex = cycleIndices[(step + 1) % cycleIndices.Length];
                LineRenderer line = CreateEdgeLine($"Cycle_{step}_{linePool.Count}", material, width);
                line.startColor = edgeColor;
                line.endColor = edgeColor;
                line.startWidth = width;
                line.endWidth = width;
                line.positionCount = 2;
                line.SetPosition(0, nodePositions[fromIndex]);
                line.SetPosition(1, nodePositions[toIndex]);
                linePool.Add(line);
            }
        }

        private LineRenderer CreateEdgeLine(string name, Material material, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.layer = gameObject.layer;
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 4;
            line.sharedMaterial = material;
            line.startWidth = width;
            line.endWidth = width;
            spawnedObjects.Add(lineObject);
            return line;
        }

        private static void AddIndices(HashSet<int> target, int[] indices)
        {
            if (indices == null)
            {
                return;
            }

            for (int index = 0; index < indices.Length; index++)
            {
                target.Add(indices[index]);
            }
        }

        private static void ClearDynamicLines(List<LineRenderer> lines)
        {
            for (int index = lines.Count - 1; index >= 0; index--)
            {
                if (lines[index] != null)
                {
                    Destroy(lines[index].gameObject);
                }
            }

            lines.Clear();
        }

        private void EnsureOrbitFocus()
        {
            if (orbitFocus != null)
            {
                return;
            }

            GameObject focusObject = new GameObject("BooleanAttractorLandscapeFocus");
            focusObject.layer = gameObject.layer;
            focusObject.transform.SetParent(transform, false);
            orbitFocus = focusObject.transform;
            spawnedObjects.Add(focusObject);
        }

        private string BuildRunLabelText()
        {
            if (batchData?.runs == null || batchData.runs.Length == 0)
            {
                return string.Empty;
            }

            if (layoutMode == BooleanLandscapeLayoutMode.HammingLayers)
            {
                int stateCount = batchData.stateCount > 0 ? batchData.stateCount : 32;
                return $"{batchData.runCount} run(s) over 3D Hamming walls ({stateCount} states, 00000 → 11111)";
            }

            return $"{batchData.runCount} run(s) over fixed Boolean state layout (32 nodes)";
        }

        private void ClearSpawnedObjects()
        {
            pathLines.Clear();
            cycleLines.Clear();
            transitionLines.Clear();
            nodeRenderers.Clear();

            for (int index = spawnedObjects.Count - 1; index >= 0; index--)
            {
                if (spawnedObjects[index] != null)
                {
                    Destroy(spawnedObjects[index]);
                }
            }

            spawnedObjects.Clear();
            stateLabelObjects = System.Array.Empty<GameObject>();
            orbitFocus = null;
            ActiveRun = null;
            ActiveStep = null;
            activeStepIndex = -1;
        }
    }
}
