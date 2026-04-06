using Microsoft.Xna.Framework;
using RiverRats.Data;
using RiverRats.Game.Core;
using RiverRats.Game.Data;
using RiverRats.Game.Data.Save;
using RiverRats.Game.Systems;

namespace RiverRats.Tests.Unit;

public sealed class SaveGameMapperTests
{
    private static QuestManager CreateQuestManagerWithDefinitions()
    {
        var eventBus = new GameEventBus();
        var manager = new QuestManager(eventBus);
        var definitions = new[]
        {
            new QuestDefinition
            {
                Id = "quest_01",
                Title = "Test Quest 1",
                AutoStart = true,
                Objectives = new[]
                {
                    new ObjectiveDefinition
                    {
                        Description = "Kill 3 gnomes",
                        Completion = new QuestEventConditionDefinition
                        {
                            EventType = GameEventType.EnemyKilled,
                            RequiredCount = 3,
                        },
                    },
                    new ObjectiveDefinition
                    {
                        Description = "Enter zone",
                        Completion = new QuestEventConditionDefinition
                        {
                            EventType = GameEventType.ZoneEntered,
                            RequiredCount = 1,
                        },
                    },
                },
            },
            new QuestDefinition
            {
                Id = "quest_02",
                Title = "Test Quest 2",
                Objectives = new[]
                {
                    new ObjectiveDefinition
                    {
                        Description = "Talk to NPC",
                        Completion = new QuestEventConditionDefinition
                        {
                            EventType = GameEventType.NpcTalkedTo,
                            RequiredCount = 1,
                        },
                    },
                },
            },
        };
        manager.LoadDefinitions(definitions);
        return manager;
    }

    [Fact]
    public void Capture__CapturesPlayerPositionAndFacing()
    {
        var position = new Vector2(123.5f, 456.7f);
        var facing = FacingDirection.Left;
        var zone = "Maps/StarterMap";
        var questManager = CreateQuestManagerWithDefinitions();

        var data = SaveGameMapper.Capture(position, facing, zone, questManager, null, 0.5f, []);

        Assert.Equal(123.5f, data.Player.X);
        Assert.Equal(456.7f, data.Player.Y);
        Assert.Equal(FacingDirection.Left, data.Player.Facing);
        Assert.Equal("Maps/StarterMap", data.Player.ZoneMapAssetName);
    }

    [Fact]
    public void Capture__CapturesVersion()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/StarterMap", questManager, null, 0f, []);

