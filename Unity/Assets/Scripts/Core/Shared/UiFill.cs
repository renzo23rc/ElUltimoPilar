using UnityEngine;
using UnityEngine.UI;

namespace UltimoPilar.Core.Shared
{
    /// <summary>
    /// Shows a 0–1 fill on a bar image. Filled images without a sprite ignore <see cref="Image.fillAmount"/>,
    /// so those bars are resized through their anchors instead.
    /// </summary>
    public static class UiFill
    {
        /// <summary>Applies the fill fraction to a bar image.</summary>
        /// <param name="bar">The bar fill image, stretched inside its background.</param>
        /// <param name="fraction">The fill fraction; values outside 0–1 are clamped.</param>
        public static void Set(Image bar, float fraction)
        {
            if (bar == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(fraction);
            bar.fillAmount = clamped;
            if (bar.type == Image.Type.Filled && bar.sprite != null)
            {
                return;
            }

            RectTransform rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(clamped, rect.anchorMax.y);
        }
    }
}
