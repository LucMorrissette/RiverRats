using Microsoft.Xna.Framework;

namespace DogDays.Game.Components;

/// <summary>
/// Reusable frame-timing logic for sprite animations.
/// Tracks elapsed time and advances a frame counter with wrapping.
/// </summary>
public struct FrameTimer
{
    private readonly int _frameCount;
    private readonly float _frameDuration;
    private float _elapsed;

    /// <summary>
    /// Creates a frame timer.
    /// </summary>
    /// <param name="frameCount">Total number of frames to cycle through.</param>
    /// <param name="frameDuration">Seconds each frame is displayed before advancing.</param>
    public FrameTimer(int frameCount, float frameDuration)
    {
        _frameCount = frameCount;
        _frameDuration = frameDuration;
        _elapsed = 0f;
        CurrentFrame = 0;
    }

    /// <summary>Current frame index (0-based).</summary>
    public int CurrentFrame { get; private set; }

    /// <summary>
    /// Advances the timer by the elapsed game time and wraps the frame counter.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    public void Advance(GameTime gameTime)
    {
        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (_elapsed >= _frameDuration)
        {
            _elapsed -= _frameDuration;
            CurrentFrame = (CurrentFrame + 1) % _frameCount;
        }
    }

    /// <summary>
    /// Resets the timer to the first frame with no accumulated time.
    /// </summary>
    public void Reset()
    {
        CurrentFrame = 0;
        _elapsed = 0f;
    }
}
