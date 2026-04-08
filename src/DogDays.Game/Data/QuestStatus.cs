namespace DogDays.Game.Data;

/// <summary>
/// Lifecycle state of a quest in the current play session.
/// </summary>
internal enum QuestStatus
{
    /// <summary>The quest exists but has not started yet.</summary>
    NotStarted,

    /// <summary>The quest is currently tracking progress.</summary>
    Active,

    /// <summary>The quest's final objective has been completed.</summary>
    Completed,

    /// <summary>The quest has failed.</summary>
    Failed,
}