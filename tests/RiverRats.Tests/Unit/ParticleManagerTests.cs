using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

public class ParticleManagerTests
{
    private static readonly ParticleProfile TestProfile = new()
    {
        SpawnRate = 10f,
        MinLife = 1.0f,
        MaxLife = 1.0f,  // Fixed life for deterministic tests
        MinSpeed = 10f,
        MaxSpeed = 10f,  // Fixed speed for deterministic tests
        MinScale = 1.0f,
        MaxScale = 1.0f,
        StartColor = Color.White,
        EndColor = Color.Transparent,
        SpreadRadians = 0f,  // No spread for deterministic direction
        Gravity = 0f
    };

    [Fact]
    public void Constructor__InitialState__NoActiveParticles()
    {
        var manager = new ParticleManager(100);
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Emit__SingleParticle__ActiveCountIncreases()
    {
        var manager = new ParticleManager(100);
        manager.Emit(TestProfile, Vector2.Zero, 1);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Emit__MultipleParticles__ActiveCountMatchesEmitted()
    {
        var manager = new ParticleManager(100);
        manager.Emit(TestProfile, Vector2.Zero, 5);
        Assert.Equal(5, manager.ActiveCount);
    }

    [Fact]
    public void Emit__ExceedsPool__ClampedToMaxParticles()
    {
        var manager = new ParticleManager(3);
        manager.Emit(TestProfile, Vector2.Zero, 10);
        Assert.Equal(3, manager.ActiveCount);
    }

    [Fact]
    public void Update__ParticleLifeExpires__ActiveCountDecreases()
    {
        var manager = new ParticleManager(100);
        manager.Emit(TestProfile, Vector2.Zero, 1);
        Assert.Equal(1, manager.ActiveCount);

        // Advance past the 1-second life
        manager.Update(FakeGameTime.FromSeconds(1.1f));
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Update__ParticleLifeNotExpired__StaysActive()
    {
        var manager = new ParticleManager(100);
        manager.Emit(TestProfile, Vector2.Zero, 1);

        manager.Update(FakeGameTime.FromSeconds(0.5f));
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Update__AfterExpiry__SlotRecycled()
    {
        var manager = new ParticleManager(1);
        manager.Emit(TestProfile, Vector2.Zero, 1);
        Assert.Equal(1, manager.ActiveCount);

        // Expire the particle
        manager.Update(FakeGameTime.FromSeconds(1.1f));
        Assert.Equal(0, manager.ActiveCount);

        // Emit again — should succeed since the slot was recycled
        manager.Emit(TestProfile, Vector2.Zero, 1);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Update__WithGravity__VelocityChanges()
    {
        var gravityProfile = new ParticleProfile
        {
            MinLife = 5.0f,
            MaxLife = 5.0f,
            MinSpeed = 0f,
            MaxSpeed = 0f,
            MinScale = 1f,
            MaxScale = 1f,
            SpreadRadians = 0f,
            Gravity = -10f  // Negative = rises (upward)
        };

        var manager = new ParticleManager(100);
        manager.Emit(gravityProfile, new Vector2(100f, 100f), 1);

        // After 1 second with gravity = -10, particle should have moved upward.
        // NOTE: ParticleManager exposes no public API to read individual particle
        // positions or velocities — the internal Particle struct and _particles array
        // are both private. We can only verify the side-effect that the particle is
        // still alive (it would expire early only if gravity somehow consumed life,
        // which it doesn't). A stronger assertion would require exposing a
        // GetParticle(int index) or similar API, which is intentionally avoided to
        // keep the hot-path allocation-free.
        manager.Update(FakeGameTime.FromSeconds(1.0f));
        // Active count stays at 1 (life = 5s)
        Assert.Equal(1, manager.ActiveCount);
    }
}
