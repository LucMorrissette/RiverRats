using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="Watercraft"/> interaction and boarding geometry.
/// </summary>
public class WatercraftTests
{
    private const int WatercraftWidth = 21;
    private const int WatercraftHeight = 64;
    private const int HorizontalWatercraftWidth = 64;
    private const int HorizontalWatercraftHeight = 16;
    private static readonly Vector2 WatercraftPosition = new(100f, 100f);

    private static Watercraft CreateWatercraft()
    {
        return new Watercraft(WatercraftPosition, WatercraftWidth, WatercraftHeight, HorizontalWatercraftWidth, HorizontalWatercraftHeight);
    }

    [Fact]
    public void Bounds__ReturnsCorrectDimensions()
    {
        var watercraft = CreateWatercraft();

        var bounds = watercraft.Bounds;

        Assert.Equal(100, bounds.X);
        Assert.Equal(100, bounds.Y);
        Assert.Equal(WatercraftWidth, bounds.Width);
        Assert.Equal(WatercraftHeight, bounds.Height);
    }

    [Fact]
    public void CanInteract__ActorBelowAndFacingUp__ReturnsTrue()
    {
        var watercraft = CreateWatercraft();
        var actorBounds = new Rectangle(102, 157, 19, 8);

        Assert.True(watercraft.CanInteract(actorBounds, FacingDirection.Up));
    }

    [Fact]
    public void CanInteract__ActorBelowAndFacingAway__ReturnsFalse()
    {
        var watercraft = CreateWatercraft();
        var actorBounds = new Rectangle(102, 157, 19, 8);

        Assert.False(watercraft.CanInteract(actorBounds, FacingDirection.Down));
    }

    [Fact]
    public void CanInteract__ActorLeftAndFacingRight__ReturnsTrue()
    {
        var watercraft = CreateWatercraft();
        var actorBounds = new Rectangle(78, 126, 19, 8);

        Assert.True(watercraft.CanInteract(actorBounds, FacingDirection.Right));
    }

    [Fact]
    public void GetBoardPosition__CentersActorInsideHull()
    {
        var watercraft = CreateWatercraft();

        var boardPosition = watercraft.GetBoardPosition(actorWidth: 32, actorHeight: 32);

        Assert.Equal(new Vector2(94.5f, 118f), boardPosition);
    }

    [Fact]
    public void GetRearSeatPosition__WhenFacingDown__PlacesActorFurtherBackInHull()
    {
        var watercraft = CreateWatercraft();

        var seatPosition = watercraft.GetRearSeatPosition(actorWidth: 32, actorHeight: 32);

        Assert.Equal(new Vector2(94.5f, 96f), seatPosition);
    }

    [Fact]
    public void GetFrontSeatPosition__WhenFacingUp__MovesPlayerToBow()
    {
        var watercraft = CreateWatercraft();
        watercraft.SetState(watercraft.Center, FacingDirection.Up);

        var seatPosition = watercraft.GetFrontSeatPosition(actorWidth: 32, actorHeight: 32);

        Assert.Equal(new Vector2(94.5f, 96f), seatPosition);
    }

    [Fact]
    public void SetState__FacingRight__UsesHorizontalBounds()
    {
        var watercraft = CreateWatercraft();

        watercraft.SetState(watercraft.Center, FacingDirection.Right);

        Assert.Equal(new Rectangle(79, 124, HorizontalWatercraftWidth, HorizontalWatercraftHeight), watercraft.Bounds);
    }

    [Fact]
    public void GetFrontSeatPosition__WhenFacingRight__PlacesPlayerInsideSideViewHull()
    {
        var watercraft = CreateWatercraft();
        watercraft.SetState(watercraft.Center, FacingDirection.Right);

        var seatPosition = watercraft.GetFrontSeatPosition(actorWidth: 32, actorHeight: 32);

        Assert.Equal(new Vector2(108f, 110f), seatPosition);
    }

    [Fact]
    public void GetRearSeatPosition__WhenFacingLeftAndRight__MirrorsDistanceFromStern()
    {
        var watercraft = CreateWatercraft();

        watercraft.SetState(watercraft.Center, FacingDirection.Right);
        var rightRearSeat = watercraft.GetRearSeatPosition(actorWidth: 32, actorHeight: 32);
        var rightSternInset = rightRearSeat.X - watercraft.Bounds.Left;

        watercraft.SetState(watercraft.Center, FacingDirection.Left);
        var leftRearSeat = watercraft.GetRearSeatPosition(actorWidth: 32, actorHeight: 32);
        var leftSternInset = watercraft.Bounds.Right - (leftRearSeat.X + 32f);

        Assert.Equal(rightSternInset, leftSternInset);
    }

    [Fact]
    public void ContainsPoint__BoardPositionCenter__ReturnsTrue()
    {
        var watercraft = CreateWatercraft();
        var boardPosition = watercraft.GetBoardPosition(actorWidth: 32, actorHeight: 32);
        var actorCenter = boardPosition + new Vector2(16f, 16f);

        Assert.True(watercraft.ContainsPoint(actorCenter));
    }

    [Fact]
    public void ContainsPoint__PointOutsideHull__ReturnsFalse()
    {
        var watercraft = CreateWatercraft();

        Assert.False(watercraft.ContainsPoint(new Vector2(84f, 134f)));
    }
}