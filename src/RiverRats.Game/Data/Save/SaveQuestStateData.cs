namespace RiverRats.Game.Data.Save;

/// <summary>
/// Snapshot of a single quest's runtime state for save/load.
/// </summary>
internal sealed class SaveQuestStateData
{
    /// <summary>Stable quest identifier matching <see cref="QuestDefinition.Id"/>.</summary>
    public string QuestId { get; set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public QuestStatus Status { get; set; }

    /// <summary>Zero-based index of the active objective.</summary>
    public int CurrentObjectiveIndex { get; set; }

    /// <summary>Progress counters for each objective (array length matches objective count).</summary>
    public int[] ObjectiveProgress { get; set; } = [];
}
