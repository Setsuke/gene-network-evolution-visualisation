using UnityEngine;

namespace RbnVisualisation.Menu
{
    /// <summary>
    /// Ensures orphan dual-pane cameras do not paint over single-pane views (Option 4 / 11).
    /// </summary>
    internal static class DualPaneCameraUtility
    {
        public const string LeftCameraName = "DualPaneLeftCamera";
        public const string RightCameraName = "DualPaneRightCamera";

        public static void DisableAllDualPaneCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < cameras.Length; index++)
            {
                Camera camera = cameras[index];
                if (camera == null || !camera.gameObject.scene.IsValid())
                {
                    continue;
                }

                string cameraName = camera.gameObject.name;
                if (cameraName == LeftCameraName || cameraName == RightCameraName)
                {
                    camera.enabled = false;
                }
            }
        }

        public static void PrepareMainCameraForSinglePaneView(Camera mainCamera)
        {
            if (mainCamera == null)
            {
                return;
            }

            mainCamera.enabled = true;
            mainCamera.depth = 0;
            mainCamera.cullingMask = -1;
        }
    }
}
