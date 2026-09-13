using System.Collections.Generic;
using System.IO;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    public enum BatchPathMoveVisibilityFilter
    {
        All,
        NeutralOnly,
        ImprovementOnly,
    }

    public enum BatchDriftHighlightMode
    {
        Off,
        UnifiedYellow,
        ByCategory,
    }

    /// <summary>
    /// Unity Option 11: first N batch runs together.
    /// X/Z = MDS components 1/2, Y = total fitness.
    /// </summary>
    public class BatchGenomeMdsFitnessPathVisualiser : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string jsonFileName = "unity_batch_genome_mds_path.json";
        [SerializeField] private TextAsset jsonOverride;
        [SerializeField] private int maxRunCount = 200;

        [Header("Layout")]
        [SerializeField] private float targetExtent = 18f;
        [SerializeField] private float fitnessHeightScale = 5f;
        [SerializeField] private float tubeDiameter = 0.06f;
        [SerializeField] private float nodeDiameter = 0.1f;
        [SerializeField] private float endpointNodeDiameter = 0.22f;

        [Header("Colours")]
        [SerializeField] private Color pathColor = new Color(0.62f, 0.64f, 0.68f, 1f);
        [SerializeField] private Color intermediateNodeColor = new Color(0.72f, 0.74f, 0.78f, 1f);
        [SerializeField] private Color initialNodeColor = Color.white;
        [SerializeField] private Color solutionNodeColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color fitness025NodeColor = new Color(0.13f, 0.77f, 0.37f, 1f);
        [SerializeField] private Color fitness05NodeColor = new Color(0.23f, 0.51f, 0.96f, 1f);
        [SerializeField] private Color fitness296NodeColor = new Color(0.98f, 0.85f, 0.18f, 1f);
        [SerializeField] private Color driftMoveColor = new Color(0.98f, 0.92f, 0.15f, 1f);

        private const float Fitness025Target = 0.25f;
        private const float Fitness05Target = 0.5f;
        private const float Fitness296Target = 0.29629629629629634f;
        private const float FitnessMatchEpsilon = 1e-4f;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly List<NodeVisual> nodeVisuals = new List<NodeVisual>();
        private readonly List<SegmentVisual> segmentVisuals = new List<SegmentVisual>();
        private readonly Dictionary<string, Material> changeTypeMaterials = new Dictionary<string, Material>();
        private Material pathMaterial;
        private Material intermediateMaterial;
        private Material initialMaterial;
        private Material solutionMaterial;
        private Material fitness025Material;
        private Material fitness05Material;
        private Material fitness296Material;
        private Material driftMaterial;
        private Material driftNeutralCategoryMaterial;
        private Material driftPathChangedCategoryMaterial;
        private Material driftAttractorChangedCategoryMaterial;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private bool hasContentBounds;
        private bool showMoveClassification;
        private BatchDriftHighlightMode driftHighlightMode = BatchDriftHighlightMode.Off;
        private BatchPathMoveVisibilityFilter moveVisibilityFilter = BatchPathMoveVisibilityFilter.All;
        private readonly HashSet<long> driftNodeKeys = new HashSet<long>();
        private readonly HashSet<long> driftSegmentKeys = new HashSet<long>();

        private struct NodeVisual
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public GenomePathPointData Point;
            public bool IsInitial;
            public bool IsSolution;
            public bool IsAcceptedGenome;
            public int RunIndex;
            public int PointIndex;
        }

        private struct SegmentVisual
        {
            public GameObject GameObject;
            public Renderer Renderer;
            public string ChangeType;
            public int RunIndex;
            public int FromPointIndex;
        }

        private struct LayoutParams
        {
            public Vector2 mdsCenter;
            public float mdsScale;
            public float fitnessScale;
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
        public float MinOrbitDistance => Mathf.Max(nodeDiameter * 4f, 0.5f);
        public string RunLabelText { get; private set; } = string.Empty;
        public int LoadedRunCount { get; private set; }
        public bool ShowMoveClassification => showMoveClassification;
        public bool ShowDriftMovesOnly => driftHighlightMode == BatchDriftHighlightMode.UnifiedYellow;
        public bool ShowDriftMovesByCategory => driftHighlightMode == BatchDriftHighlightMode.ByCategory;
        public BatchDriftHighlightMode DriftHighlightMode => driftHighlightMode;
        public BatchPathMoveVisibilityFilter MoveVisibilityFilter => moveVisibilityFilter;

        private void Start()
        {
            if (FindFirstObjectByType<Menu.AppMenuController>() != null)
            {
                return;
            }

            RebuildVisualisation();
        }

        public void SetShowMoveClassification(bool value)
        {
            if (showMoveClassification == value)
            {
                return;
            }

            showMoveClassification = value;
            if (value)
            {
                driftHighlightMode = BatchDriftHighlightMode.Off;
            }

            ApplyAppearanceMode();
        }

        public void SetShowDriftMovesOnly(bool value)
        {
            SetDriftHighlightMode(value ? BatchDriftHighlightMode.UnifiedYellow : BatchDriftHighlightMode.Off);
        }

        public void SetShowDriftMovesByCategory(bool value)
        {
            SetDriftHighlightMode(value ? BatchDriftHighlightMode.ByCategory : BatchDriftHighlightMode.Off);
        }

        public void SetDriftHighlightMode(BatchDriftHighlightMode mode)
        {
            if (driftHighlightMode == mode)
            {
                return;
            }

            driftHighlightMode = mode;
            if (mode != BatchDriftHighlightMode.Off)
            {
                showMoveClassification = false;
                moveVisibilityFilter = BatchPathMoveVisibilityFilter.All;
            }

            ApplyAppearanceMode();
        }

        public void SetMoveVisibilityFilter(BatchPathMoveVisibilityFilter filter)
        {
            if (moveVisibilityFilter == filter)
            {
                return;
            }

            moveVisibilityFilter = filter;
            if (filter != BatchPathMoveVisibilityFilter.All)
            {
                driftHighlightMode = BatchDriftHighlightMode.Off;
            }

            ApplyAppearanceMode();
        }

        [ContextMenu("Rebuild Visualisation")]
        public void RebuildVisualisation()
        {
            TrajectoryLightingSetup.Apply();

            string json = LoadJsonText();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError(
                    $"BatchGenomeMdsFitnessPathVisualiser: could not load '{jsonFileName}'. " +
                    "Run demo.py without --shared-initial-genome.");
                return;
            }

            GenomePathBatchData batch = JsonUtility.FromJson<GenomePathBatchData>(json);
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                Debug.LogError("BatchGenomeMdsFitnessPathVisualiser: JSON parsed but contains no runs.");
                return;
            }

            if (batch.sharedInitialGenome)
            {
                Debug.LogError(
                    "BatchGenomeMdsFitnessPathVisualiser: JSON is marked sharedInitialGenome=true. " +
                    "Use Option 6 or re-export without --shared-initial-genome.");
                return;
            }

            int runLimit = Mathf.Clamp(maxRunCount, 1, batch.runs.Length);
            LoadedRunCount = runLimit;
            RunLabelText = BuildRunLabelText(batch, runLimit);
            BuildVisualisation(batch, runLimit);
            Debug.Log(
                $"BatchGenomeMdsFitnessPathVisualiser: built {runLimit} run path(s) " +
                $"(X/Z = MDS 1/2, Y = fitness, {batch.genomeCount} genomes in batch).");
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

        private void BuildVisualisation(GenomePathBatchData batch, int runLimit)
        {
            ClearSpawnedObjects();
            ResetContentBounds();
            changeTypeMaterials.Clear();

            LayoutParams layout = ComputeLayout(batch, runLimit);
            BuildDriftVisibilitySets(batch, runLimit);

            for (int runIndex = 0; runIndex < runLimit; runIndex++)
            {
                CreateRunPath(batch.runs[runIndex], runIndex, layout);
            }

            FinalizeContentBounds();
            ApplyAppearanceMode();
        }

        private void BuildDriftVisibilitySets(GenomePathBatchData batch, int runLimit)
        {
            driftNodeKeys.Clear();
            driftSegmentKeys.Clear();

            for (int runIndex = 0; runIndex < runLimit; runIndex++)
            {
                PlateauEpisode[] episodes = PlateauEpisodeAnalyzer.AnalyzeRun(batch.runs[runIndex], null);
                for (int episodeIndex = 0; episodeIndex < episodes.Length; episodeIndex++)
                {
                    PlateauEpisode episode = episodes[episodeIndex];
                    for (int stepIndex = episode.StartStep; stepIndex <= episode.EndStep; stepIndex++)
                    {
                        driftNodeKeys.Add(MakeStepKey(runIndex, stepIndex));
                    }

                    for (int stepIndex = episode.StartStep; stepIndex < episode.EndStep; stepIndex++)
                    {
                        driftSegmentKeys.Add(MakeStepKey(runIndex, stepIndex));
                    }
                }
            }
        }

        private static long MakeStepKey(int runIndex, int stepIndex)
        {
            return ((long)runIndex << 20) | (uint)stepIndex;
        }

        private void CreateRunPath(GenomePathRunData run, int runIndex, LayoutParams layout)
        {
            if (run.points == null || run.points.Length == 0)
            {
                return;
            }

            string runLabel = string.IsNullOrWhiteSpace(run.sourceLabel) ? "run" : run.sourceLabel;
            GameObject pathRoot = new GameObject($"BatchMdsFitnessPath_{runLabel}");
            pathRoot.transform.SetParent(transform, false);
            spawnedObjects.Add(pathRoot);

            int lastPointIndex = run.points.Length - 1;
            int acceptedCount = run.acceptedCount > 0
                ? Mathf.Min(run.acceptedCount, run.points.Length)
                : run.points.Length;
            for (int pointIndex = 0; pointIndex < run.points.Length; pointIndex++)
            {
                GenomePathPointData point = run.points[pointIndex];
                Vector3 position = TransformPoint(point, layout);
                IncludeInBounds(position);

                bool isInitial = pointIndex == 0;
                bool isSolution = pointIndex == lastPointIndex;
                bool isAcceptedGenome = pointIndex < acceptedCount;
                float diameter = isInitial || isSolution ? endpointNodeDiameter : nodeDiameter;
                GameObject node = CreateNodeSphere(
                    pathRoot.transform,
                    position,
                    diameter,
                    ResolveGenomePointMaterial(point, isInitial, isSolution),
                    isInitial ? "InitialJoint" : isSolution ? "SolutionJoint" : $"Genome_{point.genomeIndex}");
                nodeVisuals.Add(new NodeVisual
                {
                    GameObject = node,
                    Renderer = node.GetComponent<Renderer>(),
                    Point = point,
                    IsInitial = isInitial,
                    IsSolution = isSolution,
                    IsAcceptedGenome = isAcceptedGenome,
                    RunIndex = runIndex,
                    PointIndex = pointIndex,
                });

                if (pointIndex >= lastPointIndex)
                {
                    continue;
                }

                GenomePathPointData nextPoint = run.points[pointIndex + 1];
                Vector3 end = TransformPoint(nextPoint, layout);
                IncludeInBounds(end);
                GameObject segment = CreateTubeSegment(
                    pathRoot.transform,
                    position,
                    end,
                    tubeDiameter,
                    ResolveSegmentMaterial(nextPoint.changeType));
                if (segment != null)
                {
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
        }

        private void ApplyAppearanceMode()
        {
            for (int index = 0; index < nodeVisuals.Count; index++)
            {
                NodeVisual node = nodeVisuals[index];
                if (node.GameObject == null)
                {
                    continue;
                }

                bool showNode = ShouldShowNode(node);
                node.GameObject.SetActive(showNode);
                if (!showNode || node.Renderer == null)
                {
                    continue;
                }

                Material material = ResolveGenomePointMaterial(node);
                TrajectoryRenderUtility.ConfigureLitRenderer(node.Renderer, material);
            }

            for (int index = 0; index < segmentVisuals.Count; index++)
            {
                SegmentVisual segment = segmentVisuals[index];
                if (segment.GameObject == null)
                {
                    continue;
                }

                bool showSegment = ShouldShowSegment(segment);
                segment.GameObject.SetActive(showSegment);
                if (!showSegment || segment.Renderer == null)
                {
                    continue;
                }

                Material material = ResolveSegmentMaterial(segment);
                TrajectoryRenderUtility.ConfigureLitRenderer(segment.Renderer, material);
            }
        }

        private bool ShouldShowNode(NodeVisual node)
        {
            if (driftHighlightMode != BatchDriftHighlightMode.Off)
            {
                return true;
            }

            if (node.IsInitial || node.IsSolution)
            {
                return true;
            }

            switch (moveVisibilityFilter)
            {
                case BatchPathMoveVisibilityFilter.NeutralOnly:
                    return GenomePathChangeTypeStyles.IsNeutralMove(node.Point.changeType);
                case BatchPathMoveVisibilityFilter.ImprovementOnly:
                    return GenomePathChangeTypeStyles.IsImprovementMove(node.Point.changeType);
                default:
                    return true;
            }
        }

        private bool ShouldShowSegment(SegmentVisual segment)
        {
            if (driftHighlightMode != BatchDriftHighlightMode.Off)
            {
                return true;
            }

            switch (moveVisibilityFilter)
            {
                case BatchPathMoveVisibilityFilter.NeutralOnly:
                    return GenomePathChangeTypeStyles.IsNeutralMove(segment.ChangeType);
                case BatchPathMoveVisibilityFilter.ImprovementOnly:
                    return GenomePathChangeTypeStyles.IsImprovementMove(segment.ChangeType);
                default:
                    return true;
            }
        }

        private Material ResolveGenomePointMaterial(NodeVisual node)
        {
            if (driftHighlightMode != BatchDriftHighlightMode.Off)
            {
                return ResolveDriftModeNodeMaterial(node);
            }

            return ResolveGenomePointMaterial(node.Point, node.IsInitial, node.IsSolution);
        }

        private Material ResolveDriftModeNodeMaterial(NodeVisual node)
        {
            if (node.IsInitial)
            {
                return GetInitialMaterial();
            }

            if (node.IsSolution)
            {
                return GetSolutionMaterial();
            }

            if (driftNodeKeys.Contains(MakeStepKey(node.RunIndex, node.PointIndex)))
            {
                if (driftHighlightMode == BatchDriftHighlightMode.ByCategory && node.PointIndex > 0)
                {
                    return GetDriftCategoryMaterial(node.Point.changeType);
                }

                return GetDriftMaterial();
            }

            return GetIntermediateMaterial();
        }

        private Material ResolveGenomePointMaterial(
            GenomePathPointData point,
            bool isInitial,
            bool isSolution)
        {
            if (isInitial)
            {
                return GetInitialMaterial();
            }

            if (isSolution)
            {
                return GetSolutionMaterial();
            }

            if (showMoveClassification)
            {
                if (IsHighlightedPlateauFitness(point.fitness))
                {
                    return GetIntermediateMaterial();
                }

                return GetMaterialForChangeType(point.changeType);
            }

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

        private Material ResolveSegmentMaterial(SegmentVisual segment)
        {
            if (driftHighlightMode != BatchDriftHighlightMode.Off)
            {
                return ResolveDriftModeSegmentMaterial(segment);
            }

            return ResolveSegmentMaterial(segment.ChangeType);
        }

        private Material ResolveDriftModeSegmentMaterial(SegmentVisual segment)
        {
            if (driftSegmentKeys.Contains(MakeStepKey(segment.RunIndex, segment.FromPointIndex)))
            {
                if (driftHighlightMode == BatchDriftHighlightMode.ByCategory)
                {
                    return GetDriftCategoryMaterial(segment.ChangeType);
                }

                return GetDriftMaterial();
            }

            return GetPathMaterial();
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

        private Material ResolveSegmentMaterial(string changeType)
        {
            if (!showMoveClassification)
            {
                return GetPathMaterial();
            }

            return GetMaterialForChangeType(changeType);
        }

        private Material GetMaterialForChangeType(string changeType)
        {
            string key = string.IsNullOrWhiteSpace(changeType) ? "unknown" : changeType;
            if (changeTypeMaterials.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Color color = GenomePathChangeTypeStyles.GetColor(changeType, pathColor);
            Material material = TrajectoryRenderUtility.CreateLitMaterial(color, smoothness: 0.35f);
            changeTypeMaterials[key] = material;
            return material;
        }

        private static bool IsHighlightedPlateauFitness(float fitness)
        {
            return ApproxFitness(fitness, Fitness025Target)
                   || ApproxFitness(fitness, Fitness05Target)
                   || ApproxFitness(fitness, Fitness296Target);
        }

        private static bool ApproxFitness(float fitness, float target)
        {
            return Mathf.Abs(fitness - target) <= FitnessMatchEpsilon;
        }

        private LayoutParams ComputeLayout(GenomePathBatchData batch, int runLimit)
        {
            float minMdsX = float.MaxValue;
            float maxMdsX = float.MinValue;
            float minMdsY = float.MaxValue;
            float maxMdsY = float.MinValue;
            bool hasPoints = false;

            for (int runIndex = 0; runIndex < runLimit; runIndex++)
            {
                GenomePathPointData[] points = batch.runs[runIndex].points;
                if (points == null)
                {
                    continue;
                }

                for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
                {
                    GenomePathPointData point = points[pointIndex];
                    minMdsX = Mathf.Min(minMdsX, point.x);
                    maxMdsX = Mathf.Max(maxMdsX, point.x);
                    minMdsY = Mathf.Min(minMdsY, point.y);
                    maxMdsY = Mathf.Max(maxMdsY, point.y);
                    hasPoints = true;
                }
            }

            if (!hasPoints)
            {
                return new LayoutParams
                {
                    mdsCenter = Vector2.zero,
                    mdsScale = 1f,
                    fitnessScale = fitnessHeightScale,
                };
            }

            Vector2 center = new Vector2(
                (minMdsX + maxMdsX) * 0.5f,
                (minMdsY + maxMdsY) * 0.5f);
            float extentX = maxMdsX - minMdsX;
            float extentY = maxMdsY - minMdsY;
            float maxAxis = Mathf.Max(extentX, extentY, 0.001f);

            return new LayoutParams
            {
                mdsCenter = center,
                mdsScale = targetExtent / maxAxis,
                fitnessScale = fitnessHeightScale,
            };
        }

        private Vector3 TransformPoint(GenomePathPointData point, LayoutParams layout)
        {
            float mdsX = (point.x - layout.mdsCenter.x) * layout.mdsScale;
            float mdsZ = (point.y - layout.mdsCenter.y) * layout.mdsScale;
            float fitnessHeight = point.fitness * layout.fitnessScale;
            return transform.position + new Vector3(mdsX, fitnessHeight, mdsZ);
        }

        private static string BuildRunLabelText(GenomePathBatchData batch, int runLimit)
        {
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                return string.Empty;
            }

            if (runLimit < batch.runs.Length)
            {
                return $"First {runLimit} of {batch.runCount} runs";
            }

            return $"{runLimit} runs";
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

        private void FinalizeContentBounds()
        {
            if (!hasContentBounds)
            {
                contentCenter = transform.position;
                contentExtent = Mathf.Max(targetExtent, fitnessHeightScale);
                EnsureOrbitFocus();
                orbitFocus.position = contentCenter;
                return;
            }

            contentCenter = (boundsMin + boundsMax) * 0.5f;
            Vector3 size = boundsMax - boundsMin;
            contentExtent = Mathf.Max(size.x, size.y, size.z, 1f);
            EnsureOrbitFocus();
            orbitFocus.position = contentCenter;
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

        private Material GetFitness025Material()
        {
            fitness025Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness025NodeColor, smoothness: 0.42f);
            return fitness025Material;
        }

        private Material GetFitness05Material()
        {
            fitness05Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness05NodeColor, smoothness: 0.42f);
            return fitness05Material;
        }

        private Material GetFitness296Material()
        {
            fitness296Material ??= TrajectoryRenderUtility.CreateLitMaterial(fitness296NodeColor, smoothness: 0.42f);
            return fitness296Material;
        }

        private Material GetDriftMaterial()
        {
            driftMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(driftMoveColor, smoothness: 0.42f);
            return driftMaterial;
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
            nodeVisuals.Clear();
            segmentVisuals.Clear();
        }
    }
}
