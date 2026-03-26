using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;

namespace RiverRats.Game.Components;

/// <summary>
/// Animates a sprite sheet organized as rows (directions) × columns (walk frames).
/// Each row corresponds to a <see cref="FacingDirection"/> value.
/// Columns cycle: idle (0), step A (1), idle (2), step B (3).
/// </summary>
public sealed class SpriteAnimator
{
    private readonly int _frameWidth;
    private readonly int _frameHeight;
    private readonly int _framesPerDirection;

    private FrameTimer _timer;

    /// <summary>
    /// Initializes a SpriteAnimator with sheet layout parameters.
    /// </summary>
    /// <param name="frameWidth">Width of a single frame in pixels.</param>
    /// <param name="frameHeight">Height of a single frame in pixels.</param>
    /// <param name="framesPerDirection">Number of animation columns per direction row.</param>
    /// <param name="frameDuration">Seconds each frame is displayed before advancing.</param>
    public SpriteAnimator(int frameWidth, int frameHeight, int framesPerDirection, float frameDuration)
    {
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _framesPerDirection = framesPerDirection;
        _timer = new FrameTimer(framesPerDirection, frameDuration);
    }

    /// <summary>Current facing direction (selects the sheet row).</summary>
    public FacingDirection Direction { get; set; } = FacingDirection.Down;

    /// <summary>Current animation frame index (0-based column).</summary>
    public int CurrentFrame => _timer.CurrentFrame;

    /// <summary>
    /// Returns the source rectangle for the current frame on the sprite sheet.
    /// </summary>
    public Rectangle SourceRectangle => new(
        CurrentFrame * _frameWidth,
        (int)Direction * _frameHeight,
        _frameWidth,
        _frameHeight);

    /// <summary>
    /// Updates the animation timer. Advances frames while moving; resets to idle when stopped.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    /// <param name="isMoving">Whether the entity is currently moving.</param>
    public void Update(GameTime gameTime, bool isMoving)
    {
        if (!isMoving)
        {
            _timer.Reset();
            return;
        }

        _timer.Advance(gameTime);
    }

    /// <summary>
    /// Draws the current frame from the sprite sheet at the given position.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch.</param>
    /// <param name="texture">The sprite sheet texture.</param>
    /// <param name="position">World-space top-left position.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float layerDepth = 0f, Color? tint = null)
    {
        spriteBatch.Draw(
            texture,
            position,
            SourceRectangle,
            tint ?? Color.White,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            layerDepth);
    }
}
