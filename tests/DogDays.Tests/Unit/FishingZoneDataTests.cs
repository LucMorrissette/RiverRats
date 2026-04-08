using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using Xunit;

namespace DogDays.Tests.Unit;

public sealed class FishingZoneDataTests
{
    [Fact]
    public void Bounds__StoresCorrectRectangle()
    {
        var bounds = new Rectangle(100, 200, 32, 16);
        var zone = new FishingZoneData(bounds, FacingDirection.Down);

        Assert.Equal(bounds, zone.Bounds);
    }

    [Theory]
    [InlineData(FacingDirection.Down)]
    [InlineData(FacingDirection.Up)]
    [InlineData(FacingDirection.Left)]
    [InlineData(FacingDirection.Right)]
    public void FacingDirection__StoresCorrectDirection(FacingDirection facing)
    {
        var zone = new FishingZoneData(new Rectangle(0, 0, 16, 16), facing);

        Assert.Equal(facing, zone.FacingDirection);
    }

    [Fact]
    public void Equality__SameValues__AreEqual()
    {
        var a = new FishingZoneData(new Rectangle(10, 20, 30, 40), FacingDirection.Down);
        var b = new FishingZoneData(new Rectangle(10, 20, 30, 40), FacingDirection.Down);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality__DifferentFacing__AreNotEqual()
    {
        var a = new FishingZoneData(new Rectangle(10, 20, 30, 40), FacingDirection.Down);
        var b = new FishingZoneData(new Rectangle(10, 20, 30, 40), FacingDirection.Up);

        Assert.NotEqual(a, b);
    }
}
