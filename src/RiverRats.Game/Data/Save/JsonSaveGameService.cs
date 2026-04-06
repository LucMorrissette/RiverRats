#nullable enable

using System;
using System.IO;
using System.Text.Json;
using RiverRats.Game.Core;
using RiverRats.Game.Data.Save;

namespace RiverRats.Game.Data.Save;

/// <summary>
/// Persists <see cref="SaveGameData"/> as JSON files in the user's AppData folder.
/// Writes atomically via a temp file + rename to prevent corruption on crash.
/// </summary>
internal sealed class JsonSaveGameService : ISaveGameService
{
    private const string AppFolderName = "RiverRats";
    private const string SavesFolderName = "saves";
    private const string SlotFilePrefix = "slot_";
    private const string SlotFileExtension = ".json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _saveDirectoryPath;

    /// <summary>
    /// Creates a save service writing to the default AppData location.
    /// </summary>
    public JsonSaveGameService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName,
            SavesFolderName))
    {
    }

    /// <summary>
    /// Creates a save service writing to a custom directory. Useful for tests.
    /// </summary>
    /// <param name="saveDirectoryPath">Absolute path to the saves folder.</param>
    public JsonSaveGameService(string saveDirectoryPath)
    {
        _saveDirectoryPath = saveDirectoryPath ?? throw new ArgumentNullException(nameof(saveDirectoryPath));
    }

    /// <inheritdoc />
    public int SlotCount => 3;

    /// <inheritdoc />
    public void Save(int slot, SaveGameData data)
    {
        ValidateSlot(slot);
        ArgumentNullException.ThrowIfNull(data);

        Directory.CreateDirectory(_saveDirectoryPath);

        var targetPath = GetSlotPath(slot);
        var tempPath = targetPath + ".tmp";

        var json = JsonSerializer.Serialize(data, SerializerOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, targetPath, overwrite: true);
    }

    /// <inheritdoc />
    public SaveGameData? Load(int slot)
    {
        ValidateSlot(slot);

        var path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            return null;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SaveGameData>(json, DeserializerOptions);
    }

    /// <inheritdoc />
    public bool HasSave(int slot)
    {
        ValidateSlot(slot);
        return File.Exists(GetSlotPath(slot));
    }

    /// <inheritdoc />
    public void Delete(int slot)
    {
        ValidateSlot(slot);
        var path = GetSlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetSlotPath(int slot)
    {
        return Path.Combine(_saveDirectoryPath, $"{SlotFilePrefix}{slot}{SlotFileExtension}");
    }

    private void ValidateSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot,
                $"Slot must be between 0 and {SlotCount - 1}.");
        }
    }
}
