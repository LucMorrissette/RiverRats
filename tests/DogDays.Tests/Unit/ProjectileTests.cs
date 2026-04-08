using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class ProjectileTests
{
    [Fact]
    public void Fire__VelocityProvided__SetsRotationAndPierceBudget()
    {
        var projectile = new Projectile();

        projectile.Fire(new Vector2(10f, 20f), new Vector2(5f, 0f), pierceCount: 3);

        Assert.True(projectile.IsAlive);
        Assert.Equal(3, projectile.RemainingPierces);
        Assert.InRange(projectile.Rotation, -0.001f, 0.001f);
    }

    [Fact]
    public void RegisterHit__CalledThreeTimes__ExpiresOnThirdHit()
    {
        var projectile = new Projectile();
        projectile.Fire(Vector2.Zero, Vector2.UnitX, pierceCount: 3);

        Assert.True(projectile.RegisterHit());
        Assert.Equal(2, projectile.RemainingPierces);
        Assert.True(projectile.IsAlive);

        Assert.True(projectile.RegisterHit());
        Assert.Equal(1, projectile.RemainingPierces);
        Assert.True(projectile.IsAlive);

        Assert.False(projectile.RegisterHit());
        Assert.False(projectile.IsAlive);
        Assert.Equal(0, projectile.RemainingPierces);
    }

    [Fact]
    public void Update__HalfSecondElapsed__AdvancesByVelocity()
    {
        var projectile = new Projectile();
        projectile.Fire(Vector2.Zero, new Vector2(100f, 40f), pierceCount: 3);

        projectile.Update(FakeGameTime.FromSeconds(0.5f));

        Assert.Equal(new Vector2(50f, 20f), projectile.Position);
    }
}