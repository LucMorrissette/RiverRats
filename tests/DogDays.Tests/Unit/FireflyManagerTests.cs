using Microsoft.Xna.Framework;
using DogDays.Game.Graphics;
using DogDays.Game.Systems;
using DogDays.Tests.Helpers;
using Xunit;

namespace DogDays.Tests.Unit;

public class FireflyManagerTests
{
    private const int TestSeed = 42;
    private static readonly Rectangle TestBounds = new(0, 0, 480, 270);

    private static FireflyManager CreateManager(int max = 32) => new(max, TestSeed);

    /// <summary>Runs enough simulated time at full night to guarantee spawns.</summary>
    private static void SpawnSome(FireflyManager manager, Rectangle bounds)
    {
        // 5 seconds at 0.5/sec = 2 guaranteed spawns.
        manager.Update(FakeGameTime.FromSeconds(5f), 1f, bounds);
    }

    [Fact]
    public void Constructor__StartsWithZeroActiveFireflies()
    {
        var manager = CreateManager();

        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Update__NoSpawns__WhenNightStrengthBelowThreshold()
    {
        var manager = CreateManager();

        manager.Update(FakeGameTime.FromSeconds(5f), FireflyManager.SpawnNightThreshold - 0.01f, TestBounds);

        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Update__SpawnsFireflies__WhenNightStrengthAboveThreshold()
    {
        var manager = CreateManager();

        SpawnSome(manager, TestBounds);

        Assert.True(manager.ActiveCount > 0, $"Expected at least one firefly to spawn during night. Got {manager.ActiveCount}.");
    }

    [Fact]
    public void Update__SpawnRateScalesWithNightStrength()
    {
        var managerLow = new FireflyManager(64, TestSeed);
        var managerHigh = new FireflyManager(64, TestSeed);

        var lowNight = FireflyManager.SpawnNightThreshold + 0.05f;
        const float highNight = 1f;

        // 20 seconds gives more time for the rate difference to manifest.
        for (var i = 0; i < 20; i++)
        {
            managerLow.Update(FakeGameTime.FromSeconds(1f), lowNight, TestBounds);
            managerHigh.Update(FakeGameTime.FromSeconds(1f), highNight, TestBounds);
        }

        Assert.True(managerHigh.ActiveCount > managerLow.ActiveCount,
            $"High night ({managerHigh.ActiveCount}) should have more fireflies than low night ({managerLow.ActiveCount}).");
    }

    [Fact]
    public void Update__FirefliesDieAfterLifetimeExpires()
    {
        var manager = CreateManager(8);

        SpawnSome(manager, TestBounds);
        Assert.True(manager.ActiveCount > 0);

        // Advance well past max lifetime (3.5s) with no new spawns.
        manager.Update(FakeGameTime.FromSeconds(5f), 0f, TestBounds);

        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Update__DoesNotExceedPoolCapacity()
    {
        var manager = CreateManager(4);

        // Many large steps to try to flood the pool.
        for (var i = 0; i < 10; i++)
        {
            manager.Update(FakeGameTime.FromSeconds(3f), 1f, TestBounds);
        }

        Assert.True(manager.ActiveCount <= 4);
    }

    [Fact]
    public void Update__FirefliesMove__PositionChangesOverTime()
    {
        var manager = CreateManager(4);

        SpawnSome(manager, TestBounds);
        Assert.True(manager.ActiveCount > 0);

        // Find the first active firefly and record position.
        var firstActiveIndex = -1;
        for (var i = 0; i < 4; i++)
        {
            if (manager.IsActive(i))
            {
                firstActiveIndex = i;
                break;
            }
        }

        Assert.True(firstActiveIndex >= 0);
        var posBefore = manager.GetPosition(firstActiveIndex);

        // Advance a short time — enough to move but not enough to expire.
        manager.Update(FakeGameTime.FromSeconds(0.1f), 1f, TestBounds);

        if (manager.IsActive(firstActiveIndex))
        {
            var posAfter = manager.GetPosition(firstActiveIndex);
            Assert.NotEqual(posBefore, posAfter);
        }
    }

    [Fact]
    public void WriteLightData__ReturnsZero__WhenNoActiveFireflies()
    {
        var manager = CreateManager();
        var lights = new LightData[8];

        var count = manager.WriteLightData(lights, 0);

        Assert.Equal(0, count);
    }

    [Fact]
    public void WriteLightData__WritesCorrectCount__ForActiveFireflies()
    {
        var manager = CreateManager(8);

        SpawnSome(manager, TestBounds);
        var activeCount = manager.ActiveCount;
        Assert.True(activeCount > 0);

        var lights = new LightData[activeCount + 4];
        var written = manager.WriteLightData(lights, 0);

        Assert.Equal(activeCount, written);
    }

    [Fact]
    public void WriteLightData__UsesGreenGlowColor()
    {
        var manager = CreateManager(4);

        SpawnSome(manager, TestBounds);
        Assert.True(manager.ActiveCount > 0);

        var lights = new LightData[4];
        var written = manager.WriteLightData(lights, 0);

        Assert.True(written > 0);
        Assert.Equal(FireflyManager.GlowColor, lights[0].Color);
    }

    [Fact]
    public void WriteLightData__RespectsOffset()
    {
        var manager = CreateManager(4);

        SpawnSome(manager, TestBounds);
        Assert.True(manager.ActiveCount > 0);

        var sentinel = new LightData(Vector2.One, 999f, Color.Red, 1f);
        var lights = new LightData[8];
        lights[0] = sentinel;

        var written = manager.WriteLightData(lights, 1);

        Assert.True(written > 0);
        // Slot 0 should be untouched.
        Assert.Equal(999f, lights[0].Radius);
        // Slot 1 should have firefly data.
        Assert.Equal(FireflyManager.LightRadius, lights[1].Radius);
    }

    [Fact]
    public void Update__SpawnsWithinCameraBounds()
    {
        var bounds = new Rectangle(100, 200, 50, 50);
        var manager = CreateManager(16);

        SpawnSome(manager, bounds);
        Assert.True(manager.ActiveCount > 0);

        // Check that at least one firefly spawned within or near the bounds
        // (they drift, so we check with a generous margin).
        var expanded = new Rectangle(bounds.X - 200, bounds.Y - 200, bounds.Width + 400, bounds.Height + 400);
        var foundInBounds = false;
        for (var i = 0; i < 16; i++)
        {
            if (!manager.IsActive(i)) continue;
            var pos = manager.GetPosition(i);
            if (expanded.Contains((int)pos.X, (int)pos.Y))
            {
                foundInBounds = true;
                break;
            }
        }

        Assert.True(foundInBounds, "At least one firefly should be near the camera bounds.");
    }
}
