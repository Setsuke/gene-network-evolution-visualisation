using System.Collections.Generic;
using System.IO;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// MDS embedding of accepted genome paths when every run starts from the same initial genome.
    /// Initial genome: white. Final solutions: red. Plateau fitness 0.25/0.5/0.296: green/blue/yellow.
    /// Other accepted genomes: grey.
    /// </summary>
    public class SharedGenomeMdsPathVisualiser : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string jsonFileName = "unity_shared_genome_mds_path.json";
        [SerializeField] private TextAsset jsonOverride;

        [Header("Layout")]
        [SerializeField] private float targetExtent = 18f;
        [SerializeField] private float tubeDiameter = 0.03f;
        [SerializeField] private float nodeDiameter = 0.16f;
        [SerializeField] private float endpointNodeDiameter = 0.3f;

        [Header("Axes")]
        [SerializeField] private bool showMdsAxes = true;
        [SerializeField] private float axisLengthFactor = 0.55f;
        [SerializeField] private float axisLineWidth = 0.014f;
        [SerializeField] private bool showAxisLabels = true;

        [Header("Colours")]
        [SerializeField] private Color pathColor = new Color(0.62f, 0.64f, 0.68f, 1f);
        [SerializeField] private Color intermediateNodeColor = new Color(0.72f, 0.74f, 0.78f, 1f);
        [SerializeField] private Color initialNodeColor = Color.white;
        [SerializeField] private Color solutionNodeColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color fitness025NodeColor = new Color(0.13f, 0.77f, 0.37f, 1f);
        [SerializeField] private Color fitness05NodeColor = new Color(0.23f, 0.51f, 0.96f, 1f);
        [SerializeField] private Color fitness296NodeColor = new Color(0.98f, 0.85f, 0.18f, 1f);
        private static readonly Color FarFromInitialGenomeColor = new Color(0.98f, 0.55f, 0.12f, 1f);
        [SerializeField] private int farFromInitialHammingThreshold = 60;

        private const float Fitness025Target = 0.25f;
        private const float Fitness05Target = 0.5f;
        private const float Fitness296Target = 0.29629629629629634f;
        private const float FitnessMatchEpsilon = 1e-4f;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private Material pathMaterial;
        private Material intermediateMaterial;
        private Material initialMaterial;
        private Material solutionMaterial;
        private Material fitness025Material;
        private Material fitness05Material;
        private Material fitness296Material;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private bool hasContentBounds;
        private Vector3 embeddingOriginWorld;
        private Vector3 cloudCentroidWorld;
        private readonly List<GameObject> intermediateNodeObjects = new List<GameObject>();
        private readonly List<GameObject> pathRootObjects = new List<GameObject>();
        private readonly List<SegmentVisual> segmentVisuals = new List<SegmentVisual>();
        private readonly List<NodeVisual> nodeVisuals = new List<NodeVisual>();
        private readonly HashSet<long> driftSegmentKeys = new HashSet<long>();
        private readonly HashSet<int> driftGenomeIndices = new HashSet<int>();
        private GameObject axesRootObject;
        private bool intermediateNodesVisible = true;
        private bool pathConnectionsVisible = true;
        private bool axesVisible = true;
        private bool showDriftCategories;
        private bool showDriftingGenomes;
        private bool orbitCloudCentroid;
        private Material driftNeutralCategoryMaterial;
        private Material driftPathChangedCategoryMaterial;
        private Material driftAttractorChangedCategoryMaterial;
        private Material driftModeSolutionMaterial;
        private Material driftingGenomeMaterial;

        private struct SegmentVisual
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public string ChangeType;
            public int RunIndex;
            public int FromPointIndex;
        }

        private struct NodeVisual
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public GenomePathPointData Point;
            public bool IsInitial;
            public bool IsSolution;
        }

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
        public string RunLabelText { get; private set; } = string.Empty;
        public bool IntermediateNodesVisible => intermediateNodesVisible;
        public bool PathConnectionsVisible => pathConnectionsVisible;
        public bool AxesVisible => axesVisible;
        public bool ShowDriftCategories => showDriftCategories;
        public bool ShowDriftingGenomes => showDriftingGenomes;
        public bool OrbitCloudCentroid => orbitCloudCentroid;
        public Vector3 CloudCentroidWorld => cloudCentroidWorld;

        public void SetOrbitCloudCentroid(bool enabled)
        {
            if (orbitCloudCentroid == enabled)
            {
                return;
            }

            orbitCloudCentroid = enabled;
            ApplyOrbitFocusTarget();
        }

        public void ToggleOrbitCloudCentroid()
        {
            SetOrbitCloudCentroid(!orbitCloudCentroid);
        }

        public void SetShowDriftCategories(bool visible)
        {
            if (showDriftCategories == visible)
            {
                return;
            }

            if (visible && showDriftingGenomes)
            {
                showDriftingGenomes = false;
            }

            showDriftCategories = visible;
            ApplyAppearanceMode();
        }

        public void ToggleDriftCategories()
        {
            SetShowDriftCategories(!showDriftCategories);
        }

        public void SetShowDriftingGenomes(bool visible)
        {
            if (showDriftingGenomes == visible)
            {
                return;
            }

            if (visible && showDriftCategories)
            {
                showDriftCategories = false;
            }

            showDriftingGenomes = visible;
            driftingGenomeMaterial = null;
            ApplyAppearanceMode();
        }

        public void ToggleDriftingGenomes()
        {
            SetShowDriftingGenomes(!showDriftingGenomes);
        }

        public void SetIntermediateNodesVisible(bool visible)
        {
            intermediateNodesVisible = visible;
            for (int index = 0; index < intermediateNodeObjects.Count; index++)
            {
                if (intermediateNodeObjects[index] != null)
                {
                    intermediateNodeObjects[index].SetActive(visible);
                }
            }
        }

        public void ToggleIntermediateNodesVisible()
        {
            SetIntermediateNodesVisible(!intermediateNodesVisible);
        }

        public void SetPathConnectionsVisible(bool visible)
        {
            pathConnectionsVisible = visible;
            for (int index = 0; index < pathRootObjects.Count; index++)
            {
                if (pathRootObjects[index] != null)
                {
                    pathRootObjects[index].SetActive(visible);
                }
            }
        }

        public void TogglePathConnectionsVisible()
        {
            SetPathConnectionsVisible(!pathConnectionsVisible);
        }

        public void SetAxesVisible(bool visible)
        {
            axesVisible = visible;
            if (axesRootObject != null)
            {
                axesRootObject.SetActive(visible);
            }
        }

        public void ToggleAxesVisible()
        {
            SetAxesVisible(!axesVisible);
        }

        private struct NormalizationParams
        {
            public Vector3 center;
            public float scale;
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

            string json = LoadJsonText();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError(
                    $"SharedGenomeMdsPathVisualiser: could not load '{jsonFileName}'. " +
                    "Run demo.py with --shared-initial-genome to export shared genome path JSON.");
                return;
            }

            GenomePathBatchData batch = JsonUtility.FromJson<GenomePathBatchData>(json);
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                Debug.LogError("SharedGenomeMdsPathVisualiser: JSON parsed but contains no runs.");
                return;
            }

            if (!batch.sharedInitialGenome)
            {
                Debug.LogError(
                    "SharedGenomeMdsPathVisualiser: JSON is not marked as sharedInitialGenome=true.");
                return;
            }

            BuildVisualisation(batch);
            Debug.Log(
                $"SharedGenomeMdsPathVisualiser: built {batch.runCount} shared-start path(s) " +
                $"({batch.genomeCount} unique genomes, {batch.dimensions}D).");
        }

        private string LoadJsonText()
        {
            if (jsonOverride != null)
            {
                return jsonOverride.text;
            }

            string streamingPath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
            if (File.Exists(streamingPath))
            {
                return File.ReadAllText(streamingPath);
            }

#if UNITY_EDITOR
            TextAsset editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"Assets/StreamingAssets/{jsonFileName}");
            if (editorAsset != null)
            {
                return editorAsset.text;
            }