        Assert.Equal(SaveGameData.CurrentVersion, data.Version);
    }

    [Fact]
    public void Capture__CapturesDayNightProgress()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/StarterMap", questManager, null, 0.75f, []);

        Assert.Equal(0.75f, data.DayNight.CycleProgress);
    }

    [Fact]
    public void Capture__CapturesQuestState()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        // quest_01 was auto-started, advance it with 2 kills
        var eventBus = new GameEventBus();
        questManager.GetQuest("quest_01")!
            .ApplyEvent(new GameEvent(GameEventType.EnemyKilled, null, 2));

        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/StarterMap", questManager, null, 0f, []);

        Assert.Equal(2, data.Quests.Length);

        var quest1 = data.Quests[0];
        Assert.Equal("quest_01", quest1.QuestId);
        Assert.Equal(QuestStatus.Active, quest1.Status);
        Assert.Equal(0, quest1.CurrentObjectiveIndex);
        Assert.Equal(2, quest1.ObjectiveProgress[0]);

        var quest2 = data.Quests[1];
        Assert.Equal("quest_02", quest2.QuestId);
        Assert.Equal(QuestStatus.NotStarted, quest2.Status);
    }

    [Fact]
    public void Capture__CapturesCombatStats()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var stats = new PlayerCombatStats
        {
            MaxHp = 8,
            Level = 3,
            Xp = 15,
            XpToNextLevel = 22,
            SpeedMultiplier = 1.06f,
            CooldownMultiplier = 0.78f,
            ProjectileSpeedMultiplier = 1.10f,
            ProjectileRangeMultiplier = 1.20f,
        };

        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/WoodsBehindCabin", questManager, stats, 0f, []);

        Assert.Equal(8, data.CombatStats.MaxHp);
        Assert.Equal(3, data.CombatStats.Level);
        Assert.Equal(15, data.CombatStats.Xp);
        Assert.Equal(22, data.CombatStats.XpToNextLevel);
        Assert.Equal(1.06f, data.CombatStats.SpeedMultiplier);
        Assert.Equal(0.78f, data.CombatStats.CooldownMultiplier);
        Assert.Equal(1.10f, data.CombatStats.ProjectileSpeedMultiplier);
        Assert.Equal(1.20f, data.CombatStats.ProjectileRangeMultiplier);
    }

    [Fact]
    public void Capture__CapturesDefaultsWhenCombatStatsNull()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/StarterMap", questManager, null, 0f, []);

        Assert.Equal(5, data.CombatStats.MaxHp);
        Assert.Equal(1, data.CombatStats.Level);
    }

    [Fact]
    public void Capture__CapturesWatercraftStates()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var watercraftStates = new[]
        {
            new SaveWatercraftData
            {
                MapAssetName = "Maps/StarterMap",
                InitialX = 100f,
                InitialY = 100f,
                CenterX = 110.5f,
                CenterY = 132f,
                Facing = FacingDirection.Right,
                IsOccupied = true,
            },
        };

        var data = SaveGameMapper.Capture(Vector2.Zero, FacingDirection.Down, "Maps/StarterMap", questManager, null, 0f, watercraftStates);

        Assert.Single(data.Watercraft);
        Assert.Equal("Maps/StarterMap", data.Watercraft[0].MapAssetName);
        Assert.Equal(FacingDirection.Right, data.Watercraft[0].Facing);
        Assert.True(data.Watercraft[0].IsOccupied);
    }

    [Fact]
    public void RestoreQuests__RestoresStatusAndProgress()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var savedData = new SaveGameData
        {
            Quests = new[]
            {
                new SaveQuestStateData
                {
                    QuestId = "quest_01",
                    Status = QuestStatus.Active,
                    CurrentObjectiveIndex = 1,
                    ObjectiveProgress = new[] { 3, 0 },
                },
                new SaveQuestStateData
                {
                    QuestId = "quest_02",
                    Status = QuestStatus.Completed,
                    CurrentObjectiveIndex = 1,
                    ObjectiveProgress = new[] { 1 },
                },
            },
        };

        SaveGameMapper.RestoreQuests(savedData, questManager);

        var quest1 = questManager.GetQuest("quest_01")!;
        Assert.Equal(QuestStatus.Active, quest1.Status);
        Assert.Equal(1, quest1.CurrentObjectiveIndex);
        Assert.Equal(3, quest1.GetObjectiveProgress(0));

        var quest2 = questManager.GetQuest("quest_02")!;
        Assert.Equal(QuestStatus.Completed, quest2.Status);
    }

    [Fact]
    public void RestoreQuests__SkipsUnknownQuestIds()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var savedData = new SaveGameData
        {
            Quests = new[]
            {
                new SaveQuestStateData
                {
                    QuestId = "nonexistent_quest",
                    Status = QuestStatus.Active,
                    CurrentObjectiveIndex = 0,
                    ObjectiveProgress = new[] { 5 },
                },
            },
        };

        // Should not throw.
        SaveGameMapper.RestoreQuests(savedData, questManager);

        // Original quests unchanged.
        var quest1 = questManager.GetQuest("quest_01")!;
        Assert.Equal(QuestStatus.Active, quest1.Status);
        Assert.Equal(0, quest1.CurrentObjectiveIndex);
    }

    [Fact]
    public void RestoreCombatStats__RestoresAllFields()
    {
        var savedData = new SaveGameData
        {
            CombatStats = new SaveCombatStatsData
            {
                MaxHp = 12,
                Level = 5,
                Xp = 40,
                XpToNextLevel = 50,
                SpeedMultiplier = 1.15f,
                CooldownMultiplier = 0.65f,
                ProjectileSpeedMultiplier = 1.25f,
                ProjectileRangeMultiplier = 1.50f,
            },
        };
        var stats = new PlayerCombatStats();

        SaveGameMapper.RestoreCombatStats(savedData, stats);

        Assert.Equal(12, stats.MaxHp);
        Assert.Equal(5, stats.Level);
        Assert.Equal(40, stats.Xp);
        Assert.Equal(50, stats.XpToNextLevel);
        Assert.Equal(1.15f, stats.SpeedMultiplier);
        Assert.Equal(0.65f, stats.CooldownMultiplier);
        Assert.Equal(1.25f, stats.ProjectileSpeedMultiplier);
        Assert.Equal(1.50f, stats.ProjectileRangeMultiplier);
    }

    [Fact]
    public void CaptureAndRestore__RoundTrip__QuestsPreserved()
    {
        var eventBus = new GameEventBus();
        var originalManager = CreateQuestManagerWithDefinitions();
        // Advance quest_01: kill 2 gnomes.
        originalManager.GetQuest("quest_01")!
            .ApplyEvent(new GameEvent(GameEventType.EnemyKilled, null, 2));

        var data = SaveGameMapper.Capture(
            new Vector2(100f, 200f), FacingDirection.Up, "Maps/StarterMap", originalManager, null, 0.5f, []);

        // Create a fresh quest manager and restore into it.
        var restoredManager = CreateQuestManagerWithDefinitions();
        SaveGameMapper.RestoreQuests(data, restoredManager);
        restoredManager.RebuildListsFromRestoredState();

        var quest1 = restoredManager.GetQuest("quest_01")!;
        Assert.Equal(QuestStatus.Active, quest1.Status);
        Assert.Equal(0, quest1.CurrentObjectiveIndex);
        Assert.Equal(2, quest1.GetObjectiveProgress(0));
    }

    [Fact]
    public void CaptureAndRestore__RoundTrip__CombatStatsPreserved()
    {
        var questManager = CreateQuestManagerWithDefinitions();
        var originalStats = new PlayerCombatStats();
        originalStats.ApplyLevelUp();
        originalStats.ApplyLevelUp();

        var data = SaveGameMapper.Capture(
            Vector2.Zero, FacingDirection.Down, "Maps/WoodsBehindCabin", questManager, originalStats, 0f, []);

        var restoredStats = new PlayerCombatStats();
        SaveGameMapper.RestoreCombatStats(data, restoredStats);

        Assert.Equal(originalStats.MaxHp, restoredStats.MaxHp);
        Assert.Equal(originalStats.Level, restoredStats.Level);
        Assert.Equal(originalStats.Xp, restoredStats.Xp);
        Assert.Equal(originalStats.XpToNextLevel, restoredStats.XpToNextLevel);
        Assert.Equal(originalStats.SpeedMultiplier, restoredStats.SpeedMultiplier);
        Assert.Equal(originalStats.CooldownMultiplier, restoredStats.CooldownMultiplier);
        Assert.Equal(originalStats.ProjectileSpeedMultiplier, restoredStats.ProjectileSpeedMultiplier);
        Assert.Equal(originalStats.ProjectileRangeMultiplier, restoredStats.ProjectileRangeMultiplier);
    }
}
