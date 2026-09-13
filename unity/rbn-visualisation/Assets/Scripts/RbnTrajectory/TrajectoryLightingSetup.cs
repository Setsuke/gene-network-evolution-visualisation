using UnityEngine;

namespace RbnVisualisation.Trajectory
{
    /// <summary>
    /// Configures balanced scene lighting for shaded 3D views (key + opposite fill + rim).
    /// </summary>
    public static class TrajectoryLightingSetup
    {
        public static void Apply()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.33f, 0.35f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.23f, 0.25f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.13f);

            Light keyLight = FindOrCreateKeyLight();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.98f, 0.95f);
            keyLight.intensity = 1.35f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.65f;
            keyLight.transform.rotation = Quaternion.Euler(62f, 24f, 0f);

            // Opposite-side fill so orbit angles still read node/edge colours clearly.
            Light fillLight = FindOrCreateFillLight();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.86f, 0.9f, 0.98f);
            fillLight.intensity = 0.78f;
            fillLight.shadows = LightShadows.None;
            fillLight.transform.rotation = Quaternion.Euler(58f, 204f, 0f);

            Light rimLight = FindOrCreateRimLight();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.92f, 0.94f, 1f);
            rimLight.intensity = 0.42f;
            rimLight.shadows = LightShadows.None;
            rimLight.transform.rotation = Quaternion.Euler(-18f, 204f, 0f);
        }

        private static Light FindOrCreateKeyLight()
        {
            GameObject existing = GameObject.Find("TrajectoryKeyLight");
            if (existing != null && existing.TryGetComponent(out Light light))
            {
                return light;
            }

            GameObject legacy = GameObject.Find("Directional Light");
            if (legacy != null && legacy.TryGetComponent(out light))
            {
                legacy.name = "TrajectoryKeyLight";
                return light;
            }

            GameObject lightObject = new GameObject("TrajectoryKeyLight");
            return lightObject.AddComponent<Light>();
        }

        private static Light FindOrCreateFillLight()
        {
            GameObject existing = GameObject.Find("TrajectoryFillLight");
            if (existing != null && existing.TryGetComponent(out Light light))
            {
                return light;
            }

            GameObject lightObject = new GameObject("TrajectoryFillLight");
            return lightObject.AddComponent<Light>();
        }

        private static Light FindOrCreateRimLight()
        {
            GameObject existing = GameObject.Find("TrajectoryRimLight");
            if (existing != null && existing.TryGetComponent(out Light light))
            {
                return light;
            }

            GameObject lightObject = new GameObject("TrajectoryRimLight");
            return lightObject.AddComponent<Light>();
        }
    }
}
