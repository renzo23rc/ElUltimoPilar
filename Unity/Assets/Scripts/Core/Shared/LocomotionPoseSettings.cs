using System;
using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>Tunable amplitudes and rhythms of the procedural locomotion pose.</summary>
    [Serializable]
    public sealed class LocomotionPoseSettings
    {
        [SerializeField, Min(0f)] private float idleBreathHertz = 0.4f;
        [SerializeField, Min(0f)] private float idleBreathAmplitude = 0.02f;
        [SerializeField, Min(0f)] private float stepHertz = 2.2f;
        [SerializeField, Min(0f)] private float squashAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float bobMeters = 0.08f;
        [SerializeField, Min(0f)] private float rollDegrees = 3f;

        /// <summary>Gets the breathing cycles per second while standing still.</summary>
        public float IdleBreathHertz => idleBreathHertz;

        /// <summary>Gets the breathing height variation as a fraction of the base scale.</summary>
        public float IdleBreathAmplitude => idleBreathAmplitude;

        /// <summary>Gets the stride cycles (two bounces, one per leg) per second at the reference speed.</summary>
        public float StepHertz => stepHertz;

        /// <summary>Gets the squash and stretch variation while moving, as a fraction of the base scale.</summary>
        public float SquashAmplitude => squashAmplitude;

        /// <summary>Gets the vertical bounce while moving at the reference speed.</summary>
        public float BobMeters => bobMeters;

        /// <summary>Gets the side-to-side sway while moving at the reference speed.</summary>
        public float RollDegrees => rollDegrees;
    }
}
