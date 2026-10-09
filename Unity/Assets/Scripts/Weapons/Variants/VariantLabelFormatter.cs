/// <summary>
/// Unity-free formatting of the temporary weapon variant label shown in the HUD.
/// </summary>
public static class VariantLabelFormatter
{
    /// <summary>Returns the display name only while the variant is active.</summary>
    /// <param name="variantIsActive">Whether a variant is active.</param>
    /// <param name="semanticDisplayName">The display name of the active variant.</param>
    /// <returns>The display name, or an empty string.</returns>
    public static string GetDisplayName(bool variantIsActive, string semanticDisplayName)
    {
        if (!variantIsActive || string.IsNullOrEmpty(semanticDisplayName))
            return string.Empty;

        return semanticDisplayName;
    }

    /// <summary>Formats the variant label; only damage variants show their multiplier.</summary>
    /// <param name="displayName">The variant display name.</param>
    /// <param name="multipliesDamage">Whether the variant multiplies damage.</param>
    /// <param name="multiplier">The damage multiplier.</param>
    /// <param name="remainingSeconds">The remaining duration in seconds.</param>
    /// <returns>The label, or an empty string without a display name.</returns>
    public static string FormatLabel(string displayName, bool multipliesDamage, float multiplier, float remainingSeconds)
    {
        if (string.IsNullOrEmpty(displayName))
            return string.Empty;

        string prefix = multipliesDamage ? $"x{multiplier:F0} " : string.Empty;
        return $"¡{prefix}{displayName}! {remainingSeconds:F0}s";
    }
}
