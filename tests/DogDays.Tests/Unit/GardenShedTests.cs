using Microsoft.Xna.Framework;
using DogDays.Game.Entities;

namespace DogDays.Tests.Unit;

public sealed class GardenShedTests
{
    [Fact]
    public void Constructor__StartOpenFalse__StartsClosed()
    {
        var shed = new GardenShed(new Vector2(100f, 200f), new Point(64, 64));

        Assert.False(shed.IsDoorOpen);
    }

    [Fact]
    public void Constructor__StartOpenTrue__StartsOpen()
    {
        var shed = new GardenShed(new Vector2(100f, 200f), new Point(64, 64), startOpen: true);

        Assert.True(shed.IsDoorOpen);
    }

    [Fact]
    public void UpdateDoorState__PlayerFootBoundsOnRamp__OpensDoor()
    {
        var shed = new GardenShed(new Vector2(100f, 200f), new Point(64, 64));

        shed.UpdateDoorState(new Rectangle(126, 252, 8, 8));

        Assert.True(shed.IsDoorOpen);
    }

    [Fact]
    public void UpdateDoorState__PlayerFootBoundsOffRamp__ClosesDoor()
    {
        var shed = new GardenShed(new Vector2(100f, 200f), new Point(64, 64), startOpen: true);

        shed.UpdateDoorState(new Rectangle(104, 206, 8, 8));

        Assert.False(shed.IsDoorOpen);
    }
}
