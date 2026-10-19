using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes Statue of Liberty meshes on import. The star platform ships with inverted
/// winding and empty UVs, which makes it look broken in Unity without touching other maps.
/// </summary>
public sealed class EstatuaLibMeshPostprocessor : AssetPostprocessor
{
    private const string EstatuaLibFolderFragment = "/EstatuaLib/";
    private const string StarMeshObjectName = "Base.001";
    private const string FlippedMeshSuffix = "_Fixed";
    private const int TriangleIndexStride = 3;
    private const float MinimumUvExtent = 0.0001f;

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
        Bounds bounds = mesh.bounds;
        float extentX = Mathf.Max(bounds.size.x, MinimumUvExtent);
        float extentY = Mathf.Max(bounds.size.y, MinimumUvExtent);
        var uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(
                (vertices[i].x - bounds.min.x) / extentX,
                (vertices[i].y - bounds.min.y) / extentY);
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
