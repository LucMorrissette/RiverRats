#nullable enable

using RiverRats.Game.Data.Save;

namespace RiverRats.Game.Core;

/// <summary>
/// Abstraction for persisting and loading save game data.
/// Implementations handle the underlying storage mechanism (file system, cloud, etc.).
/// </summary>
internal interface ISaveGameService
{
    /// <summary>Maximum number of save slots supported.</summary>
    int SlotCount { get; }

    /// <summary>
    /// Persists save data to the given slot.
    /// </summary>
    /// <param name="slot">Zero-based slot index.</param>
    /// <param name="data">Save data to write.</param>
    void Save(int slot, SaveGameData data);

    /// <summary>
    /// Loads save data from the given slot, or returns null when the slot is empty.
    /// </summary>
    /// <param name="slot">Zero-based slot index.</param>
    SaveGameData? Load(int slot);

    /// <summary>
    /// Returns true when the given slot contains a save file.
    /// </summary>
    /// <param name="slot">Zero-based slot index.</param>
    bool HasSave(int slot);

    /// <summary>
    /// Deletes the save file in the given slot, if it exists.
    /// </summary>
    /// <param name="slot">Zero-based slot index.</param>
    void Delete(int slot);
}
