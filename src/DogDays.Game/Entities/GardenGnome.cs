using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#nullable enable

namespace DogDays.Game.Entities;

/// <summary>
/// Decorative garden gnome that peeks out from behind a tree.
/// When the player approaches within a proximity radius the gnome
/// slides behind the nearest tree and hides; it slides back out
/// once the player moves away.
/// </summary>
public sealed class GardenGnome : IWorldProp
{
    /// <summary>Proximity radius in pixels — 3 tiles of 32px each.</summary>
    private const float ProximityRadius = 3f * 32f;
    private const float ProximityRadiusSquared = ProximityRadius * ProximityRadius;

    /// <summary>How far the gnome slides to hide (in pixels).</summary>
    private const float HideDistance = 20f;

    /// <summary>Hide speed in pixels per second when the player gets close.</summary>
    private const float HideSlideSpeed = 120f;

    /// <summary>Reveal speed in pixels per second when the player moves away.</summary>
    private const float RevealSlideSpeed = 36f;

    private readonly Texture2D? _texture;
    private readonly Vector2 _homePosition;
    private readonly Vector2 _hideDirection;
    private readonly float _rotationRadians;
    private readonly Point _size;

    private Vector2 _currentPosition;
    private float _slideProgress; // 0 = home, 1 = fully hidden

    /// <summary>
    /// Creates a logic-only garden gnome for tests.
    /// </summary>
    /// <param name="homePosition">World-space top-left position.</param>
    /// <param name="size">Gnome sprite size in pixels.</param>
    /// <param name="hideTarget">World-space center of the tree trunk to hide behind.</param>
    /// <param name="rotationRadians">Clockwise rotation in radians from Tiled.</param>
    public GardenGnome(Vector2 homePosition, Point size, Vector2 hideTarget, float rotationRadians = 0f)
    {
        _homePosition = homePosition;
        _currentPosition = homePosition;
        _rotationRadians = rotationRadians;
        _size = size;

        var gnomeCenter = homePosition + new Vector2(size.X * 0.5f, size.Y * 0.5f);
        var toTree = hideTarget - gnomeCenter;
        _hideDirection = toTree.LengthSquared() > 0.001f
            ? Vector2.Normalize(toTree)
            : -Vector2.UnitX; // fallback: slide left
    }

    /// <summary>
    /// Creates a garden gnome that hides behind the nearest tree.
    /// </summary>
    /// <param name="homePosition">World-space top-left position from the TMX placement.</param>
    /// <param name="texture">Gnome sprite texture.</param>
    /// <param name="hideTarget">World-space center of the tree trunk to hide behind.</param>
    /// <param name="rotationRadians">Clockwise rotation in radians from Tiled.</param>
    public GardenGnome(Vector2 homePosition, Texture2D texture, Vector2 hideTarget, float rotationRadians = 0f)
        : this(homePosition, new Point(texture.Width, texture.Height), hideTarget, rotationRadians)
    {
        _texture = texture;
    }

    /// <summary>Top-left world position in pixels (current, not home).</summary>
    public Vector2 Position => _currentPosition;

    /// <summary>World-space bounding rectangle at current position.</summary>
    public Rectangle Bounds => new(
        (int)_currentPosition.X,
        (int)_currentPosition.Y,
        _size.X,
        _size.Y);

    /// <summary>
    /// Updates the gnome's hide/reveal state based on player proximity.
    /// </summary>
    public void Update(GameTime gameTime, Vector2 playerPosition)
    {
        var gnomeCenter = _homePosition + new Vector2(_size.X * 0.5f, _size.Y * 0.5f);
        var distanceSquared = Vector2.DistanceSquared(playerPosition, gnomeCenter);
        var playerIsNear = distanceSquared < ProximityRadiusSquared;

        var targetProgress = playerIsNear ? 1f : 0f;
        if (MathF.Abs(_slideProgress - targetProgress) > 0.001f)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var slideSpeed = playerIsNear ? HideSlideSpeed : RevealSlideSpeed;
            var step = slideSpeed / HideDistance * dt; // normalized speed
            _slideProgress = playerIsNear
                ? MathF.Min(_slideProgress + step, 1f)
                : MathF.Max(_slideProgress - step, 0f);
        }
        else
        {
            _slideProgress = targetProgress;
        }

        _currentPosition = _homePosition + _hideDirection * (HideDistance * _slideProgress);
    }

    /// <summary>
    /// Draws the gnome at its current position. Fully hidden gnomes are not drawn.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        if (_texture is null)
        {
            throw new InvalidOperationException("A texture is required to draw GardenGnome.");
        }

        if (_slideProgress >= 1f)
        {
            return; // fully hidden behind the tree
        }

        if (_rotationRadians == 0f)
        {
            spriteBatch.Draw(_texture, _currentPosition, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        }
        else
        {
            var anchor = new Vector2(_currentPosition.X, _currentPosition.Y + _texture.Height);
            var origin = new Vector2(0f, _texture.Height);
            spriteBatch.Draw(_texture, anchor, null, Color.White, _rotationRadians, origin, 1f, SpriteEffects.None, layerDepth);
        }
    }
}
