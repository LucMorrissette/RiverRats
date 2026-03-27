using System;
using Microsoft.Xna.Framework;
using RiverRats.Data;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;
using Xunit;

namespace RiverRats.Tests.Unit;

public class WaveManagerTests
{
    private static readonly Rectangle CameraBounds = new(0, 0, 1000, 1000);

    private static GnomeSpawner CreateSpawner(int maxActive = 200)
    {
        return new GnomeSpawner(0, 999f, maxActive);
    }

    /// <summary>
    /// Advances past the countdown phase to reach Active state.
    /// </summary>
    private static void TickPastCountdown(WaveManager manager)
    {
        manager.Update(FakeGameTime.FromSeconds(WaveManager.CountdownDuration + 0.1f), CameraBounds);
    }

    // ── State Flow Tests ─────────────────────────────────────────────────

    [Fact]
    public void StartFirstWave__TransitionsToCountdown__FromPreWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        Assert.Equal(WaveState.PreWave, manager.State);

        manager.StartFirstWave();

        Assert.Equal(WaveState.Countdown, manager.State);
    }

    [Fact]
    public void Update__TransitionsToActive__AfterCountdownExpires()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        Assert.Equal(WaveState.Countdown, manager.State);

        TickPastCountdown(manager);

        Assert.Equal(WaveState.Active, manager.State);
    }

    [Fact]
    public void Update__FiresOnWaveStarted__WhenCountdownEnds()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        int startedWave = -1;
        manager.OnWaveStarted += w => startedWave = w;

        TickPastCountdown(manager);

        Assert.Equal(1, startedWave);
    }

    [Fact]
    public void Update__TransitionsToCleared__WhenWaveTimerExpires()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        Assert.Equal(WaveState.Active, manager.State);

        // Wave 1 duration is 20 seconds.
        var waveDuration = manager.Waves[0].DurationSeconds;
        manager.Update(FakeGameTime.FromSeconds(waveDuration + 0.1f), CameraBounds);

        // Cleared is transitional — it immediately moves to Intermission.
        Assert.Equal(WaveState.Intermission, manager.State);
    }

    [Fact]
    public void Update__FiresOnWaveCleared__WhenTimerExpires()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        int clearedWave = -1;
        manager.OnWaveCleared += w => clearedWave = w;

        var waveDuration = manager.Waves[0].DurationSeconds;
        manager.Update(FakeGameTime.FromSeconds(waveDuration + 0.1f), CameraBounds);

        Assert.Equal(1, clearedWave);
    }

    [Fact]
    public void Update__DoesNotClearWave__BeforeTimerExpires()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        // Advance half the wave duration.
        var waveDuration = manager.Waves[0].DurationSeconds;
        manager.Update(FakeGameTime.FromSeconds(waveDuration / 2f), CameraBounds);

        Assert.Equal(WaveState.Active, manager.State);
    }

    [Fact]
    public void Update__TransitionsToIntermission__AfterWaveCleared()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        var waveDuration = manager.Waves[0].DurationSeconds;
        manager.Update(FakeGameTime.FromSeconds(waveDuration + 0.1f), CameraBounds);

        Assert.Equal(WaveState.Intermission, manager.State);
    }

    [Fact]
    public void Update__StartsCountdownForNextWave__AfterIntermissionEnds()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        // Complete wave 1.
        var waveDuration = manager.Waves[0].DurationSeconds;
        manager.Update(FakeGameTime.FromSeconds(waveDuration + 0.1f), CameraBounds);

        Assert.Equal(WaveState.Intermission, manager.State);

        // Complete intermission.
        manager.Update(FakeGameTime.FromSeconds(WaveManager.IntermissionDuration + 0.1f), CameraBounds);

        Assert.Equal(WaveState.Countdown, manager.State);
        Assert.Equal(2, manager.CurrentWaveNumber);
    }

    [Fact]
    public void Update__FiresOnAllWavesComplete__AfterFinalWaveTimerExpires()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        bool allComplete = false;
        manager.OnAllWavesComplete += () => allComplete = true;

        // Run through all 10 waves.
        for (var wave = 0; wave < WaveManager.TotalWaves; wave++)
        {
            TickPastCountdown(manager);

            var waveDuration = manager.Waves[wave].DurationSeconds;
            manager.Update(FakeGameTime.FromSeconds(waveDuration + 0.1f), CameraBounds);

            if (wave < WaveManager.TotalWaves - 1)
            {
                Assert.Equal(WaveState.Intermission, manager.State);
                manager.Update(FakeGameTime.FromSeconds(WaveManager.IntermissionDuration + 0.1f), CameraBounds);
            }
        }

        Assert.True(allComplete);
        Assert.Equal(WaveState.AllWavesComplete, manager.State);
    }

    // ── Countdown Tests ──────────────────────────────────────────────────

    [Fact]
    public void CountdownSeconds__Returns5__AtStart()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        Assert.Equal(5, manager.CountdownSeconds);
    }

    [Fact]
    public void CountdownSeconds__Decreases__OverTime()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        // Advance 1.5 seconds — should show 4 (ceiling of 3.5).
        manager.Update(FakeGameTime.FromSeconds(1.5f), CameraBounds);

        Assert.Equal(4, manager.CountdownSeconds);
    }

    // ── Continuous Spawning Tests ─────────────────────────────────────────

    [Fact]
    public void Update__SpawnsEnemiesDuringActive__Continuously()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        Assert.Empty(spawner.Gnomes);

        // One tick past the stagger interval should spawn a batch.
        manager.Update(FakeGameTime.FromSeconds(WaveManager.SpawnStaggerInterval + 0.01f), CameraBounds);

        Assert.True(spawner.Gnomes.Count > 0, "Should spawn enemies during Active phase");
    }

    [Fact]
    public void Update__ContinuesSpawning__ThroughoutWaveDuration()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        // Advance 5 seconds — should spawn multiple batches.
        manager.Update(FakeGameTime.FromSeconds(5f), CameraBounds);

        var countAfter5s = spawner.Gnomes.Count;

        // Advance another 5 seconds — should spawn more.
        manager.Update(FakeGameTime.FromSeconds(5f), CameraBounds);

        Assert.True(spawner.Gnomes.Count > countAfter5s, "Should keep spawning throughout the wave");
    }

    // ── Wave Config Tests ─────────────────────────────────────────────────

    [Fact]
    public void CurrentWaveNumber__Returns1Based__WaveIndex()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        Assert.Equal(1, manager.CurrentWaveNumber);

        manager.StartFirstWave();
        Assert.Equal(1, manager.CurrentWaveNumber);
    }

    [Fact]
    public void WaveScaling__DurationIncreases__PerWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var waves = manager.Waves;
        Assert.Equal(WaveManager.TotalWaves, waves.Length);

        // Wave 1: 20s, Wave 10: 65s.
        Assert.Equal(20f, waves[0].DurationSeconds);
        Assert.Equal(65f, waves[9].DurationSeconds);

        // Verify strictly increasing.
        for (var i = 1; i < waves.Length; i++)
        {
            Assert.True(waves[i].DurationSeconds > waves[i - 1].DurationSeconds);
        }
    }

    [Fact]
    public void WaveScaling__EnemySpeedIncreases__PerWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var waves = manager.Waves;

        Assert.Equal(1.0f, waves[0].EnemySpeedMultiplier);
        Assert.Equal(2.08f, waves[9].EnemySpeedMultiplier, precision: 2);

        for (var i = 1; i < waves.Length; i++)
        {
            Assert.True(waves[i].EnemySpeedMultiplier > waves[i - 1].EnemySpeedMultiplier);
        }
    }

    // ── Enemy Type Mix Tests ──────────────────────────────────────────────

    [Fact]
    public void WaveConfig__Wave1__HasOnlyStandardType()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var mix = manager.Waves[0].EnemyTypeMix;
        Assert.Single(mix);
        Assert.True(mix.ContainsKey(EnemyType.Standard));
    }

    [Fact]
    public void WaveConfig__Wave3__HasStandardAndRusher()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var mix = manager.Waves[2].EnemyTypeMix; // index 2 = wave 3
        Assert.Equal(2, mix.Count);
        Assert.True(mix.ContainsKey(EnemyType.Standard));
        Assert.True(mix.ContainsKey(EnemyType.Rusher));
    }

    [Fact]
    public void WaveConfig__Wave5__HasStandardRusherBrute()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var mix = manager.Waves[4].EnemyTypeMix; // index 4 = wave 5
        Assert.Equal(3, mix.Count);
        Assert.True(mix.ContainsKey(EnemyType.Standard));
        Assert.True(mix.ContainsKey(EnemyType.Rusher));
        Assert.True(mix.ContainsKey(EnemyType.Brute));
    }

    [Fact]
    public void WaveConfig__Wave7__HasAllFourTypes()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var mix = manager.Waves[6].EnemyTypeMix; // index 6 = wave 7
        Assert.Equal(4, mix.Count);
        Assert.True(mix.ContainsKey(EnemyType.Standard));
        Assert.True(mix.ContainsKey(EnemyType.Rusher));
        Assert.True(mix.ContainsKey(EnemyType.Brute));
        Assert.True(mix.ContainsKey(EnemyType.Bomber));
    }

    [Fact]
    public void PickEnemyType__ReturnsValidType__FromCurrentWaveMix()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        // Wave 1 only has Standard.
        var type = manager.PickEnemyType();
        Assert.Equal(EnemyType.Standard, type);
    }

    // ── Timer Property Tests ──────────────────────────────────────────────

    [Fact]
    public void WaveTimeRemaining__DecreasesDuringActive()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickPastCountdown(manager);

        var initial = manager.WaveTimeRemaining;
        manager.Update(FakeGameTime.FromSeconds(3f), CameraBounds);

        Assert.True(manager.WaveTimeRemaining < initial);
        Assert.Equal(initial - 3f, manager.WaveTimeRemaining, precision: 1);
    }

    [Fact]
    public void WaveDuration__MatchesCurrentWaveConfig()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        Assert.Equal(manager.Waves[0].DurationSeconds, manager.WaveDuration);
    }
}
