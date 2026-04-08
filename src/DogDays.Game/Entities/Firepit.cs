using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Components;
using DogDays.Game.Graphics;

#nullable enable

namespace DogDays.Game.Entities;

/// <summary>
/// Interactive world prop rendered from a firepit sprite.
/// Can compose an attached <see cref="SmallFire"/> and toggle it on or off.
/// </summary>
public sealed class Firepit : IWorldProp
{
    private const int InteractionPaddingPixels = 12;

    private readonly Texture2D? _texture;
    private readonly Vector2 _position;
    private readonly Point _size;
    private readonly SmallFire? _smallFire;

    /// <summary>
    /// Creates a logic-only firepit at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="size">Firepit size in pixels.</param>
    /// <param name="smallFire">Optional attached fire effect.</param>
    /// <param name="startLit">Whether the attached fire starts lit.</param>
    public Firepit(Vector2 position, Point size, SmallFire? smallFire = null, bool startLit = true)
    {
        _position = position;
        _size = size;
        _smallFire = smallFire;
        IsLit = smallFire is not null && startLit;
    }

    /// <summary>
    /// Creates a drawable firepit prop at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="texture">Firepit texture used for drawing.</param>
    /// <param name="smallFire">Optional attached fire effect.</param>
    /// <param name="startLit">Whether the attached fire starts lit.</param>
    public Firepit(Vector2 position, Texture2D texture, SmallFire? smallFire = null, bool startLit = true)
        : this(position, new Point(texture.Width, texture.Height), smallFire, startLit)
    {
        _texture = texture;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>World-space center point used for proximity checks.</summary>
    public Vector2 Center => new(_position.X + (_size.X * 0.5f), _position.Y + (_size.Y * 0.5f));

    /// <summary>World-space area covered by this firepit sprite.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        _size.X,
        _size.Y);

    /// <summary>Expanded world-space area that actors can use to interact with the firepit.</summary>
    public Rectangle InteractionBounds => new(
        Bounds.X - InteractionPaddingPixels,
        Bounds.Y - InteractionPaddingPixels,
        Bounds.Width + (InteractionPaddingPixels * 2),
        Bounds.Height + (InteractionPaddingPixels * 2));

    /// <summary>Whether the attached fire is currently lit.</summary>
    public bool IsLit { get; private set; }

    /// <summary>
    /// Attaches a smoke emitter to the composed fire, if present.
    /// </summary>
    public void AttachSmokeEmitter(ParticleEmitter emitter)
    {
        _smallFire?.AttachSmokeEmitter(emitter);
    }

    /// <summary>
    /// Attaches a spark emitter to the composed fire, if present.
    /// </summary>
    public void AttachSparkEmitter(ParticleEmitter emitter)
    {
        _smallFire?.AttachSparkEmitter(emitter);
    }

    /// <summary>
    /// Returns true when the actor is close enough to interact with this firepit.
    /// </summary>
    public bool CanInteract(Rectangle actorBounds)
    {
        return InteractionBounds.Intersects(actorBounds);
    }

    /// <summary>
    /// Toggles the attached fire on or off.
    /// Does nothing if no fire effect is attached.
    /// </summary>
    public void ToggleLit()
    {
        if (_smallFire is null)
        {
            return;
        }

        IsLit = !IsLit;
    }

    /// <summary>
    /// Updates the attached fire effect when lit.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!IsLit || _smallFire is null)
        {
            return;
        }

        _smallFire.Update(gameTime);
    }

    /// <summary>
    /// Returns the current light snapshot when the firepit is lit.
    /// </summary>
    public bool TryGetLightData(out LightData lightData)
    {
        if (!IsLit || _smallFire is null)
        {
            lightData = default;
            return false;
        }

        lightData = _smallFire.GetLightData();
        return true;
    }

    /// <summary>
    /// Draws the firepit stone base in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        if (_texture is null)
        {
            throw new InvalidOperationException("A texture is required to draw Firepit.");
        }

        spriteBatch.Draw(_texture, _position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
    }

    /// <summary>
    /// Draws the attached fire effect in world space. Called in a separate pass
    /// after Y-sorted entities so flames always render on top of the stone base
    /// and are not occluded by characters walking past.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the fire overlay pass.</param>
    public void DrawFire(SpriteBatch spriteBatch)
    {
        if (IsLit)
        {
            _smallFire?.Draw(spriteBatch);
        }
    }
}
