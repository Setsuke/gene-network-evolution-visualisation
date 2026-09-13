using System.Collections.Generic;
using System.IO;
using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    /// <summary>
    /// Option 10 left pane: local MDS path for the active plateau segment plus exit step.
    /// </summary>
    public class PlateauDriftPathVisualiser : MonoBehaviour
    {
        private static readonly Color UnknownSegmentColor = new Color(0.08f, 0.45f, 1f, 1f);

        [Header("Data")]
        [SerializeField] private string jsonFileName = "unity_batch_genome_mds_path.json";
        [SerializeField] private TextAsset jsonOverride;

        [Header("Layout")]
        [SerializeField] private float targetExtent = 12f;
        [SerializeField] private float tubeDiameter = 0.1f;
        [SerializeField] private float jointDiameter = 0.14f;
        [SerializeField] private float endpointJointDiameter = 0.22f;

        [Header("Appearance")]
        [SerializeField] private Color focusedJointColor = Color.cyan;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private readonly Dictionary<string, Material> changeTypeMaterials = new Dictionary<string, Material>();
        private Material focusedJointMaterial;

        private GenomePathBatchData loadedBatch;
        private GenomePathRunData activeGenomeRun;
        private BooleanAttractorLandscapeRunData activeAttractorRun;
        private int activeRunIndex;
        private PlateauEpisode[] plateauEpisodes = System.Array.Empty<PlateauEpisode>();
        private PlateauEpisode activeEpisode;
        private int highlightedStepIndex = -1;
        private Transform orbitFocus;
        private Vector3 contentCenter;
        private float contentExtent;
        private Vector3 boundsMin;
        private Vector3 boundsMax;
        private bool hasContentBounds;

        private struct SegmentRunState
        {
            public int[] AbsoluteStepIndices;
            public Vector3[] WorldPositions;
            public GameObject[] JointObjects;
            public Vector3[] JointBaseScales;
            public Material[] JointBaseMaterials;
        }

        private SegmentRunState segmentState;
        private NormalizationParams normalization;

        private struct NormalizationParams
        {
            public Vector3 center;
            public float scale;
        }

        public Transform OrbitFocus
        {
            get
            {
                EnsureOrbitFocus();
                return orbitFocus;
            }
        }

        public float ContentExtent => contentExtent;
        public float MinOrbitDistance => Mathf.Max(jointDiameter * 4f, 0.5f);
        public bool HasActiveRun => activeGenomeRun?.points != null && activeGenomeRun.points.Length > 0;
        public int StepCount => activeEpisode?.SegmentStepCount ?? 0;
        public PlateauEpisode[] PlateauEpisodes => plateauEpisodes;
        public PlateauEpisode ActiveEpisode => activeEpisode;
        public int HighlightedAbsoluteStepIndex => highlightedStepIndex;
        public GenomePathRunData LoadedGenomeRun => activeGenomeRun;
        public GenomePathBatchData LoadedBatch => loadedBatch;
        public int RunCount => loadedBatch?.runs?.Length ?? 0;
        public int ActiveRunIndex => activeRunIndex;
        public string ActiveRunLabel =>
            activeGenomeRun != null && !string.IsNullOrWhiteSpace(activeGenomeRun.sourceLabel)
                ? activeGenomeRun.sourceLabel
                : $"run {activeRunIndex + 1}";
        public string JsonFileName => jsonFileName;

        public void SetJsonFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName == jsonFileName)
            {
                return;
            }

            jsonFileName = fileName;
            loadedBatch = null;
            activeGenomeRun = null;
        }

        public bool TryGetAbsoluteStepIndex(int relativeStepIndex, out int absoluteStepIndex)
        {
            absoluteStepIndex = -1;
            if (activeEpisode == null
                || relativeStepIndex < 0
                || relativeStepIndex >= activeEpisode.SegmentStepCount)
            {
                return false;
            }

            absoluteStepIndex = activeEpisode.StartStep + relativeStepIndex;
            return true;
        }

        public bool TryGetRelativeStepIndex(int absoluteStepIndex, out int relativeStepIndex)
        {
            relativeStepIndex = -1;
            if (activeEpisode == null
                || absoluteStepIndex < activeEpisode.StartStep
                || absoluteStepIndex > activeEpisode.ExitStep)
            {
                return false;
            }

            relativeStepIndex = absoluteStepIndex - activeEpisode.StartStep;
            return true;
        }

        public void RebuildVisualisation()
        {
            TrajectoryLightingSetup.Apply();

            string json = LoadJsonText();
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError(
                    $"PlateauDriftPathVisualiser: could not load '{jsonFileName}'. " +
                    $"Expected at StreamingAssets/{jsonFileName}.");
                return;
            }

            loadedBatch = JsonUtility.FromJson<GenomePathBatchData>(json);
            if (loadedBatch?.runs == null || loadedBatch.runs.Length == 0)
            {
                Debug.LogError("PlateauDriftPathVisualiser: JSON parsed but contains no runs.");
                return;
            }

            activeRunIndex = Mathf.Clamp(activeRunIndex, 0, loadedBatch.runs.Length - 1);
            activeGenomeRun = loadedBatch.runs[activeRunIndex];
            normalization = ComputeNormalization(loadedBatch);
            plateauEpisodes = System.Array.Empty<PlateauEpisode>();
            activeEpisode = null;
            highlightedStepIndex = -1;
            ClearSpawnedObjects();
        }

        public void SetActiveRun(int runIndex)
        {
            if (loadedBatch?.runs == null || loadedBatch.runs.Length == 0)
            {
                return;
            }

            activeRunIndex = Mathf.Clamp(runIndex, 0, loadedBatch.runs.Length - 1);
            activeGenomeRun = loadedBatch.runs[activeRunIndex];
            plateauEpisodes = System.Array.Empty<PlateauEpisode>();
            activeEpisode = null;
            highlightedStepIndex = -1;
            ClearSpawnedObjects();
        }

        public void SetAttractorRun(BooleanAttractorLandscapeRunData attractorRun)
        {
            activeAttractorRun = attractorRun;
        }

        public void SetPlateauEpisodes(PlateauEpisode[] episodes)
        {
            plateauEpisodes = episodes ?? System.Array.Empty<PlateauEpisode>();
            if (plateauEpisodes.Length == 0)
            {
                ClearSpawnedObjects();
                activeEpisode = null;
                return;
            }

            ShowEpisode(0);
        }

        public void ShowEpisode(int episodeIndex)
        {
            if (plateauEpisodes == null || plateauEpisodes.Length == 0)
            {
                return;
            }

            episodeIndex = Mathf.Clamp(episodeIndex, 0, plateauEpisodes.Length - 1);
            activeEpisode = plateauEpisodes[episodeIndex];
            BuildSegmentVisualisation(activeEpisode);
            HighlightRelativeStep(0);
        }

        public void HighlightAbsoluteStep(int absoluteStepIndex, float scaleMultiplier = 1.35f)
        {
            if (!TryGetRelativeStepIndex(absoluteStepIndex, out int relativeStepIndex))
            {
                ClearStepHighlight();
                return;
            }

            HighlightRelativeStep(relativeStepIndex, scaleMultiplier);
        }

        public void HighlightRelativeStep(int relativeStepIndex, float scaleMultiplier = 1.35f)
        {
            ClearStepHighlight();
            if (relativeStepIndex < 0 || relativeStepIndex >= segmentState.JointObjects.Length)
            {
                return;
            }

            highlightedStepIndex = activeEpisode.StartStep + relativeStepIndex;
            GameObject joint = segmentState.JointObjects[relativeStepIndex];
            if (joint == null)
            {
                return;
            }

            joint.transform.localScale = segmentState.JointBaseScales[relativeStepIndex] * scaleMultiplier;
            if (joint.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = GetFocusedJointMaterial();
            }

            OrbitFocus.position = segmentState.WorldPositions[relativeStepIndex];
        }

        public void ClearStepHighlight()
        {
            if (highlightedStepIndex < 0)
            {
                return;
            }

            if (TryGetRelativeStepIndex(highlightedStepIndex, out int relativeStepIndex)
                && relativeStepIndex < segmentState.JointObjects.Length)
            {
                GameObject joint = segmentState.JointObjects[relativeStepIndex];
                if (joint != null)
                {
                    joint.transform.localScale = segmentState.JointBaseScales[relativeStepIndex];
                    if (joint.TryGetComponent(out Renderer renderer)
                        && segmentState.JointBaseMaterials[relativeStepIndex] != null)
                    {
                        renderer.sharedMaterial = segmentState.JointBaseMaterials[relativeStepIndex];
                    }
                }
            }

            highlightedStepIndex = -1;
        }

        private void BuildSegmentVisualisation(PlateauEpisode episode)
        {
            ClearSpawnedObjects();
            ResetContentBounds();

            if (activeGenomeRun?.points == null || episode == null)
            {
                return;
            }

            int segmentLength = episode.SegmentStepCount;
            Vector3[] positions = new Vector3[segmentLength];
            segmentState.AbsoluteStepIndices = new int[segmentLength];
            segmentState.WorldPositions = new Vector3[segmentLength];
            segmentState.JointObjects = new GameObject[segmentLength];
            segmentState.JointBaseScales = new Vector3[segmentLength];
            segmentState.JointBaseMaterials = new Material[segmentLength];

            for (int offset = 0; offset < segmentLength; offset++)
            {
                int absoluteStep = episode.StartStep + offset;
                segmentState.AbsoluteStepIndices[offset] = absoluteStep;
                positions[offset] = TransformPoint(activeGenomeRun.points[absoluteStep]);
                segmentState.WorldPositions[offset] = positions[offset];
                IncludeInBounds(positions[offset]);
            }

            GameObject pathRoot = new GameObject($"PlateauSegment_{episode.EpisodeIndex}");
            pathRoot.layer = gameObject.layer;
            pathRoot.transform.SetParent(transform, false);
            spawnedObjects.Add(pathRoot);

            for (int offset = 0; offset < segmentLength - 1; offset++)
            {
                int previousAbsoluteStep = episode.StartStep + offset;
                int destinationAbsoluteStep = previousAbsoluteStep + 1;
                GenomePathPointData destinationPoint = activeGenomeRun.points[destinationAbsoluteStep];
                Color segmentColor = GenomePathChangeTypeStyles.GetColor(
                    destinationPoint.changeType,
                    UnknownSegmentColor,
                    activeAttractorRun,
                    previousAbsoluteStep,
                    destinationAbsoluteStep);
                CreateTubeSegment(
                    pathRoot.transform,
                    positions[offset],
                    positions[offset + 1],
                    tubeDiameter,
                    GetMaterialForColor(segmentColor));
            }

            for (int offset = 0; offset < segmentLength; offset++)
            {
                int absoluteStep = episode.StartStep + offset;
                bool isStart = offset == 0;
                bool isExit = absoluteStep == episode.ExitStep;
                float diameter = isStart || isExit ? endpointJointDiameter : jointDiameter;
                GenomePathPointData point = activeGenomeRun.points[absoluteStep];
                Color jointColor = Color.white;
                if (!isStart)
                {
                    jointColor = GenomePathChangeTypeStyles.GetColor(
                        point.changeType,
                        UnknownSegmentColor,
                        activeAttractorRun,
                        absoluteStep - 1,
                        absoluteStep);
                }
                Material material = GetMaterialForColor(jointColor);
                string jointName = isStart ? "PlateauStart" : isExit ? "PlateauExit" : "PlateauDrift";
                GameObject joint = CreateJointSphere(
                    pathRoot.transform,
                    positions[offset],
                    diameter,
                    material,
                    jointName);
                segmentState.JointObjects[offset] = joint;
                segmentState.JointBaseScales[offset] = joint.transform.localScale;
                if (joint.TryGetComponent(out Renderer renderer))
                {
                    segmentState.JointBaseMaterials[offset] = renderer.sharedMaterial;
                }
            }

            FinalizeContentBounds();
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

        private Vector3 TransformPoint(GenomePathPointData point)
        {
            Vector3 raw = new Vector3(point.x, point.y, point.z);
            return (raw - normalization.center) * normalization.scale + transform.position;
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
                    Vector3 raw = new Vector3(points[pointIndex].x, points[pointIndex].y, points[pointIndex].z);
                    min = Vector3.Min(min, raw);
                    max = Vector3.Max(max, raw);
                    hasPoints = true;
                }
            }

            if (!hasPoints)
            {
                return new NormalizationParams { center = Vector3.zero, scale = 1f };
            }

            Vector3 extent = max - min;
            float maxAxis = Mathf.Max(extent.x, extent.y, extent.z, 0.001f);
            return new NormalizationParams
            {
                center = (min + max) * 0.5f,
                scale = targetExtent / maxAxis,
            };
        }

        private Material GetMaterialForColor(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color);
            if (!changeTypeMaterials.TryGetValue(key, out Material material))
            {
                material = TrajectoryRenderUtility.CreateLitMaterial(color, smoothness: 0.35f);
                changeTypeMaterials[key] = material;
            }

            return material;
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

        private GameObject CreateJointSphere(
            Transform parent,
            Vector3 position,
            float diameter,
            Material material,
            string objectName)
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
            for (int index = spawnedObjects.Count - 1; index >= 0; index--)
            {
                if (spawnedObjects[index] != null)
                {
                    Destroy(spawnedObjects[index]);
                }
            }

            spawnedObjects.Clear();
            segmentState = default;
        }
    }
}
