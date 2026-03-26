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
    /// Removes all gnomes from the spawner to simulate clearing a wave.
    /// </summary>
    private static void ClearAllGnomes(GnomeSpawner spawner)
    {
        for (var i = spawner.Gnomes.Count - 1; i >= 0; i--)
            spawner.RemoveAt(i);
    }

    /// <summary>
    /// Advances the WaveManager enough frames to finish stagger-spawning.
    /// </summary>
    private static void TickUntilActive(WaveManager manager)
    {
        // Each tick spawns a batch. 50 waves * 0.15s ticks = plenty.
        var tick = FakeGameTime.FromSeconds(WaveManager.SpawnStaggerInterval + 0.001f);
        for (var i = 0; i < 50; i++)
        {
            if (manager.State != WaveState.Spawning)
                break;
            manager.Update(tick, CameraBounds);
        }
    }

    [Fact]
    public void StartFirstWave__TransitionsToSpawning__FromPreWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        Assert.Equal(WaveState.PreWave, manager.State);

        manager.StartFirstWave();

        Assert.Equal(WaveState.Spawning, manager.State);
    }

    [Fact]
    public void Update__SpawnsEnemiesInBatches__DuringSpawningState()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        Assert.Empty(spawner.Gnomes);

        // One tick should spawn a batch of 3.
        var tick = FakeGameTime.FromSeconds(WaveManager.SpawnStaggerInterval + 0.001f);
        manager.Update(tick, CameraBounds);

        Assert.True(spawner.Gnomes.Count > 0);
        Assert.True(spawner.Gnomes.Count <= 3);
    }

    [Fact]
    public void Update__TransitionsToActive__WhenAllEnemiesSpawned()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        TickUntilActive(manager);

        Assert.Equal(WaveState.Active, manager.State);
        // Wave 1 has 8 enemies.
        Assert.Equal(8, spawner.Gnomes.Count);
    }

    [Fact]
    public void Update__TransitionsToCleared__WhenAllEnemiesDead()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickUntilActive(manager);

        ClearAllGnomes(spawner);

        // Next update should detect all dead.
        manager.Update(FakeGameTime.OneFrame(), CameraBounds);

        // Cleared is transitional — it immediately moves to Intermission (or AllWavesComplete).
        Assert.Equal(WaveState.Intermission, manager.State);
    }

    [Fact]
    public void Update__TransitionsToIntermission__AfterWaveCleared()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickUntilActive(manager);

        int clearedWave = -1;
        manager.OnWaveCleared += w => clearedWave = w;

        ClearAllGnomes(spawner);
        manager.Update(FakeGameTime.OneFrame(), CameraBounds);

        Assert.Equal(WaveState.Intermission, manager.State);
        Assert.Equal(1, clearedWave);
    }

    [Fact]
    public void Update__StartsNextWave__AfterIntermissionEnds()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();
        TickUntilActive(manager);
        ClearAllGnomes(spawner);
        manager.Update(FakeGameTime.OneFrame(), CameraBounds);

        Assert.Equal(WaveState.Intermission, manager.State);

        int startedWave = -1;
        manager.OnWaveStarted += w => startedWave = w;

        // Advance past the intermission.
        manager.Update(FakeGameTime.FromSeconds(WaveManager.IntermissionDuration + 0.1f), CameraBounds);

        Assert.Equal(WaveState.Spawning, manager.State);
        Assert.Equal(2, manager.CurrentWaveNumber);
        Assert.Equal(2, startedWave);
    }

    [Fact]
    public void Update__FiresOnAllWavesComplete__AfterWave10Cleared()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);
        manager.StartFirstWave();

        bool allComplete = false;
        manager.OnAllWavesComplete += () => allComplete = true;

        // Run through all 10 waves.
        for (var wave = 0; wave < WaveManager.TotalWaves; wave++)
        {
            TickUntilActive(manager);
            ClearAllGnomes(spawner);
            manager.Update(FakeGameTime.OneFrame(), CameraBounds);

            if (wave < WaveManager.TotalWaves - 1)
            {
                // Intermission -> next wave.
                Assert.Equal(WaveState.Intermission, manager.State);
                manager.Update(FakeGameTime.FromSeconds(WaveManager.IntermissionDuration + 0.1f), CameraBounds);
            }
        }

        Assert.True(allComplete);
        Assert.Equal(WaveState.AllWavesComplete, manager.State);
    }

    [Fact]
    public void CurrentWaveNumber__Returns1Based__WaveIndex()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        Assert.Equal(1, manager.CurrentWaveNumber);

        manager.StartFirstWave();
        Assert.Equal(1, manager.CurrentWaveNumber);

        // Advance to wave 2.
        TickUntilActive(manager);
        ClearAllGnomes(spawner);
        manager.Update(FakeGameTime.OneFrame(), CameraBounds);
        manager.Update(FakeGameTime.FromSeconds(WaveManager.IntermissionDuration + 0.1f), CameraBounds);

        Assert.Equal(2, manager.CurrentWaveNumber);
    }

    [Fact]
    public void WaveScaling__EnemyCountIncreases__PerWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var waves = manager.Waves;
        Assert.Equal(WaveManager.TotalWaves, waves.Length);

        // Wave 1 (index 0): 8 + 0*4 = 8.
        Assert.Equal(8, waves[0].EnemyCount);
        // Wave 10 (index 9): 8 + 9*4 = 44.
        Assert.Equal(44, waves[9].EnemyCount);

        // Verify strictly increasing.
        for (var i = 1; i < waves.Length; i++)
        {
            Assert.True(waves[i].EnemyCount > waves[i - 1].EnemyCount);
        }
    }

    [Fact]
    public void WaveScaling__EnemySpeedIncreases__PerWave()
    {
        var spawner = CreateSpawner();
        var manager = new WaveManager(spawner);

        var waves = manager.Waves;

        // Wave 1 (index 0): 1.0 + 0*0.12 = 1.0.
        Assert.Equal(1.0f, waves[0].EnemySpeedMultiplier);
        // Wave 10 (index 9): 1.0 + 9*0.12 = 2.08.
        Assert.Equal(2.08f, waves[9].EnemySpeedMultiplier, precision: 2);

        // Verify strictly increasing.
        for (var i = 1; i < waves.Length; i++)
        {
            Assert.True(waves[i].EnemySpeedMultiplier > waves[i - 1].EnemySpeedMultiplier);
        }
    }
}
