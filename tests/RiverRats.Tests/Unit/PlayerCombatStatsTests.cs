using RiverRats.Data;
using Xunit;

namespace RiverRats.Tests.Unit;

public class PlayerCombatStatsTests
{
    [Fact]
    public void ApplyLevelUp__IncrementsLevel__ByOne()
    {
        var stats = new PlayerCombatStats { Xp = 10 };

        stats.ApplyLevelUp();

        Assert.Equal(2, stats.Level);
    }

    [Fact]
    public void ApplyLevelUp__IncreasesMaxHp__ByOne()
    {
        var stats = new PlayerCombatStats { Xp = 10 };

        stats.ApplyLevelUp();

        Assert.Equal(6, stats.MaxHp);
    }

    [Fact]
    public void ApplyLevelUp__ReducesCooldownMultiplier__By8Percent()
    {
        var stats = new PlayerCombatStats { Xp = 10 };

        stats.ApplyLevelUp();

        Assert.Equal(0.92f, stats.CooldownMultiplier, precision: 4);
    }

    [Fact]
    public void ApplyLevelUp__IncreasesSpeedMultiplier__By3Percent()
    {
        var stats = new PlayerCombatStats { Xp = 10 };

        stats.ApplyLevelUp();

        Assert.Equal(1.03f, stats.SpeedMultiplier, precision: 4);
    }

    [Fact]
    public void ApplyLevelUp__CarriesOverExcessXp__AfterLevelUp()
    {
        var stats = new PlayerCombatStats { Xp = 13 };

        stats.ApplyLevelUp();

        Assert.Equal(3, stats.Xp);
    }

    [Fact]
    public void ApplyLevelUp__ScalesXpThreshold__By1Point5x()
    {
        var stats = new PlayerCombatStats { Xp = 10 };

        stats.ApplyLevelUp();

        Assert.Equal(15, stats.XpToNextLevel);
    }

    [Fact]
    public void Reset__RestoresAllDefaults__AfterModification()
    {
        var stats = new PlayerCombatStats();
        stats.ApplyLevelUp();
        stats.Xp = 50;

        stats.Reset();

        Assert.Equal(5, stats.MaxHp);
        Assert.Equal(1.0f, stats.SpeedMultiplier);
        Assert.Equal(1.0f, stats.CooldownMultiplier);
        Assert.Equal(1.0f, stats.ProjectileSpeedMultiplier);
        Assert.Equal(1.0f, stats.ProjectileRangeMultiplier);
        Assert.Equal(1, stats.Level);
        Assert.Equal(0, stats.Xp);
        Assert.Equal(10, stats.XpToNextLevel);
    }
}
