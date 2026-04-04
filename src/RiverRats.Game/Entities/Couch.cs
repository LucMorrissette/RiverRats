using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;

namespace RiverRats.Game.Entities;

/// <summary>
/// Interactable couch entity that supports a sit sequence.
/// Characters can sit on the couch when facing it and pressing the action button.
/// Each couch provides two seat positions for the player and follower.
/// </summary>
public sealed class Couch : IWorldProp
{
    /// <summary>Interaction reach in pixels beyond the sprite bounds.</summary>
    private const int InteractionPaddingPixels = 14;

    /// <summary>
    /// Horizontal offset from the couch's left edge for a left-facing seated pose.
    /// Shifting the sprite left places the character on the open side of the couch
    /// instead of centering them over the full prop width.
    /// </summary>
        private const float SeatXOffsetFromCouch = -12f;

    /// <summary>Vertical offset from the couch top to the first (upper) seat.</summary>
        private const float SeatTopYOffset = -10f;

    /// <summary>Vertical offset from the couch top to the second (lower) seat.</summary>
        private const float SeatBottomYOffset = 22f;

        /// <summary>Reach in pixels for approaching the couch from its front side.</summary>
        private const int FrontApproachReachPixels = 20;

    private readonly Texture2D _texture;
    private readonly Vector2 _position;
    private readonly float _rotationRadians;
    private readonly int _scaledWidth;
    private readonly int _scaledHeight;

    /// <summary>
    /// Creates a couch entity at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels (after TMX origin conversion).</param>
    /// <param name="texture">Couch texture used for drawing and bounds.</param>
    /// <param name="rotationRadians">Clockwise rotation in radians from Tiled.</param>
    public Couch(Vector2 position, Texture2D texture, float rotationRadians = 0f)
        : this(position, texture.Width, texture.Height, rotationRadians)
    {
        _texture = texture;
    }

    /// <summary>
    /// Creates a couch entity with explicit dimensions (no texture required).
    /// Used for testing interaction logic without a GPU.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="width">Couch width in pixels.</param>
    /// <param name="height">Couch height in pixels.</param>
    /// <param name="rotationRadians">Clockwise rotation in radians.</param>
    internal Couch(Vector2 position, int width, int height, float rotationRadians = 0f)
    {
        _position = position;
        _rotationRadians = rotationRadians;
        _scaledWidth = width;
        _scaledHeight = height;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>World-space blocking bounds for this couch.</summary>
    public Rectangle Bounds => new(
        (int)_position.X,
        (int)_position.Y,
        _scaledWidth,
        _scaledHeight);

    /// <summary>World-space bounds expanded by the interaction padding.</summary>
    public Rectangle InteractionBounds => new(
        (int)_position.X - InteractionPaddingPixels,
        (int)_position.Y - InteractionPaddingPixels,
        _scaledWidth + InteractionPaddingPixels * 2,
        _scaledHeight + InteractionPaddingPixels * 2);

    /// <summary>
    /// World-space interaction strip on the open side of the couch.
    /// This old couch can only be sat on from the left while facing right.
    /// </summary>
    public Rectangle FrontInteractionBounds => new(
        (int)_position.X - FrontApproachReachPixels,
        (int)_position.Y - InteractionPaddingPixels,
        FrontApproachReachPixels,
        _scaledHeight + InteractionPaddingPixels * 2);

    /// <summary>Upper seat world position (top-left of a 32×32 character sprite).</summary>
    public Vector2 SeatPositionA => new(
        _position.X + SeatXOffsetFromCouch,
        _position.Y + SeatTopYOffset);

    /// <summary>Lower seat world position (top-left of a 32×32 character sprite).</summary>
    public Vector2 SeatPositionB => new(
        _position.X + SeatXOffsetFromCouch,
        _position.Y + SeatBottomYOffset);

    /// <summary>
    /// Determines whether an actor can interact with this couch.
    /// The actor must overlap the interaction bounds and face toward the couch.
    /// </summary>
    /// <param name="actorFootBounds">Actor's foot collision rectangle.</param>
    /// <param name="actorFacing">Direction the actor is currently facing.</param>
    /// <returns>True when the actor is in range and facing toward the couch.</returns>
    public bool CanInteract(Rectangle actorFootBounds, FacingDirection actorFacing)
    {
        if (!actorFootBounds.Intersects(FrontInteractionBounds))
        {
            return false;
        }

        return actorFacing == FacingDirection.Right;
    }

    /// <summary>
    /// Draws the couch in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth = 0f)
    {
        if (_rotationRadians == 0f)
        {
            spriteBatch.Draw(_texture, _position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        }
        else
        {
            var anchor = new Vector2(_position.X, _position.Y + _scaledHeight);
            var origin = new Vector2(0f, _texture.Height);
            spriteBatch.Draw(_texture, anchor, null, Color.White, _rotationRadians, origin, 1f, SpriteEffects.None, layerDepth);
        }
    }
}
