#nullable enable

using Microsoft.Xna.Framework;

namespace DogDays.Game.World;

/// <summary>
/// A single navigable point within an indoor map, typically sourced from a Tiled object.
/// </summary>
public sealed class IndoorNavNode
{
    /// <summary>
    /// Unique identifier for this node (matches the Tiled object id).
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// World-space position of this node in pixels.
    /// </summary>
    public Vector2 Position { get; }

    /// <summary>
    /// Optional human-readable label (e.g. "kitchen", "doorway").
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Behavior hints such as "idle", "lounge", "entry".
    /// Empty array if none were specified.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Creates a new navigation node.
    /// </summary>
    /// <param name="id">Unique identifier from Tiled object id.</param>
    /// <param name="position">World-space position in pixels.</param>
    /// <param name="name">Optional human-readable label.</param>
    /// <param name="tags">
    /// Comma-separated behavior hints (e.g. "idle,lounge").
    /// Pass <c>null</c> or empty string for no tags.
    /// </param>
    public IndoorNavNode(int id, Vector2 position, string? name, string? tags)
    {
        Id = id;
        Position = position;
        Name = name;
        Tags = string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
    }
}
