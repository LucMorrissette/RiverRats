using System;
using RiverRats.Game.Systems;

namespace RiverRats.Game.Core;

/// <summary>
/// Shared runtime services that must survive screen replacement during a play session.
/// </summary>
internal sealed class GameSessionServices
{
    /// <summary>
    /// Creates a shared runtime service bundle.
    /// </summary>
    /// <param name="eventBus">Global gameplay event bus for the current session.</param>
    /// <param name="questManager">Quest manager that owns quest state for the current session.</param>
    internal GameSessionServices(GameEventBus eventBus, QuestManager questManager)
    {
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Quests = questManager ?? throw new ArgumentNullException(nameof(questManager));
    }

    /// <summary>Shared gameplay event bus.</summary>
    internal GameEventBus EventBus { get; }

    /// <summary>Shared quest manager.</summary>
    internal QuestManager Quests { get; }
}