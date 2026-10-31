using System;

/// <summary>
/// Unity-free map between match scenes and player materials.
/// Obelisco wears Argentina, Cristo Redentor wears Brasil, any other scene wears USA.
/// </summary>
public static class PlayerMapSkinCatalog
{
    private const string P1ModelResource = "Models/Personajes/p1";
    private const string P2ModelResource = "Models/Personajes/p2";

    private const string ObeliscoSceneName = "Obelisco";
    private const string CristoSceneName = "CristoRedentor";

    private const string ArgentinaSkinKey = "arg";
    private const string BrasilSkinKey = "brasil";
    private const string UnitedStatesSkinKey = "usa";

    private const string P1ArgentinaBodyResource = "Models/Personajes/Textures/p1/arg/texturap1argbody";
    private const string P1ArgentinaEquipmentResource = "Models/Personajes/Textures/p1/arg/texturap1argobj";
    private const string P1BrasilBodyResource = "Models/Personajes/Textures/p1/brasil/bodyp1brasil";
    private const string P1BrasilEquipmentResource = "Models/Personajes/Textures/p1/brasil/objp1brasil";
    private const string P1UnitedStatesBodyResource = "Models/Personajes/Textures/p1/usa/bodyp1usa";
    private const string P1UnitedStatesEquipmentResource = "Models/Personajes/Textures/p1/usa/objp1usa";

    private const string P2ArgentinaBodyResource = "Models/Personajes/Textures/p2/arg/bodyobjp2arg";
    private const string P2ArgentinaEquipmentResource = "Models/Personajes/Textures/p2/arg/objp2arg";
    private const string P2BrasilBodyResource = "Models/Personajes/Textures/p2/brasil/bodyp2brasil";
    private const string P2BrasilEquipmentResource = "Models/Personajes/Textures/p2/brasil/objp2brasil";
    private const string P2UnitedStatesBodyResource = "Models/Personajes/Textures/p2/usa/bodyp2usa";
    private const string P2UnitedStatesEquipmentResource = "Models/Personajes/Textures/p2/usa/objp2usa";

    /// <summary>Returns the skin key for a match scene: arg, brasil, or usa. Unknown scenes default to usa.</summary>
    /// <param name="sceneName">The active scene name.</param>
    /// <returns>The skin key.</returns>
    public static string SkinKeyForScene(string sceneName)
    {
        if (string.Equals(sceneName, ObeliscoSceneName, StringComparison.Ordinal))
        {
            return ArgentinaSkinKey;
        }

        if (string.Equals(sceneName, CristoSceneName, StringComparison.Ordinal))
        {
            return BrasilSkinKey;
        }

        return UnitedStatesSkinKey;
    }

    /// <summary>Gets the body material resource path for a player model in a scene.</summary>
    /// <param name="modelResource">The player model resource, p1 or p2.</param>
    /// <param name="sceneName">The active scene name.</param>
    /// <returns>The body material resource path.</returns>
    public static string BodyMaterialResource(string modelResource, string sceneName)
    {
        string skinKey = SkinKeyForScene(sceneName);
        bool isSecondPlayer = IsSecondPlayer(modelResource);

        if (!isSecondPlayer)
        {
            return BodyMaterialResourceForFirstPlayer(skinKey);
        }

        return BodyMaterialResourceForSecondPlayer(skinKey);
    }

    /// <summary>Gets the equipment material resource path for a player model in a scene.</summary>
    /// <param name="modelResource">The player model resource, p1 or p2.</param>
    /// <param name="sceneName">The active scene name.</param>
    /// <returns>The equipment material resource path.</returns>
    public static string EquipmentMaterialResource(string modelResource, string sceneName)
    {
        string skinKey = SkinKeyForScene(sceneName);
        bool isSecondPlayer = IsSecondPlayer(modelResource);

        if (!isSecondPlayer)
        {
            return EquipmentMaterialResourceForFirstPlayer(skinKey);
        }

        return EquipmentMaterialResourceForSecondPlayer(skinKey);
    }

    private static bool IsSecondPlayer(string modelResource)
    {
        if (string.Equals(modelResource, P1ModelResource, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(modelResource, P2ModelResource, StringComparison.Ordinal))
        {
            return true;
        }

        throw new ArgumentOutOfRangeException(nameof(modelResource), modelResource, "Unknown player model resource.");
    }

    private static string BodyMaterialResourceForFirstPlayer(string skinKey)
    {
        if (string.Equals(skinKey, ArgentinaSkinKey, StringComparison.Ordinal))
        {
            return P1ArgentinaBodyResource;
        }

        if (string.Equals(skinKey, BrasilSkinKey, StringComparison.Ordinal))
        {
            return P1BrasilBodyResource;
        }

        return P1UnitedStatesBodyResource;
    }

    private static string EquipmentMaterialResourceForFirstPlayer(string skinKey)
    {
        if (string.Equals(skinKey, ArgentinaSkinKey, StringComparison.Ordinal))
        {
            return P1ArgentinaEquipmentResource;
        }

        if (string.Equals(skinKey, BrasilSkinKey, StringComparison.Ordinal))
        {
            return P1BrasilEquipmentResource;
        }

        return P1UnitedStatesEquipmentResource;
    }

    private static string BodyMaterialResourceForSecondPlayer(string skinKey)
    {
        if (string.Equals(skinKey, ArgentinaSkinKey, StringComparison.Ordinal))
        {
            return P2ArgentinaBodyResource;
        }

        if (string.Equals(skinKey, BrasilSkinKey, StringComparison.Ordinal))
        {
            return P2BrasilBodyResource;
        }

        return P2UnitedStatesBodyResource;
    }

    private static string EquipmentMaterialResourceForSecondPlayer(string skinKey)
    {
        if (string.Equals(skinKey, ArgentinaSkinKey, StringComparison.Ordinal))
        {
            return P2ArgentinaEquipmentResource;
        }

        if (string.Equals(skinKey, BrasilSkinKey, StringComparison.Ordinal))
        {
            return P2BrasilEquipmentResource;
        }

        return P2UnitedStatesEquipmentResource;
    }
}
