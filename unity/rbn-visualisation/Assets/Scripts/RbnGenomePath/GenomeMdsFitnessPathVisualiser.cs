using System.Collections.Generic;
using System.IO;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// MDS plane (components 1 and 2) with total fitness on the vertical axis.
    /// </summary>
    public class GenomeMdsFitnessPathVisualiser : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string jsonFileName = "unity_genome_mds_path.json";
        [SerializeField] private string batchJsonFileName = "unity_batch_genome_mds_path.json";
        [SerializeField] private string sharedJsonFileName = "unity_shared_genome_mds_path.json";
        [SerializeField] private TextAsset jsonOverride;
        [SerializeField] private int maxRunCount = 200;

        [Header("Layout")]
        [SerializeField] private float targetExtent = 18f;
        [SerializeField] private float fitnessHeightScale = 5f;
        [SerializeField] private float tubeDiameter = 0.12f;
        [SerializeField] private float jointDiameter = 0.16f;
        [SerializeField] private float endpointJointDiameter = 0.28f;

        [Header("Appearance")]
        [SerializeField] private Color defaultPathColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color initialJointColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color solutionJointColor = new Color(0.86f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color focusedJointColor = Color.cyan;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly Dictionary<string, Material> changeTypeMaterials = new Dictionary<string, Material>();
        private Material focusedJointMaterial;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private bool hasContentBounds;
        private GenomeMdsFitnessPathRunState[] runStates = System.Array.Empty<GenomeMdsFitnessPathRunState>();
        private GenomeMdsFitnessPathRunState activeRun;
        private int activeRunIndex;
        private int highlightedStepIndex = -1;

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
        public int GeneCount { get; private set; }
        public int ContextCount { get; private set; }
        public string[] ContextTargets { get; private set; }
        public float MinOrbitDistance => Mathf.Max(jointDiameter * 4f, 0.5f);
        public float RecommendedFocusDistance =>
            Mathf.Max(endpointJointDiameter * 10f, jointDiameter * 16f, 2f);
        public string RunLabelText { get; private set; } = string.Empty;
        public string ActiveRunLabel =>
            activeRun != null && !string.IsNullOrWhiteSpace(activeRun.RunLabel)
                ? activeRun.RunLabel
                : RunLabelText;
        public int RunCount => runStates?.Length ?? 0;
        public int ActiveRunIndex => activeRunIndex;
        public GenomeMdsFitnessPathRunState ActiveRun => activeRun;
        public bool HasActiveRun => activeRun != null && activeRun.PointCount > 0;

        public void SetActiveRun(int runIndex)
        {
            if (runStates == null || runStates.Length == 0)
            {
                return;
            }

            runIndex = Mathf.Clamp(runIndex, 0, runStates.Length - 1);
            if (runIndex == activeRunIndex && activeRun == runStates[runIndex])
            {
                return;
            }

            activeRunIndex = runIndex;
            activeRun = runStates[runIndex];
            ApplyRunVisibility();
            ClearStepHighlight();
            RunLabelText = ActiveRunLabel;
        }

        private struct LayoutParams
        {
            public Vector2 mdsCenter;
            public float mdsScale;
            public float fitnessScale;
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
                    $"GenomeMdsFitnessPathVisualiser: could not load '{jsonFileName}'. " +
                    $"Expected at StreamingAssets/{jsonFileName} or assign a TextAsset override.");
                return;
            }

            GenomePathBatchData batch = JsonUtility.FromJson<GenomePathBatchData>(json);
            if (batch?.runs == null || batch.runs.Length == 0)
            {
                Debug.LogError("GenomeMdsFitnessPathVisualiser: JSON parsed but contains no runs.");
                return;
            }

            EnsureContextTargets(batch, json);

            BuildVisualisation(batch);
            Debug.Log(
                $"GenomeMdsFitnessPathVisualiser: built {RunCount} path(s) from '{LoadedJsonFileName}' " +
                $"(X/Z = MDS components 1/2, Y = fitness).");
        }

        private string LoadedJsonFileName { get; set; } = string.Empty;

        private string LoadJsonText()
        {
            if (jsonOverride != null)
            {
                LoadedJsonFileName = jsonOverride.name;
                return jsonOverride.text;
            }

            string batchJson = TryLoadJsonFile(batchJsonFileName);
            if (!string.IsNullOrWhiteSpace(batchJson))
            {
                LoadedJsonFileName = batchJsonFileName;
                return batchJson;
            }

            string sharedJson = TryLoadJsonFile(sharedJsonFileName);
            if (!string.IsNullOrWhiteSpace(sharedJson))
            {
                LoadedJsonFileName = sharedJsonFileName;
                return sharedJson;
            }

            string singleRunJson = TryLoadJsonFile(jsonFileName);
            if (!string.IsNullOrWhiteSpace(singleRunJson))
            {
                LoadedJsonFileName = jsonFileName;
                return singleRunJson;
            }

            LoadedJsonFileName = string.Empty;
            return null;
        }

        private static string TryLoadJsonFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
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

        private void BuildVisualisation(GenomePathBatchData batch)
        {
            ClearSpawnedObjects();
            changeTypeMaterials.Clear();
            focusedJointMaterial = null;
            ResetContentBounds();
            runStates = System.Array.Empty<GenomeMdsFitnessPathRunState>();
            activeRun = null;
            activeRunIndex = 0;
            highlightedStepIndex = -1;
            GeneCount = 0;
            ContextCount = 0;
            RunLabelText = string.Empty;
            ApplyBatchFactorMetadata(batch);

            LayoutParams layout = ComputeLayout(batch);
            int runLimit = Mathf.Min(batch.runs.Length, Mathf.Max(maxRunCount, 1));
            var builtRuns = new List<GenomeMdsFitnessPathRunState>(runLimit);

            for (int runIndex = 0; runIndex < runLimit; runIndex++)
            {
                GenomeMdsFitnessPathRunState runState = CreateRunPath(batch.runs[runIndex], layout);
                if (runState != null && runState.PointCount > 0)
                {
                    builtRuns.Add(runState);
                }
            }

            runStates = builtRuns.ToArray();
            if (runStates.Length > 0)
            {
                SetActiveRun(0);
            }
            else
            {
                activeRun = null;
                RunLabelText = BuildRunLabelText(batch);
            }

            FinalizeContentBounds();
        }

        private void ApplyRunVisibility()
        {
            if (runStates == null)
            {
                return;
            }

            for (int runIndex = 0; runIndex < runStates.Length; runIndex++)
            {
                GameObject pathRoot = runStates[runIndex].PathRoot;
                if (pathRoot != null)
                {
                    pathRoot.SetActive(runIndex == activeRunIndex);
                }
            }
        }

        private GenomeMdsFitnessPathRunState CreateRunPath(GenomePathRunData run, LayoutParams layout)
        {
            if (run.points == null || run.points.Length == 0)
            {
                return null;
            }

            string runLabel = string.IsNullOrWhiteSpace(run.sourceLabel) ? "run" : run.sourceLabel;
            int pointCount = run.points.Length;
            Vector3[] positions = new Vector3[pointCount];
            float[] fitnessValues = new float[pointCount];
            float[][] factorScoresPerStep = new float[pointCount][];
            GameObject[] jointObjects = new GameObject[pointCount];
            Vector3[] jointBaseScales = new Vector3[pointCount];
            Material[] jointBaseMaterials = new Material[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                positions[i] = TransformPoint(run.points[i], layout);
                fitnessValues[i] = run.points[i].fitness;
                factorScoresPerStep[i] = run.points[i].factorScores ?? System.Array.Empty<float>();
                IncludeInBounds(positions[i]);
            }

            GameObject pathRoot = new GameObject($"MdsFitnessPath_{runLabel}");
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
                jointObjects[i] = joint;
                jointBaseScales[i] = joint.transform.localScale;
                jointBaseMaterials[i] = material;
            }

            int totalAccepted = run.acceptedCount > 0
                ? Mathf.Min(run.acceptedCount, pointCount)
                : pointCount;
            return new GenomeMdsFitnessPathRunState
            {
                RunLabel = runLabel,
                PathRoot = pathRoot,
                TotalAcceptedGenomes = totalAccepted,
                WorldPositions = positions,
                FitnessValues = fitnessValues,
                JointObjects = jointObjects,
                JointBaseScales = jointBaseScales,
                JointBaseMaterials = jointBaseMaterials,
                FactorScoresPerStep = factorScoresPerStep,
            };
        }

        private void ApplyBatchFactorMetadata(GenomePathBatchData batch)
        {
            if (batch == null)
            {
                GeneCount = 0;
                ContextCount = 0;
                ContextTargets = null;
                return;
            }

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

            var parsed = new System.Collections.Generic.List<GenomePathContextData>();
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

        private LayoutParams ComputeLayout(GenomePathBatchData batch)
        {
            float minMdsX = float.MaxValue;
            float maxMdsX = float.MinValue;
            float minMdsY = float.MaxValue;
            float maxMdsY = float.MinValue;
            bool hasPoints = false;
            int runLimit = batch?.runs != null
                ? Mathf.Min(batch.runs.Length, Mathf.Max(maxRunCount, 1))
                : 0;

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
            float mdsComponent1 = (point.x - layout.mdsCenter.x) * layout.mdsScale;
            float mdsComponent2 = (point.y - layout.mdsCenter.y) * layout.mdsScale;
            float fitnessHeight = point.fitness * layout.fitnessScale;
            return transform.position + new Vector3(mdsComponent1, fitnessHeight, mdsComponent2);
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
            orbitFocus = focusObject.transform;
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
            segment.transform.SetParent(parent, false);
            segment.transform.position = (start + end) * 0.5f;
            segment.transform.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            segment.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            spawnedObjects.Add(segment);

            TrajectoryRenderUtility.ConfigureLitRenderer(segment.GetComponent<Renderer>(), material);
            DestroyCollider(segment);
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
