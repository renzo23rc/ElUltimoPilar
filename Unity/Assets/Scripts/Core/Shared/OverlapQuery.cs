using System.Collections.Generic;
using UnityEngine;

namespace UltimoPilar.Core.Shared
{
    /// <summary>Finds distinct components inside physics overlaps, regardless of how many colliders each owns.</summary>
    public static class OverlapQuery
    {
        /// <summary>Returns each component found once, resolving colliders on child objects.</summary>
        /// <typeparam name="T">The component type to collect.</typeparam>
        /// <param name="center">The sphere center in world space.</param>
        /// <param name="radius">The sphere radius in meters.</param>
        /// <returns>The distinct components whose colliders overlap the sphere.</returns>
        public static List<T> FindUniqueInSphere<T>(Vector3 center, float radius) where T : Component
        {
            return CollectUnique<T>(Physics.OverlapSphere(center, radius));
        }

        /// <summary>Returns each component found once among the supplied colliders.</summary>
        /// <typeparam name="T">The component type to collect.</typeparam>
        /// <param name="colliders">The colliders to inspect.</param>
        /// <returns>The distinct components owning the colliders.</returns>
        public static List<T> CollectUnique<T>(IReadOnlyList<Collider> colliders) where T : Component
        {
            var components = new List<T>();
            if (colliders == null)
            {
                return components;
            }

            var seen = new HashSet<T>();
            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                T component = collider.GetComponentInParent<T>();
                if (component != null && seen.Add(component))
                {
                    components.Add(component);
                }
            }

            return components;
        }
    }
}
