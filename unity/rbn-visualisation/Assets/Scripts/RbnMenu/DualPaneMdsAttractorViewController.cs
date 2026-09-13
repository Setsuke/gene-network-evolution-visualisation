using RbnVisualisation.AttractorLandscape;
using RbnVisualisation.GenomePath;
using RbnVisualisation.Trajectory;
using UnityEngine;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Side-by-side Option 1 (MDS path) and Option 4 (Boolean attractor landscape) with linked steps.
    /// </summary>
    public class DualPaneMdsAttractorViewController : MonoBehaviour
    {
        private const string LeftAnchorName = "LeftPaneAnchor";
        private const string RightAnchorName = "RightPaneAnchor";
        private const string MdsRootName = "DualPaneMdsPathRoot";
        private const string AttractorRootName = "DualPaneAttractorRoot";
        private const string LeftCameraName = "DualPaneLeftCamera";
        private const string RightCameraName = "DualPaneRightCamera";

        [SerializeField] private Transform leftPaneAnchor;
        [SerializeField] private Transform rightPaneAnchor;
        [SerializeField] private GenomeMdsPathVisualiser mdsPathVisualiser;
        [SerializeField] private BooleanAttractorLandscapeVisualiser attractorVisualiser;
        [SerializeField] private Camera leftCamera;
        [SerializeField] private Camera rightCamera;
        [SerializeField] private TrajectorySceneCamera leftSceneCamera;
        [SerializeField] private TrajectorySceneCamera rightSceneCamera;

        public GenomeMdsPathVisualiser MdsPathVisualiser => mdsPathVisualiser;
        public BooleanAttractorLandscapeVisualiser AttractorVisualiser => attractorVisualiser;
        public TrajectorySceneCamera LeftSceneCamera => leftSceneCamera;
        public TrajectorySceneCamera RightSceneCamera => rightSceneCamera;

        private const float DefaultDistanceExtentMultiplier = 1.85f;
        private const float MaxDistanceExtentMultiplier = 2.4f;

        public static float GetDefaultCameraDistance(float contentExtent) =>
            Mathf.Max(contentExtent * DefaultDistanceExtentMultiplier, 14f);

        public static float GetMaxCameraDistance(float contentExtent) =>
            Mathf.Max(contentExtent * MaxDistanceExtentMultiplier, 18f);

        public void EnsureReady(Camera mainCamera)
        {
            EnsureHierarchy(mainCamera);
            EnsureReferences();
            ValidateVisualisers();
        }

        public void ConfigureCameras(Camera mainCamera)
        {
            EnsureReady(mainCamera);
            ApplyRenderIsolation();

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

            if (leftSceneCamera != null)
            {
                leftSceneCamera.SetRestrictOrbitToViewport(true);
            }

            if (rightSceneCamera != null)
            {
                rightSceneCamera.SetRestrictOrbitToViewport(true);
            }

            ConfigureInitialCameraFocus();
        }

        public void SetViewActive(bool active, Camera mainCamera)
        {
            gameObject.SetActive(active);

            if (leftCamera != null)
            {
                leftCamera.enabled = active;
                leftCamera.depth = 10;
            }

            if (rightCamera != null)
            {
                rightCamera.enabled = active;
                rightCamera.depth = 11;
            }

            if (mainCamera != null)
            {
                if (active)
                {
                    mainCamera.enabled = true;
                    mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.cullingMask = 0;
                    mainCamera.depth = -1;
                }
                else
                {
                    mainCamera.enabled = true;
                    mainCamera.cullingMask = -1;
                }
            }

            if (leftPaneAnchor != null)
            {
                leftPaneAnchor.gameObject.SetActive(active);
            }

            if (rightPaneAnchor != null)
            {
                rightPaneAnchor.gameObject.SetActive(active);
            }
        }

        public void RebuildVisualisations()
        {
            EnsureReferences();
            TrajectoryLightingSetup.Apply();

            if (mdsPathVisualiser != null)
            {
                mdsPathVisualiser.RebuildVisualisation();
            }

            if (attractorVisualiser != null)
            {
                attractorVisualiser.RebuildVisualisation();
            }

            ApplyRenderIsolation();
            ConfigureInitialCameraFocus();
        }

        public void ApplyRenderIsolation()
        {
            Transform leftContentRoot = mdsPathVisualiser != null
                ? mdsPathVisualiser.transform
                : leftPaneAnchor;
            Transform rightContentRoot = attractorVisualiser != null
                ? attractorVisualiser.transform
                : rightPaneAnchor;

            DualPaneRenderIsolation.Apply(
                leftContentRoot,
                rightContentRoot,
                leftCamera,
                rightCamera);
        }

        private void ConfigureInitialCameraFocus()
        {
            if (leftSceneCamera != null && mdsPathVisualiser != null)
            {
                leftSceneCamera.SetFocus(mdsPathVisualiser.OrbitFocus);
                leftSceneCamera.SetLookAtHeight(0f);
                leftSceneCamera.SetVerticalOrbitLimited(true);
                leftSceneCamera.SetPitchDegrees(22f);
                leftSceneCamera.SetDistanceLimits(
                    mdsPathVisualiser.MinOrbitDistance,
                    GetMaxCameraDistance(mdsPathVisualiser.ContentExtent));
                leftSceneCamera.SetDistance(GetDefaultCameraDistance(mdsPathVisualiser.ContentExtent));
                leftSceneCamera.PlaceCameraFixed();
            }

            if (rightSceneCamera != null && attractorVisualiser != null)
            {
                rightSceneCamera.SetFocus(attractorVisualiser.OrbitFocus);
                rightSceneCamera.SetLookAtHeight(0f);
                rightSceneCamera.SetVerticalOrbitLimited(false);
                rightSceneCamera.SetDistanceLimits(
                    attractorVisualiser.MinOrbitDistance,
                    GetMaxCameraDistance(attractorVisualiser.ContentExtent));
                rightSceneCamera.SetDistance(GetDefaultCameraDistance(attractorVisualiser.ContentExtent));
                rightSceneCamera.PlaceCameraFixed();
            }
        }

        private void EnsureHierarchy(Camera mainCamera)
        {
            if (leftPaneAnchor == null)
            {
                leftPaneAnchor = transform.Find(LeftAnchorName);
            }

            if (rightPaneAnchor == null)
            {
                rightPaneAnchor = transform.Find(RightAnchorName);
            }

            if (leftPaneAnchor == null)
            {
                leftPaneAnchor = CreateChildAnchor(LeftAnchorName).transform;
            }

            if (rightPaneAnchor == null)
            {
                rightPaneAnchor = CreateChildAnchor(RightAnchorName).transform;
            }

            leftPaneAnchor.localPosition = Vector3.zero;
            rightPaneAnchor.localPosition = Vector3.zero;

            mdsPathVisualiser = ResolvePaneVisualiser(
                mdsPathVisualiser,
                leftPaneAnchor,
                MdsRootName,
                static root => root.GetComponent<GenomeMdsPathVisualiser>()
                    ?? root.gameObject.AddComponent<GenomeMdsPathVisualiser>());

            attractorVisualiser = ResolvePaneVisualiser(
                attractorVisualiser,
                rightPaneAnchor,
                AttractorRootName,
                static root => root.GetComponent<BooleanAttractorLandscapeVisualiser>()
                    ?? root.gameObject.AddComponent<BooleanAttractorLandscapeVisualiser>());

            if (leftCamera == null || rightCamera == null)
            {
                Camera template = mainCamera != null ? mainCamera : Camera.main;
                if (leftCamera == null)
                {
                    leftCamera = EnsurePaneCamera(LeftCameraName, template, 10, out leftSceneCamera);
                }

                if (rightCamera == null)
                {
                    rightCamera = EnsurePaneCamera(RightCameraName, template, 11, out rightSceneCamera);
                }
            }
        }

        private void EnsureReferences()
        {
            if (leftSceneCamera == null && leftCamera != null)
            {
                leftSceneCamera = leftCamera.GetComponent<TrajectorySceneCamera>()
                    ?? leftCamera.gameObject.AddComponent<TrajectorySceneCamera>();
            }

            if (rightSceneCamera == null && rightCamera != null)
            {
                rightSceneCamera = rightCamera.GetComponent<TrajectorySceneCamera>()
                    ?? rightCamera.gameObject.AddComponent<TrajectorySceneCamera>();
            }
        }

        private static T ResolvePaneVisualiser<T>(
            T currentVisualiser,
            Transform paneAnchor,
            string rootName,
            System.Func<Transform, T> ensureComponent) where T : Component
        {
            if (currentVisualiser != null && IsUnderAnchor(currentVisualiser.transform, paneAnchor))
            {
                return currentVisualiser;
            }

            Transform dedicatedRoot = paneAnchor.Find(rootName);
            if (dedicatedRoot == null)
            {
                GameObject rootObject = new GameObject(rootName);
                rootObject.transform.SetParent(paneAnchor, false);
                dedicatedRoot = rootObject.transform;
            }

            return ensureComponent(dedicatedRoot);
        }

        private static bool IsUnderAnchor(Transform target, Transform anchor)
        {
            return target != null && anchor != null
                && (target == anchor || target.IsChildOf(anchor));
        }

        private void ValidateVisualisers()
        {
            if (mdsPathVisualiser == null || attractorVisualiser == null)
            {
                Debug.LogError("DualPaneMdsAttractorViewController: missing pane visualisers.");
            }
        }

        private GameObject CreateChildAnchor(string anchorName)
        {
            GameObject anchor = new GameObject(anchorName);
            anchor.transform.SetParent(transform, false);
            return anchor;
        }

        private Camera EnsurePaneCamera(
            string cameraName,
            Camera template,
            int depth,
            out TrajectorySceneCamera sceneCamera)
        {
            Transform existing = transform.Find(cameraName);
            GameObject cameraObject;
            if (existing != null)
            {
                cameraObject = existing.gameObject;
            }
            else
            {
                cameraObject = new GameObject(cameraName);
                cameraObject.transform.SetParent(transform, false);
            }

            Camera camera = cameraObject.GetComponent<Camera>() ?? cameraObject.AddComponent<Camera>();
            sceneCamera = cameraObject.GetComponent<TrajectorySceneCamera>()
                ?? cameraObject.AddComponent<TrajectorySceneCamera>();

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = template != null
                ? template.backgroundColor
                : new Color(0.08f, 0.09f, 0.12f, 1f);
            camera.nearClipPlane = template != null ? template.nearClipPlane : 0.1f;
            camera.farClipPlane = template != null ? template.farClipPlane : 500f;
            camera.depth = depth;
            camera.enabled = false;
            return camera;
        }
    }
}
