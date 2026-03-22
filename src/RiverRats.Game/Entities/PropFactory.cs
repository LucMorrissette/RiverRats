using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.World;

namespace RiverRats.Game.Entities;

/// <summary>
/// Static factory methods for creating world prop entity arrays from TMX prop placements.
/// </summary>
internal static class PropFactory
{
    internal const int SmallFireFramePixels = 16;
    internal const int SmallFireFrameCount = 8;
    internal const float SmallFireFrameDuration = 0.1f;
    internal const float FirepitAttachDistancePixels = 24f;
    internal const float FirepitAttachDistanceSquared = FirepitAttachDistancePixels * FirepitAttachDistancePixels;

    internal static Boulder[] CreateBoulders(Texture2D boulderTexture, IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var boulders = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "boulder", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            boulders.Add(new Boulder(placement.Position, boulderTexture));
        }

        return boulders.ToArray();
    }

    internal static Dock[] CreateDocks(Texture2D dockTexture, IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var docks = new List<Dock>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "dock", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            docks.Add(new Dock(placement.Position, dockTexture));
        }

        return docks.ToArray();
    }

    internal static Firepit[] CreateFirepits(
        Texture2D firepitTexture,
        Texture2D smallFireSpriteSheet,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var firepitPlacements = new List<TiledWorldRenderer.MapPropPlacement>(placements.Count);
        var smallFirePlacements = new List<TiledWorldRenderer.MapPropPlacement>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (string.Equals(placement.PropType, "firepit", StringComparison.OrdinalIgnoreCase))
            {
                firepitPlacements.Add(placement);
                continue;
            }

            if (string.Equals(placement.PropType, "small-fire", StringComparison.OrdinalIgnoreCase))
            {
                smallFirePlacements.Add(placement);
            }
        }

        var firepits = new List<Firepit>(firepitPlacements.Count);
        var assignedSmallFires = new bool[smallFirePlacements.Count];
        for (var i = 0; i < firepitPlacements.Count; i++)
        {
            var firepitPlacement = firepitPlacements[i];
            SmallFire attachedFire = null!;
            var nearestSmallFireIndex = FindNearestSmallFireIndex(firepitPlacement.Position, smallFirePlacements, assignedSmallFires);
            if (nearestSmallFireIndex >= 0)
            {
                assignedSmallFires[nearestSmallFireIndex] = true;
                attachedFire = CreateSmallFire(smallFireSpriteSheet, smallFirePlacements[nearestSmallFireIndex].Position);
            }

            firepits.Add(new Firepit(firepitPlacement.Position, firepitTexture, attachedFire));
        }

        return firepits.ToArray();
    }

    internal static int FindNearestSmallFireIndex(
        Vector2 firepitPosition,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> smallFirePlacements,
        bool[] assignedSmallFires)
    {
        var nearestIndex = -1;
        var nearestDistanceSquared = FirepitAttachDistanceSquared;
        for (var i = 0; i < smallFirePlacements.Count; i++)
        {
            if (assignedSmallFires[i])
            {
                continue;
            }

            var distanceSquared = Vector2.DistanceSquared(firepitPosition, smallFirePlacements[i].Position);
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    internal static SmallFire CreateSmallFire(Texture2D spriteSheet, Vector2 position)
    {
        var animator = new LoopAnimator(SmallFireFramePixels, SmallFireFramePixels, SmallFireFrameCount, SmallFireFrameDuration);
        return new SmallFire(position, spriteSheet, animator);
    }

    internal static Boulder[] CreatePropsByType(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        string propType,
        bool isUnderwater,
        bool reachesSurface = false)
    {
        var props = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, propType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater != isUnderwater)
            {
                continue;
            }

            if (placement.ReachesSurface != reachesSurface)
            {
                continue;
            }

            props.Add(new Boulder(placement.Position, texture));
        }

        return props.ToArray();
    }

    internal static SunkenChest[] CreateSunkenChests(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        bool isUnderwater)
    {
        var chests = new List<SunkenChest>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "sunken-chest", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater != isUnderwater)
            {
                continue;
            }

            chests.Add(new SunkenChest(placement.Position, texture));
        }

        return chests.ToArray();
    }

    internal static FlatShoreDepthSimulator[] CreateFlatShoreDepthSimulators(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var props = new List<FlatShoreDepthSimulator>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "flat-shore-depth-simulator", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            props.Add(new FlatShoreDepthSimulator(placement.Position, texture));
        }

        return props.ToArray();
    }

    internal static Boulder[] CreateSeaweeds(
        Texture2D[] variantTextures,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var seaweeds = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!placement.PropType.StartsWith("seaweed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse variant index from prop type (seaweed1 → 0, seaweed2 → 1, etc.)
            var lastChar = placement.PropType[^1];
            var variantIndex = lastChar - '1';
            if (variantIndex < 0 || variantIndex >= variantTextures.Length)
            {
                continue;
            }

            seaweeds.Add(new Boulder(placement.Position, variantTextures[variantIndex]));
        }

        return seaweeds.ToArray();
    }

    internal static Rectangle[] MergeObstacleBounds(Rectangle[] boulderBounds, IReadOnlyList<Rectangle> colliderBounds)
    {
        if (colliderBounds.Count == 0)
        {
            return boulderBounds;
        }

        var merged = new Rectangle[boulderBounds.Length + colliderBounds.Count];
        boulderBounds.CopyTo(merged, 0);
        for (var i = 0; i < colliderBounds.Count; i++)
        {
            merged[boulderBounds.Length + i] = colliderBounds[i];
        }

        return merged;
    }

    internal static Rectangle[] MergeRectangleArrays(Rectangle[] first, Rectangle[] second)
    {
        if (second.Length == 0)
        {
            return first;
        }

        if (first.Length == 0)
        {
            return second;
        }

        var merged = new Rectangle[first.Length + second.Length];
        first.CopyTo(merged, 0);
        second.CopyTo(merged, first.Length);
        return merged;
    }

    internal static Rectangle[] GetBoulderBounds(Boulder[] boulders)
    {
        var bounds = new Rectangle[boulders.Length];
        for (var i = 0; i < boulders.Length; i++)
        {
            bounds[i] = boulders[i].Bounds;
        }

        return bounds;
    }

    internal static Rectangle[] GetDockBounds(Dock[] docks)
    {
        var bounds = new Rectangle[docks.Length];
        for (var i = 0; i < docks.Length; i++)
        {
            bounds[i] = docks[i].Bounds;
        }

        return bounds;
    }

    internal static Rectangle[] GetFirepitBounds(Firepit[] firepits)
    {
        var bounds = new Rectangle[firepits.Length];
        for (var i = 0; i < firepits.Length; i++)
        {
            bounds[i] = firepits[i].Bounds;
        }

        return bounds;
    }
}
