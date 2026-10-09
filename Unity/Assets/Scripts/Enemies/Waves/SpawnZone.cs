using UnityEngine;

/// <summary>
/// Rectangular area on the ground where enemies may appear. Place one or more in the scene over streets and
/// open ground (never over buildings); <see cref="EnemySpawner"/> picks among them, larger zones more often.
/// The rectangle follows the transform's position, Y rotation and X/Z scale.
/// </summary>
[DisallowMultipleComponent]
public class SpawnZone : MonoBehaviour
{
    private const float DefaultZoneSizeMeters = 10f;
    private const float MinimumZoneSizeMeters = 0.5f;
    private const float DefaultSpawnHeightMeters = 1f;
    private const float GizmoHeightMeters = 0.1f;
    private static readonly Color GizmoFillColor = new Color(1f, 0.35f, 0.1f, 0.25f);
    private static readonly Color GizmoWireColor = new Color(1f, 0.35f, 0.1f, 1f);

    [Tooltip("Ancho (X) y largo (Z) de la zona en metros, antes de aplicar la escala del Transform.")]
    [SerializeField] private Vector2 sizeMeters = new Vector2(DefaultZoneSizeMeters, DefaultZoneSizeMeters);

    [Tooltip("Altura sobre el Transform a la que aparecen los enemigos, en metros.")]
    [SerializeField, Min(0f)] private float spawnHeightMeters = DefaultSpawnHeightMeters;

    /// <summary>Gets the world-space area of the rectangle, used to weight the zone choice.</summary>
    public float Area
    {
        get
        {
            Vector3 scale = transform.lossyScale;
            return sizeMeters.x * Mathf.Abs(scale.x) * sizeMeters.y * Mathf.Abs(scale.z);
        }
    }

    /// <summary>Returns a random world position inside the zone.</summary>
    public Vector3 SamplePoint()
    {
        Vector3 local = new Vector3(
            Random.Range(-0.5f, 0.5f) * sizeMeters.x,
            spawnHeightMeters / Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Epsilon),
            Random.Range(-0.5f, 0.5f) * sizeMeters.y);
        return transform.TransformPoint(local);
    }

    private void OnValidate()
    {
        sizeMeters.x = Mathf.Max(sizeMeters.x, MinimumZoneSizeMeters);
        sizeMeters.y = Mathf.Max(sizeMeters.y, MinimumZoneSizeMeters);
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 size = new Vector3(sizeMeters.x, GizmoHeightMeters, sizeMeters.y);
        Gizmos.color = GizmoFillColor;
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = GizmoWireColor;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = previousMatrix;
    }
}
