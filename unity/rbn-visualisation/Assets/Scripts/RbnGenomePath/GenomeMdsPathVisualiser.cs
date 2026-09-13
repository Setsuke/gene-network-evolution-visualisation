using System.Collections.Generic;
using System.IO;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Loads unity_genome_mds_path.json and draws accepted genome paths in 3D MDS space.
    /// </summary>
    public class GenomeMdsPathVisualiser : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string jsonFileName = "unity_genome_mds_path.json";
        [SerializeField] private TextAsset jsonOverride;

        [Header("Layout")]
        [SerializeField] private float targetExtent = 18f;
        [SerializeField] private float tubeDiameter = 0.12f;
        [SerializeField] private float jointDiameter = 0.16f;
        [SerializeField] private float endpointJointDiameter = 0.28f;

        [Header("Appearance")]
        [SerializeField] private Color defaultPathColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color initialJointColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color solutionJointColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color focusedJointColor = Color.cyan;
        [SerializeField] private Color groundColor = new Color(0.16f, 0.17f, 0.19f, 1f);
        [SerializeField] private bool drawGround = false;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly Dictionary<string, Material> changeTypeMaterials = new Dictionary<string, Material>();
        private Material groundMaterial;
        private Material focusedJointMaterial;
        private GenomeMdsFitnessPathRunState activeRun;
        private int highlightedStepIndex = -1;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private bool hasContentBounds;

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
        public bool HasActiveRun => activeRun != null && activeRun.PointCount > 0;
        public int StepCount => activeRun?.PointCount ?? 0;
        public int GeneCount { get; private set; }
        public int ContextCount { get; private set; }
        public string[] ContextTargets { get; private set; }
        public float MinOrbitDistance => Mathf.Max(jointDiameter * 4f, 0.5f);
        public float RecommendedFocusDistance =>
            Mathf.Max(endpointJointDiameter * 10f, jointDiameter * 16f, 2f);
        public int HighlightedStepIndex => highlightedStepIndex;

        public bool TryGetStepData(int stepIndex, out float fitness, out float[] factorScores)
        {
            fitness = 0f;
            factorScores = null;
            if (activeRun == null || stepIndex < 0 || stepIndex >= activeRun.PointCount)
            {
                return false;
            }

            fitness = activeRun.FitnessValues[stepIndex];
            if (activeRun.FactorScoresPerStep != null && stepIndex < activeRun.FactorScoresPerStep.Length)
            {
                factorScores = activeRun.FactorScoresPerStep[stepIndex];
            }

            return true;
        }

        public int TotalAcceptedGenomes => activeRun?.TotalAcceptedGenomes ?? 0;

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
                    $"GenomeMdsPathVisualiser: could not load '{jsonFileName}'. " +
                    $"Expected at StreamingAssets/{jsonFileName} or assign a TextAsset override.");
                return;
            }

            GenomePathBatchData batch = JsonUtility.FromJson<GenomePathBatchData>(json);
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                Debug.LogError("GenomeMdsPathVisualiser: JSON parsed but contains no runs.");
                return;
            }

            BuildVisualisation(batch, json);
            Debug.Log(
                $"GenomeMdsPathVisualiser: built {batch.runCount} MDS path(s) " +
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

        private void BuildVisualisation(GenomePathBatchData batch, string json)
        {
            ClearSpawnedObjects();
            changeTypeMaterials.Clear();
            ResetContentBounds();
            activeRun = null;
            highlightedStepIndex = -1;
            RunLabelText = BuildRunLabelText(batch);
            ApplyBatchFactorMetadata(batch, json);

            NormalizationParams normalization = ComputeNormalization(batch);

            if (drawGround)
            {
                CreateGroundPlane(targetExtent);
            }

            activeRun = null;
            highlightedStepIndex = -1;

            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                CreateRunPath(batch.runs[runIndex], normalization, runIndex == 0);
            }

            FinalizeContentBounds();
        }

        public void HighlightStep(int stepIndex, float scaleMultiplier)
        {
            ClearStepHighlight();
            if (activeRun == null || stepIndex < 0 || stepIndex >= activeRun.PointCount)
            {
                return;
            }

            highlightedStepIndex = stepIndex;
            GameObject joint = activeRun.JointObjects[stepIndex];
            if (joint == null)
            {
                return;
            }

            Vector3 baseScale = activeRun.JointBaseScales[stepIndex];
            joint.transform.localScale = baseScale * scaleMultiplier;

            if (joint.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = GetFocusedJointMaterial();
            }

            OrbitFocus.position = activeRun.WorldPositions[stepIndex];
        }

        public void ClearStepHighlight()
        {
            if (activeRun == null || highlightedStepIndex < 0)
            {
                highlightedStepIndex = -1;
                return;
            }

            int index = highlightedStepIndex;
            highlightedStepIndex = -1;
            if (index >= activeRun.JointObjects.Length)
            {
                return;
            }

            GameObject joint = activeRun.JointObjects[index];
            if (joint != null)
            {
                joint.transform.localScale = activeRun.JointBaseScales[index];

                if (joint.TryGetComponent(out Renderer renderer)
                    && index < activeRun.JointBaseMaterials.Length
                    && activeRun.JointBaseMaterials[index] != null)
                {
                    renderer.sharedMaterial = activeRun.JointBaseMaterials[index];
                }
            }
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
                contentExtent = targetExtent;
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
            focusObject.layer = gameObject.layer;
            orbitFocus = focusObject.transform;
        }

        private NormalizationParams ComputeNormalization(GenomePathBatchData batch)
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

        private void CreateRunPath(
            GenomePathRunData run,
            NormalizationParams normalization,
            bool trackForStepNavigation)
        {
            if (run.points == null || run.points.Length == 0)
            {
                return;
            }

            string runLabel = string.IsNullOrWhiteSpace(run.sourceLabel) ? "run" : run.sourceLabel;
            int pointCount = run.points.Length;
            Vector3[] positions = new Vector3[pointCount];
            GenomeMdsFitnessPathRunState runState = trackForStepNavigation
                ? new GenomeMdsFitnessPathRunState
                {
                    RunLabel = runLabel,
                    TotalAcceptedGenomes = run.acceptedCount > 0
                        ? Mathf.Min(run.acceptedCount, pointCount)
                        : pointCount,
                    WorldPositions = new Vector3[pointCount],
                    FitnessValues = new float[pointCount],
                    FactorScoresPerStep = new float[pointCount][],
                    JointObjects = new GameObject[pointCount],
                    JointBaseScales = new Vector3[pointCount],
                    JointBaseMaterials = new Material[pointCount],
                }
                : null;

            for (int i = 0; i < pointCount; i++)
            {
                positions[i] = TransformPoint(run.points[i], normalization);
                IncludeInBounds(positions[i]);
                if (runState != null)
                {
                    runState.WorldPositions[i] = positions[i];
                    runState.FitnessValues[i] = run.points[i].fitness;
                    runState.FactorScoresPerStep[i] =
                        run.points[i].factorScores ?? System.Array.Empty<float>();
                }
            }

            GameObject pathRoot = new GameObject($"MdsPath_{runLabel}");
            pathRoot.layer = gameObject.layer;
            pathRoot.transform.SetParent(transform, false);
            spawnedObjects.Add(pathRoot);

            for (int i = 0; i < pointCount - 1; i++)
            {
                string changeType = run.points[i + 1].changeType;
                CreateTubeSegment(
                    pathRoot.transform,
                    positions[i],
                    positions[i + 1],
                    tubeDiameter,
                    GetMaterialForChangeType(changeType));
            }

            for (int i = 0; i < pointCount; i++)
            {
                bool isInitial = i == 0;
                bool isSolution = i == pointCount - 1;
                float diameter = isInitial || isSolution ? endpointJointDiameter : jointDiameter;
                Material material = ResolveJointMaterial(run.points[i], isInitial, isSolution);
                string jointName = isInitial ? "InitialJoint" : isSolution ? "SolutionJoint" : "Joint";
                GameObject joint = CreateJointSphere(
                    pathRoot.transform,
                    positions[i],
                    diameter,
                    material,
                    jointName);
                if (runState != null)
                {
                    runState.JointObjects[i] = joint;
                    runState.JointBaseScales[i] = joint.transform.localScale;
                    if (joint.TryGetComponent(out Renderer renderer))
                    {
                        runState.JointBaseMaterials[i] = renderer.sharedMaterial;
                    }
                }
            }

            if (runState != null)
            {
                activeRun = runState;
            }
        }

        private static string BuildRunLabelText(GenomePathBatchData batch)
        {
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                return string.Empty;
            }

            List<string> labels = new List<string>(batch.runs.Length);
            for (int runIndex = 0; runIndex < batch.runs.Length; runIndex++)
            {
                GenomePathRunData run = batch.runs[runIndex];
                labels.Add(string.IsNullOrWhiteSpace(run.sourceLabel) ? "run" : run.sourceLabel);
            }

            return string.Join(", ", labels);
        }

        private static Vector3 RawPoint(GenomePathPointData point)
        {
            return new Vector3(point.x, point.y, point.z);
        }

        private Vector3 TransformPoint(GenomePathPointData point, NormalizationParams normalization)
        {
            return transform.position + (RawPoint(point) - normalization.center) * normalization.scale;
        }

        private Material ResolveJointMaterial(GenomePathPointData point, bool isInitial, bool isSolution)
        {
            if (isInitial)
            {
                return GetEndpointMaterial("initial", initialJointColor);
            }

            if (isSolution)
            {
                return GetEndpointMaterial("solution", solutionJointColor);
            }

            return GetMaterialForChangeType(point.changeType);
        }

        private Material GetEndpointMaterial(string key, Color color)
        {
            if (changeTypeMaterials.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Material material = TrajectoryRenderUtility.CreateLitMaterial(color, smoothness: 0.4f);
            changeTypeMaterials[key] = material;
            return material;
        }

        private Material GetMaterialForChangeType(string changeType)
        {
            string key = string.IsNullOrWhiteSpace(changeType) ? "unknown" : changeType;
            if (changeTypeMaterials.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Color color = GenomePathChangeTypeStyles.GetColor(changeType, defaultPathColor);

            Material material = TrajectoryRenderUtility.CreateLitMaterial(color, smoothness: 0.35f);
            changeTypeMaterials[key] = material;
            return material;
        }

        private void ApplyBatchFactorMetadata(GenomePathBatchData batch, string json)
        {
            if (batch == null)
            {
                GeneCount = 0;
                ContextCount = 0;
                ContextTargets = null;
                return;
            }

            EnsureContextTargets(batch, json);
            GeneCount = batch.geneCount;
            ContextCount = batch.contextCount;

            if (GeneCount > 0 && ContextCount > 0)
            {
                ContextTargets = ExtractContextTargets(batch.contexts, ContextCount, GeneCount);
                return;
            }

            if (batch.runs == null || batch.runs.Length == 0)
            {
                ContextTargets = ExtractContextTargets(batch.contexts, ContextCount, GeneCount);
                return;
            }

            GenomePathPointData[] points = batch.runs[0].points;
            if (points == null || points.Length == 0 || points[0].factorScores == null)
            {
                ContextTargets = ExtractContextTargets(batch.contexts, ContextCount, GeneCount);
                return;
            }

            int factorCount = points[0].factorScores.Length;
            if (ContextCount > 0)
            {
                GeneCount = factorCount / ContextCount;
            }
            else if (GeneCount > 0)
            {
                ContextCount = factorCount / GeneCount;
            }

            ContextTargets = ExtractContextTargets(batch.contexts, ContextCount, GeneCount);
        }

        private static void EnsureContextTargets(GenomePathBatchData batch, string json)
        {
            if (batch == null || batch.contextCount <= 0)
            {
                return;
            }

            if (batch.contexts != null && batch.contexts.Length > 0)
            {
                return;
            }

            batch.contexts = ParseContextsFromJson(json, batch.contextCount);
        }

        private static GenomePathContextData[] ParseContextsFromJson(string json, int contextCount)
        {
            if (string.IsNullOrEmpty(json) || contextCount <= 0)
            {
                return null;
            }

            int contextsIndex = json.IndexOf("\"contexts\"", System.StringComparison.Ordinal);
            if (contextsIndex < 0)
            {
                return null;
            }

            var parsed = new List<GenomePathContextData>();
            int searchStart = contextsIndex;
            const string targetKey = "\"target\"";
            const string labelKey = "\"label\"";

            while (parsed.Count < contextCount)
            {
                int labelIndex = json.IndexOf(labelKey, searchStart, System.StringComparison.Ordinal);
                int targetIndex = json.IndexOf(targetKey, searchStart, System.StringComparison.Ordinal);
                if (targetIndex < 0)
                {
                    break;
                }

                string label = TryReadJsonStringValue(json, labelIndex, labelKey);
                string target = TryReadJsonStringValue(json, targetIndex, targetKey);
                if (string.IsNullOrEmpty(target))
                {
                    break;
                }

                parsed.Add(new GenomePathContextData
                {
                    label = string.IsNullOrEmpty(label) ? $"Context {parsed.Count + 1}" : label,
                    target = target,
                });
                searchStart = targetIndex + targetKey.Length;
            }

            return parsed.Count > 0 ? parsed.ToArray() : null;
        }

        private static string TryReadJsonStringValue(string json, int keyIndex, string key)
        {
            if (keyIndex < 0)
            {
                return null;
            }

            int colonIndex = json.IndexOf(':', keyIndex + key.Length);
            if (colonIndex < 0)
            {
                return null;
            }

            int quoteStart = json.IndexOf('"', colonIndex + 1);
            if (quoteStart < 0)
            {
                return null;
            }

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0)
            {
                return null;
            }

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        private static string[] ExtractContextTargets(
            GenomePathContextData[] contexts,
            int contextCount,
            int geneCount)
        {
            if (contexts == null || contexts.Length == 0 || contextCount <= 0)
            {
                return null;
            }

            string[] targets = new string[contextCount];
            for (int contextIndex = 0; contextIndex < contextCount; contextIndex++)
            {
                if (contextIndex < contexts.Length && !string.IsNullOrEmpty(contexts[contextIndex].target))
                {
                    targets[contextIndex] = contexts[contextIndex].target;
                }
                else
                {
                    int placeholderLength = geneCount > 0 ? geneCount : 5;
                    targets[contextIndex] = new string('?', placeholderLength);
                }
            }

            return targets;
        }

        private Material GetGroundMaterial()
        {
            groundMaterial ??= TrajectoryRenderUtility.CreateLitMaterial(groundColor, smoothness: 0.18f);
            return groundMaterial;
        }

        private void CreateGroundPlane(float extent)
        {
            float size = extent * 1.35f;
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ground.name = "Ground";
            ground.layer = gameObject.layer;
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            ground.transform.localScale = new Vector3(size, 0.03f, size);
            spawnedObjects.Add(ground);

            TrajectoryRenderUtility.ConfigureLitRenderer(ground.GetComponent<Renderer>(), GetGroundMaterial());
            DestroyCollider(ground);
        }

        private void CreateTubeSegment(
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
                return;
            }

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            segment.name = "Segment";
            segment.layer = gameObject.layer;
            segment.transform.SetParent(parent, false);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            segment.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            spawnedObjects.Add(segment);

            TrajectoryRenderUtility.ConfigureLitRenderer(segment.GetComponent<Renderer>(), material);
            DestroyCollider(segment);
        }

        private Material GetFocusedJointMaterial()
        {
            if (focusedJointMaterial == null)
            {
                focusedJointMaterial = TrajectoryRenderUtility.CreateLitMaterial(
                    focusedJointColor,
                    smoothness: 0.45f);
            }

            return focusedJointMaterial;
        }

        private GameObject CreateJointSphere(
            Transform parent,
            Vector3 position,
            float diameter,
            Material material,
            string objectName = "Joint")
        {
            GameObject joint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            joint.name = objectName;
            joint.layer = gameObject.layer;
            joint.transform.SetParent(parent, false);
            joint.transform.position = position;
            joint.transform.localScale = Vector3.one * diameter;
            spawnedObjects.Add(joint);

            TrajectoryRenderUtility.ConfigureLitRenderer(joint.GetComponent<Renderer>(), material);
            DestroyCollider(joint);
            return joint;
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
        }
    }
}
