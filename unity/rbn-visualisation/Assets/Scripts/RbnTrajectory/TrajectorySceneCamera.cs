using UnityEngine;
using UnityEngine.InputSystem;

namespace RbnVisualisation.Trajectory
{
    /// <summary>
    /// Orbit camera around the disk centre. Right-click and drag to look around.
    /// </summary>
    public class TrajectorySceneCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform focus;

        [Header("Orbit")]
        [SerializeField] private float distance = 22f;
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 45f;
        private float defaultMinDistance = 8f;
        private float defaultMaxDistance = 45f;
        [SerializeField] private float lookAtHeight = 2.5f;
        [SerializeField] private float yawDegrees = 25f;
        [SerializeField] private float pitchDegrees = 35f;
        [SerializeField] private float minPitchDegrees = 10f;
        [SerializeField] private float maxPitchDegrees = 80f;
        [SerializeField] private bool limitVerticalOrbit = true;

        [Header("Input")]
        [SerializeField] private float orbitSensitivity = 0.25f;
        [SerializeField] private bool restrictOrbitToViewport;

        private Camera orbitCamera;
        private Rect? orbitViewportOverride;

        private void Awake()
        {
            defaultMinDistance = minDistance;
            defaultMaxDistance = maxDistance;
            orbitCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            ApplyCameraTransform();
        }

        private void Update()
        {
            if (focus == null || Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.rightButton.isPressed)
            {
                return;
            }

            if (!IsPointerOverViewport())
            {
                return;
            }

            Vector2 delta = Mouse.current.delta.ReadValue();
            yawDegrees += delta.x * orbitSensitivity;
            pitchDegrees -= delta.y * orbitSensitivity;
            if (limitVerticalOrbit)
            {
                pitchDegrees = Mathf.Clamp(pitchDegrees, minPitchDegrees, maxPitchDegrees);
            }

            ApplyCameraTransform();
        }

        private void ApplyCameraTransform()
        {
            Transform target = focus != null ? focus : transform;
            Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;

            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

            transform.position = lookTarget + offset;
            transform.LookAt(lookTarget);
        }

        public void PlaceCameraFixed()
        {
            ApplyCameraTransform();
        }

        public void SetFocus(Transform newFocus)
        {
            focus = newFocus;
            ApplyCameraTransform();
        }

        public void SetLookAtHeight(float height)
        {
            lookAtHeight = height;
            ApplyCameraTransform();
        }

        public void SetPitchDegrees(float pitch)
        {
            pitchDegrees = limitVerticalOrbit
                ? Mathf.Clamp(pitch, minPitchDegrees, maxPitchDegrees)
                : pitch;
            ApplyCameraTransform();
        }

        public void SetYawDegrees(float yaw)
        {
            yawDegrees = yaw;
            ApplyCameraTransform();
        }

        public void SetVerticalOrbitLimited(bool limited)
        {
            limitVerticalOrbit = limited;
            if (limited)
            {
                pitchDegrees = Mathf.Clamp(pitchDegrees, minPitchDegrees, maxPitchDegrees);
                ApplyCameraTransform();
            }
        }

        public float GetNormalizedZoom()
        {
            return Mathf.InverseLerp(maxDistance, minDistance, distance);
        }

        public void SetNormalizedZoom(float normalizedZoom)
        {
            distance = Mathf.Lerp(maxDistance, minDistance, Mathf.Clamp01(normalizedZoom));
            ApplyCameraTransform();
        }

        public void SetDistance(float newDistance)
        {
            distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            ApplyCameraTransform();
        }

        public void SetDistanceLimits(float newMinDistance, float newMaxDistance)
        {
            minDistance = Mathf.Max(0.1f, newMinDistance);
            maxDistance = Mathf.Max(minDistance + 0.1f, newMaxDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            ApplyCameraTransform();
        }

        public void ResetDistanceLimits()
        {
            SetDistanceLimits(defaultMinDistance, defaultMaxDistance);
        }

        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;

        public void SetRestrictOrbitToViewport(bool enabled)
        {
            restrictOrbitToViewport = enabled;
        }

        public void SetOrbitViewportOverride(Rect? normalizedViewport)
        {
            orbitViewportOverride = normalizedViewport;
        }

        private bool IsPointerOverViewport()
        {
            if (!restrictOrbitToViewport || orbitCamera == null || Mouse.current == null)
            {
                return true;
            }

            Vector2 pointer = Mouse.current.position.ReadValue();
            float normalizedX = pointer.x / Mathf.Max(Screen.width, 1f);
            float normalizedY = pointer.y / Mathf.Max(Screen.height, 1f);
            Rect viewport = orbitViewportOverride ?? orbitCamera.rect;
            return normalizedX >= viewport.x
                && normalizedX <= viewport.x + viewport.width
                && normalizedY >= viewport.y
                && normalizedY <= viewport.y + viewport.height;
        }
    }
}
