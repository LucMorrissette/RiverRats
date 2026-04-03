namespace RiverRats.Game.World;

/// <summary>
/// Deterministic, position-stable tile variant selection logic.
/// Each terrain type uses independent hash seeds so variant patterns don't
/// correlate across terrain types sharing the same tile coordinates.
/// Extracted from <see cref="TiledWorldRenderer"/>.
/// </summary>
internal static class TileVariantPicker
{
    // ── Grass ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0-based variant index for a grass tile at <paramref name="x"/>, <paramref name="y"/>.
    /// Six variants, weighted so plain grass is most common and rare variants appear infrequently.
    /// </summary>
    internal static int PickGrassVariantIndex(int x, int y)
    {
        var roll = GetDeterministicPercentRoll(x, y);

        if (roll < 32) return 0;
        if (roll < 54) return 1;
        if (roll < 79) return 2;
        if (roll < 98) return 3;
        if (roll < 99) return 4;
        return 5;
    }

    private static int GetDeterministicPercentRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ 0x9E3779B9u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (int)(hash % 100u);
        }
    }

    // ── Sand ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0-based variant index for a sand tile.
    /// Three equal-weight variants.
    /// </summary>
    internal static int PickSandVariantIndex(int x, int y)
    {
        var roll = GetDeterministicSandRoll(x, y);
        if (roll < 34) return 0;
        if (roll < 67) return 1;
        return 2;
    }

    private static int GetDeterministicSandRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 83492791u) ^ ((uint)y * 27644437u) ^ 0x517CC1B7u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (int)(hash % 100u);
        }
    }

    // ── Wood floor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0-based variant index for a wood-floor tile.
    /// Uniformly distributed across all available variants.
    /// </summary>
    internal static int PickWoodFloorVariantIndex(int x, int y, int variantCount)
    {
        if (variantCount <= 0) return 0;

        unchecked
        {
            var hash = ((uint)x * 83492791u) ^ ((uint)y * 2971215073u) ^ 0x7F4A7C15u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (int)(hash % (uint)variantCount);
        }
    }

    // ── Riverbed ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0-based variant index for a riverbed tile.
    /// Uses region-based category assignment to create natural clustering,
    /// with occasional accent tiles from a neighbouring category.
    /// </summary>
    internal static int PickRiverbedVariantIndex(int x, int y, int variantCount)
    {
        if (variantCount <= 0) return 0;

        const int categoryCount = 4;
        var regionCategory = GetDeterministicRiverbedRegionCategory(x, y);
        var localVariantCount = (variantCount + categoryCount - 1) / categoryCount;
        var preferredVariant = (GetDeterministicRiverbedLocalRoll(x, y) % localVariantCount) * categoryCount;
        preferredVariant += regionCategory;

        if (preferredVariant >= variantCount)
        {
            preferredVariant = regionCategory % variantCount;
        }

        var accentRoll = GetDeterministicRiverbedAccentRoll(x, y);
        if (accentRoll < 72)
        {
            return preferredVariant;
        }

        var neighboringCategory = (regionCategory + 1 + (accentRoll % 3)) % categoryCount;
        var neighboringVariant = (GetDeterministicRiverbedNeighborRoll(x, y) % localVariantCount) * categoryCount;
        neighboringVariant += neighboringCategory;

        if (neighboringVariant >= variantCount)
        {
            neighboringVariant = neighboringCategory % variantCount;
        }

        return neighboringVariant;
    }

    private static int GetDeterministicRiverbedRegionCategory(int x, int y)
    {
        unchecked
        {
            var regionX = x / 4;
            var regionY = y / 3;
            var hash = ((uint)regionX * 2246822519u) ^ ((uint)regionY * 3266489917u) ^ 0xC2B2AE35u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (int)(hash % 4u);
        }
    }

    private static int GetDeterministicRiverbedLocalRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 668265263u) ^ ((uint)y * 2147483647u) ^ 0x165667B1u;
            hash ^= hash >> 13;
            hash *= 2246822519u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static int GetDeterministicRiverbedAccentRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 374761393u) ^ ((uint)y * 1103515245u) ^ 0x85EBCA77u;
            hash ^= hash >> 15;
            hash *= 1274126177u;
            hash ^= hash >> 14;
            return (int)(hash % 100u);
        }
    }

    private static int GetDeterministicRiverbedNeighborRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 1597334677u) ^ ((uint)y * 3812015801u) ^ 0x27D4EB2Fu;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    // ── Shoreline ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a 0-based variant index for a shoreline tile.
    /// Uniformly distributed across all available variants using a distinct seed.
    /// </summary>
    internal static int PickShorelineVariantIndex(int x, int y, int variantCount)
    {
        if (variantCount <= 0) return 0;

        return GetDeterministicShorelineRoll(x, y) % variantCount;
    }

    private static int GetDeterministicShorelineRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 2654435761u) ^ ((uint)y * 2246822519u) ^ 0xA136AAABu;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
