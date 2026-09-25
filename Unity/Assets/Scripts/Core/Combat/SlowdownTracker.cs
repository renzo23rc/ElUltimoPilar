using System;
using System.Collections.Generic;

namespace UltimoPilar.Core.Combat
{
    /// <summary>
    /// Tracks timed slowdowns per source without stacking multipliers: the strongest active slowdown wins.
    /// </summary>
    public sealed class SlowdownTracker
    {
        /// <summary>The speed factor applied when no slowdown is active.</summary>
        public const float NoSlowdownFactor = 1f;

        private const float MinimumFactor = 0f;

        private readonly Dictionary<object, Entry> entries = new Dictionary<object, Entry>();
        private readonly List<object> expiredSources = new List<object>();

        /// <summary>Gets whether any slowdown was registered and not yet removed or pruned.</summary>
        public bool HasEntries => entries.Count > 0;

        /// <summary>Applies or refreshes the slowdown owned by a source.</summary>
        /// <param name="source">The owner of the slowdown; reapplying the same source replaces it.</param>
        /// <param name="factor">The speed multiplier between 0 and 1.</param>
        /// <param name="durationSeconds">The slowdown duration in seconds.</param>
        /// <param name="nowSeconds">The current time in seconds.</param>
        public void Apply(object source, float factor, float durationSeconds, float nowSeconds)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (durationSeconds <= 0f)
            {
                return;
            }

            float clampedFactor = Math.Min(NoSlowdownFactor, Math.Max(MinimumFactor, factor));
            entries[source] = new Entry(clampedFactor, nowSeconds + durationSeconds);
        }

        /// <summary>Removes the slowdown owned by a source.</summary>
        /// <param name="source">The owner of the slowdown.</param>
        /// <returns><see langword="true"/> when a slowdown was removed.</returns>
        public bool Remove(object source)
        {
            return source != null && entries.Remove(source);
        }

        /// <summary>Removes every slowdown.</summary>
        public void Clear()
        {
            entries.Clear();
        }

        /// <summary>Prunes expired slowdowns and returns the strongest active factor.</summary>
        /// <param name="nowSeconds">The current time in seconds.</param>
        /// <returns>The active speed factor, or <see cref="NoSlowdownFactor"/> when none is active.</returns>
        public float GetFactor(float nowSeconds)
        {
            if (entries.Count == 0)
            {
                return NoSlowdownFactor;
            }

            float factor = NoSlowdownFactor;
            expiredSources.Clear();
            foreach (KeyValuePair<object, Entry> pair in entries)
            {
                if (nowSeconds >= pair.Value.ExpiresAtSeconds)
                {
                    expiredSources.Add(pair.Key);
                    continue;
                }

                factor = Math.Min(factor, pair.Value.Factor);
            }

            foreach (object source in expiredSources)
            {
                entries.Remove(source);
            }

            return factor;
        }

        /// <summary>Gets whether any slowdown is active at the supplied time.</summary>
        /// <param name="nowSeconds">The current time in seconds.</param>
        /// <returns><see langword="true"/> when the active factor is below <see cref="NoSlowdownFactor"/>.</returns>
        public bool IsActive(float nowSeconds)
        {
            return GetFactor(nowSeconds) < NoSlowdownFactor;
        }

        private readonly struct Entry
        {
            public Entry(float factor, float expiresAtSeconds)
            {
                Factor = factor;
                ExpiresAtSeconds = expiresAtSeconds;
            }

            public float Factor { get; }

            public float ExpiresAtSeconds { get; }
        }
    }
}
