using Microsoft.Xna.Framework;
using DogDays.Game.Entities;

namespace DogDays.Tests.Unit;

public sealed class FrontDoorTests
{
    [Fact]
    public void Constructor__StartOpenFalse__StartsClosed()
    {
        var door = new FrontDoor(new Vector2(100f, 200f), new Point(20, 32));

        Assert.False(door.IsOpen);
    }

    [Fact]
    public void UpdateInvitationState__PlayerWithinSixteenthTile__OpensDoor()
    {
        var door = new FrontDoor(new Vector2(100f, 200f), new Point(20, 32));
        var playerBounds = new Rectangle(122, 202, 32, 32);

        door.UpdateInvitationState(playerBounds, invitationDistancePixels: 2);

        Assert.True(door.IsOpen);
    }

    [Fact]
    public void UpdateInvitationState__FollowerWithinRange__DoesNotOpenDoor()
    {
        var door = new FrontDoor(new Vector2(100f, 200f), new Point(20, 32));
        var followerBounds = new Rectangle(124, 202, 32, 32);

        door.UpdateInvitationState(new Rectangle(400, 400, 32, 32), invitationDistancePixels: 2);

        Assert.False(door.IsOpen);
    }

    [Fact]
    public void UpdateInvitationState__PlayerFarAway__ClosesDoor()
    {
        var door = new FrontDoor(new Vector2(100f, 200f), new Point(20, 32), startOpen: true);

        door.UpdateInvitationState(new Rectangle(300, 300, 32, 32), invitationDistancePixels: 2);

        Assert.False(door.IsOpen);
    }
}