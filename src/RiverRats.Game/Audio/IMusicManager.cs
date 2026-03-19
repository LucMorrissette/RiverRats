namespace RiverRats.Game.Audio;

/// <summary>
/// Manages background music playback with support for delayed looping.
/// Wraps MonoGame's MediaPlayer API for Song-based music.
/// </summary>
public interface IMusicManager
{
    /// <summary>Whether music is currently playing.</summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Loads all music assets from the content pipeline.
    /// </summary>
    void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content);

    /// <summary>
    /// Plays a song by registered name. If the same song is already playing, the call is ignored.
    /// </summary>
    /// <param name="songName">Registered song name (e.g., "GameplayTheme").</param>
    /// <param name="loopDelaySeconds">Seconds to wait after the song ends before replaying. 0 = immediate loop. Negative = no loop.</param>
    void PlaySong(string songName, float loopDelaySeconds = 0f);

    /// <summary>Stops the currently playing song and cancels any pending loop.</summary>
    void StopSong();

    /// <summary>
    /// Must be called every frame. Handles delayed loop logic — checks if the song
    /// has ended and counts down the delay before replaying.
    /// </summary>
    void Update(Microsoft.Xna.Framework.GameTime gameTime);

    /// <summary>Sets the music playback volume (0.0 to 1.0).</summary>
    void SetVolume(float volume);
}
