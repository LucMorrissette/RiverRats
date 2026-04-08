#nullable enable

using System;
using DogDays.Game.Core;

namespace DogDays.Game.Data;

/// <summary>
/// Event requirement used by quest start triggers and objective completion checks.
/// </summary>
internal sealed class QuestEventConditionDefinition
{
    /// <summary>The gameplay event type that satisfies the condition.</summary>
    public GameEventType EventType { get; init; }

    /// <summary>
    /// Optional identifier the event must target. Null or empty means any target is accepted.
    /// </summary>
    public string? TargetId { get; init; }

    /// <summary>The amount required before the condition is satisfied.</summary>
    public int RequiredCount { get; init; } = 1;

    /// <summary>
    /// Returns true when the supplied gameplay event matches this requirement's type and target.
    /// </summary>
    /// <param name="gameEvent">Gameplay event to test.</param>
    internal bool Matches(GameEvent gameEvent)
    {
        if (EventType != gameEvent.Type)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetId))
        {
            return true;
        }

        return string.Equals(TargetId, gameEvent.TargetId, StringComparison.Ordinal);
    }
}