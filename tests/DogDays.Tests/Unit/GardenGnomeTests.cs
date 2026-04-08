using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class GardenGnomeTests
{
    [Fact]
    public void Update__PlayerWithinThreeTiles__HidesQuickly()
    {
        var gnome = new GardenGnome(new Vector2(100f, 100f), new Point(9, 16), new Vector2(140f, 108f));

        gnome.Update(FakeGameTime.FromSeconds(0.2f), new Vector2(160f, 108f));

        Assert.Equal(120f, gnome.Position.X, precision: 3);
        Assert.Equal(100f, gnome.Position.Y, precision: 3);
    }

    [Fact]
    public void Update__PlayerBeyondThreeTiles__StaysAtHome()
    {
        var gnome = new GardenGnome(new Vector2(100f, 100f), new Point(9, 16), new Vector2(140f, 108f));

        gnome.Update(FakeGameTime.FromSeconds(0.2f), new Vector2(220f, 108f));

        Assert.Equal(100f, gnome.Position.X, precision: 3);
        Assert.Equal(100f, gnome.Position.Y, precision: 3);
    }

    [Fact]
    public void Update__PlayerLeavesAfterHiding__ReappearsSlowly()
    {
        var gnome = new GardenGnome(new Vector2(100f, 100f), new Point(9, 16), new Vector2(140f, 108f));

        gnome.Update(FakeGameTime.FromSeconds(0.2f), new Vector2(160f, 108f));
        gnome.Update(FakeGameTime.FromSeconds(0.2f), new Vector2(220f, 108f));

        Assert.Equal(112.8f, gnome.Position.X, precision: 3);
        Assert.Equal(100f, gnome.Position.Y, precision: 3);
    }
}