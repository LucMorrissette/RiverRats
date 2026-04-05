#nullable enable

using System;

namespace RiverRats.Game.Core;

/// <summary>
/// Lightweight gameplay event payload published through <see cref="GameEventBus"/>.
/// </summary>
internal readonly struct GameEvent
{
    /// <summary>
    /// Creates a gameplay event.
    /// </summary>
    /// <param name="type">High-level gameplay event kind.</param>
    /// <param name="targetId">Optional string identifier for the event target, such as an NPC id or map asset name.</param>
    /// <param name="amount">Amount contributed by the event. Values below one are clamped to one.</param>
    internal GameEvent(GameEventType type, string? targetId = null, int amount = 1)
    {
        Type = type;
        TargetId = string.IsNullOrWhiteSpace(targetId)
            ? null
            : targetId;
        Amount = Math.Max(1, amount);
    }

    /// <summary>High-level gameplay event kind.</summary>
    internal GameEventType Type { get; }

    /// <summary>Optional identifier for the event target.</summary>
    internal string? TargetId { get; }

    /// <summary>Amount contributed by the event.</summary>
    internal int Amount { get; }
}