using System;
using System.IO;
using RiverRats.Game.Data;
using RiverRats.Game.Data.Save;

namespace RiverRats.Tests.Unit;

public sealed class JsonSaveGameServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly JsonSaveGameService _service;

    public JsonSaveGameServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "RiverRatsTests", Guid.NewGuid().ToString("N"));
        _service = new JsonSaveGameService(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Save__CreatesJsonFile()
    {
        var data = CreateTestSaveData();

        _service.Save(0, data);

        Assert.True(_service.HasSave(0));
        Assert.True(File.Exists(Path.Combine(_testDir, "slot_0.json")));
    }

    [Fact]
    public void Load__ReturnsNullForEmptySlot()
    {
        var result = _service.Load(0);

        Assert.Null(result);
    }

    [Fact]
    public void HasSave__ReturnsFalseForEmptySlot()
    {
        Assert.False(_service.HasSave(0));
    }

    [Fact]
    public void SaveAndLoad__RoundTrip__PreservesData()
    {
        var original = CreateTestSaveData();

        _service.Save(1, original);
        var loaded = _service.Load(1);

        Assert.NotNull(loaded);
        Assert.Equal(SaveGameData.CurrentVersion, loaded.Version);
        Assert.Equal(100.5f, loaded.Player.X);
        Assert.Equal(200.5f, loaded.Player.Y);
        Assert.Equal(FacingDirection.Right, loaded.Player.Facing);
        Assert.Equal("Maps/StarterMap", loaded.Player.ZoneMapAssetName);
        Assert.Equal(0.6f, loaded.DayNight.CycleProgress);
        Assert.Equal(8, loaded.CombatStats.MaxHp);
        Assert.Equal(3, loaded.CombatStats.Level);
        Assert.Single(loaded.Watercraft);
        Assert.Equal(FacingDirection.Up, loaded.Watercraft[0].Facing);
    }

    [Fact]
    public void SaveAndLoad__RoundTrip__PreservesQuestData()
    {
        var original = CreateTestSaveData();

        _service.Save(0, original);
        var loaded = _service.Load(0);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Quests);
        Assert.Equal("quest_01", loaded.Quests[0].QuestId);
        Assert.Equal(QuestStatus.Active, loaded.Quests[0].Status);
        Assert.Equal(1, loaded.Quests[0].CurrentObjectiveIndex);
        Assert.Equal(new[] { 3, 0 }, loaded.Quests[0].ObjectiveProgress);
    }

    [Fact]
    public void Delete__RemovesSaveFile()
    {
        _service.Save(0, CreateTestSaveData());
        Assert.True(_service.HasSave(0));

        _service.Delete(0);

        Assert.False(_service.HasSave(0));
        Assert.Null(_service.Load(0));
    }

    [Fact]
    public void Delete__NoOpForEmptySlot()
    {
        // Should not throw.
        _service.Delete(2);
    }

    [Fact]
    public void Save__OverwritesExistingSlot()
    {
        _service.Save(0, CreateTestSaveData());

        var updated = CreateTestSaveData();
        updated.Player.X = 999f;
        _service.Save(0, updated);

        var loaded = _service.Load(0);
        Assert.Equal(999f, loaded!.Player.X);
    }

    [Fact]
    public void Save__ThrowsForInvalidSlot()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Save(-1, CreateTestSaveData()));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Save(3, CreateTestSaveData()));
    }

    [Fact]
    public void Load__ThrowsForInvalidSlot()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Load(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.Load(3));
    }

    [Fact]
    public void Save__MultipleSlots__Independent()
    {
        var data0 = CreateTestSaveData();
        data0.Player.X = 10f;
        var data1 = CreateTestSaveData();
        data1.Player.X = 20f;
        var data2 = CreateTestSaveData();
        data2.Player.X = 30f;

        _service.Save(0, data0);
        _service.Save(1, data1);
        _service.Save(2, data2);

        Assert.Equal(10f, _service.Load(0)!.Player.X);
        Assert.Equal(20f, _service.Load(1)!.Player.X);
        Assert.Equal(30f, _service.Load(2)!.Player.X);
    }

    private static SaveGameData CreateTestSaveData()
    {
        return new SaveGameData
        {
            Version = SaveGameData.CurrentVersion,
            SavedAtUtc = DateTime.UtcNow,
            Player = new SavePlayerData
            {
                X = 100.5f,
                Y = 200.5f,
                Facing = FacingDirection.Right,
                ZoneMapAssetName = "Maps/StarterMap",
            },
            Quests = new[]
            {
                new SaveQuestStateData
                {
                    QuestId = "quest_01",
                    Status = QuestStatus.Active,
                    CurrentObjectiveIndex = 1,
                    ObjectiveProgress = new[] { 3, 0 },
                },
            },
            CombatStats = new SaveCombatStatsData
            {
                MaxHp = 8,
                Level = 3,
                Xp = 15,
                XpToNextLevel = 22,
                SpeedMultiplier = 1.06f,
                CooldownMultiplier = 0.78f,
                ProjectileSpeedMultiplier = 1.10f,
                ProjectileRangeMultiplier = 1.20f,
            },
            DayNight = new SaveDayNightData
            {
                CycleProgress = 0.6f,
            },
            Watercraft = new[]
            {
                new SaveWatercraftData
                {
                    MapAssetName = "Maps/StarterMap",
                    InitialX = 100f,
                    InitialY = 100f,
                    CenterX = 110.5f,
                    CenterY = 132f,
                    Facing = FacingDirection.Up,
                    IsOccupied = false,
                },
            },
        };
    }
}
