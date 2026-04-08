#nullable enable

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.Entities;

namespace DogDays.Game.Systems;

/// <summary>
/// Evaluates whether a candidate watercraft hull overlaps any solid blockers.
/// </summary>
internal static class WatercraftCollisionRules
{
    /// <summary>
    /// Returns true when the candidate bounds overlap a static watercraft blocker or another craft hull.
    /// </summary>
    public static bool IsBlocked(
        Rectangle candidateBounds,
        Rectangle currentBounds,
        IReadOnlyList<Rectangle> staticBlockerBounds,
        IReadOnlyList<Watercraft> allWatercraft,
        Watercraft? activeWatercraft)
    {
        for (var i = 0; i < staticBlockerBounds.Count; i++)
        {
            if (OverlapWouldIncrease(currentBounds, candidateBounds, staticBlockerBounds[i]))
            {
                return true;
            }
        }

        for (var i = 0; i < allWatercraft.Count; i++)
        {
            var watercraft = allWatercraft[i];
            if (ReferenceEquals(watercraft, activeWatercraft))
            {
                continue;
            }

            if (OverlapWouldIncrease(currentBounds, candidateBounds, watercraft.Bounds))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverlapWouldIncrease(Rectangle currentBounds, Rectangle candidateBounds, Rectangle blockerBounds)
    {
        var currentOverlapArea = GetIntersectionArea(currentBounds, blockerBounds);
        var candidateOverlapArea = GetIntersectionArea(candidateBounds, blockerBounds);
        return candidateOverlapArea > currentOverlapArea;
    }

    private static int GetIntersectionArea(Rectangle a, Rectangle b)
    {
        var intersection = Rectangle.Intersect(a, b);
        return intersection.IsEmpty ? 0 : intersection.Width * intersection.Height;
    }
}