using UnityEngine;

namespace RbnVisualisation.Trajectory
{
    public class BillboardLabel : MonoBehaviour
    {
        private Camera mainCamera;

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }
            }

            transform.rotation = Quaternion.LookRotation(
                transform.position - mainCamera.transform.position,
                Vector3.up);
        }
    }
}
