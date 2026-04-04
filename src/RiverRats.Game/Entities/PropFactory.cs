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

    internal static float CalculateUniformScale(int sourceWidthPixels, int targetWidthPixels)
    {
        if (sourceWidthPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidthPixels), "Source width must be greater than zero.");
        }

        if (targetWidthPixels == 0)
        {
            return 1f;
        }

        if (targetWidthPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWidthPixels), "Target width cannot be negative.");
        }

        return targetWidthPixels / (float)sourceWidthPixels;
    }

    /// <summary>
    /// Collision boxes for pine trees relative to the 80×128 sprite.
    /// Multiple rectangles hug the trunk shape from the narrow mid-section
    /// down to the wider root base.
    /// </summary>
    internal static readonly Rectangle[] PineTreeCollisionBoxes =
    [
        new(33, 110, 15, 9),   // Narrow trunk above base
        new(25, 119, 31, 6),   // Main trunk base strip
        new(29, 115, 4, 4),    // Left bark edge
        new(49, 114, 3, 5),    // Right bark edge
        new(36, 125, 10, 3),   // Right root extension
    ];

    /// <summary>
    /// Collision boxes for birch trees relative to the 98×128 sprite.
    /// Multiple rectangles hug the birch trunk from the narrow mid-section
    /// down to the root flare.
    /// </summary>
    internal static readonly Rectangle[] BirchTreeCollisionBoxes =
    [
        new(33, 107, 15, 21),  // Main trunk column
        new(47, 112, 3, 8),    // Right bark edge
        new(51, 116, 3, 4),    // Far right bark
        new(47, 120, 10, 5),   // Right root flare
        new(25, 120, 8, 6),    // Left root flare
        new(29, 116, 4, 4),    // Left bark edge
    ];

    /// <summary>
    /// Collision boxes for dead tree variant 1 relative to the 95×127 sprite.
    /// Wide gnarled trunk with spreading base.
    /// </summary>
    internal static readonly Rectangle[] DeadTree1CollisionBoxes =
    [
        new(32, 113, 28, 15),  // Main trunk base
        new(28, 110, 8, 7),    // Left bark edge
        new(60, 112, 7, 5),    // Right bark edge
    ];

    /// <summary>
    /// Collision boxes for dead tree variant 2 relative to the 70×128 sprite.
    /// Narrow straight trunk.
    /// </summary>
    internal static readonly Rectangle[] DeadTree2CollisionBoxes =
    [
        new(24, 115, 20, 13),  // Main trunk base
        new(20, 112, 6, 5),    // Left bark edge
        new(43, 114, 5, 5),    // Right bark edge
    ];

    /// <summary>
    /// Collision boxes for dead tree variant 3 relative to the 97×128 sprite.
    /// Short stubby trunk with wide base.
    /// </summary>
    internal static readonly Rectangle[] DeadTree3CollisionBoxes =
    [
        new(24, 110, 48, 18),  // Main trunk base
        new(20, 106, 8, 8),    // Left bark edge
        new(68, 108, 8, 6),    // Right bark edge
    ];

    /// <summary>
    /// Collision boxes for dead tree variant 4 relative to the 91×128 sprite.
    /// Wide Y-shaped trunk.
    /// </summary>
    internal static readonly Rectangle[] DeadTree4CollisionBoxes =
    [
        new(30, 112, 34, 16),  // Main trunk base
        new(24, 109, 10, 6),   // Left bark edge
        new(62, 110, 8, 6),    // Right bark edge
    ];

    /// <summary>
    /// Collision boxes for deciduous tree variant 1 relative to the 137×128 sprite.
    /// Big wide oak canopy with thick trunk.
    /// </summary>
    internal static readonly Rectangle[] DeciduousTree1CollisionBoxes =
    [
        new(48, 108, 38, 20),  // Main trunk base
    ];

    /// <summary>
    /// Collision boxes for deciduous tree variant 2 relative to the 112×128 sprite.
    /// Medium oak with brown-green foliage.
    /// </summary>
    internal static readonly Rectangle[] DeciduousTree2CollisionBoxes =
    [
        new(40, 108, 30, 20),  // Main trunk base
    ];

    /// <summary>
    /// Collision boxes for deciduous tree variant 3 relative to the 77×128 sprite.
    /// Slender birch-like deciduous tree.
    /// </summary>
    internal static readonly Rectangle[] DeciduousTree3CollisionBoxes =
    [
        new(28, 112, 18, 16),  // Main trunk base
    ];

    /// <summary>
    /// Collision boxes for deciduous tree variant 4 relative to the 128×128 sprite.
    /// Round bushy shrub-tree.
    /// </summary>
    internal static readonly Rectangle[] DeciduousTree4CollisionBoxes =
    [
        new(46, 112, 32, 16),  // Main trunk base
    ];

    /// <summary>
    /// Collision boxes for bush variant 1 relative to the 32×31 sprite.
    /// Single rectangle covering the bottom body of the bush.
    /// </summary>
    internal static readonly Rectangle[] Bush1CollisionBoxes =
    [
        new(2, 10, 28, 21),
    ];

    /// <summary>
    /// Collision boxes for bush variant 2 relative to the 64×35 sprite.
    /// Single rectangle covering the bottom body of the wide bush.
    /// </summary>
    internal static readonly Rectangle[] Bush2CollisionBoxes =
    [
        new(4, 12, 56, 23),
    ];

    /// <summary>
    /// Collision boxes for bush variant 3 relative to the 32×33 sprite.
    /// Single rectangle covering the bottom body of the bush.
    /// </summary>
    internal static readonly Rectangle[] Bush3CollisionBoxes =
    [
        new(2, 11, 28, 22),
    ];

    /// <summary>
    /// Collision boxes for cozy lake cabins relative to the 160×109 sprite.
    /// Multiple rectangles form the building footprint, leaving the porch
    /// steps (bottom-right) walkable.
    /// </summary>
    internal static readonly Rectangle[] CozyCabinCollisionBoxes =
    [
        new(14, 57, 135, 20),  // Main wall strip across the front
        new(14, 73, 70, 18),   // Lower-left flower bed area
        new(3, 82, 15, 12),    // Left corner flower nook
        new(0, 89, 97, 20),    // Bottom-left flower bed
        new(121, 90, 40, 18),  // Bottom-right flower bed (before porch steps)
        new(127, 74, 29, 19),  // Right wall beside porch
    ];

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

            boulders.Add(new Boulder(placement.Position, boulderTexture, placement.SuppressOcclusion, placement.RotationRadians));
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

    internal static FrontDoor[] CreateFrontDoors(
        Texture2D closedTexture,
        Texture2D openTexture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var doors = new List<FrontDoor>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (placement.IsUnderwater)
            {
                continue;
            }

            if (string.Equals(placement.PropType, "front-door-closed", StringComparison.OrdinalIgnoreCase))
            {
                doors.Add(new FrontDoor(placement.Position, closedTexture, openTexture, startOpen: false, placement.SuppressOcclusion));
                continue;
            }

            if (string.Equals(placement.PropType, "front-door-open", StringComparison.OrdinalIgnoreCase))
            {
                doors.Add(new FrontDoor(placement.Position, closedTexture, openTexture, startOpen: true, placement.SuppressOcclusion));
            }
        }

        return doors.ToArray();
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
        bool reachesSurface = false,
        int collisionHeightPixels = 0,
        int targetWidthPixels = 0)
    {
        var props = new List<Boulder>(placements.Count);
        var scale = CalculateUniformScale(texture.Width, targetWidthPixels);
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

            props.Add(new Boulder(
                placement.Position,
                texture,
                rotationRadians: placement.RotationRadians,
                collisionHeightPixels: collisionHeightPixels,
                scale: scale));
        }

        return props.ToArray();
    }

    /// <summary>
    /// Creates garden gnome entities that hide behind the nearest tree when the player approaches.
    /// </summary>
    internal static GardenGnome[] CreateGardenGnomes(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        IReadOnlyList<Tree> trees)
    {
        var gnomes = new List<GardenGnome>();
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "garden-gnome", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater)
            {
                continue;
            }

            var hideTarget = FindNearestTreeCenter(placement.Position, texture, trees);
            gnomes.Add(new GardenGnome(placement.Position, texture, hideTarget, placement.RotationRadians));
        }

        return gnomes.ToArray();
    }

    private static Vector2 FindNearestTreeCenter(Vector2 gnomePosition, Texture2D gnomeTexture, IReadOnlyList<Tree> trees)
    {
        var gnomeCenter = gnomePosition + new Vector2(gnomeTexture.Width * 0.5f, gnomeTexture.Height * 0.5f);
        var bestDistSq = float.MaxValue;
        var bestCenter = gnomeCenter - Vector2.UnitX * 20f; // fallback: hide to the left

        for (var i = 0; i < trees.Count; i++)
        {
            var treeBounds = trees[i].Bounds;
            var treeCenter = new Vector2(
                treeBounds.X + treeBounds.Width * 0.5f,
                treeBounds.Y + treeBounds.Height * 0.5f);

            var distSq = Vector2.DistanceSquared(gnomeCenter, treeCenter);
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestCenter = treeCenter;
            }
        }

        return bestCenter;
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

    /// <summary>
    /// Creates area rug decorative floor props from placements with propType "area-rug".
    /// These are purely decorative — no collision — and always rendered at ground depth.
    /// </summary>
    internal static Boulder[] CreateAreaRugs(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var rugs = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "area-rug", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            rugs.Add(new Boulder(placement.Position, texture, suppressOcclusion: true, rotationRadians: placement.RotationRadians));
        }

        return rugs.ToArray();
    }

    /// <summary>
    /// Creates interactable couch props from placements with propType "old-couch".
    /// Blocks movement via full-texture collision bounds and supports a sit interaction.
    /// </summary>
    internal static Couch[] CreateCouches(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var couches = new List<Couch>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "old-couch", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            couches.Add(new Couch(placement.Position, texture, placement.RotationRadians));
        }

        return couches.ToArray();
    }

    /// <summary>
    /// Creates welcome mat decorative floor props from placements with propType "welcome-mat".
    /// These are purely decorative — no collision — and rendered below other props via Y-sort.
    /// </summary>
    internal static Boulder[] CreateWelcomeMats(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var mats = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "welcome-mat", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            mats.Add(new Boulder(placement.Position, texture, suppressOcclusion: true, rotationRadians: placement.RotationRadians));
        }

        return mats.ToArray();
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

            seaweeds.Add(new Boulder(placement.Position, variantTextures[variantIndex], rotationRadians: placement.RotationRadians));
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

    internal static Rectangle[] GetCouchBounds(Couch[] couches)
    {
        var bounds = new Rectangle[couches.Length];
        for (var i = 0; i < couches.Length; i++)
        {
            bounds[i] = couches[i].Bounds;
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

    internal static Tree[] CreateTrees(
        Texture2D texture,
        Rectangle[] localCollisionBoxes,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        string propType)
    {
        var trees = new List<Tree>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, propType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater)
            {
                continue;
            }

            trees.Add(new Tree(placement.Position, texture, localCollisionBoxes, placement.SuppressOcclusion));
        }

        return trees.ToArray();
    }

    /// <summary>
    /// Creates tree props from placements matching a numbered variant prefix
    /// (e.g. "dead-tree1"–"dead-tree4" or "deciduous-tree1"–"deciduous-tree4").
    /// Each variant uses its own texture and collision boxes.
    /// </summary>
    internal static Tree[] CreateVariantTrees(
        Texture2D[] variantTextures,
        Rectangle[][] variantCollisionBoxes,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        string propTypePrefix)
    {
        var trees = new List<Tree>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!placement.PropType.StartsWith(propTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater)
            {
                continue;
            }

            // Parse variant index from prop type (dead-tree1 → 0, dead-tree2 → 1, etc.)
            var lastChar = placement.PropType[^1];
            var variantIndex = lastChar - '1';
            if (variantIndex < 0 || variantIndex >= variantTextures.Length)
            {
                continue;
            }

            trees.Add(new Tree(placement.Position, variantTextures[variantIndex], variantCollisionBoxes[variantIndex], placement.SuppressOcclusion));
        }

        return trees.ToArray();
    }

    internal static Cabin[] CreateCabins(
        Texture2D texture,
        Rectangle[] localCollisionBoxes,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        string propType)
    {
        var cabins = new List<Cabin>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, propType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater)
            {
                continue;
            }

            cabins.Add(new Cabin(placement.Position, texture, localCollisionBoxes, placement.SuppressOcclusion));
        }

        return cabins.ToArray();
    }

    internal static Rectangle[] GetTreeCollisionBounds(Tree[] trees)
    {
        var totalCount = 0;
        for (var i = 0; i < trees.Length; i++)
        {
            totalCount += trees[i].CollisionBoxCount;
        }

        var bounds = new Rectangle[totalCount];
        var offset = 0;
        for (var i = 0; i < trees.Length; i++)
        {
            for (var j = 0; j < trees[i].CollisionBoxCount; j++)
            {
                bounds[offset++] = trees[i].GetCollisionBounds(j);
            }
        }

        return bounds;
    }

    internal static Rectangle[] GetCabinCollisionBounds(Cabin[] cabins)
    {
        var totalCount = 0;
        for (var i = 0; i < cabins.Length; i++)
        {
            totalCount += cabins[i].CollisionBoxCount;
        }

        var bounds = new Rectangle[totalCount];
        var offset = 0;
        for (var i = 0; i < cabins.Length; i++)
        {
            for (var j = 0; j < cabins[i].CollisionBoxCount; j++)
            {
                bounds[offset++] = cabins[i].GetCollisionBounds(j);
            }
        }

        return bounds;
    }
}
