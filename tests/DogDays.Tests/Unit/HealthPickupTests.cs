using Microsoft.Xna.Framework;
using DogDays.Game.Entities;

namespace DogDays.Tests.Unit;

public sealed class HealthPickupTests
{
    [Fact]
    public void Spawn__SetsActiveAndPosition__WhenCalled()
    {
        var pickup = new HealthPickup();

        pickup.Spawn(new Vector2(100f, 200f));

        Assert.True(pickup.IsActive);
        Assert.Equal(new Vector2(100f, 200f), pickup.Position);
    }

    [Fact]
    public void Update__DeactivatesAfterDespawnTime__WhenLifetimeExpires()
    {
        var pickup = new HealthPickup();
        pickup.Spawn(Vector2.Zero);

        // Advance past the 10s despawn time.
        pickup.Update(10.1f);

        Assert.False(pickup.IsActive);
    }

    [Fact]
    public void Opacity__Returns1__BeforeFadeStart()
    {
        var pickup = new HealthPickup();
        pickup.Spawn(Vector2.Zero);

        // Age is 0 right after spawn.
        Assert.Equal(1f, pickup.Opacity);
    }

    [Fact]
    public void Opacity__FadesToZero__AtDespawnTime()
    {
        var pickup = new HealthPickup();
        pickup.Spawn(Vector2.Zero);

        // Advance to exactly the despawn boundary (10s).
        pickup.Update(10f);

        Assert.Equal(0f, pickup.Opacity, precision: 4);
    }

    [Fact]
    public void Deactivate__SetsInactive__WhenCalled()
    {
        var pickup = new HealthPickup();
        pickup.Spawn(Vector2.Zero);

        pickup.Deactivate();

        Assert.False(pickup.IsActive);
    }
}
