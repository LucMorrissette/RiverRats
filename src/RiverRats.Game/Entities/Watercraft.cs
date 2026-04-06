using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;

namespace RiverRats.Game.Entities;

/// <summary>
/// Static surface craft prop rendered from a single sprite and placed from TMX prop metadata.
/// </summary>
public sealed class Watercraft : IWorldProp
{
    private const int InteractionPaddingPixels = 16;
    private const float UpperSeatInsetRatio = -0.125f;
    private const float LowerSeatInsetRatio = 0.55f;
    private const float LeftSeatInsetRatio = 0.08f;
    private const float RightSeatInsetRatio = 0.72f;
    private const float SideSeatHullCoverRatio = 0.90f;

    private readonly Texture2D _verticalTexture = null!;
    private readonly Texture2D _horizontalTexture = null!;
    private readonly Vector2 _initialPlacementPosition;
    private readonly int _verticalWidth;
    private readonly int _verticalHeight;
    private readonly int _horizontalWidth;
    private readonly int _horizontalHeight;

    private Vector2 _center;
    private FacingDirection _facing = FacingDirection.Down;

    /// <summary>
    /// Creates a watercraft prop at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="verticalTexture">Vertical watercraft texture used for up/down facing.</param>
    /// <param name="horizontalTexture">Horizontal watercraft texture used for left/right facing.</param>
    public Watercraft(Vector2 position, Texture2D verticalTexture, Texture2D horizontalTexture)
        : this(position, verticalTexture.Width, verticalTexture.Height, horizontalTexture.Width, horizontalTexture.Height)
    {
        _verticalTexture = verticalTexture;
        _horizontalTexture = horizontalTexture;
    }

