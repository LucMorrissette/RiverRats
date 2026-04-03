using RiverRats.Game.Data;

namespace RiverRats.Game.Data;

/// <summary>
/// Immutable per-map configuration that controls which gameplay systems are active.
/// Replaces the five <c>string switch</c> helper methods that lived in <c>GameplayScreen</c>:
/// <c>GetSongForMap</c>, <c>GetFollowerConfigForMap</c>, <c>HasDayNightCycle</c>,
/// <c>HasCloudShadows</c>, and <c>HasAmbientFireflies</c>.
/// </summary>
/// <param name="SongName">Content asset name for the background music track.</param>
/// <param name="FollowerConfig">Follower movement tuning for this map.</param>
/// <param name="HasDayNightCycle">Whether the day/night lighting cycle runs on this map.</param>
/// <param name="HasCloudShadows">Whether cloud shadow rendering is active on this map.</param>
/// <param name="HasAmbientFireflies">Whether ambient fireflies spawn on this map.</param>
public sealed record MapConfig(
    string SongName,
    FollowerMovementConfig FollowerConfig,
    bool HasDayNightCycle,
    bool HasCloudShadows,
    bool HasAmbientFireflies)
{
    // ── Known map asset names ───────────────────────────────────────────────
    private const string WoodsMap = "Maps/WoodsBehindCabin";
    private const string CabinIndoorsMap = "Maps/CabinIndoors";

    // ── Follower config variants ────────────────────────────────────────────
    private static readonly FollowerMovementConfig DefaultFollower = FollowerMovementConfig.Default;

    /// <summary>Follower trails farther behind the player in the combat forest map.</summary>
    private static readonly FollowerMovementConfig WoodsFollower = FollowerMovementConfig.Default with
    {
        FollowDistancePixels = 60f,
    };

    // ── Factory ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="MapConfig"/> for the given content asset name,
    /// falling back to sensible defaults for unknown maps.
    /// </summary>
    public static MapConfig ForMap(string mapAssetName) => mapAssetName switch
    {
        WoodsMap => new MapConfig(
            SongName: "WoodsBehindCabinTheme",
            FollowerConfig: WoodsFollower,
            HasDayNightCycle: false,
            HasCloudShadows: true,
            HasAmbientFireflies: true),

        CabinIndoorsMap => new MapConfig(
            SongName: "CabinIndoorsTheme",
            FollowerConfig: DefaultFollower,
            HasDayNightCycle: true,
            HasCloudShadows: false,
            HasAmbientFireflies: false),

        _ => new MapConfig(
            SongName: "GameplayTheme",
            FollowerConfig: DefaultFollower,
            HasDayNightCycle: true,
            HasCloudShadows: true,
            HasAmbientFireflies: true),
    };
}
