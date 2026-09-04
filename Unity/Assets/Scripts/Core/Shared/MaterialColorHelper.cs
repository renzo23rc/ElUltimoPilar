using UnityEngine;

namespace UltimoPilar.Core.Shared;

/// <summary>Applies a color consistently across supported Unity material properties.</summary>
public static class MaterialColorHelper
{
    /// <summary>Sets base, legacy, and emission color properties when available.</summary>
    public static void SetBaseAndEmissionColor(Material material, Color color, float emissionMultiplier = 1f)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * emissionMultiplier);
            material.EnableKeyword("_EMISSION");
        }
    }
}
