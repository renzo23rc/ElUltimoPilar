using UnityEngine;

/// <summary>
/// Defines the shared asset conventions used by the Editor prefab generators.
/// </summary>
internal static class PrefabAssetConventions
{
    internal const string ResourcesFolder = "Assets/Resources";
    internal const string TestsFolder = "Assets/Tests";
    internal const string MaterialsFolder = "Assets/Materials";
    internal const string ResourcesMaterialsFolder = ResourcesFolder + "/Materials";
    internal const string ResourcesPrefabFolder = ResourcesFolder + "/Prefabs";
    internal const string TestsPrefabFolder = TestsFolder + "/Prefabs";
    internal const string PrefabExtension = ".prefab";
    internal const string MaterialExtension = ".mat";

    internal const string LitShaderName = "Universal Render Pipeline/Lit";
    internal const string StandardShaderName = "Standard";
    internal const string SpriteShaderName = "Sprites/Default";
    internal const string BaseColorPropertyName = "_BaseColor";
    internal const string ColorPropertyName = "_Color";
    internal const string EmissionColorPropertyName = "_EmissionColor";
    internal const string EmissionKeyword = "_EMISSION";

    internal const string EnergyPickupPoolKey = "EnergyPickup";
    internal const string ProjectilePoolKey = "Proyectil";
    internal const string EnergyPickupPrefabName = "EnergiaPickup";
    internal const string ProjectilePrefabName = "ProyectilBase";
    internal const string RunnerPrefabName = "Corredor";
    internal const string ArtilleryPrefabName = "Artillero";
    internal const string ExplosivePrefabName = "Explosivo";
    internal const string WeaverPrefabName = "Tejedor";
    internal const string NestPrefabName = "Nido";
    internal const string ColossusPrefabName = "Coloso";
    internal const string TurretPrefabName = "Torreta";
    internal const string TurretMuzzleName = "PuntoDisparo";

    internal const string RunnerMaterialName = "Mat_Corredor";
    internal const string ArtilleryMaterialName = "Mat_Artillero";
    internal const string ExplosiveMaterialName = "Mat_Explosivo";
    internal const string WeaverMaterialName = "Mat_Tejedor";
    internal const string NestMaterialName = "Mat_Nido";
    internal const string ColossusMaterialName = "Mat_Coloso";
    internal const string TurretMaterialName = "Mat_Torreta";
    internal const string ProjectileMaterialName = "Mat_Proyectil";
    internal const string EnergyPickupMaterialName = "Mat_Energia";

    internal const float NoEmissionIntensity = 0f;
    internal const float ExplosiveEmissionIntensity = 0.4f;
    internal const float WeaverEmissionIntensity = 0.3f;
    internal const float ColossusEmissionIntensity = 0.2f;
    internal const float TurretEmissionIntensity = 0.6f;
    internal const float ProjectileEmissionIntensity = 1.2f;
    internal const float EnergyPickupEmissionIntensity = 0.8f;

    internal static readonly Color EnergyPickupColor = Color.cyan;
    internal static readonly Color RunnerColor = Color.red;
    internal static readonly Color ArtilleryColor = Color.blue;
    internal static readonly Color ExplosiveColor = Color.yellow;
    internal static readonly Color WeaverColor = new Color(1f, 0f, 1f);
    internal static readonly Color NestColor = Color.gray;
    internal static readonly Color ColossusColor = new Color(0.5f, 0f, 0f);
    internal static readonly Color TurretColor = new Color(1f, 0.85f, 0.1f);
    internal static readonly Color ProjectileColor = new Color(1f, 0.5f, 0f);

    internal static readonly string[] GeneratedPrefabNames = new string[]
    {
        EnergyPickupPrefabName,
        ProjectilePrefabName,
        RunnerPrefabName,
        ArtilleryPrefabName,
        ExplosivePrefabName,
        WeaverPrefabName,
        NestPrefabName,
        ColossusPrefabName,
        TurretPrefabName
    };

    internal static string GetPrefabPath(string folder, string prefabName)
    {
        return folder + "/" + prefabName + PrefabExtension;
    }

    internal static string GetResourcePrefabPath(string prefabName)
    {
        return GetPrefabPath(ResourcesPrefabFolder, prefabName);
    }

    internal static string GetTestsPrefabPath(string prefabName)
    {
        return GetPrefabPath(TestsPrefabFolder, prefabName);
    }

    internal static string GetMaterialPath(string materialName)
    {
        return MaterialsFolder + "/" + materialName + MaterialExtension;
    }

    internal static string GetResourceMaterialPath(string materialName)
    {
        return ResourcesMaterialsFolder + "/" + materialName + MaterialExtension;
    }
}
