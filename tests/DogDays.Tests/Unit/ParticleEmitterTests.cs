using Microsoft.Xna.Framework;
using DogDays.Game.Components;
using DogDays.Game.Data;
using DogDays.Game.Systems;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public class ParticleEmitterTests
{
    private static readonly ParticleProfile TestProfile = new()
    {
        SpawnRate = 10f,  // 10 particles per second = one every 0.1s
        MinLife = 1.0f,
        MaxLife = 1.0f,
        MinSpeed = 10f,
        MaxSpeed = 10f,
        MinScale = 1.0f,
        MaxScale = 1.0f,
        SpreadRadians = 0f,
        Gravity = 0f
    };

    [Fact]
    public void Constructor__ValidArgs__DoesNotThrow()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);
        Assert.True(emitter.IsEnabled);
    }

    [Fact]
    public void Update__AccumulatesTimeAndEmits__ParticlesSpawned()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);

        // At 10/sec, one tick of 0.1s should spawn 1 particle
        emitter.Update(FakeGameTime.FromSeconds(0.1f), Vector2.Zero);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Update__LargeTimestep__EmitsMultipleParticles()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);

        // 0.5s at 10/sec = 5 particles
        emitter.Update(FakeGameTime.FromSeconds(0.5f), Vector2.Zero);
        Assert.Equal(5, manager.ActiveCount);
    }

    [Fact]
    public void Update__Disabled__NoParticlesEmitted()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);
        emitter.IsEnabled = false;

        emitter.Update(FakeGameTime.FromSeconds(1.0f), Vector2.Zero);
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Update__ReEnabled__ResumesEmission()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);
        emitter.IsEnabled = false;

        emitter.Update(FakeGameTime.FromSeconds(0.2f), Vector2.Zero);
        Assert.Equal(0, manager.ActiveCount);

        emitter.IsEnabled = true;
        emitter.Update(FakeGameTime.FromSeconds(0.1f), Vector2.Zero);
        Assert.Equal(1, manager.ActiveCount);
    }

    [Fact]
    public void Update__SmallTimesteps__AccumulatesCorrectly()
    {
        var manager = new ParticleManager(100);
        var emitter = new ParticleEmitter(manager, TestProfile);

        // Two 0.05s updates = 0.1s total = 1 particle at 10/sec
        emitter.Update(FakeGameTime.FromSeconds(0.05f), Vector2.Zero);
        Assert.Equal(0, manager.ActiveCount);

        emitter.Update(FakeGameTime.FromSeconds(0.05f), Vector2.Zero);
        Assert.Equal(1, manager.ActiveCount);
    }
}
