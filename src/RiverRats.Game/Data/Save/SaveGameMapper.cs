#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RiverRats.Data;
using RiverRats.Game.Data;
using RiverRats.Game.Data.Save;
using RiverRats.Game.Systems;

namespace RiverRats.Game.Data.Save;

/// <summary>
/// Captures and restores game state to/from <see cref="SaveGameData"/>.
/// Single source of truth for save serialization logic.
/// </summary>
internal static class SaveGameMapper
{
    /// <summary>
    /// Captures the current game state into a <see cref="SaveGameData"/> snapshot.
    /// </summary>
    /// <param name="playerPosition">Player world position.</param>
    /// <param name="playerFacing">Player facing direction.</param>
    /// <param name="mapAssetName">Current zone map asset name.</param>
    /// <param name="questManager">Quest manager holding all quest states.</param>
    /// <param name="combatStats">Player combat stats (may be null if not in forest).</param>
    /// <param name="dayNightCycleProgress">Current day/night cycle progress (0–1).</param>
    /// <param name="watercraftStates">Saved state for movable watercraft across visited maps.</param>
    internal static SaveGameData Capture(
        Vector2 playerPosition,
        FacingDirection playerFacing,
        string mapAssetName,
        QuestManager questManager,
        PlayerCombatStats? combatStats,
        float dayNightCycleProgress,
        SaveWatercraftData[] watercraftStates)
    {
        var data = new SaveGameData
        {
            Version = SaveGameData.CurrentVersion,
            SavedAtUtc = DateTime.UtcNow,
            Player = CapturePlayer(playerPosition, playerFacing, mapAssetName),
            Quests = CaptureQuests(questManager),
            CombatStats = CaptureCombatStats(combatStats),
            DayNight = new SaveDayNightData { CycleProgress = dayNightCycleProgress },
            Watercraft = CaptureWatercraft(watercraftStates),
        };

        return data;
    }

    /// <summary>
    /// Restores quest state from a save snapshot. Call after quest definitions are loaded.
    /// </summary>
    /// <param name="data">Saved data to restore from.</param>
    /// <param name="questManager">Quest manager with definitions already loaded.</param>
    internal static void RestoreQuests(SaveGameData data, QuestManager questManager)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(questManager);

        if (data.Quests is null)
        {
            return;
        }

        for (var i = 0; i < data.Quests.Length; i++)
        {
            var savedQuest = data.Quests[i];
            var questState = questManager.GetQuest(savedQuest.QuestId);
            if (questState is null)
            {
                continue;
            }

            questState.RestoreState(savedQuest.Status, savedQuest.CurrentObjectiveIndex, savedQuest.ObjectiveProgress);
        }
    }

    /// <summary>
    /// Restores combat stats from a save snapshot.
    /// </summary>
    /// <param name="data">Saved data to restore from.</param>
    /// <param name="combatStats">Target combat stats to populate.</param>
    internal static void RestoreCombatStats(SaveGameData data, PlayerCombatStats combatStats)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(combatStats);

        var saved = data.CombatStats;
        if (saved is null)
        {
            return;
        }

        combatStats.MaxHp = saved.MaxHp;
        combatStats.Level = saved.Level;
        combatStats.Xp = saved.Xp;
        combatStats.XpToNextLevel = saved.XpToNextLevel;
        combatStats.SpeedMultiplier = saved.SpeedMultiplier;
        combatStats.CooldownMultiplier = saved.CooldownMultiplier;
        combatStats.ProjectileSpeedMultiplier = saved.ProjectileSpeedMultiplier;
        combatStats.ProjectileRangeMultiplier = saved.ProjectileRangeMultiplier;
    }

    private static SavePlayerData CapturePlayer(Vector2 position, FacingDirection facing, string mapAssetName)
    {
        return new SavePlayerData
        {
            X = position.X,
            Y = position.Y,
            Facing = facing,
            ZoneMapAssetName = mapAssetName ?? string.Empty,
        };
    }

    private static SaveQuestStateData[] CaptureQuests(QuestManager questManager)
    {
        var allQuests = questManager.AllQuests;
        var result = new SaveQuestStateData[allQuests.Count];

        for (var i = 0; i < allQuests.Count; i++)
        {
            var quest = allQuests[i];
            var objectiveCount = quest.Definition.Objectives.Length;
            var progress = new int[objectiveCount];
            for (var j = 0; j < objectiveCount; j++)
            {
                progress[j] = quest.GetObjectiveProgress(j);
            }

            result[i] = new SaveQuestStateData
            {
                QuestId = quest.Definition.Id,
                Status = quest.Status,
                CurrentObjectiveIndex = quest.CurrentObjectiveIndex,
                ObjectiveProgress = progress,
            };
        }

        return result;
    }

    private static SaveCombatStatsData CaptureCombatStats(PlayerCombatStats? stats)
    {
        if (stats is null)
        {
            return new SaveCombatStatsData();
        }

        return new SaveCombatStatsData
        {
            MaxHp = stats.MaxHp,
            Level = stats.Level,
            Xp = stats.Xp,
            XpToNextLevel = stats.XpToNextLevel,
            SpeedMultiplier = stats.SpeedMultiplier,
            CooldownMultiplier = stats.CooldownMultiplier,
            ProjectileSpeedMultiplier = stats.ProjectileSpeedMultiplier,
            ProjectileRangeMultiplier = stats.ProjectileRangeMultiplier,
        };
    }

    private static SaveWatercraftData[] CaptureWatercraft(SaveWatercraftData[] watercraftStates)
    {
        if (watercraftStates is null || watercraftStates.Length == 0)
        {
            return [];
        }

        var result = new SaveWatercraftData[watercraftStates.Length];
        for (var i = 0; i < watercraftStates.Length; i++)
        {
            var state = watercraftStates[i];
            result[i] = new SaveWatercraftData
            {
                MapAssetName = state.MapAssetName,
                InitialX = state.InitialX,
                InitialY = state.InitialY,
                CenterX = state.CenterX,
                CenterY = state.CenterY,
                Facing = state.Facing,
                IsOccupied = state.IsOccupied,
            };
        }

        return result;
    }
}
