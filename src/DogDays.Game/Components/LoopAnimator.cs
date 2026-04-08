using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Components;

/// <summary>
/// Animates a single-row horizontal sprite sheet in a continuous loop.
/// Unlike <see cref="SpriteAnimator"/>, this has no direction rows or movement gating —
/// it always plays, cycling through frames left to right.
/// </summary>
public sealed class LoopAnimator
{
    private readonly int _frameWidth;
    private readonly int _frameHeight;

    private FrameTimer _timer;

    /// <summary>
    /// Initializes a LoopAnimator for a horizontal sprite strip.
    /// </summary>
    /// <param name="frameWidth">Width of a single frame in pixels.</param>
    /// <param name="frameHeight">Height of a single frame in pixels.</param>
    /// <param name="frameCount">Total number of frames in the strip.</param>
    /// <param name="frameDuration">Seconds each frame is displayed before advancing.</param>
    public LoopAnimator(int frameWidth, int frameHeight, int frameCount, float frameDuration)
    {
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _timer = new FrameTimer(frameCount, frameDuration);
    }

    /// <summary>Current animation frame index (0-based).</summary>
    public int CurrentFrame => _timer.CurrentFrame;

    /// <summary>
    /// Returns the source rectangle for the current frame on the sprite sheet.
    /// Frames are laid out in a single horizontal row.
    /// </summary>
    public Rectangle SourceRectangle => new(
        CurrentFrame * _frameWidth,
        0,
        _frameWidth,
        _frameHeight);

    /// <summary>
    /// Advances the animation timer. Always loops — no movement gating.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    public void Update(GameTime gameTime)
    {
        _timer.Advance(gameTime);
    }

    /// <summary>
    /// Draws the current frame from the sprite sheet at the given position.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="texture">The sprite sheet texture.</param>
    /// <param name="position">World-space top-left position.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float layerDepth = 0f)
    {
        spriteBatch.Draw(
            texture,
            position,
            SourceRectangle,
            Color.White,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            layerDepth);
    }
}
