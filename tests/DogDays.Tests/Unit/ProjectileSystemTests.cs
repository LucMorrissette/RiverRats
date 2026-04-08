using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Systems;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class ProjectileSystemTests
{
    [Fact]
    public void TryFireProjectile__TargetProvided__UsesConfiguredProjectileSpeed()
    {
        var system = new ProjectileSystem(maxProjectiles: 1, fireIntervalSeconds: 1f);

        var fired = system.TryFireProjectile(new Vector2(10f, 10f), new Vector2(30f, 10f));
        var projectile = system.Projectiles[0];

        Assert.True(fired);
        projectile.Update(FakeGameTime.FromSeconds(0.5f));
        Assert.Equal(new Vector2(10f + ProjectileSystem.ProjectileSpeed * 0.5f, 10f), projectile.Position);
        Assert.Equal(ProjectileSystem.ProjectilePierceCount, projectile.RemainingPierces);
    }

    [Fact]
    public void ResolveProjectileHits__FourOverlappingGnomes__KillsThreeThenExpires()
    {
        var projectile = new Projectile();
        projectile.Fire(new Vector2(8f, 8f), Vector2.UnitX * ProjectileSystem.ProjectileSpeed, ProjectileSystem.ProjectilePierceCount);

        var gnomes = new List<GnomeEnemy>
        {
            new(new Vector2(0f, 0f), 0f),
            new(new Vector2(2f, 0f), 0f),
            new(new Vector2(4f, 0f), 0f),
            new(new Vector2(6f, 0f), 0f),
        };

        ProjectileSystem.ResolveProjectileHits(projectile, gnomes);

        Assert.Equal(3, gnomes.Count(gnome => gnome.State == GnomeState.Dying));
        Assert.Equal(1, gnomes.Count(gnome => gnome.State == GnomeState.Chasing));
        Assert.False(projectile.IsAlive);
        Assert.Equal(0, projectile.RemainingPierces);
    }

    [Fact]
    public void Update__TrailParticlesConfigured__EmitsSparkTrailFromActiveProjectile()
    {
        var particleManager = new ParticleManager(16);
        var trailProfile = new ParticleProfile
        {
            MinLife = 1f,
            MaxLife = 1f,
            MinSpeed = 0f,
            MaxSpeed = 0f,
            MinScale = 1f,
            MaxScale = 1f,
            SpreadRadians = 0f,
            Gravity = 0f,
        };
        var system = new ProjectileSystem(1, 1f, particleManager, trailProfile);
        var spawner = new GnomeSpawner(0, 99f, 0);

        Assert.True(system.TryFireProjectile(new Vector2(10f, 10f), new Vector2(30f, 10f)));

        system.Update(FakeGameTime.OneFrame(), Vector2.Zero, Vector2.Zero, spawner, new NoCollisionData());

        Assert.True(particleManager.ActiveCount > 0);
    }

    private sealed class NoCollisionData : DogDays.Game.World.IMapCollisionData
    {
        public bool IsWorldRectangleBlocked(Rectangle worldBounds) => false;
    }
}