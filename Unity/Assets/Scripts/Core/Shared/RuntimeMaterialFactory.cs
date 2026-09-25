using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>Creates procedural materials from a lit shader resolved once per session.</summary>
    public static class RuntimeMaterialFactory
    {
        private const string UniversalLitShaderName = "Universal Render Pipeline/Lit";
        private const string UniversalUnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string StandardShaderName = "Standard";
        private const string SpritesDefaultShaderName = "Sprites/Default";

        private static Shader litShader;
        private static Shader unlitShader;

        /// <summary>Creates a lit material with base and emission color applied.</summary>
        /// <param name="color">The material color.</param>
        /// <param name="emissionMultiplier">The emission intensity relative to the color.</param>
        /// <returns>A new material owned by the caller.</returns>
        public static Material CreateLit(Color color, float emissionMultiplier = 1f)
        {
            var material = new Material(ResolveLitShader());
            MaterialColorHelper.SetBaseAndEmissionColor(material, color, emissionMultiplier);
            return material;
        }

        /// <summary>Creates an unlit material, suitable for line renderers.</summary>
        /// <param name="color">The material color.</param>
        /// <returns>A new material owned by the caller.</returns>
        public static Material CreateUnlit(Color color)
        {
            var material = new Material(ResolveUnlitShader());
            MaterialColorHelper.SetBaseAndEmissionColor(material, color);
            return material;
        }

        private static Shader ResolveLitShader()
        {
            if (litShader == null)
            {
                litShader = Shader.Find(UniversalLitShaderName)
                    ?? Shader.Find(StandardShaderName)
                    ?? Shader.Find(SpritesDefaultShaderName);
            }

            return litShader;
        }

        private static Shader ResolveUnlitShader()
        {
            if (unlitShader == null)
            {
                unlitShader = Shader.Find(UniversalUnlitShaderName)
                    ?? Shader.Find(SpritesDefaultShaderName);
            }

            return unlitShader;
        }
    }
}
