using System;

/// <summary>
/// Maps each GDD weapon variant to the base weapon it boosts, its effect, and its display name.
/// </summary>
public static class WeaponVariantCatalog
{
    /// <summary>Gets the definition of a weapon variant.</summary>
    /// <param name="variant">The weapon variant.</param>
    /// <returns>The weapon, effect, and display name of the variant.</returns>
    public static WeaponSystem.VariantDefinition GetDefinition(WeaponSystem.WeaponVariant variant)
    {
        return variant switch
        {
            WeaponSystem.WeaponVariant.PrecisionRifle => new WeaponSystem.VariantDefinition(
                WeaponSystem.TipoArma.Directa, WeaponSystem.VariantEffect.DamageMultiplier, "Rifle de precisión"),
            WeaponSystem.WeaponVariant.Decoy => new WeaponSystem.VariantDefinition(
                WeaponSystem.TipoArma.Area, WeaponSystem.VariantEffect.Decoy, "Señuelo"),
            WeaponSystem.WeaponVariant.Slowdown => new WeaponSystem.VariantDefinition(
                WeaponSystem.TipoArma.Area, WeaponSystem.VariantEffect.Slowdown, "Ralentización"),
            WeaponSystem.WeaponVariant.PushStrike => new WeaponSystem.VariantDefinition(
                WeaponSystem.TipoArma.CuerpoACuerpo, WeaponSystem.VariantEffect.Push, "Golpe de empuje"),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported weapon variant.")
        };
    }
}
