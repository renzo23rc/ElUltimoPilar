using UltimoPilar.Core.Shared;
using UnityEngine;

namespace UltimoPilar.Core.Pilar
{
    /// <summary>Builds the procedural emergency turret used when no turret prefab is available.</summary>
    public static class TurretFallbackFactory
    {
        private const string FallbackName = "TorretaPrefab";
        private const string FirePointName = "PuntoDisparo";
        private const float WidthMeters = 1.4f;
        private const float HeightMeters = 2.2f;
        private const float EmissionMultiplier = 0.6f;
        private const float LightRangeMeters = 6f;
        private const float LightIntensity = 2f;
        private const float FirePointForwardMeters = 0.8f;
        private const float FirePointHeightMeters = 0.6f;
        private static readonly Color TurretColor = new Color(1f, 0.85f, 0.1f);

        /// <summary>Creates an active turret cube with collider, light, fire point, and <see cref="Torreta"/>.</summary>
        /// <param name="position">The world position.</param>
        /// <param name="rotation">The world rotation.</param>
        /// <returns>The created turret object.</returns>
        public static GameObject Create(Vector3 position, Quaternion rotation)
        {
            GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turret.name = FallbackName;
            turret.transform.SetPositionAndRotation(position, rotation);
            turret.transform.localScale = new Vector3(WidthMeters, HeightMeters, WidthMeters);

            OwnedMaterialCleanup.Assign(
                turret.GetComponent<Renderer>(),
                RuntimeMaterialFactory.CreateLit(TurretColor, EmissionMultiplier));

            // El cubo primitivo ya trae BoxCollider: se reutiliza en vez de destruirlo y recrearlo.
            if (!turret.TryGetComponent(out BoxCollider collider))
            {
                collider = turret.AddComponent<BoxCollider>();
            }

            collider.isTrigger = false;
            collider.center = Vector3.zero;
            collider.size = Vector3.one;

            var light = turret.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = TurretColor;
            light.range = LightRangeMeters;
            light.intensity = LightIntensity;

            Torreta turretComponent = turret.AddComponent<Torreta>();
            var firePoint = new GameObject(FirePointName);
            firePoint.transform.SetParent(turret.transform);
            firePoint.transform.localPosition = (Vector3.forward * FirePointForwardMeters) + (Vector3.up * FirePointHeightMeters);
            firePoint.transform.localRotation = Quaternion.identity;
            firePoint.transform.localScale = Vector3.one;
            turretComponent.puntoDisparo = firePoint.transform;
            return turret;
        }
    }
}
