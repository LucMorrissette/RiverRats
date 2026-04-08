#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Data;

namespace DogDays.Game.Systems;

/// <summary>
/// Manages wave-based enemy spawning for the forest survival minigame.
/// Owns the wave lifecycle and tells <see cref="GnomeSpawner"/> what to spawn.
/// Does NOT call <see cref="GnomeSpawner.Update"/> — that remains in the gameplay screen.
///
/// State flow:
///   PreWave → Countdown → Active → Cleared → Intermission → Countdown → ...
///   After the final wave: Cleared → AllWavesComplete.
/// </summary>
internal sealed class WaveManager
{
    // ── Tuning constants ────────────────────────────────────────────────

    /// <summary>Total number of waves in the minigame.</summary>
    internal const int TotalWaves = 10;

    /// <summary>Seconds of downtime between waves (extended for orb collection).</summary>
    internal const float IntermissionDuration = 8.0f;

    /// <summary>Seconds of countdown before each wave starts (5-4-3-2-1).</summary>
    internal const float CountdownDuration = 5.0f;

    /// <summary>Seconds between each spawn batch during the Active phase.</summary>
    internal const float SpawnStaggerInterval = 0.8f;

    /// <summary>Number of gnomes spawned per batch tick.</summary>
    private const int SpawnBatchSize = 2;

    /// <summary>Hard cap on simultaneously active enemies.</summary>
    private const int MaxActiveEnemies = 60;

    // ── Fields ──────────────────────────────────────────────────────────

    private readonly WaveConfig[] _waves;
    private readonly GnomeSpawner _spawner;
    private readonly Random _rng = new Random(42);

    private int _currentWaveIndex;
    private WaveState _state;
    private float _intermissionTimer;
    private float _countdownTimer;
    private float _waveTimer;
    private float _spawnTimer;
    private EnemyType _lastPickedType;

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Fired when a wave starts (countdown ends, Active begins). Parameter is the 1-based wave number.</summary>
    internal event Action<int>? OnWaveStarted;

    /// <summary>Fired when a wave is cleared (timer expired). Parameter is the 1-based wave number.</summary>
    internal event Action<int>? OnWaveCleared;

    /// <summary>Fired when all waves have been beaten.</summary>
    internal event Action? OnAllWavesComplete;

