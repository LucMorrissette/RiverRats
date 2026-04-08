using System;
using System.Collections.Generic;
using DogDays.Game.Systems;

namespace DogDays.Game.Core;

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
    /// <param name="saveGameService">Save game persistence service.</param>
    internal GameSessionServices(GameEventBus eventBus, QuestManager questManager, ISaveGameService saveGameService)
    {
        EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        Quests = questManager ?? throw new ArgumentNullException(nameof(questManager));
        SaveGame = saveGameService ?? throw new ArgumentNullException(nameof(saveGameService));
    }

    /// <summary>Shared gameplay event bus.</summary>
    internal GameEventBus EventBus { get; }

    /// <summary>Shared quest manager.</summary>
    internal QuestManager Quests { get; }

    /// <summary>Save game persistence service.</summary>
    internal ISaveGameService SaveGame { get; }

    /// <summary>Per-map mutable watercraft state that survives screen replacement.</summary>
    internal Dictionary<string, Data.Save.SaveWatercraftData[]> WatercraftStatesByMap { get; } = new(StringComparer.Ordinal);

    /// <summary>Zero-based slot index used for the most recent manual save. Defaults to slot 1.</summary>
    internal int LastUsedSaveSlot { get; set; } = 1;
}