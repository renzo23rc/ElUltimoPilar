using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>
    /// Tints a renderer through a property block, so the material color is never overwritten
    /// and overlapping flashes cannot leave the flash color behind.
    /// </summary>
    public static class RendererFlash
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static MaterialPropertyBlock propertyBlock;

        /// <summary>Overrides the renderer color until <see cref="Clear"/> is called.</summary>
        /// <param name="renderer">The renderer to tint.</param>
        /// <param name="color">The flash color.</param>
        public static void Apply(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.Clear();
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>Removes the flash override and shows the material color again.</summary>
        /// <param name="renderer">The renderer to restore.</param>
        public static void Clear(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.SetPropertyBlock(null);
        }
    }
}
