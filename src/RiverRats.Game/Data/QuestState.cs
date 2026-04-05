#nullable enable

using RiverRats.Game.Core;

namespace RiverRats.Game.Data;

/// <summary>
/// Mutable runtime state for a single quest definition.
/// </summary>
internal sealed class QuestState
{
    private readonly int[] _objectiveProgress;

    /// <summary>
    /// Creates runtime state for the supplied quest definition.
    /// </summary>
    /// <param name="definition">Quest definition to track.</param>
    internal QuestState(QuestDefinition definition)
    {
        Definition = definition;
        _objectiveProgress = new int[definition.Objectives.Length];
    }

    /// <summary>Quest definition this state instance tracks.</summary>
    internal QuestDefinition Definition { get; }

    /// <summary>Current quest lifecycle state.</summary>
    internal QuestStatus Status { get; private set; } = QuestStatus.NotStarted;

    /// <summary>Zero-based index of the objective currently being progressed.</summary>
    internal int CurrentObjectiveIndex { get; private set; }

    /// <summary>Returns the currently active objective, or null when the quest is not active.</summary>
    internal ObjectiveDefinition? CurrentObjective =>
        Status == QuestStatus.Active && CurrentObjectiveIndex < Definition.Objectives.Length
            ? Definition.Objectives[CurrentObjectiveIndex]
            : null;

    /// <summary>Current progress value for the active objective.</summary>
    internal int CurrentObjectiveProgress => CurrentObjective is null
        ? 0
        : _objectiveProgress[CurrentObjectiveIndex];

    /// <summary>Required count for the active objective.</summary>
    internal int CurrentObjectiveRequiredCount => CurrentObjective?.Completion.RequiredCount ?? 0;

    /// <summary>
    /// Returns the stored progress value for the objective at the supplied index.
    /// </summary>
    /// <param name="objectiveIndex">Zero-based objective index.</param>
    internal int GetObjectiveProgress(int objectiveIndex)
    {
        return _objectiveProgress[objectiveIndex];
    }

    /// <summary>
    /// Moves the quest into the active state.
    /// </summary>
    internal bool Start()
    {
        if (Status != QuestStatus.NotStarted)
        {
            return false;
        }

        Status = QuestStatus.Active;
        return true;
    }

    /// <summary>
    /// Applies a gameplay event to the current objective.
    /// </summary>
    /// <param name="gameEvent">Gameplay event to evaluate.</param>
    /// <returns>The objective that completed from this event, or null when no objective completed.</returns>
    internal ObjectiveDefinition? ApplyEvent(GameEvent gameEvent)
    {
        var currentObjective = CurrentObjective;
        if (currentObjective is null)
        {
            return null;
        }

        var completion = currentObjective.Completion;
        if (!completion.Matches(gameEvent))
        {
            return null;
        }

        var currentProgress = _objectiveProgress[CurrentObjectiveIndex];
        var nextProgress = currentProgress + gameEvent.Amount;
        if (nextProgress > completion.RequiredCount)
        {
            nextProgress = completion.RequiredCount;
        }

        if (nextProgress == currentProgress)
        {
            return null;
        }

        _objectiveProgress[CurrentObjectiveIndex] = nextProgress;
        if (nextProgress < completion.RequiredCount)
        {
            return null;
        }

        CurrentObjectiveIndex += 1;
        if (CurrentObjectiveIndex >= Definition.Objectives.Length)
        {
            Status = QuestStatus.Completed;
        }

        return currentObjective;
    }
}