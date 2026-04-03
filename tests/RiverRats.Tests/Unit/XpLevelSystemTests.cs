using RiverRats.Components;
using RiverRats.Data;
using RiverRats.Game.Systems;
using Xunit;

namespace RiverRats.Tests.Unit;

public class XpLevelSystemTests
{
    private static (XpLevelSystem system, PlayerCombatStats stats, Health health) CreateSystem()
    {
        var stats = new PlayerCombatStats();
        var health = new Health(stats.MaxHp);
        var system = new XpLevelSystem(stats, health);
        return (system, stats, health);
    }

    [Fact]
    public void AddXp__IncrementsXp__ByAmount()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(3);

        Assert.Equal(3, stats.Xp);
    }

    [Fact]
    public void AddXp__TriggersLevelUp__WhenXpReachesThreshold()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(10); // XpToNextLevel starts at 10

        Assert.Equal(2, stats.Level);
    }

    [Fact]
    public void AddXp__FiresOnLevelUp__WithNewLevel()
    {
        var (system, _, _) = CreateSystem();
        int firedLevel = 0;
        system.OnLevelUp += level => firedLevel = level;

        system.AddXp(10);

        Assert.Equal(2, firedLevel);
    }

    [Fact]
    public void AddXp__HandlesMultipleLevelUps__WhenExcessXpIsLarge()
    {
        var (system, stats, _) = CreateSystem();
        var levelUps = new System.Collections.Generic.List<int>();
        system.OnLevelUp += level => levelUps.Add(level);

        // Level 1->2 requires 10 XP, level 2->3 requires 15 XP (10 * 1.5).
        // 25 XP should trigger two level-ups.
        system.AddXp(25);

        Assert.Equal(3, stats.Level);
        Assert.Equal(2, levelUps.Count);
        Assert.Equal(2, levelUps[0]);
        Assert.Equal(3, levelUps[1]);
    }

    [Fact]
    public void AddXp__IncreasesPlayerMaxHp__OnLevelUp()
    {
        var (system, _, health) = CreateSystem();
        var initialMaxHp = health.MaxHp;

        system.AddXp(10); // Trigger one level-up

        // ApplyLevelUp adds 1 to stats.MaxHp, and XpLevelSystem calls IncreaseMax(1) on Health.
        Assert.Equal(initialMaxHp + 1, health.MaxHp);
    }

    // ── Edge cases ─────────────────────────────────────────────────────────

    [Fact]
    public void AddXp__ZeroAmount__DoesNotChangeLevelOrXp()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(0);

        Assert.Equal(1, stats.Level);
        Assert.Equal(0, stats.Xp);
    }

    [Fact]
    public void AddXp__OneXpBelowThreshold__DoesNotLevelUp()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(stats.XpToNextLevel - 1); // 9 XP — just below threshold

        Assert.Equal(1, stats.Level);
        Assert.Equal(9, stats.Xp);
    }

    [Fact]
    public void AddXp__ExactlyAtThreshold__LevelsUpAndResetsXp()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(10); // Exactly 10 = XpToNextLevel

        Assert.Equal(2, stats.Level);
        Assert.Equal(0, stats.Xp); // No excess XP.
    }

    [Fact]
    public void AddXp__ExcessXp__CarriesOverAfterLevelUp()
    {
        var (system, stats, _) = CreateSystem();

        system.AddXp(13); // 10 for level-up, 3 excess

        Assert.Equal(2, stats.Level);
        Assert.Equal(3, stats.Xp);
    }

    [Fact]
    public void AddXp__XpToNextLevel__ScalesUpAfterLevelUp()
    {
        var (system, stats, _) = CreateSystem();
        var firstThreshold = stats.XpToNextLevel; // 10

        system.AddXp(10); // Level 1 → 2

        // XpToNextLevel = (int)(10 * 1.5) = 15
        Assert.Equal((int)(firstThreshold * 1.5f), stats.XpToNextLevel);
    }

    [Fact]
    public void AddXp__DoesNotFireOnLevelUp__WhenNoThresholdCrossed()
    {
        var (system, _,  _) = CreateSystem();
        int levelUpCount = 0;
        system.OnLevelUp += _ => levelUpCount++;

        system.AddXp(5); // Below threshold — no level-up

        Assert.Equal(0, levelUpCount);
    }
}
