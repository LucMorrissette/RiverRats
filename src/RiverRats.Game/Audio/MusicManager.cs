#nullable enable

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace RiverRats.Game.Audio;

/// <summary>
/// Manages background music playback with delayed looping support.
/// Uses MonoGame's static <see cref="MediaPlayer"/> API for Song-based music.
/// Does NOT use MediaPlayer.IsRepeating — instead tracks song completion
/// and applies a configurable delay before replaying.
/// </summary>
public sealed class MusicManager : IMusicManager
{
    private readonly Dictionary<string, Song> _songs = new();
    private string? _currentSongName;
    private float _loopDelaySeconds;
    private float _delayTimer;
    private bool _waitingToLoop;

    /// <inheritdoc />
    public bool IsPlaying => MediaPlayer.State == MediaState.Playing;

    /// <inheritdoc />
    public void LoadContent(ContentManager content)
    {
        _songs["GameplayTheme"] = content.Load<Song>("Audio/Music/river_rats_theme");
        _songs["WoodsBehindCabinTheme"] = content.Load<Song>("Audio/Music/CottageBehindWoods_theme");
        _songs["ForestFailSong"] = content.Load<Song>("Audio/Music/forest_fail_song");
    }

    /// <inheritdoc />
    public void PlaySong(string songName, float loopDelaySeconds = 0f)
    {
        // Idempotent: don't restart if the same song is already playing
        if (_currentSongName == songName && MediaPlayer.State == MediaState.Playing)
        {
            return;
        }

        if (_songs.TryGetValue(songName, out var song))
        {
            // Never use MediaPlayer.IsRepeating — we handle loop timing ourselves.
            MediaPlayer.IsRepeating = false;
            MediaPlayer.Play(song);
            _currentSongName = songName;
            _loopDelaySeconds = loopDelaySeconds;
            _waitingToLoop = false;
            _delayTimer = 0f;
        }
    }

    /// <inheritdoc />
    public void StopSong()
    {
        MediaPlayer.Stop();
        _currentSongName = null;
        _waitingToLoop = false;
        _delayTimer = 0f;
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime)
    {
        if (_currentSongName == null)
        {
            return;
        }

        // Song just finished — start the delay timer
        if (!_waitingToLoop && MediaPlayer.State == MediaState.Stopped)
        {
            if (_loopDelaySeconds < 0f)
            {
                // Negative delay = no loop. Song is done.
                _currentSongName = null;
                return;
            }

            _waitingToLoop = true;
            _delayTimer = _loopDelaySeconds;
        }

        // Count down delay, then replay
        if (_waitingToLoop)
        {
            _delayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_delayTimer <= 0f)
            {
                _waitingToLoop = false;
                if (_songs.TryGetValue(_currentSongName, out var song))
                {
                    MediaPlayer.Play(song);
                }
            }
        }
    }

    /// <inheritdoc />
    public void SetVolume(float volume)
    {
        MediaPlayer.Volume = MathHelper.Clamp(volume, 0f, 1f);
    }
}
