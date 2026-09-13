using UnityEngine;
using UnityEngine.UI;

namespace RbnVisualisation.Trajectory
{
    public class TrajectoryZoomSlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TrajectorySceneCamera sceneCamera;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }

            if (sceneCamera == null)
            {
                sceneCamera = FindFirstObjectByType<TrajectorySceneCamera>();
            }
        }

        private void Start()
        {
            if (slider == null || sceneCamera == null)
            {
                return;
            }

            slider.onValueChanged.AddListener(OnSliderChanged);
            SyncFromCamera();
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderChanged);
            }
        }

        public void SyncFromCamera()
        {
            if (slider == null || sceneCamera == null)
            {
                return;
            }

            slider.SetValueWithoutNotify(sceneCamera.GetNormalizedZoom());
        }

        public void Configure(Slider newSlider, TrajectorySceneCamera newSceneCamera)
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(OnSliderChanged);
            }

            slider = newSlider;
            sceneCamera = newSceneCamera;

            if (slider != null)
            {
                slider.onValueChanged.AddListener(OnSliderChanged);
                SyncFromCamera();
            }
        }

        private void OnSliderChanged(float value)
        {
            if (sceneCamera == null)
            {
                return;
            }

            sceneCamera.SetNormalizedZoom(value);
        }
    }
}
