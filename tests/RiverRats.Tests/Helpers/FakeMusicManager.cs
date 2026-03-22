using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using RiverRats.Game.Audio;

namespace RiverRats.Tests.Helpers;

/// <summary>
/// Test fake for IMusicManager. Records method calls for assertions.
/// </summary>
public sealed class FakeMusicManager : IMusicManager
{
    private readonly List<float> _volumeHistory = new();

    /// <summary>Whether music is currently "playing" (settable for tests).</summary>
    public bool IsPlaying { get; set; }

    /// <summary>The last volume set via SetVolume.</summary>
    public float LastVolume { get; private set; } = 1f;

    /// <summary>All volume values passed to SetVolume, in order.</summary>
    public IReadOnlyList<float> VolumeHistory => _volumeHistory;

    /// <summary>Number of times LoadContent was called.</summary>
    public int LoadContentCallCount { get; private set; }

    /// <summary>Number of times StopSong was called.</summary>
    public int StopSongCallCount { get; private set; }

    /// <summary>The last song name passed to PlaySong, or null if PlaySong has not been called.</summary>
    public string? LastPlayedSong { get; private set; }

    /// <inheritdoc />
    public void LoadContent(ContentManager content) => LoadContentCallCount++;

    /// <inheritdoc />
    public void PlaySong(string songName, float loopDelaySeconds = 0f)
    {
        LastPlayedSong = songName;
        IsPlaying = true;
    }

    /// <inheritdoc />
    public void StopSong()
    {
        StopSongCallCount++;
        IsPlaying = false;
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime) { }

    /// <inheritdoc />
    public void SetVolume(float volume)
    {
        LastVolume = volume;
        _volumeHistory.Add(volume);
    }
}
