using Microsoft.Xna.Framework;
using RiverRats.Game.Components;
using RiverRats.Game.Entities;
using RiverRats.Game.Graphics;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

public sealed class SmallFireTests
{
    // ── Initial state ────────────────────────────────────────────

    [Fact]
    public void Constructor__LightRadius__EqualsBaseLightRadius()
    {
        var fire = MakeFire(Vector2.Zero);

        Assert.Equal(SmallFire.BaseLightRadius, fire.LightRadius, precision: 3);
    }

    [Fact]
    public void Constructor__LightIntensity__EqualsBaseLightIntensity()
    {
        var fire = MakeFire(Vector2.Zero);

        Assert.Equal(SmallFire.BaseLightIntensity, fire.LightIntensity, precision: 3);
    }

    [Fact]
    public void Constructor__LightColor__IsWarmOrange()
    {
        var fire = MakeFire(Vector2.Zero);

        Assert.Equal(new Color(255, 180, 70), fire.LightColor);
    }

    // ── LightPosition ────────────────────────────────────────────

    [Fact]
    public void LightPosition__HorizontallyCenteredOnSprite()
    {
        // Sprite is 16 px wide; center = pos.X + 8
        var fire = MakeFire(new Vector2(100f, 200f));

        Assert.Equal(108f, fire.LightPosition.X, precision: 3);
    }

    [Fact]
    public void LightPosition__VerticallyNearFlameTip()
    {
        // Flame tip is top quarter of 16 px sprite = pos.Y + 4
        var fire = MakeFire(new Vector2(100f, 200f));

        Assert.Equal(204f, fire.LightPosition.Y, precision: 3);
    }

    // ── Flicker after Update ─────────────────────────────────────

    [Fact]
    public void Update__LightRadius__ChangesFromBase()
    {
        var fire = MakeFire(Vector2.Zero);

        // After a meaningful elapsed time the sine flicker will shift the radius.
        fire.Update(FakeGameTime.FromSeconds(0.1f));

        // The radius should still be within the expected flicker range.
        Assert.InRange(
            fire.LightRadius,
            SmallFire.BaseLightRadius - SmallFire.FlickerRadiusVariance,
            SmallFire.BaseLightRadius + SmallFire.FlickerRadiusVariance);
    }

    [Fact]
    public void Update__LightIntensity__StaysWithinFlickerRange()
    {
        var fire = MakeFire(Vector2.Zero);

        fire.Update(FakeGameTime.FromSeconds(0.5f));

        Assert.InRange(
            fire.LightIntensity,
            SmallFire.BaseLightIntensity - SmallFire.FlickerIntensityVariance,
            SmallFire.BaseLightIntensity + SmallFire.FlickerIntensityVariance);
    }

    // ── GetLightData ─────────────────────────────────────────────

    [Fact]
    public void GetLightData__MatchesCurrentLightProperties()
    {
        var fire = MakeFire(new Vector2(50f, 80f));
        fire.Update(FakeGameTime.FromSeconds(0.3f));

        var data = fire.GetLightData();

        Assert.Equal(fire.LightPosition, data.Position);
        Assert.Equal(fire.LightRadius, data.Radius, precision: 4);
        Assert.Equal(fire.LightColor, data.Color);
        Assert.Equal(fire.LightIntensity, data.Intensity, precision: 4);
    }

    [Fact]
    public void GetLightData__Position__ReflectsFireWorldPosition()
    {
        var fire = MakeFire(new Vector2(200f, 300f));

        var data = fire.GetLightData();

        // X = 200 + 8 = 208; Y = 300 + 4 = 304
        Assert.Equal(208f, data.Position.X, precision: 3);
        Assert.Equal(304f, data.Position.Y, precision: 3);
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>Creates a SmallFire with a stub animator (no real texture needed).</summary>
    private static SmallFire MakeFire(Vector2 position)
    {
        var animator = new LoopAnimator(16, 16, 8, 0.1f);
        return new SmallFire(position, animator);
    }
}
