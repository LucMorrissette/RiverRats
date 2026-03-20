using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Components;

/// <summary>
/// Animates a single-row horizontal sprite sheet in a continuous loop.
/// Unlike <see cref="SpriteAnimator"/>, this has no direction rows or movement gating —
/// it always plays, cycling through frames left to right.
/// </summary>
public sealed class LoopAnimator
{
    private readonly int _frameWidth;
    private readonly int _frameHeight;
    private readonly int _frameCount;
    private readonly float _frameDuration;

    private float _elapsed;
    private int _currentFrame;

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
        _frameCount = frameCount;
        _frameDuration = frameDuration;
    }

    /// <summary>Current animation frame index (0-based).</summary>
    public int CurrentFrame => _currentFrame;

    /// <summary>
    /// Returns the source rectangle for the current frame on the sprite sheet.
    /// Frames are laid out in a single horizontal row.
    /// </summary>
    public Rectangle SourceRectangle => new(
        _currentFrame * _frameWidth,
        0,
        _frameWidth,
        _frameHeight);

    /// <summary>
    /// Advances the animation timer. Always loops — no movement gating.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    public void Update(GameTime gameTime)
    {
        _elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        while (_elapsed >= _frameDuration)
        {
            _elapsed -= _frameDuration;
            _currentFrame = (_currentFrame + 1) % _frameCount;
        }
    }

    /// <summary>
    /// Draws the current frame from the sprite sheet at the given position.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="texture">The sprite sheet texture.</param>
    /// <param name="position">World-space top-left position.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position)
    {
        spriteBatch.Draw(
            texture,
            position,
            SourceRectangle,
            Color.White);
    }
}
