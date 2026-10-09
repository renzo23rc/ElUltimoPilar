using System;

namespace UltimoPilar.Core.Shared
{
    /// <summary>
    /// Turns a movement speed into a breathing, bouncing and swaying pose without any clip.
    /// It is pure logic: the caller supplies time and speed and applies the result.
    /// </summary>
    public sealed class LocomotionPoseModel
    {
        private const float FullTurn = (float)(Math.PI * 2.0);
        private const float MeanStride = 0.5f;
        private const float NeutralScale = 1f;

        private readonly LocomotionPoseSettings settings;
        private readonly float referenceSpeed;
        private float breathPhase;
        private float stepPhase;

        /// <summary>Creates the model.</summary>
        /// <param name="settings">The pose amplitudes and rhythms.</param>
        /// <param name="referenceSpeed">The speed, in meters per second, that counts as full effort.</param>
        public LocomotionPoseModel(LocomotionPoseSettings settings, float referenceSpeed)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (referenceSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(referenceSpeed), "The reference speed must be positive.");
            }

            this.referenceSpeed = referenceSpeed;
        }

        /// <summary>Gets how hard the body is working, from 0 (standing) to 1 (reference speed or more).</summary>
        /// <param name="speed">The current speed in meters per second.</param>
        /// <returns>The movement intensity.</returns>
        public float GetIntensity(float speed)
        {
            return Math.Min(Math.Max(speed / referenceSpeed, 0f), 1f);
        }

        /// <summary>Advances the cycles and returns the pose for this frame.</summary>
        /// <param name="speed">The current speed in meters per second.</param>
        /// <param name="deltaSeconds">The elapsed time.</param>
        /// <returns>The pose to apply.</returns>
        public LocomotionPose Advance(float speed, float deltaSeconds)
        {
            float intensity = GetIntensity(speed);
            breathPhase = Wrap(breathPhase + (settings.IdleBreathHertz * deltaSeconds));
            stepPhase = Wrap(stepPhase + (settings.StepHertz * intensity * deltaSeconds));

            float breath = (float)Math.Sin(breathPhase * FullTurn) * settings.IdleBreathAmplitude * (1f - intensity);
            float stride = (float)Math.Abs(Math.Sin(stepPhase * FullTurn));
            float squash = (MeanStride - stride) * settings.SquashAmplitude * intensity;
            float verticalScale = NeutralScale + breath + squash;
            float horizontalScale = NeutralScale / (float)Math.Sqrt(verticalScale);
            float bob = stride * settings.BobMeters * intensity;
            float roll = (float)Math.Sin(stepPhase * FullTurn) * settings.RollDegrees * intensity;
            return new LocomotionPose(verticalScale, horizontalScale, bob, roll);
        }

        private static float Wrap(float phase)
        {
            return phase - (float)Math.Floor(phase);
        }
    }
}
