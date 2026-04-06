using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#nullable enable

namespace RiverRats.Game.Entities;

/// <summary>
/// Static garden shed prop that swaps between closed/open door sprites.
/// The door opens while an actor's foot bounds overlap the shed ramp area.
/// </summary>
public sealed class GardenShed : IWorldProp
{
    private static readonly Point RampReferenceSize = new(64, 64);
    private static readonly Rectangle RampLocalBoundsReference = new(17, 48, 30, 16);

    private readonly Texture2D? _closedTexture;
    private readonly Texture2D? _openTexture;
    private readonly Vector2 _position;
    private readonly Point _size;
    private readonly int _collisionHeightPixels;
    private readonly int _collisionYOffset;
    private readonly Rectangle _rampBounds;

    /// <summary>
    /// Creates a logic-only garden shed at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="size">Shed draw size in pixels.</param>
    /// <param name="startOpen">Whether the shed door starts open.</param>
    /// <param name="collisionHeightPixels">Collision height in pixels measured from the bottom of the shed.</param>
    /// <param name="collisionYOffset">Pixel offset applied to the collision bounds Y position.</param>
    /// <param name="suppressOcclusion">When true, the reveal lens will not activate behind this prop.</param>
    public GardenShed(
        Vector2 position,
        Point size,
        bool startOpen = false,
        int collisionHeightPixels = 0,
        int collisionYOffset = 0,
        bool suppressOcclusion = false)
        : this(
            position,
            size,
            ScaleRampLocalBounds(size),
            startOpen,
            collisionHeightPixels,
            collisionYOffset,
            suppressOcclusion)
    {
    }

