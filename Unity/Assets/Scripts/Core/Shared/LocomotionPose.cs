namespace UltimoPilar.Core.Shared
{
    /// <summary>One frame of procedural body motion, relative to the resting pose.</summary>
    public readonly struct LocomotionPose
    {
        /// <summary>Creates a pose.</summary>
        /// <param name="verticalScale">The height multiplier.</param>
        /// <param name="horizontalScale">The width and depth multiplier.</param>
        /// <param name="bobMeters">The vertical offset in meters.</param>
        /// <param name="rollDegrees">The sideways tilt in degrees.</param>
        public LocomotionPose(float verticalScale, float horizontalScale, float bobMeters, float rollDegrees)
        {
            VerticalScale = verticalScale;
            HorizontalScale = horizontalScale;
            BobMeters = bobMeters;
            RollDegrees = rollDegrees;
        }

        /// <summary>Gets the height multiplier.</summary>
        public float VerticalScale { get; }

        /// <summary>Gets the width and depth multiplier.</summary>
        public float HorizontalScale { get; }

        /// <summary>Gets the vertical offset in meters.</summary>
        public float BobMeters { get; }

        /// <summary>Gets the sideways tilt in degrees.</summary>
        public float RollDegrees { get; }
    }
}
