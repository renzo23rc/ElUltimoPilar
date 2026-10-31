using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes Statue of Liberty meshes on import. The star platform ships with inverted
/// winding and empty UVs, so it gets a planar UV and its own baked stone material.
/// </summary>
public sealed class EstatuaLibMeshPostprocessor : AssetPostprocessor
{
    private const string EstatuaLibFolderFragment = "/EstatuaLib/";
    private const string StarMeshObjectName = "Base.001";
    private const string StarMaterialAssetPath = "Assets/Models/EstatuaLib/Materials/piedra.001_Desgastado_Estrella.mat";
    private const string FlippedMeshSuffix = "_Fixed";
    private const int TriangleIndexStride = 3;
    private const float MinimumUvExtent = 0.0001f;
    private const float StarUvInset = 0.02f;

    private void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.Contains(EstatuaLibFolderFragment))
        {
            return;
        }

        Transform star = FindChildByName(root.transform, StarMeshObjectName);
        if (star == null || !star.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
        {
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        ReverseTriangles(mesh);
        EnsurePlanarUv(mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        if (!mesh.name.EndsWith(FlippedMeshSuffix))
        {
            mesh.name += FlippedMeshSuffix;
        }

        AssignStarMaterial(star);
    }

    private static void AssignStarMaterial(Transform star)
    {
        if (!star.TryGetComponent(out MeshRenderer meshRenderer))
        {
            return;
        }

        Material starMaterial = AssetDatabase.LoadAssetAtPath<Material>(StarMaterialAssetPath);
        if (starMaterial == null)
        {
            return;
        }

        meshRenderer.sharedMaterial = starMaterial;
    }

    private static void ReverseTriangles(Mesh mesh)
    {
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            int[] triangles = mesh.GetTriangles(subMesh);
            for (int i = 0; i < triangles.Length; i += TriangleIndexStride)
            {
                int swap = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = swap;
            }

            mesh.SetTriangles(triangles, subMesh);
        }
    }

    private static void EnsurePlanarUv(Mesh mesh)
    {
        Vector2[] existing = mesh.uv;
        if (HasMappedUv(existing))
        {
            return;
        }

        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0)
        {
            return;
        }

        float minX = vertices[0].x;
        float maxX = vertices[0].x;
        float minY = vertices[0].y;
        float maxY = vertices[0].y;
        for (int i = 1; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            minX = Mathf.Min(minX, vertex.x);
            maxX = Mathf.Max(maxX, vertex.x);
            minY = Mathf.Min(minY, vertex.y);
            maxY = Mathf.Max(maxY, vertex.y);
        }

        float extentX = Mathf.Max(maxX - minX, MinimumUvExtent);
        float extentY = Mathf.Max(maxY - minY, MinimumUvExtent);
        float coveredSpan = 1f - (StarUvInset + StarUvInset);
        var uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            float horizontal = (vertices[i].x - minX) / extentX;
            float vertical = (vertices[i].y - minY) / extentY;
            uvs[i] = new Vector2(
                StarUvInset + (coveredSpan * horizontal),
                StarUvInset + (coveredSpan * vertical));
        }

        mesh.uv = uvs;
    }

    private static bool HasMappedUv(Vector2[] uvs)
    {
        if (uvs == null || uvs.Length == 0)
        {
            return false;
        }

        Vector2 first = uvs[0];
        for (int i = 1; i < uvs.Length; i++)
        {
            if (uvs[i] != first)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
