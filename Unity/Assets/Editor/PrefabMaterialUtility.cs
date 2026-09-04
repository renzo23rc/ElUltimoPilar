using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Provides shared shader, material, and renderer handling for prefab generation.
/// </summary>
internal static class PrefabMaterialUtility
{
    internal static Material CreateConfiguredMaterial(
        Color color,
        float emissionIntensity,
        bool enableEmission)
    {
        Shader shader = ResolveCompatibleShader();
        Material material = new Material(shader);

        try
        {
            ConfigureColor(material, color);
            if (enableEmission && material.HasProperty(PrefabAssetConventions.EmissionColorPropertyName))
            {
                material.SetColor(
                    PrefabAssetConventions.EmissionColorPropertyName,
                    color * emissionIntensity);
                material.EnableKeyword(PrefabAssetConventions.EmissionKeyword);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            return material;
        }
        catch (InvalidOperationException)
        {
            DestroyTemporaryMaterial(material);
            throw;
        }
    }

    internal static Shader ResolveCompatibleShader()
    {
        Shader shader = Shader.Find(PrefabAssetConventions.LitShaderName);
        if (shader == null)
        {
            shader = Shader.Find(PrefabAssetConventions.StandardShaderName);
        }
        if (shader == null)
        {
            shader = Shader.Find(PrefabAssetConventions.SpriteShaderName);
        }
        if (shader == null)
        {
            throw new InvalidOperationException(
                "No se encontró un shader compatible para los materiales de prefabs.");
        }

        return shader;
    }

    internal static void AssignSharedMaterial(
        Renderer renderer,
        Material material,
        string context)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }
        if (material == null)
        {
            throw new ArgumentNullException(nameof(material));
        }
        if (material.shader == null)
        {
            throw new InvalidOperationException(
                $"El material no tiene shader para {context}.");
        }
        if (!HasColorProperty(material))
        {
            throw new InvalidOperationException(
                $"El material no expone una propiedad de color compatible para {context}.");
        }

        renderer.sharedMaterial = material;
    }

    internal static void DestroyTemporaryMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }
        if (AssetDatabase.Contains(material))
        {
            return;
        }

        UnityEngine.Object.DestroyImmediate(material);
    }

    private static void ConfigureColor(Material material, Color color)
    {
        bool colorConfigured = false;
        if (material.HasProperty(PrefabAssetConventions.BaseColorPropertyName))
        {
            material.SetColor(PrefabAssetConventions.BaseColorPropertyName, color);
            colorConfigured = true;
        }
        if (material.HasProperty(PrefabAssetConventions.ColorPropertyName))
        {
            material.SetColor(PrefabAssetConventions.ColorPropertyName, color);
            colorConfigured = true;
        }
        if (!colorConfigured)
        {
            throw new InvalidOperationException(
                "El shader no expone una propiedad de color compatible.");
        }
    }

    private static bool HasColorProperty(Material material)
    {
        return material.HasProperty(PrefabAssetConventions.BaseColorPropertyName)
            || material.HasProperty(PrefabAssetConventions.ColorPropertyName);
    }
}
