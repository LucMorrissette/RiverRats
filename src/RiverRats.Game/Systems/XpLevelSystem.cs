#nullable enable
using System;
using RiverRats.Components;
using RiverRats.Data;

namespace RiverRats.Game.Systems;

/// <summary>
/// Tracks XP and handles level-ups by modifying <see cref="PlayerCombatStats"/>.
/// </summary>
public sealed class XpLevelSystem
{
    private readonly PlayerCombatStats _stats;
    private readonly Health _playerHealth;

    /// <summary>Fired when the player levels up. Parameter is the new level number.</summary>
    public event Action<int>? OnLevelUp;

    /// <summary>
    /// Creates an XP/level system that modifies the given stats and health on level-up.
    /// </summary>
    /// <param name="stats">Combat stats to track XP and apply level-ups to.</param>
    /// <param name="playerHealth">Player health component — MaxHp is increased on level-up.</param>
    public XpLevelSystem(PlayerCombatStats stats, Health playerHealth)
    {
        _stats = stats;
        _playerHealth = playerHealth;
    }

    /// <summary>
    /// Adds XP and processes any resulting level-ups.
    /// </summary>
    /// <param name="amount">XP to add (positive).</param>
    public void AddXp(int amount)
    {
        _stats.Xp += amount;

        while (_stats.Xp >= _stats.XpToNextLevel)
        {
            _stats.ApplyLevelUp();
            _playerHealth.IncreaseMax(1);
            OnLevelUp?.Invoke(_stats.Level);
        }
    }
}