    // ── Constructor ─────────────────────────────────────────────────────

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
                DurationSeconds = 20f + i * 5f,
                EnemySpeedMultiplier = 1.0f + i * 0.12f,
                EnemyHp = 1 + i / 2,
                EnemyTypeMix = BuildEnemyTypeMix(i),
            };
        }
    }

    // ── Public Properties ───────────────────────────────────────────────

    /// <summary>1-based wave number for UI display.</summary>
    internal int CurrentWaveNumber => _currentWaveIndex + 1;

    /// <summary>Current lifecycle state of the wave system.</summary>
    internal WaveState State => _state;

    /// <summary>Pre-built wave configurations (exposed for testing).</summary>
    internal WaveConfig[] Waves => _waves;

    /// <summary>Countdown seconds remaining, ceiling'd to an integer for HUD display (5, 4, 3, 2, 1).</summary>
    internal int CountdownSeconds => (int)MathF.Ceiling(_countdownTimer);

    /// <summary>Seconds remaining in the current wave (for HUD timer bar).</summary>
    internal float WaveTimeRemaining => _waveTimer;

    /// <summary>Total duration of the current wave in seconds (for HUD timer bar percentage).</summary>
    internal float WaveDuration => _currentWaveIndex < _waves.Length
        ? _waves[_currentWaveIndex].DurationSeconds
        : 0f;

    /// <summary>The most recently picked enemy type (ready for spawner integration).</summary>
    internal EnemyType LastPickedType => _lastPickedType;

    // ── Public Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Begins the first wave. Transitions from <see cref="WaveState.PreWave"/>
    /// to <see cref="WaveState.Countdown"/>.
    /// </summary>
    internal void StartFirstWave()
    {
        _currentWaveIndex = 0;
        EnterCountdown();
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

            case WaveState.Countdown:
                UpdateCountdown(dt);
                break;

            case WaveState.Active:
                UpdateActive(dt, cameraBounds);
                break;

            case WaveState.Cleared:
                HandleCleared();
                break;

            case WaveState.Intermission:
                UpdateIntermission(dt);
                break;
        }
    }

    // ── Enemy Type Selection ────────────────────────────────────────────

    /// <summary>
    /// Selects a random <see cref="EnemyType"/> from the current wave's mix
    /// using weighted random selection.
    /// </summary>
    internal EnemyType PickEnemyType()
    {
        var mix = _waves[_currentWaveIndex].EnemyTypeMix;

        // Sum up weights.
        var totalWeight = 0f;
        foreach (var kvp in mix)
            totalWeight += kvp.Value;

        // Roll a random value in [0, totalWeight).
        var roll = (float)(_rng.NextDouble() * totalWeight);

        // Walk through entries accumulating weight until we pass the roll.
        var accumulated = 0f;
        foreach (var kvp in mix)
        {
            accumulated += kvp.Value;
            if (roll < accumulated)
                return kvp.Key;
        }

        // Fallback (should not be reached with valid data).
        return EnemyType.Standard;
    }

    // ── State Transitions ───────────────────────────────────────────────

    private void EnterCountdown()
    {
        _countdownTimer = CountdownDuration;
        _state = WaveState.Countdown;
    }

    private void EnterActive()
    {
        var wave = _waves[_currentWaveIndex];
        _spawner.GnomeHp = wave.EnemyHp;
        _spawner.GnomeSpeedMultiplier = wave.EnemySpeedMultiplier;
        _waveTimer = wave.DurationSeconds;
        _spawnTimer = 0f;
        _state = WaveState.Active;
        OnWaveStarted?.Invoke(CurrentWaveNumber);
    }

    // ── State Handlers ──────────────────────────────────────────────────

    private void UpdateCountdown(float dt)
    {
        _countdownTimer -= dt;
        if (_countdownTimer <= 0f)
        {
            _countdownTimer = 0f;
            EnterActive();
        }
    }

    private void UpdateActive(float dt, Rectangle cameraBounds)
    {
        _waveTimer -= dt;

        // Trickle-spawn enemies throughout the wave.
        _spawnTimer += dt;
        while (_spawnTimer >= SpawnStaggerInterval)
        {
            _spawnTimer -= SpawnStaggerInterval;

            if (_spawner.Gnomes.Count < MaxActiveEnemies)
            {
                var batchCount = Math.Min(SpawnBatchSize, MaxActiveEnemies - _spawner.Gnomes.Count);

                // Pick enemy type for this batch.
                _lastPickedType = PickEnemyType();

                _spawner.SpawnBatch(batchCount, cameraBounds, _lastPickedType);
            }
        }

        // Time-based completion: wave ends when timer expires, regardless of remaining enemies.
        if (_waveTimer <= 0f)
        {
            _waveTimer = 0f;
            _spawner.KillAll();
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
            EnterCountdown();
        }
    }

    // ── Wave Config Builder ─────────────────────────────────────────────

    private static Dictionary<EnemyType, float> BuildEnemyTypeMix(int waveIndex)
    {
        // waveIndex is 0-based (wave 1 = index 0).
        return waveIndex switch
        {
            // Waves 1-2 (index 0-1): Standard only
            <= 1 => new Dictionary<EnemyType, float>
            {
                { EnemyType.Standard, 1.0f },
            },
            // Waves 3-4 (index 2-3): Standard + Rusher
            <= 3 => new Dictionary<EnemyType, float>
            {
                { EnemyType.Standard, 0.7f },
                { EnemyType.Rusher, 0.3f },
            },
            // Waves 5-6 (index 4-5): Standard + Rusher + Brute
            <= 5 => new Dictionary<EnemyType, float>
            {
                { EnemyType.Standard, 0.5f },
                { EnemyType.Rusher, 0.25f },
                { EnemyType.Brute, 0.25f },
            },
            // Waves 7+ (index 6+): Standard + Rusher + Brute + Bomber
            _ => new Dictionary<EnemyType, float>
            {
                { EnemyType.Standard, 0.4f },
                { EnemyType.Rusher, 0.2f },
                { EnemyType.Brute, 0.2f },
                { EnemyType.Bomber, 0.2f },
            },
        };
    }
}
