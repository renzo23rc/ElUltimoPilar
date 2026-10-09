using UnityEngine;
using UnityEngine.Rendering;

namespace UltimoPilar.Core.Shared
{
    /// <summary>Creates procedural materials from a lit shader resolved once per session.</summary>
    public static class RuntimeMaterialFactory
    {
        private const string UniversalLitShaderName = "Universal Render Pipeline/Lit";
        private const string UniversalUnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string StandardShaderName = "Standard";
        private const string SpritesDefaultShaderName = "Sprites/Default";

        private const string SurfaceProperty = "_Surface";
        private const string BlendModeProperty = "_Blend";
        private const string SourceBlendProperty = "_SrcBlend";
        private const string DestinationBlendProperty = "_DstBlend";
        private const string DepthWriteProperty = "_ZWrite";
        private const string TransparentSurfaceKeyword = "_SURFACE_TYPE_TRANSPARENT";
        private const string AlphaBlendKeyword = "_ALPHABLEND_ON";
        private const string RenderTypeTag = "RenderType";
        private const string TransparentRenderType = "Transparent";
        private const float TransparentSurface = 1f;
        private const float AlphaBlendMode = 0f;
        private const int DepthWriteOff = 0;
        private const int TransparentRenderQueue = 3000;

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

        /// <summary>
        /// Creates an unlit material that really blends by alpha under URP. Setting only a color alpha
        /// is not enough: without the transparent surface type the shader stays opaque and ignores it.
        /// </summary>
        /// <param name="color">The material color; its alpha is the opacity.</param>
        /// <returns>A new material owned by the caller.</returns>
        public static Material CreateTransparentUnlit(Color color)
        {
            Material material = CreateUnlit(color);
            if (material.HasProperty(SurfaceProperty))
            {
                material.SetFloat(SurfaceProperty, TransparentSurface);
            }

            if (material.HasProperty(BlendModeProperty))
            {
                material.SetFloat(BlendModeProperty, AlphaBlendMode);
            }

            if (material.HasProperty(SourceBlendProperty))
            {
                material.SetInt(SourceBlendProperty, (int)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DestinationBlendProperty))
            {
                material.SetInt(DestinationBlendProperty, (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(DepthWriteProperty))
            {
                material.SetInt(DepthWriteProperty, DepthWriteOff);
            }

            material.EnableKeyword(TransparentSurfaceKeyword);
            material.EnableKeyword(AlphaBlendKeyword);
            material.SetOverrideTag(RenderTypeTag, TransparentRenderType);
            material.renderQueue = TransparentRenderQueue;
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
