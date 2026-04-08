using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="Couch"/> interaction detection.
/// Uses the internal test constructor (no Texture2D required).
/// </summary>
public class CouchTests
{
    private const int CouchWidth = 24;
    private const int CouchHeight = 80;
    private static readonly Vector2 CouchPosition = new(100f, 100f);

    private static Couch CreateTestCouch()
    {
        return new Couch(CouchPosition, CouchWidth, CouchHeight);
    }

    [Fact]
    public void Bounds__ReturnsCorrectDimensions()
    {
        var couch = CreateTestCouch();

        var bounds = couch.Bounds;

        Assert.Equal(100, bounds.X);
        Assert.Equal(100, bounds.Y);
        Assert.Equal(CouchWidth, bounds.Width);
        Assert.Equal(CouchHeight, bounds.Height);
    }

    [Fact]
    public void SeatPositionA__IsInUpperArea()
    {
        var couch = CreateTestCouch();

        var seat = couch.SeatPositionA;

        Assert.Equal(new Vector2(88f, 90f), seat);
    }

    [Fact]
    public void SeatPositionB__IsInLowerArea()
    {
        var couch = CreateTestCouch();

        var seat = couch.SeatPositionB;

        Assert.Equal(new Vector2(88f, 122f), seat);
    }

    [Fact]
    public void CanInteract__ActorFarAway__ReturnsFalse()
    {
        var couch = CreateTestCouch();
        var actorBounds = new Rectangle(300, 300, 19, 8);

        Assert.False(couch.CanInteract(actorBounds, FacingDirection.Up));
    }

    [Fact]
    public void CanInteract__ActorNearAndFacingToward__ReturnsTrue()
    {
        var couch = CreateTestCouch();
        // Actor is on the open side of the couch, facing right toward the seat.
        var actorBounds = new Rectangle(82, 130, 19, 8);

        Assert.True(couch.CanInteract(actorBounds, FacingDirection.Right));
    }

    [Fact]
    public void CanInteract__ActorNearButFacingAway__ReturnsFalse()
    {
        var couch = CreateTestCouch();
        // Actor is on the open side of the couch but facing the couch back.
        var actorBounds = new Rectangle(82, 130, 19, 8);

        Assert.False(couch.CanInteract(actorBounds, FacingDirection.Left));
    }

    [Fact]
    public void CanInteract__ActorToLeft_FacingRight__ReturnsTrue()
    {
        var couch = CreateTestCouch();
        // Actor to the left of the couch, facing right toward it.
        var actorBounds = new Rectangle(82, 130, 19, 8);

        Assert.True(couch.CanInteract(actorBounds, FacingDirection.Right));
    }

    [Fact]
    public void CanInteract__ActorToLeft_FacingLeft__ReturnsFalse()
    {
        var couch = CreateTestCouch();
        // Actor to the left of the couch, facing left (away).
        var actorBounds = new Rectangle(82, 130, 19, 8);

        Assert.False(couch.CanInteract(actorBounds, FacingDirection.Left));
    }

    [Fact]
    public void CanInteract__ActorAbove_FacingDown__ReturnsFalse()
    {
        var couch = CreateTestCouch();
        // Actor above the couch cannot sit because this couch is only enterable from the left side.
        var actorBounds = new Rectangle(105, 88, 19, 8);

        Assert.False(couch.CanInteract(actorBounds, FacingDirection.Down));
    }

    [Fact]
    public void CanInteract__ActorToRight_FacingLeft__ReturnsFalse()
    {
        var couch = CreateTestCouch();
        var actorBounds = new Rectangle(124, 130, 19, 8);

        Assert.False(couch.CanInteract(actorBounds, FacingDirection.Left));
    }
}
