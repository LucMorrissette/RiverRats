namespace RiverRats.Game.Data;

/// <summary>
/// Immutable quest objective definition loaded from JSON.
/// </summary>
internal sealed class ObjectiveDefinition
{
    /// <summary>Stable objective identifier within a quest.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Player-facing objective text.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Event condition that completes the objective.</summary>
    public QuestEventConditionDefinition Completion { get; init; } = new();
}