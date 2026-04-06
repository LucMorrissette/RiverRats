#nullable enable

using System.Collections.Generic;
using RiverRats.Game.Core;
using RiverRats.Game.Data.Save;

namespace RiverRats.Tests.Helpers;

/// <summary>
/// In-memory fake of <see cref="ISaveGameService"/> for unit tests.
/// Records all save/delete operations for assertions.
/// </summary>
internal sealed class FakeSaveGameService : ISaveGameService
{
    private readonly Dictionary<int, SaveGameData> _slots = new();

    /// <inheritdoc />
    public int SlotCount => 3;

    /// <summary>Number of times <see cref="Save"/> was called.</summary>
    internal int SaveCallCount { get; private set; }

    /// <summary>Number of times <see cref="Delete"/> was called.</summary>
    internal int DeleteCallCount { get; private set; }

    /// <inheritdoc />
    public void Save(int slot, SaveGameData data)
    {
        SaveCallCount++;
        _slots[slot] = data;
    }

    /// <inheritdoc />
    public SaveGameData? Load(int slot)
    {
        return _slots.TryGetValue(slot, out var data) ? data : null;
    }

    /// <inheritdoc />
    public bool HasSave(int slot) => _slots.ContainsKey(slot);

    /// <inheritdoc />
    public void Delete(int slot)
    {
        DeleteCallCount++;
        _slots.Remove(slot);
    }
}
