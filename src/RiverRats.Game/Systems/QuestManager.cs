#nullable enable

using System;
using System.Collections.Generic;
using RiverRats.Game.Core;
using RiverRats.Game.Data;

namespace RiverRats.Game.Systems;

/// <summary>
/// Owns quest state for the current play session and advances quests in response to gameplay events.
/// </summary>
internal sealed class QuestManager
{
    private readonly List<QuestState> _availableQuests = new();
    private readonly Dictionary<string, QuestState> _questStatesById = new(StringComparer.Ordinal);
    private readonly List<QuestState> _questStatesInLoadOrder = new();
    private readonly List<QuestState> _activeQuests = new();
    private QuestState? _trackedQuest;

    /// <summary>
    /// Creates a quest manager bound to the supplied gameplay event bus.
    /// </summary>
    /// <param name="eventBus">Gameplay event bus to subscribe to.</param>
    internal QuestManager(GameEventBus eventBus)
    {
        ArgumentNullException.ThrowIfNull(eventBus);

        foreach (var eventType in Enum.GetValues<GameEventType>())
        {
            eventBus.Subscribe(eventType, HandleGameEvent);
        }
    }

    /// <summary>Fired when a quest starts.</summary>
    internal event Action<QuestState>? QuestStarted;

    /// <summary>Fired when an objective completes.</summary>
    internal event Action<QuestState, ObjectiveDefinition>? ObjectiveCompleted;

    /// <summary>Fired when a quest completes.</summary>
    internal event Action<QuestState>? QuestCompleted;

    /// <summary>Returns true after definitions have been loaded.</summary>
    internal bool IsInitialized { get; private set; }

    /// <summary>All known quests in definition load order.</summary>
    internal IReadOnlyList<QuestState> AllQuests => _questStatesInLoadOrder;

    /// <summary>All currently active quests.</summary>
    internal IReadOnlyList<QuestState> ActiveQuests => _activeQuests;

    /// <summary>All quests that have become visible to the player during the current session.</summary>
    internal IReadOnlyList<QuestState> AvailableQuests => _availableQuests;

    /// <summary>The quest currently selected for HUD tracking.</summary>
    internal QuestState? TrackedQuest => _trackedQuest;

    /// <summary>Fired when the tracked quest changes.</summary>
    internal event Action<QuestState?>? TrackedQuestChanged;

    /// <summary>
    /// Rebuilds the active/available/tracked lists from restored quest state.
    /// Call after <see cref="SaveGameMapper.RestoreQuests"/> has applied saved data.
    /// </summary>
    internal void RebuildListsFromRestoredState()
    {
        _activeQuests.Clear();
        _availableQuests.Clear();
        _trackedQuest = null;

        for (var i = 0; i < _questStatesInLoadOrder.Count; i++)
        {
            var quest = _questStatesInLoadOrder[i];
            if (quest.Status == QuestStatus.Active || quest.Status == QuestStatus.Completed)
            {
                _availableQuests.Add(quest);
            }

            if (quest.Status == QuestStatus.Active)
            {
                _activeQuests.Add(quest);
                _trackedQuest ??= quest;
            }
        }
    }

    /// <summary>
    /// Loads quest definitions and prepares their runtime state.
    /// </summary>
    /// <param name="definitions">Validated quest definitions in load order.</param>
    internal void LoadDefinitions(IEnumerable<QuestDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        if (IsInitialized)
        {
            throw new InvalidOperationException("Quest definitions have already been loaded for this session.");
        }

        foreach (var definition in definitions)
        {
            var questState = new QuestState(definition);
            _questStatesById.Add(definition.Id, questState);
            _questStatesInLoadOrder.Add(questState);
        }

        IsInitialized = true;

        for (var i = 0; i < _questStatesInLoadOrder.Count; i++)
        {
            if (_questStatesInLoadOrder[i].Definition.AutoStart)
            {
                StartQuest(_questStatesInLoadOrder[i].Definition.Id);
            }
        }
    }

    /// <summary>
    /// Returns the quest state for the supplied quest id, or null when the id is unknown.
    /// </summary>
    /// <param name="questId">Quest identifier to look up.</param>
    internal QuestState? GetQuest(string questId)
    {
        if (questId is null)
        {
            return null;
        }

        return _questStatesById.TryGetValue(questId, out var questState)
            ? questState
            : null;
    }

    /// <summary>
    /// Starts the supplied quest when it is still in the not-started state.
    /// </summary>
    /// <param name="questId">Quest identifier to start.</param>
    /// <returns>True when the quest transitioned into the active state.</returns>
    internal bool StartQuest(string questId)
    {
        var questState = GetQuest(questId);
        if (questState is null || !questState.Start())
        {
            return false;
        }

        _availableQuests.Add(questState);
        _activeQuests.Add(questState);

        if (_trackedQuest is null)
        {
            SetTrackedQuest(questState.Definition.Id);
        }

        QuestStarted?.Invoke(questState);
        return true;
    }

    /// <summary>
    /// Sets the tracked quest to any quest that has already become available.
    /// </summary>
    /// <param name="questId">Quest id to track, or null to clear tracking.</param>
    /// <returns>True when the tracked quest changed successfully.</returns>
    internal bool SetTrackedQuest(string? questId)
    {
        if (questId is null)
        {
            return SetTrackedQuestInternal(null);
        }

        var questState = GetQuest(questId);
        if (questState is null || questState.Status == QuestStatus.NotStarted)
        {
            return false;
        }

        return SetTrackedQuestInternal(questState);
    }

    private void HandleGameEvent(GameEvent gameEvent)
    {
        if (!IsInitialized)
        {
            return;
        }

        StartTriggeredQuests(gameEvent);
        AdvanceActiveQuests(gameEvent);
    }

    private void StartTriggeredQuests(GameEvent gameEvent)
    {
        for (var i = 0; i < _questStatesInLoadOrder.Count; i++)
        {
            var questState = _questStatesInLoadOrder[i];
            if (questState.Status != QuestStatus.NotStarted)
            {
                continue;
            }

            var startCondition = questState.Definition.StartCondition;
            if (startCondition is null || !startCondition.Matches(gameEvent))
            {
                continue;
            }

            StartQuest(questState.Definition.Id);
        }
    }

    private void AdvanceActiveQuests(GameEvent gameEvent)
    {
        for (var i = 0; i < _activeQuests.Count; i++)
        {
            var questState = _activeQuests[i];
            var completedObjective = questState.ApplyEvent(gameEvent);
            if (completedObjective is not null)
            {
                ObjectiveCompleted?.Invoke(questState, completedObjective);
            }

            if (questState.Status == QuestStatus.Completed)
            {
                _activeQuests.RemoveAt(i);
                if (_trackedQuest == questState)
                {
                    var replacementTrackedQuest = FindReplacementTrackedQuest();
                    SetTrackedQuestInternal(replacementTrackedQuest);
                }

                i -= 1;
                QuestCompleted?.Invoke(questState);
            }
        }
    }

    private QuestState? FindReplacementTrackedQuest()
    {
        for (var i = 0; i < _activeQuests.Count; i++)
        {
            if (_activeQuests[i].Status == QuestStatus.Active)
            {
                return _activeQuests[i];
            }
        }

        return null;
    }

    private bool SetTrackedQuestInternal(QuestState? questState)
    {
        if (_trackedQuest == questState)
        {
            return false;
        }

        _trackedQuest = questState;
        TrackedQuestChanged?.Invoke(_trackedQuest);
        return true;
    }
}