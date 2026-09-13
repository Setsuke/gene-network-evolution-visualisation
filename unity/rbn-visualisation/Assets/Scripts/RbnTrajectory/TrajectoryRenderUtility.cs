using UnityEngine;
using UnityEngine.Rendering;

namespace RbnVisualisation.Trajectory
{
    internal static class TrajectoryRenderUtility
    {
        public static Material CreateLitMaterial(Color color, float smoothness = 0.42f, float metallic = 0.04f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");

            Material material = new Material(shader);
            ApplyBaseColor(material, color);

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 1f);
            }

            return material;
        }

        public static Material CreateLineMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            ApplyBaseColor(material, color);

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)CullMode.Off);
            }

            return material;
        }

        public static Material CreateOverlayLineMaterial(Color color)
        {
            Material material = CreateLineMaterial(color);
            material.renderQueue = (int)RenderQueue.Overlay;
            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_ZTest"))
            {
                material.SetInt("_ZTest", (int)CompareFunction.Always);
            }

            return material;
        }

        public static Material CreateMarkerMaterial(Color color)
        {
            return CreateLitMaterial(color, smoothness: 0.48f);
        }

        public static void ConfigureLitRenderer(Renderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static void ApplyBaseColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.color = color;
        }
    }
}
