using System;
using System.Collections.Generic;

/// <summary>
/// Unity-free choice of the spawn zone for the next enemy. Larger zones are proportionally more likely.
/// </summary>
public static class SpawnZoneSelector
{
    /// <summary>
    /// Picks a zone index weighted by area.
    /// </summary>
    /// <param name="areas">The area of each zone; zones with zero or negative area are never picked.</param>
    /// <param name="roll">A random value in [0, 1).</param>
    /// <returns>The picked index, or -1 when no zone has a positive area.</returns>
    public static int PickIndex(IReadOnlyList<float> areas, float roll)
    {
        if (areas == null)
            throw new ArgumentNullException(nameof(areas));

        float totalArea = 0f;
        for (int i = 0; i < areas.Count; i++)
        {
            totalArea += Math.Max(0f, areas[i]);
        }

        if (totalArea <= 0f)
            return -1;

        float target = Math.Min(Math.Max(roll, 0f), 1f) * totalArea;
        int lastValidIndex = -1;
        for (int i = 0; i < areas.Count; i++)
        {
            if (areas[i] <= 0f)
                continue;

            lastValidIndex = i;
            target -= areas[i];
            if (target < 0f)
                return i;
        }

        return lastValidIndex;
    }
}
