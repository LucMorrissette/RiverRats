using Microsoft.Xna.Framework;
using RiverRats.Game.Components;
using RiverRats.Game.Entities;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

public sealed class FirepitTests
{
    [Fact]
    public void Constructor__WithAttachedFire__StartsLit()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));

        Assert.True(firepit.IsLit);
    }

    [Fact]
    public void Constructor__WithoutAttachedFire__StartsUnlit()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24));

        Assert.False(firepit.IsLit);
    }

    [Fact]
    public void ToggleLit__WithAttachedFire__FlipsLitState()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));

        firepit.ToggleLit();
        Assert.False(firepit.IsLit);

        firepit.ToggleLit();
        Assert.True(firepit.IsLit);
    }

    [Fact]
    public void CanInteract__ActorBoundsInsideInteractionBounds__ReturnsTrue()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));
        var actorBounds = new Rectangle(96, 220, 20, 8);

        Assert.True(firepit.CanInteract(actorBounds));
    }

    [Fact]
    public void CanInteract__ActorBoundsFarAway__ReturnsFalse()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));
        var actorBounds = new Rectangle(200, 320, 20, 8);

        Assert.False(firepit.CanInteract(actorBounds));
    }

    [Fact]
    public void TryGetLightData__WhenUnlit__ReturnsFalse()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));
        firepit.ToggleLit();

        var hasLight = firepit.TryGetLightData(out _);

        Assert.False(hasLight);
    }

    [Fact]
    public void TryGetLightData__WhenLit__ReturnsTrue()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));

        var hasLight = firepit.TryGetLightData(out var lightData);

        Assert.True(hasLight);
        Assert.Equal(116f, lightData.Position.X, precision: 3);
        Assert.Equal(194f, lightData.Position.Y, precision: 3);
    }

    [Fact]
    public void ToggleLit__WithoutAttachedFire__DoesNotThrow()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24));

        // Should be a no-op, not crash
        firepit.ToggleLit();

        Assert.False(firepit.IsLit);
    }

    [Fact]
    public void Update__WhenUnlit__DoesNotThrow()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));
        firepit.ToggleLit(); // Now unlit

        // Update while unlit should not throw
        firepit.Update(FakeGameTime.FromSeconds(0.016f));

        Assert.False(firepit.IsLit);
    }

    [Fact]
    public void Update__WhenLit__AdvancesFireAnimation()
    {
        var firepit = new Firepit(new Vector2(100f, 200f), new Point(32, 24), MakeFire(new Vector2(108f, 190f)));

        // Get initial light data — radius starts at SmallFire.BaseLightRadius (110f), flicker at 0
        firepit.TryGetLightData(out var before);

        // Advance enough to see flicker change (0.5s → flickerTime = 5.0 → sin(5.0) ≈ -0.959)
        firepit.Update(FakeGameTime.FromSeconds(0.5f));
        firepit.TryGetLightData(out var after);

        // Light radius should change due to flicker
        Assert.NotEqual(before.Radius, after.Radius);
    }

    private static SmallFire MakeFire(Vector2 position)
    {
        return new SmallFire(position, new LoopAnimator(16, 16, 8, 0.1f));
    }
}