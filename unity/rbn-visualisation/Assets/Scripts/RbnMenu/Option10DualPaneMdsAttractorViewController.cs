using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.Menu
{
    public class Option10DualPaneMdsAttractorViewController : DualPaneMdsAttractorViewController
    {
        private const string PlateauRootName = "PlateauDriftPathRoot";
        private const string BatchGenomeMdsJsonFileName = "unity_batch_genome_mds_path.json";

        [SerializeField] private PlateauDriftPathVisualiser plateauDriftPathVisualiser;

        public PlateauDriftPathVisualiser PlateauDriftVisualiser => ResolvePlateauDriftVisualiser();

        public int RunCount
        {
            get
            {
                int genomeRuns = PlateauDriftVisualiser != null ? PlateauDriftVisualiser.RunCount : 0;
                int attractorRuns = AttractorVisualiser != null ? AttractorVisualiser.RunCount : 0;
                if (genomeRuns <= 0)
                {
                    return attractorRuns;
                }

                if (attractorRuns <= 0)
                {
                    return genomeRuns;
                }

                return Mathf.Min(genomeRuns, attractorRuns);
            }
        }

        public int ActiveRunIndex =>
            AttractorVisualiser != null
                ? AttractorVisualiser.ActiveRunIndex
                : PlateauDriftVisualiser != null
                    ? PlateauDriftVisualiser.ActiveRunIndex
                    : 0;

        public void SetActiveRun(int runIndex)
        {
            runIndex = Mathf.Clamp(runIndex, 0, Mathf.Max(RunCount - 1, 0));
            PlateauDriftPathVisualiser plateauVisualiser = ResolvePlateauDriftVisualiser();
            BooleanAttractorLandscapeVisualiser attractorVisualiser = AttractorVisualiser;

            plateauVisualiser?.SetActiveRun(runIndex);
            attractorVisualiser?.SetActiveRun(runIndex);
            RefreshPlateauEpisodesForActiveRun();
        }

        public void RefreshPlateauEpisodesForActiveRun()
        {
            PlateauDriftPathVisualiser plateauVisualiser = ResolvePlateauDriftVisualiser();
            BooleanAttractorLandscapeVisualiser attractorVisualiser = AttractorVisualiser;

            if (plateauVisualiser == null
                || !plateauVisualiser.HasActiveRun
                || attractorVisualiser == null
                || !attractorVisualiser.HasActiveRun)
            {
                plateauVisualiser?.SetPlateauEpisodes(System.Array.Empty<PlateauEpisode>());
                return;
            }

            PlateauEpisode[] episodes = PlateauEpisodeAnalyzer.AnalyzeRun(
                plateauVisualiser.LoadedGenomeRun,
                attractorVisualiser.ActiveRun);
            plateauVisualiser.SetAttractorRun(attractorVisualiser.ActiveRun);
            plateauVisualiser.SetPlateauEpisodes(episodes);
        }

        public new void RebuildVisualisations()
        {
            EnsureReady(Camera.main);
            TrajectoryLightingSetup.Apply();

            PlateauDriftPathVisualiser plateauVisualiser = ResolvePlateauDriftVisualiser();
            BooleanAttractorLandscapeVisualiser attractorVisualiser = AttractorVisualiser;

            plateauVisualiser?.RebuildVisualisation();
            attractorVisualiser?.RebuildVisualisation();
            EnsurePlateauDriftMatchesAttractorRunCount(plateauVisualiser, attractorVisualiser);

            int runIndex = Mathf.Clamp(ActiveRunIndex, 0, Mathf.Max(RunCount - 1, 0));
            SetActiveRun(runIndex);

            ApplyRenderIsolationForOption10();
            ConfigureInitialCameraFocusForOption10();
        }

        public new void ConfigureCameras(Camera mainCamera)
        {
            EnsureReady(mainCamera);
            ApplyRenderIsolationForOption10();

            Camera leftCamera = LeftSceneCamera != null ? LeftSceneCamera.GetComponent<Camera>() : null;
            Camera rightCamera = RightSceneCamera != null ? RightSceneCamera.GetComponent<Camera>() : null;

            if (leftCamera != null)
            {
                leftCamera.rect = DualPaneLayout.LeftPaneViewport;
                leftCamera.clearFlags = CameraClearFlags.SolidColor;
                leftCamera.backgroundColor = mainCamera != null
                    ? mainCamera.backgroundColor
                    : new Color(0.08f, 0.09f, 0.12f, 1f);
                leftCamera.enabled = false;
            }

            if (rightCamera != null)
            {
                rightCamera.rect = DualPaneLayout.RightPaneViewport;
                rightCamera.clearFlags = CameraClearFlags.SolidColor;
                rightCamera.backgroundColor = mainCamera != null
                    ? mainCamera.backgroundColor
                    : new Color(0.08f, 0.09f, 0.12f, 1f);
                rightCamera.enabled = false;
            }

            LeftSceneCamera?.SetRestrictOrbitToViewport(true);
            RightSceneCamera?.SetRestrictOrbitToViewport(true);
            ConfigureInitialCameraFocusForOption10();
        }

        public new void ApplyRenderIsolation()
        {
            ApplyRenderIsolationForOption10();
        }

        private void ApplyRenderIsolationForOption10()
        {
            PlateauDriftPathVisualiser plateauVisualiser = ResolvePlateauDriftVisualiser();
            BooleanAttractorLandscapeVisualiser attractorVisualiser = AttractorVisualiser;
            Camera leftCamera = LeftSceneCamera != null ? LeftSceneCamera.GetComponent<Camera>() : null;
            Camera rightCamera = RightSceneCamera != null ? RightSceneCamera.GetComponent<Camera>() : null;

            DualPaneRenderIsolation.Apply(
                plateauVisualiser != null ? plateauVisualiser.transform : null,
                attractorVisualiser != null ? attractorVisualiser.transform : null,
                leftCamera,
                rightCamera);
        }

        private void ConfigureInitialCameraFocusForOption10()
        {
            PlateauDriftPathVisualiser plateauVisualiser = ResolvePlateauDriftVisualiser();
            if (LeftSceneCamera != null && plateauVisualiser != null && plateauVisualiser.HasActiveRun)
            {
                LeftSceneCamera.SetFocus(plateauVisualiser.OrbitFocus);
                LeftSceneCamera.SetLookAtHeight(0f);
                LeftSceneCamera.SetVerticalOrbitLimited(true);
                LeftSceneCamera.SetPitchDegrees(22f);
                LeftSceneCamera.SetDistanceLimits(
                    plateauVisualiser.MinOrbitDistance,
                    GetMaxCameraDistance(plateauVisualiser.ContentExtent));
                LeftSceneCamera.SetDistance(GetDefaultCameraDistance(plateauVisualiser.ContentExtent));
                LeftSceneCamera.PlaceCameraFixed();
            }

            BooleanAttractorLandscapeVisualiser attractorVisualiser = AttractorVisualiser;
            if (RightSceneCamera != null && attractorVisualiser != null)
            {
                RightSceneCamera.SetFocus(attractorVisualiser.OrbitFocus);
                RightSceneCamera.SetLookAtHeight(0f);
                RightSceneCamera.SetVerticalOrbitLimited(false);
                RightSceneCamera.SetDistanceLimits(
                    attractorVisualiser.MinOrbitDistance,
                    GetMaxCameraDistance(attractorVisualiser.ContentExtent));
                RightSceneCamera.SetDistance(GetDefaultCameraDistance(attractorVisualiser.ContentExtent));
                RightSceneCamera.PlaceCameraFixed();
            }
        }

        private PlateauDriftPathVisualiser ResolvePlateauDriftVisualiser()
        {
            if (plateauDriftPathVisualiser != null)
            {
                return plateauDriftPathVisualiser;
            }

            plateauDriftPathVisualiser = GetComponentInChildren<PlateauDriftPathVisualiser>(true);
            return plateauDriftPathVisualiser;
        }

        private void Awake()
        {
            DisableLegacyLeftPaneVisualiser();
        }

        private void OnValidate()
        {
            DisableLegacyLeftPaneVisualiser();
        }

        private void DisableLegacyLeftPaneVisualiser()
        {
            Transform leftAnchor = transform.Find("LeftPaneAnchor");
            if (leftAnchor == null)
            {
                return;
            }

            Transform legacyRoot = leftAnchor.Find("DualPaneMdsPathRoot");
            if (legacyRoot != null)
            {
                GenomeMdsPathVisualiser legacyVisualiser = legacyRoot.GetComponent<GenomeMdsPathVisualiser>();
                if (legacyVisualiser != null)
                {
                    legacyVisualiser.enabled = false;
                }
            }

            Transform plateauRoot = leftAnchor.Find(PlateauRootName);
            if (plateauRoot == null)
            {
                GameObject rootObject = new GameObject(PlateauRootName);
                rootObject.transform.SetParent(leftAnchor, false);
                plateauRoot = rootObject.transform;
            }

            plateauDriftPathVisualiser = plateauRoot.GetComponent<PlateauDriftPathVisualiser>()
                ?? plateauRoot.gameObject.AddComponent<PlateauDriftPathVisualiser>();
            if (plateauDriftPathVisualiser.JsonFileName != BatchGenomeMdsJsonFileName)
            {
                plateauDriftPathVisualiser.SetJsonFileName(BatchGenomeMdsJsonFileName);
            }
        }

        private static void EnsurePlateauDriftMatchesAttractorRunCount(
            PlateauDriftPathVisualiser plateauVisualiser,
            BooleanAttractorLandscapeVisualiser attractorVisualiser)
        {
            if (plateauVisualiser == null || attractorVisualiser == null)
            {
                return;
            }

            int attractorRuns = attractorVisualiser.RunCount;
            int genomeRuns = plateauVisualiser.RunCount;
            if (attractorRuns <= 1 || genomeRuns >= attractorRuns)
            {
                return;
            }

            if (plateauVisualiser.JsonFileName == BatchGenomeMdsJsonFileName)
            {
                return;
            }

            plateauVisualiser.SetJsonFileName(BatchGenomeMdsJsonFileName);
            plateauVisualiser.RebuildVisualisation();
        }
    }
}
