namespace UltimoPilar.Arena
{
    /// <summary>
    /// Walk, spawn, hazard, and turret heights for the Statue of Liberty map, derived from mesh tops.
    /// </summary>
    public readonly struct EstatuaSurfaceHeights
    {
        /// <summary>
        /// Pivot height above the star, matching <c>Pilar</c> turret spawn and the prefab model offset
        /// so the turret feet sit on the platform instead of 1.1 m below it.
        /// </summary>
        public const float DefaultTurretPivotAboveSurfaceMeters = 1.1f;

        /// <summary>Gets the Y of the lawn the player walks on.</summary>
        public float WalkSurfaceY { get; }

        /// <summary>Gets the Y of the player transform so the CharacterController feet rest on the lawn.</summary>
        public float PlayerSpawnY { get; }

        /// <summary>Gets the Y of emergency turret roots on the star.</summary>
        public float TurretY { get; }

        /// <summary>Gets the Y of the pit and gravity zone, on the star platform.</summary>
        public float HazardY { get; }

        /// <summary>Creates an alignment from already computed heights.</summary>
        public EstatuaSurfaceHeights(float walkSurfaceY, float playerSpawnY, float turretY, float hazardY)
        {
            WalkSurfaceY = walkSurfaceY;
            PlayerSpawnY = playerSpawnY;
            TurretY = turretY;
            HazardY = hazardY;
        }

        /// <summary>
        /// Builds heights from the grass top and the star top. Pit, gravity, and turrets use the star;
        /// the player walk plane stays on the lawn.
        /// </summary>
        public static EstatuaSurfaceHeights FromMeshTops(
            float grassTopY,
            float starTopY,
            float controllerHalfHeightMeters,
            float turretPivotAboveSurfaceMeters)
        {
            float walkSurfaceY = grassTopY;
            float playerSpawnY = walkSurfaceY + controllerHalfHeightMeters;
            float hazardY = starTopY > walkSurfaceY ? starTopY : walkSurfaceY;
            float turretY = hazardY + turretPivotAboveSurfaceMeters;
            return new EstatuaSurfaceHeights(walkSurfaceY, playerSpawnY, turretY, hazardY);
        }

        /// <summary>
        /// Picks this map's pit radius: an Inspector override, else the pedestal plus padding,
        /// else the shared default used by SampleScene.
        /// </summary>
        public static float ResolvePitRadius(
            float overrideRadiusMeters,
            float pedestalHalfExtentMeters,
            float paddingMeters,
            float fallbackRadiusMeters)
        {
            if (overrideRadiusMeters > 0f)
            {
                return overrideRadiusMeters;
            }

            float fittedRadiusMeters = pedestalHalfExtentMeters + paddingMeters;
            return fittedRadiusMeters > 0f ? fittedRadiusMeters : fallbackRadiusMeters;
        }
    }
}
