#nullable enable
using System;
using Microsoft.Xna.Framework;
using RiverRats.Data;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages wave-based enemy spawning for the forest survival minigame.
/// Owns the wave lifecycle and tells <see cref="GnomeSpawner"/> what to spawn.
/// Does NOT call <see cref="GnomeSpawner.Update"/> — that remains in the gameplay screen.
/// </summary>
internal sealed class WaveManager
{
    /// <summary>Total number of waves in the minigame.</summary>
    internal const int TotalWaves = 10;

    /// <summary>Seconds of downtime between waves.</summary>
    internal const float IntermissionDuration = 3.0f;

    /// <summary>Seconds between each spawn batch during the Spawning phase.</summary>
    internal const float SpawnStaggerInterval = 0.15f;

    /// <summary>Number of gnomes spawned per batch tick.</summary>
    private const int SpawnBatchSize = 3;

    private readonly WaveConfig[] _waves;
    private readonly GnomeSpawner _spawner;

    private int _currentWaveIndex;
    private WaveState _state;
    private float _intermissionTimer;
    private int _enemiesSpawnedThisWave;
    private float _spawnTimer;

    /// <summary>Fired when a wave starts. Parameter is the 1-based wave number.</summary>
    internal event Action<int>? OnWaveStarted;

    /// <summary>Fired when a wave is cleared. Parameter is the 1-based wave number.</summary>
    internal event Action<int>? OnWaveCleared;

    /// <summary>Fired when all waves have been beaten.</summary>
    internal event Action? OnAllWavesComplete;

    /// <summary>
    /// Creates a WaveManager that controls the given spawner.
    /// Disables auto-spawning on the spawner so wave logic takes over.
    /// </summary>
    /// <param name="spawner">The gnome spawner to drive.</param>
    internal WaveManager(GnomeSpawner spawner)
    {
        _spawner = spawner;
        _spawner.AutoSpawnEnabled = false;
        _state = WaveState.PreWave;

        _waves = new WaveConfig[TotalWaves];
        for (var i = 0; i < TotalWaves; i++)
        {
            _waves[i] = new WaveConfig
            {
                WaveNumber = i + 1,
                EnemyCount = 8 + i * 4,
                EnemySpeedMultiplier = 1.0f + i * 0.12f,
                EnemyHp = 1 + i / 2,
            };
        }
    }

    /// <summary>1-based wave number for UI display.</summary>
    internal int CurrentWaveNumber => _currentWaveIndex + 1;

    /// <summary>Current lifecycle state of the wave system.</summary>
    internal WaveState State => _state;

    /// <summary>Pre-built wave configurations (exposed for testing).</summary>
    internal WaveConfig[] Waves => _waves;

    /// <summary>
    /// Begins the first wave. Transitions from <see cref="WaveState.PreWave"/>
    /// to <see cref="WaveState.Spawning"/> and fires <see cref="OnWaveStarted"/>.
    /// </summary>
    internal void StartFirstWave()
    {
        _currentWaveIndex = 0;
        BeginSpawning();
    }

    /// <summary>
    /// Advances the wave state machine. Call once per frame from the gameplay screen.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    /// <param name="cameraBounds">Camera world bounds for spawn placement.</param>
    internal void Update(GameTime gameTime, Rectangle cameraBounds)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case WaveState.PreWave:
            case WaveState.AllWavesComplete:
                break;

            case WaveState.Spawning:
                UpdateSpawning(dt, cameraBounds);
                break;

            case WaveState.Active:
                UpdateActive();
                break;

            case WaveState.Cleared:
                HandleCleared();
                break;

            case WaveState.Intermission:
                UpdateIntermission(dt);
                break;
        }
    }

    private void BeginSpawning()
    {
        var wave = _waves[_currentWaveIndex];
        _spawner.GnomeHp = wave.EnemyHp;
        _spawner.GnomeSpeedMultiplier = wave.EnemySpeedMultiplier;
        _enemiesSpawnedThisWave = 0;
        _spawnTimer = 0f;
        _state = WaveState.Spawning;
        OnWaveStarted?.Invoke(CurrentWaveNumber);
    }

    private void UpdateSpawning(float dt, Rectangle cameraBounds)
    {
        var wave = _waves[_currentWaveIndex];
        _spawnTimer += dt;

        while (_spawnTimer >= SpawnStaggerInterval && _enemiesSpawnedThisWave < wave.EnemyCount)
        {
            _spawnTimer -= SpawnStaggerInterval;
            var remaining = wave.EnemyCount - _enemiesSpawnedThisWave;
            var batchCount = Math.Min(SpawnBatchSize, remaining);
            _spawner.SpawnBatch(batchCount, cameraBounds);
            _enemiesSpawnedThisWave += batchCount;
        }

        if (_enemiesSpawnedThisWave >= wave.EnemyCount)
        {
            _state = WaveState.Active;
        }
    }

    private void UpdateActive()
    {
        if (_spawner.Gnomes.Count == 0)
        {
            _state = WaveState.Cleared;
            HandleCleared();
        }
    }

    private void HandleCleared()
    {
        OnWaveCleared?.Invoke(CurrentWaveNumber);

        if (_currentWaveIndex >= TotalWaves - 1)
        {
            _state = WaveState.AllWavesComplete;
            OnAllWavesComplete?.Invoke();
        }
        else
        {
            _intermissionTimer = IntermissionDuration;
            _state = WaveState.Intermission;
        }
    }

    private void UpdateIntermission(float dt)
    {
        _intermissionTimer -= dt;
        if (_intermissionTimer <= 0f)
        {
            _currentWaveIndex++;
            BeginSpawning();
        }
    }
}