    /// <summary>
    /// Creates a watercraft prop with explicit dimensions (no texture required).
    /// Used for testing interaction logic without a GPU.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="verticalWidth">Vertical watercraft width in pixels.</param>
    /// <param name="verticalHeight">Vertical watercraft height in pixels.</param>
    /// <param name="horizontalWidth">Horizontal watercraft width in pixels.</param>
    /// <param name="horizontalHeight">Horizontal watercraft height in pixels.</param>
    internal Watercraft(Vector2 position, int verticalWidth, int verticalHeight, int horizontalWidth, int horizontalHeight)
    {
        _initialPlacementPosition = position;
        _verticalWidth = verticalWidth;
        _verticalHeight = verticalHeight;
        _horizontalWidth = horizontalWidth;
        _horizontalHeight = horizontalHeight;
        _center = ComputeCenterFromTopLeft(position, FacingDirection.Down);
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => GetTopLeftForState(_center, _facing);

    /// <summary>Initial TMX placement position.</summary>
    public Vector2 InitialPlacementPosition => _initialPlacementPosition;

    /// <summary>Current center point used for movement and orientation swaps.</summary>
    public Vector2 Center => _center;

    /// <summary>Current travel-facing direction.</summary>
    public FacingDirection Facing => _facing;

    /// <summary>Whether the craft is currently in its side-view orientation.</summary>
    public bool IsSideView => _facing == FacingDirection.Left || _facing == FacingDirection.Right;

    /// <summary>World-space sprite bounds used for Y-sorting.</summary>
    public Rectangle Bounds => GetBoundsForState(_center, _facing);

    /// <summary>World-space bounds expanded slightly for boarding interaction checks.</summary>
    public Rectangle InteractionBounds => new(
        Bounds.X - InteractionPaddingPixels,
        Bounds.Y - InteractionPaddingPixels,
        Bounds.Width + (InteractionPaddingPixels * 2),
        Bounds.Height + (InteractionPaddingPixels * 2));

    /// <summary>
    /// Applies a new center point and facing to the craft.
    /// </summary>
    internal void SetState(Vector2 center, FacingDirection facing)
    {
        _center = center;
        _facing = facing;
    }

    /// <summary>
    /// Computes the bounds the craft would occupy for a candidate state.
    /// </summary>
    internal Rectangle GetBoundsForState(Vector2 center, FacingDirection facing)
    {
        var size = GetSizeForFacing(facing);
        var position = GetTopLeftForState(center, facing);
        return new Rectangle(
            (int)position.X,
            (int)position.Y,
            size.X,
            size.Y);
    }

    /// <summary>
    /// Determines whether an actor can board this watercraft from their current position.
    /// The actor must be near the craft and facing toward its center.
    /// </summary>
    /// <param name="actorFootBounds">Actor foot collision rectangle.</param>
    /// <param name="actorFacing">Direction the actor is currently facing.</param>
    /// <returns>True when the actor is close enough and looking toward the craft.</returns>
    public bool CanInteract(Rectangle actorFootBounds, FacingDirection actorFacing)
    {
        if (!actorFootBounds.Intersects(InteractionBounds))
        {
            return false;
        }

        var deltaX = Bounds.Center.X - actorFootBounds.Center.X;
        var deltaY = Bounds.Center.Y - actorFootBounds.Center.Y;
        if (deltaX == 0 && deltaY == 0)
        {
            return false;
        }

        var requiredFacing = Math.Abs(deltaX) >= Math.Abs(deltaY)
            ? deltaX >= 0 ? FacingDirection.Right : FacingDirection.Left
            : deltaY >= 0 ? FacingDirection.Down : FacingDirection.Up;

        return actorFacing == requiredFacing;
    }

    /// <summary>
    /// Returns the preferred lower-seat landing point used for one-shot board-position checks.
    /// </summary>
    /// <param name="actorWidth">Actor sprite width in pixels.</param>
    /// <param name="actorHeight">Actor sprite height in pixels.</param>
    internal Vector2 GetBoardPosition(int actorWidth, int actorHeight)
    {
        return GetFrontSeatPosition(actorWidth, actorHeight);
    }

    /// <summary>Front seat world position (top-left of the actor sprite).</summary>
    internal Vector2 GetFrontSeatPosition(int actorWidth, int actorHeight)
    {
        return _facing switch
        {
            FacingDirection.Up => GetVerticalSeatPosition(actorWidth, actorHeight, UpperSeatInsetRatio),
            FacingDirection.Down => GetVerticalSeatPosition(actorWidth, actorHeight, LowerSeatInsetRatio),
            FacingDirection.Left => GetHorizontalSeatPosition(actorWidth, actorHeight, LeftSeatInsetRatio),
            _ => GetHorizontalSeatPosition(actorWidth, actorHeight, RightSeatInsetRatio),
        };
    }

    /// <summary>Rear seat world position (top-left of the actor sprite).</summary>
    internal Vector2 GetRearSeatPosition(int actorWidth, int actorHeight)
    {
        return _facing switch
        {
            FacingDirection.Up => GetVerticalSeatPosition(actorWidth, actorHeight, LowerSeatInsetRatio),
            FacingDirection.Down => GetVerticalSeatPosition(actorWidth, actorHeight, UpperSeatInsetRatio),
            FacingDirection.Left => GetHorizontalSeatPosition(actorWidth, actorHeight, RightSeatInsetRatio),
            _ => GetHorizontalSeatPosition(actorWidth, actorHeight, LeftSeatInsetRatio),
        };
    }

    private Vector2 GetVerticalSeatPosition(int actorWidth, int actorHeight, float verticalInsetRatio)
    {
        var verticalInset = MathF.Round((_verticalHeight - actorHeight) * verticalInsetRatio);
        var position = GetTopLeftForState(_center, _facing);
        return new Vector2(
            position.X + ((_verticalWidth - actorWidth) * 0.5f),
            position.Y + verticalInset);
    }

    private Vector2 GetHorizontalSeatPosition(int actorWidth, int actorHeight, float horizontalInsetRatio)
    {
        var position = GetTopLeftForState(_center, _facing);
        var verticalLift = MathF.Round((actorHeight - _horizontalHeight) * SideSeatHullCoverRatio);
        return new Vector2(
            position.X + MathF.Round((_horizontalWidth - actorWidth) * horizontalInsetRatio),
            position.Y - verticalLift);
    }

    /// <summary>
    /// Returns whether a world-space point lies within the watercraft hull bounds.
    /// Used to keep occupants rendered above the craft.
    /// </summary>
    /// <param name="worldPoint">World-space point to test.</param>
    internal bool ContainsPoint(Vector2 worldPoint)
    {
        return Bounds.Contains((int)MathF.Round(worldPoint.X), (int)MathF.Round(worldPoint.Y));
    }

    /// <summary>
    /// Watercraft should not trigger the occlusion reveal lens.
    /// </summary>
    public bool SuppressOcclusion => true;

    /// <summary>
    /// Draws the watercraft in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the world pass.</param>
    /// <param name="layerDepth">Depth value for Y-sorting (0 = back, 1 = front).</param>
    public void Draw(SpriteBatch spriteBatch, float layerDepth)
    {
        var texture = IsSideView ? _horizontalTexture : _verticalTexture;
        if (texture is null)
        {
            throw new InvalidOperationException("Watercraft textures are required for drawing.");
        }

        spriteBatch.Draw(texture, Position, null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
    }

    /// <summary>
    /// Computes the new center that keeps the rear-seat occupant fixed when turning.
    /// The pivot is the center of the rear seat actor sprite.
    /// </summary>
    /// <param name="newFacing">Direction to turn toward.</param>
    /// <param name="actorWidth">Actor sprite width in pixels.</param>
    /// <param name="actorHeight">Actor sprite height in pixels.</param>
    internal Vector2 GetPivotedCenterForTurn(FacingDirection newFacing, int actorWidth, int actorHeight)
    {
        // Compute rear-seat actor center in current orientation.
        var oldRearTopLeft = GetRearSeatPosition(actorWidth, actorHeight);
        var rearAnchor = new Vector2(
            oldRearTopLeft.X + (actorWidth * 0.5f),
            oldRearTopLeft.Y + (actorHeight * 0.5f));

        // Brute-force solve: find the watercraft center such that the rear seat
        // center in the new facing matches rearAnchor.
        // RearSeat = f(center, facing) → center = inverse(rearAnchor, facing).
        return SolveRearSeatCenter(rearAnchor, newFacing, actorWidth, actorHeight);
    }

    private Vector2 SolveRearSeatCenter(Vector2 rearActorCenter, FacingDirection facing, int actorWidth, int actorHeight)
    {
        // Rear seat inset ratio for the target facing.
        var isVertical = facing == FacingDirection.Up || facing == FacingDirection.Down;
        if (isVertical)
        {
            var insetRatio = facing == FacingDirection.Down ? UpperSeatInsetRatio : LowerSeatInsetRatio;
            var verticalInset = MathF.Round((_verticalHeight - actorHeight) * insetRatio);
            // rearActorCenter.Y = topLeft.Y + verticalInset + actorHeight * 0.5
            // topLeft.Y = center.Y - verticalHeight * 0.5
            // → center.Y = rearActorCenter.Y - verticalInset - actorHeight * 0.5 + verticalHeight * 0.5
            var centerY = rearActorCenter.Y - verticalInset - (actorHeight * 0.5f) + (_verticalHeight * 0.5f);
            return new Vector2(rearActorCenter.X, centerY);
        }
        else
        {
            var insetRatio = facing == FacingDirection.Left ? RightSeatInsetRatio : LeftSeatInsetRatio;
            var horizontalInset = MathF.Round((_horizontalWidth - actorWidth) * insetRatio);
            // rearActorCenter.X = topLeft.X + horizontalInset + actorWidth * 0.5
            // topLeft.X = center.X - horizontalWidth * 0.5
            // → center.X = rearActorCenter.X - horizontalInset - actorWidth * 0.5 + horizontalWidth * 0.5
            var centerX = rearActorCenter.X - horizontalInset - (actorWidth * 0.5f) + (_horizontalWidth * 0.5f);
            // Y: rear seat verticalLift positions actor relative to canoe, keep hull center Y.
            var verticalLift = MathF.Round((actorHeight - _horizontalHeight) * SideSeatHullCoverRatio);
            // rearActorCenter.Y = topLeft.Y - verticalLift + actorHeight * 0.5
            // topLeft.Y = center.Y - horizontalHeight * 0.5
            var centerY = rearActorCenter.Y + verticalLift - (actorHeight * 0.5f) + (_horizontalHeight * 0.5f);
            return new Vector2(centerX, centerY);
        }
    }

    private Point GetSizeForFacing(FacingDirection facing)
    {
        return facing == FacingDirection.Left || facing == FacingDirection.Right
            ? new Point(_horizontalWidth, _horizontalHeight)
            : new Point(_verticalWidth, _verticalHeight);
    }

    private Vector2 GetTopLeftForState(Vector2 center, FacingDirection facing)
    {
        var size = GetSizeForFacing(facing);
        return new Vector2(
            MathF.Round(center.X - (size.X * 0.5f), MidpointRounding.AwayFromZero),
            MathF.Round(center.Y - (size.Y * 0.5f), MidpointRounding.AwayFromZero));
    }

    private Vector2 ComputeCenterFromTopLeft(Vector2 position, FacingDirection facing)
    {
        var size = GetSizeForFacing(facing);
        return new Vector2(
            position.X + (size.X * 0.5f),
            position.Y + (size.Y * 0.5f));
    }
}