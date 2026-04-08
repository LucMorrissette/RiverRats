using Microsoft.Xna.Framework;

namespace DogDays.Game.Data;

/// <summary>
/// A zone transition trigger parsed from a TMX ZoneTriggers object layer.
/// </summary>
/// <param name="Bounds">World-space rectangle the player must overlap to trigger the transition.</param>
/// <param name="TargetMap">Content asset name for the destination map (e.g., "Maps/WoodsBehindCabin").</param>
/// <param name="TargetSpawnId">Name of the spawn point in the destination map where the player should appear.</param>
public readonly record struct ZoneTriggerData(Rectangle Bounds, string TargetMap, string TargetSpawnId);
