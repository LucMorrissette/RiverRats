#nullable enable

namespace RiverRats.Game.Data;

/// <summary>
/// Immutable quest definition loaded from JSON.
/// </summary>
internal sealed class QuestDefinition
{
    /// <summary>Stable quest identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Player-facing quest title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Player-facing quest description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>When true, the quest starts as soon as definitions are loaded.</summary>
    public bool AutoStart { get; init; }

    /// <summary>
    /// Optional event condition that starts the quest when it is still in the not-started state.
    /// </summary>
    public QuestEventConditionDefinition? StartCondition { get; init; }

    /// <summary>Ordered linear objectives for the quest.</summary>
    public ObjectiveDefinition[] Objectives { get; init; } = [];
}