#endif

            return null;
        }

        private void BuildVisualisation(GenomePathBatchData batch)
        {
            ClearSpawnedObjects();
            ResetContentBounds();
            driftingGenomeMaterial = null;
            RunLabelText = BuildRunLabelText(batch);

            NormalizationParams normalization = ComputeNormalization(batch);
            Vector3 embeddingOriginWorld = ResolveEmbeddingOriginWorld(batch, normalization);
            HashSet<int> solutionIndices = CollectSolutionIndices(batch);
            HashSet<int> drawnNodeIndices = new HashSet<int>();
            BuildDriftVisibilitySets(batch);

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                CreateRunPath(batch.runs[runIndex], runIndex, normalization);
            }

            CreateEndpointNode(
                batch.initialGenomeIndex,
                batch,
                normalization,
                initialNodeColor,
                endpointNodeDiameter,
                "InitialGenome",
                isInitial: true,
                isSolution: false);

            foreach (int solutionIndex in solutionIndices)
            {
                CreateEndpointNode(
                    solutionIndex,
                    batch,
                    normalization,
                    solutionNodeColor,
                    endpointNodeDiameter,
                    "SolutionGenome",
                    isInitial: false,
                    isSolution: true);
            }

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                AddIntermediateNodes(batch.runs[runIndex], batch.initialGenomeIndex, solutionIndices, drawnNodeIndices, normalization);
            }

            FinalizeContentBounds(embeddingOriginWorld);

            if (showMdsAxes)
            {
                axesRootObject = new GameObject("MdsAxesRoot");
                axesRootObject.transform.SetParent(transform, false);
                axesRootObject.transform.localPosition = Vector3.zero;
                axesRootObject.transform.localRotation = Quaternion.identity;
                spawnedObjects.Add(axesRootObject);

                GenomePathMdsAxesBuilder.Build(
                    axesRootObject.transform,
                    embeddingOriginWorld,
                    contentExtent * axisLengthFactor,
                    axisLineWidth,
                    showAxisLabels,
                    spawnedObjects);

                axesRootObject.SetActive(axesVisible);
            }
            else
            {
                axesRootObject = null;
            }

            ApplyAppearanceMode();
        }

        private void ApplyAppearanceMode()
        {
            ApplyNodeAppearance();
            ApplySegmentAppearance();
        }

        private void ApplyNodeAppearance()
        {
            for (int index = 0; index < nodeVisuals.Count; index++)
            {
                NodeVisual node = nodeVisuals[index];
                if (node.GameObject == null || node.Renderer == null)
                {
                    continue;
                }

                Material material = ResolveNodeMaterial(node);
                TrajectoryRenderUtility.ConfigureLitRenderer(node.Renderer, material);
            }
        }

        private void BuildDriftVisibilitySets(GenomePathBatchData batch)
        {
            driftSegmentKeys.Clear();
            driftGenomeIndices.Clear();

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null || points.Length < 2)
                {
                    continue;
                }

                PlateauEpisode[] episodes = PlateauEpisodeAnalyzer.AnalyzeRun(batch.runs[runIndex], null);
                for (int episodeIndex = 0; episodeIndex < episodes.Length; episodeIndex++)
                {
                    PlateauEpisode episode = episodes[episodeIndex];
                    for (int stepIndex = episode.StartStep; stepIndex < episode.EndStep; stepIndex++)
                    {
                        driftSegmentKeys.Add(MakeStepKey(runIndex, stepIndex));
                    }
                }
            }

            if (SharedGenomeHammingUtility.TryCollectGenomeIndicesAtOrAboveHammingFromPoints(
                    batch,
                    farFromInitialHammingThreshold,
                    out HashSet<int> farFromInitialIndices)
                || SharedGenomeHammingUtility.TryCollectGenomeIndicesAtOrAboveHamming(
                    jsonFileName,
                    batch.initialGenomeIndex,
                    farFromInitialHammingThreshold,
                    out farFromInitialIndices))
            {
                driftGenomeIndices.UnionWith(farFromInitialIndices);
            }
            else
            {
                Debug.LogWarning(
                    "SharedGenomeMdsPathVisualiser: could not resolve genomes far from the shared initial genome. " +
                    "Ensure unity_shared_genome_mds_path_genomes.json is present or re-export with hammingFromInitial.");
            }
        }

        private static long MakeStepKey(int runIndex, int stepIndex)
        {
            return ((long)runIndex << 20) | (uint)stepIndex;
        }

        private void ApplySegmentAppearance()
        {
            for (int index = 0; index < segmentVisuals.Count; index++)
            {
                SegmentVisual segment = segmentVisuals[index];
                if (segment.GameObject == null || segment.Renderer == null)
                {
                    continue;
                }

                Material material = showDriftCategories
                    ? ResolveDriftSegmentMaterial(segment)
                    : GetPathMaterial();
                TrajectoryRenderUtility.ConfigureLitRenderer(segment.Renderer, material);
            }
        }

        private Material ResolveDriftSegmentMaterial(SegmentVisual segment)
        {
            if (driftSegmentKeys.Contains(MakeStepKey(segment.RunIndex, segment.FromPointIndex)))
            {
                return GetDriftCategoryMaterial(segment.ChangeType);
            }

            return GetPathMaterial();
        }

        private Material ResolveNodeMaterial(NodeVisual node)
        {
            if (showDriftCategories)
            {
                if (node.IsInitial)
                {
                    return GetInitialMaterial();
                }

                if (node.IsSolution)
                {
                    return GetDriftModeSolutionMaterial();
                }

                return GetIntermediateMaterial();
            }

            if (showDriftingGenomes)
            {
                if (node.IsInitial)
                {
                    return GetInitialMaterial();
                }

                if (node.IsSolution)
                {
                    return GetSolutionMaterial();
                }

                if (IsDriftingGenome(node.Point))
                {
                    return GetDriftingGenomeMaterial();
                }

                return GetIntermediateMaterial();
            }

            if (node.IsInitial)
            {
                return GetInitialMaterial();
            }

            if (node.IsSolution)
            {
                return GetSolutionMaterial();
            }

            return ResolveIntermediateMaterial(node.Point);
        }

        private bool IsDriftingGenome(GenomePathPointData point)
        {
            return point != null && driftGenomeIndices.Contains(point.genomeIndex);
        }

        private Material GetDriftCategoryMaterial(string changeType)
        {
            GenomePathChangeTypeStyles.PlateauDriftCategoryBucket bucket =
                GenomePathChangeTypeStyles.GetDriftCategoryBucket(changeType);
            switch (bucket)
            {
                case GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.PathChangedAttractorUnchanged:
                    driftPathChangedCategoryMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(
                        GenomePathChangeTypeStyles.GetDriftCategoryColor(bucket),
                        smoothness: 0.42f);
                    return driftPathChangedCategoryMaterial;
                case GenomePathChangeTypeStyles.PlateauDriftCategoryBucket.AttractorChanged:
                    driftAttractorChangedCategoryMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(
                        GenomePathChangeTypeStyles.GetDriftCategoryColor(bucket),
                        smoothness: 0.42f);
                    return driftAttractorChangedCategoryMaterial;
                default:
                    driftNeutralCategoryMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(
                        GenomePathChangeTypeStyles.GetDriftCategoryColor(bucket),
                        smoothness: 0.42f);
                    return driftNeutralCategoryMaterial;
            }
        }

        private static HashSet<int> CollectSolutionIndices(GenomePathBatchData batch)
        {
            HashSet<int> solutionIndices = new HashSet<int>();
            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null || points.Length == 0)
                {
                    continue;
                }

                solutionIndices.Add(points[points.Length - 1].genomeIndex);
            }

            return solutionIndices;
        }

        private void CreateRunPath(GenomePathRunData run, int runIndex, NormalizationParams normalization)
        {
            if (run.points == null || run.points.Length < 2)
            {
                return;
            }

            string runLabel = string.IsNullOrWhiteSpace(run.sourceLabel) ? "run" : run.sourceLabel;
            GameObject pathRoot = new GameObject($"SharedMdsPath_{runLabel}");
            pathRoot.transform.SetParent(transform, false);
            pathRoot.SetActive(pathConnectionsVisible);
            spawnedObjects.Add(pathRoot);
            pathRootObjects.Add(pathRoot);

            for (int pointIndex = 0; pointIndex < run.points.Length - 1; pointIndex++)
            {
                Vector3 start = TransformPoint(run.points[pointIndex], normalization);
                Vector3 end = TransformPoint(run.points[pointIndex + 1], normalization);
                IncludeInBounds(start);
                IncludeInBounds(end);
                GenomePathPointData nextPoint = run.points[pointIndex + 1];
                GameObject segment = CreateTubeSegment(
                    pathRoot.transform,
                    start,
                    end,
                    tubeDiameter,
                    GetPathMaterial());
                if (segment == null)
                {
                    continue;
                }

                segmentVisuals.Add(new SegmentVisual
                {
                    GameObject = segment,
                    Renderer = segment.GetComponent<Renderer>(),
                    ChangeType = nextPoint.changeType,
                    RunIndex = runIndex,
                    FromPointIndex = pointIndex,
                });
            }
        }

        private void AddIntermediateNodes(
            GenomePathRunData run,
            int initialGenomeIndex,
            HashSet<int> solutionIndices,
            HashSet<int> drawnNodeIndices,
            NormalizationParams normalization)
        {
            if (run.points == null)
            {
                return;
            }

            for (int pointIndex = 0; pointIndex < run.points.Length; pointIndex++)
            {
                int genomeIndex = run.points[pointIndex].genomeIndex;
                if (genomeIndex == initialGenomeIndex || solutionIndices.Contains(genomeIndex))
                {
                    continue;
                }

                if (!drawnNodeIndices.Add(genomeIndex))
                {
                    continue;
                }

                Vector3 position = TransformPoint(run.points[pointIndex], normalization);
                IncludeInBounds(position);
                Material nodeMaterial = ResolveIntermediateMaterial(run.points[pointIndex]);
                GameObject node = CreateNodeSphere(
                    transform,
                    position,
                    nodeDiameter,
                    nodeMaterial,
                    $"Genome_{genomeIndex}");
                if (node != null)
                {
                    node.SetActive(intermediateNodesVisible);
                    intermediateNodeObjects.Add(node);
                    nodeVisuals.Add(new NodeVisual
                    {
                        GameObject = node,
                        Renderer = node.GetComponent<Renderer>(),
                        Point = run.points[pointIndex],
                        IsInitial = false,
                        IsSolution = false,
                    });
                }
            }
        }

        private GameObject CreateEndpointNode(
            int genomeIndex,
            GenomePathBatchData batch,
            NormalizationParams normalization,
            Color color,
            float diameter,
            string objectName,
            bool isInitial,
            bool isSolution)
        {
            if (!TryFindPointByGenomeIndex(batch, genomeIndex, out GenomePathPointData point))
            {
                return null;
            }

            Vector3 position = TransformPoint(point, normalization);
            IncludeInBounds(position);
            Material material = color == initialNodeColor ? GetInitialMaterial() : GetSolutionMaterial();
            GameObject node = CreateNodeSphere(transform, position, diameter, material, objectName);
            if (node != null)
            {
                nodeVisuals.Add(new NodeVisual
                {
                    GameObject = node,
                    Renderer = node.GetComponent<Renderer>(),
                    Point = point,
                    IsInitial = isInitial,
                    IsSolution = isSolution,
                });
            }

            return node;
        }

        private static bool TryFindPointByGenomeIndex(
            GenomePathBatchData batch,
            int genomeIndex,
            out GenomePathPointData point)
        {
            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    if (points[pointIndex].genomeIndex == genomeIndex)
                    {
                        point = points[pointIndex];
                        return true;
                    }
                }
            }

            point = null;
            return false;
        }

        private void ResetContentBounds()
        {
            boundsMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            boundsMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            hasContentBounds = false;
        }

        private void IncludeInBounds(Vector3 position)
        {
            boundsMin = Vector3.Min(boundsMin, position);
            boundsMax = Vector3.Max(boundsMax, position);
            hasContentBounds = true;
        }

        private void FinalizeContentBounds(Vector3 originWorld)
        {
            embeddingOriginWorld = originWorld;

            if (!hasContentBounds)
            {
                cloudCentroidWorld = originWorld;
                contentCenter = originWorld;
                contentExtent = targetExtent;
                ApplyOrbitFocusTarget();
                return;
            }

            cloudCentroidWorld = (boundsMin + boundsMax) * 0.5f;
            contentCenter = originWorld;
            Vector3 size = boundsMax - boundsMin;
            contentExtent = Mathf.Max(size.x, size.y, size.z, 1f);
            ApplyOrbitFocusTarget();
        }

        private void ApplyOrbitFocusTarget()
        {
            EnsureOrbitFocus();
            orbitFocus.position = orbitCloudCentroid ? cloudCentroidWorld : embeddingOriginWorld;
        }

        private void EnsureOrbitFocus()
        {
            if (orbitFocus != null)
            {
                return;
            }

            GameObject focusObject = new GameObject("OrbitFocus");
            focusObject.transform.SetParent(transform, false);
            orbitFocus = focusObject.transform;
        }

        private NormalizationParams ComputeNormalization(GenomePathBatchData batch)
        {
            if (!TryFindPointByGenomeIndex(batch, batch.initialGenomeIndex, out GenomePathPointData initialPoint))
            {
                return ComputeBoundingBoxNormalization(batch);
            }

            Vector3 center = RawPoint(initialPoint);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool hasPoints = false;

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    Vector3 shifted = RawPoint(points[pointIndex]) - center;
                    min = Vector3.Min(min, shifted);
                    max = Vector3.Max(max, shifted);
                    hasPoints = true;
                }
            }

            if (!hasPoints)
            {
                return new NormalizationParams
                {
                    center = center,
                    scale = 1f,
                };
            }

            Vector3 extent = max - min;
            float maxAxis = Mathf.Max(extent.x, extent.y, extent.z, 0.001f);
            return new NormalizationParams
            {
                center = center,
                scale = targetExtent / maxAxis,
            };
        }

        private NormalizationParams ComputeBoundingBoxNormalization(GenomePathBatchData batch)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool hasPoints = false;

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    Vector3 raw = RawPoint(points[pointIndex]);
                    min = Vector3.Min(min, raw);
                    max = Vector3.Max(max, raw);
                    hasPoints = true;
                }
            }

            if (!hasPoints)
            {
                return new NormalizationParams
                {
                    center = Vector3.zero,
                    scale = 1f,
                };
            }

            Vector3 extent = max - min;
            float maxAxis = Mathf.Max(extent.x, extent.y, extent.z, 0.001f);
            return new NormalizationParams
            {
                center = (min + max) * 0.5f,
                scale = targetExtent / maxAxis,
            };
        }

        private Vector3 ResolveEmbeddingOriginWorld(
            GenomePathBatchData batch,
            NormalizationParams normalization)
        {
            if (TryFindPointByGenomeIndex(batch, batch.initialGenomeIndex, out GenomePathPointData initialPoint))
            {
                return TransformPoint(initialPoint, normalization);
            }

            return transform.position;
        }

        private static string BuildRunLabelText(GenomePathBatchData batch)
        {
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                return string.Empty;
            }

            int runCount = batch.runCount > 0 ? batch.runCount : batch.runs.Length;
            return $"{runCount} runs";
        }

        private static Vector3 RawPoint(GenomePathPointData point)
        {
            return new Vector3(point.x, point.y, point.z);
        }

        private Vector3 TransformPoint(GenomePathPointData point, NormalizationParams normalization)
        {
            return transform.position + (RawPoint(point) - normalization.center) * normalization.scale;
        }

        private Material GetPathMaterial()
        {
            pathMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(pathColor, smoothness: 0.3f);
            return pathMaterial;
        }

        private Material GetIntermediateMaterial()
        {
            intermediateMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(intermediateNodeColor, smoothness: 0.35f);
            return intermediateMaterial;
        }

        private Material ResolveIntermediateMaterial(GenomePathPointData point)
        {
            if (ApproxFitness(point.fitness, Fitness025Target))
            {
                return GetFitness025Material();
            }

            if (ApproxFitness(point.fitness, Fitness05Target))
            {
                return GetFitness05Material();
            }

            if (ApproxFitness(point.fitness, Fitness296Target))
            {
                return GetFitness296Material();
            }

            return GetIntermediateMaterial();
        }

        private static bool ApproxFitness(float fitness, float target)
        {
            return Mathf.Abs(fitness - target) <= FitnessMatchEpsilon;
        }

        private Material GetFitness025Material()
        {
            fitness025Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness025NodeColor, smoothness: 0.35f);
            return fitness025Material;
        }

        private Material GetFitness05Material()
        {
            fitness05Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness05NodeColor, smoothness: 0.35f);
            return fitness05Material;
        }

        private Material GetFitness296Material()
        {
            fitness296Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness296NodeColor, smoothness: 0.35f);
            return fitness296Material;
        }

        private Material GetInitialMaterial()
        {
            initialMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(initialNodeColor, smoothness: 0.45f);
            return initialMaterial;
        }

        private Material GetSolutionMaterial()
        {
            solutionMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(solutionNodeColor, smoothness: 0.45f);
            return solutionMaterial;
        }

        private Material GetDriftModeSolutionMaterial()
        {
            driftModeSolutionMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(
                solutionNodeColor,
                smoothness: 0.45f);
            return driftModeSolutionMaterial;
        }

        private Material GetDriftingGenomeMaterial()
        {
            driftingGenomeMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(
                FarFromInitialGenomeColor,
                smoothness: 0.42f);
            return driftingGenomeMaterial;
        }

        private GameObject CreateTubeSegment(
            Transform parent,
            Vector3 start,
            Vector3 end,
            float diameter,
            Material material)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length <= 0.0001f)
            {
                return null;
            }

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            segment.name = "Segment";
            segment.transform.SetParent(parent, false);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            segment.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            spawnedObjects.Add(segment);

            TrajectoryRenderUtility.ConfigureLitRenderer(segment.GetComponent<Renderer>(), material);
            DestroyCollider(segment);
            return segment;
        }

        private GameObject CreateNodeSphere(
            Transform parent,
            Vector3 position,
            float diameter,
            Material material,
            string objectName)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.name = objectName;
            node.transform.SetParent(parent, false);
            node.transform.position = position;
            node.transform.localScale = Vector3.one * diameter;
            spawnedObjects.Add(node);

            TrajectoryRenderUtility.ConfigureLitRenderer(node.GetComponent<Renderer>(), material);
            DestroyCollider(node);
            return node;
        }

        private static void DestroyCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void ClearSpawnedObjects()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }

            spawnedObjects.Clear();
            intermediateNodeObjects.Clear();
            pathRootObjects.Clear();
            segmentVisuals.Clear();
            nodeVisuals.Clear();
            axesRootObject = null;
        }
    }
}
