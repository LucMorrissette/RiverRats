using Microsoft.Xna.Framework;

namespace DogDays.Game.Data;

/// <summary>
/// A named spawn point parsed from a TMX SpawnPoints object layer.
/// </summary>
/// <param name="Name">Unique name identifying this spawn point (e.g., "from-starter", "default").</param>
/// <param name="Position">World-space position in pixels.</param>
public readonly record struct SpawnPointData(string Name, Vector2 Position);