    /// <summary>
    /// Creates a logic-only garden shed at a world position with a custom local ramp bounds.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="size">Shed draw size in pixels.</param>
    /// <param name="rampLocalBounds">Ramp bounds relative to the shed top-left origin.</param>
    /// <param name="startOpen">Whether the shed door starts open.</param>
    /// <param name="collisionHeightPixels">Collision height in pixels measured from the bottom of the shed.</param>
    /// <param name="collisionYOffset">Pixel offset applied to the collision bounds Y position.</param>
    /// <param name="suppressOcclusion">When true, the reveal lens will not activate behind this prop.</param>
    public GardenShed(
        Vector2 position,
        Point size,
        Rectangle rampLocalBounds,
        bool startOpen = false,
        int collisionHeightPixels = 0,
        int collisionYOffset = 0,
        bool suppressOcclusion = false)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Shed size must be greater than zero on both axes.");
        }

        _position = position;
        _size = size;
        _collisionHeightPixels = Math.Min(
            size.Y,
            collisionHeightPixels > 0 ? collisionHeightPixels : size.Y);
        _collisionYOffset = collisionYOffset;

        var clampedRampLocalBounds = ClampLocalBounds(rampLocalBounds, size);
        _rampBounds = ToWorldBounds(position, clampedRampLocalBounds);

        IsDoorOpen = startOpen;
        SuppressOcclusion = suppressOcclusion;
    }

    /// <summary>
    /// Creates a textured garden shed prop at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="closedTexture">Closed-door shed texture used for drawing.</param>
    /// <param name="openTexture">Open-door shed texture used for drawing.</param>
    /// <param name="startOpen">Whether the shed door starts open.</param>
    /// <param name="collisionHeightPixels">Collision height in source-texture pixels measured from the bottom of the shed.</param>
    /// <param name="scale">Uniform draw scale applied to both textures and bounds.</param>
    /// <param name="collisionYOffset">Pixel offset applied to collision Y in source-texture pixels.</param>
    /// <param name="suppressOcclusion">When true, the reveal lens will not activate behind this prop.</param>
    public GardenShed(
        Vector2 position,
        Texture2D closedTexture,
        Texture2D openTexture,
        bool startOpen = false,
        int collisionHeightPixels = 0,
        float scale = 1f,
        int collisionYOffset = 0,
        bool suppressOcclusion = false)
    {
        if (closedTexture is null)
        {
            throw new ArgumentNullException(nameof(closedTexture));
        }

        if (openTexture is null)
        {
            throw new ArgumentNullException(nameof(openTexture));
        }

        if (scale <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be greater than zero.");
        }

        _position = position;
        _closedTexture = closedTexture;
        _openTexture = openTexture;

        var sourceWidth = Math.Max(closedTexture.Width, openTexture.Width);
        var sourceHeight = Math.Max(closedTexture.Height, openTexture.Height);
        _size = new Point(ScaleDimension(sourceWidth, scale), ScaleDimension(sourceHeight, scale));

        var sourceCollisionHeight = collisionHeightPixels > 0 ? collisionHeightPixels : sourceHeight;
        _collisionHeightPixels = Math.Min(_size.Y, ScaleDimension(sourceCollisionHeight, scale));
        _collisionYOffset = ScaleSignedDimension(collisionYOffset, scale);

        var rampLocalBounds = ScaleRampLocalBounds(_size);
        _rampBounds = ToWorldBounds(position, rampLocalBounds);

        IsDoorOpen = startOpen;
        SuppressOcclusion = suppressOcclusion;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>When true this instance uses the open-door shed sprite.</summary>
    public bool IsDoorOpen { get; private set; }

    /// <summary>When true, the reveal lens will not activate when a character walks behind this shed.</summary>
    public bool SuppressOcclusion { get; }

    /// <summary>World-space footprint of the ramp used to trigger door opening.</summary>
    public Rectangle RampBounds => _rampBounds;

    /// <summary>World-space blocking bounds for this shed.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y + _size.Y - _collisionHeightPixels + _collisionYOffset,
        _size.X,
        _collisionHeightPixels);

    /// <summary>
    /// Updates the door state from the provided actor foot bounds.
    /// </summary>
    /// <param name="actorFootBounds">Actor foot bounds in world space.</param>
    public void UpdateDoorState(Rectangle actorFootBounds)
    {
        if (actorFootBounds.Width <= 0 || actorFootBounds.Height <= 0)
        {
            IsDoorOpen = false;
            return;
        }

        IsDoorOpen = actorFootBounds.Intersects(_rampBounds);
    }

    /// <summary>
    /// Draws the shed in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        var texture = IsDoorOpen ? _openTexture : _closedTexture;
        if (texture is null)
        {
            throw new InvalidOperationException("Shed textures are required to draw GardenShed.");
        }

        var destination = new Rectangle((int)_position.X, (int)_position.Y, _size.X, _size.Y);
        spriteBatch.Draw(texture, destination, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, layerDepth);
    }

    private static Rectangle ScaleRampLocalBounds(Point targetSize)
    {
        var x = ScaleAxis(RampLocalBoundsReference.X, RampReferenceSize.X, targetSize.X);
        var y = ScaleAxis(RampLocalBoundsReference.Y, RampReferenceSize.Y, targetSize.Y);
        var width = Math.Max(1, ScaleAxis(RampLocalBoundsReference.Width, RampReferenceSize.X, targetSize.X));
        var height = Math.Max(1, ScaleAxis(RampLocalBoundsReference.Height, RampReferenceSize.Y, targetSize.Y));
        return new Rectangle(x, y, width, height);
    }

    private static Rectangle ClampLocalBounds(Rectangle localBounds, Point size)
    {
        var x = Math.Clamp(localBounds.X, 0, Math.Max(0, size.X - 1));
        var y = Math.Clamp(localBounds.Y, 0, Math.Max(0, size.Y - 1));
        var width = Math.Clamp(localBounds.Width, 1, Math.Max(1, size.X - x));
        var height = Math.Clamp(localBounds.Height, 1, Math.Max(1, size.Y - y));
        return new Rectangle(x, y, width, height);
    }

    private static Rectangle ToWorldBounds(Vector2 position, Rectangle localBounds)
    {
        return new Rectangle(
            (int)position.X + localBounds.X,
            (int)position.Y + localBounds.Y,
            localBounds.Width,
            localBounds.Height);
    }

    private static int ScaleAxis(int value, int sourceSize, int targetSize)
    {
        return (int)MathF.Round(value * (targetSize / (float)sourceSize));
    }

    private static int ScaleDimension(int pixelSize, float scale)
    {
        return Math.Max(1, (int)MathF.Round(pixelSize * scale));
    }

    private static int ScaleSignedDimension(int pixelOffset, float scale)
    {
        return (int)MathF.Round(pixelOffset * scale);
    }
}
