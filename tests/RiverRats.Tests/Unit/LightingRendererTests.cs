using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Graphics;

namespace RiverRats.Tests.Unit;

public sealed class LightingRendererTests
{
    // ── ShouldSkip ────────────────────────────────

    [Fact]
    public void ShouldSkip__NightStrengthZero__ReturnsTrue()
    {
        Assert.True(LightingRenderer.ShouldSkip(0f));
    }

    [Fact]
    public void ShouldSkip__NightStrengthNegative__ReturnsTrue()
    {
        Assert.True(LightingRenderer.ShouldSkip(-0.1f));
    }

    [Fact]
    public void ShouldSkip__NightStrengthPositive__ReturnsFalse()
    {
        Assert.False(LightingRenderer.ShouldSkip(0.01f));
    }

    [Fact]
    public void ShouldSkip__NightStrengthOne__ReturnsFalse()
    {
        Assert.False(LightingRenderer.ShouldSkip(1f));
    }

    // ── GetAmbientColor ───────────────────────────

    [Fact]
    public void GetAmbientColor__NightStrengthZero__ReturnsWhite()
    {
        Assert.Equal(Color.White, LightingRenderer.GetAmbientColor(0f));
    }

    [Fact]
    public void GetAmbientColor__NightStrengthOne__ReturnsNightTint()
    {
        var expected = DayNightCycle.NightTint;
        Assert.Equal(expected, LightingRenderer.GetAmbientColor(1f));
    }

    [Fact]
    public void GetAmbientColor__NightStrengthHalf__ReturnsLerpedColor()
    {
        var expected = Color.Lerp(Color.White, DayNightCycle.NightTint, 0.5f);
        Assert.Equal(expected, LightingRenderer.GetAmbientColor(0.5f));
    }

    [Fact]
    public void GetAmbientColor__NightStrengthNegative__ReturnsWhite()
    {
        Assert.Equal(Color.White, LightingRenderer.GetAmbientColor(-1f));
    }

    [Fact]
    public void GetAmbientColor__NightStrengthAboveOne__ReturnsNightTint()
    {
        Assert.Equal(DayNightCycle.NightTint, LightingRenderer.GetAmbientColor(2f));
    }

    // ── SetLights boundary ────────────────────────

    [Fact]
    public void SetLights__NegativeCount__ThrowsArgumentOutOfRange()
    {
        var renderer = new LightingRenderer(null!, 480, 270);
        var lights = new LightData[4];

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetLights(lights, -1));
    }

    [Fact]
    public void SetLights__CountExceedsArrayLength__ThrowsArgumentOutOfRange()
    {
        var renderer = new LightingRenderer(null!, 480, 270);
        var lights = new LightData[4];

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetLights(lights, 5));
    }

    [Fact]
    public void SetLights__ValidCount__DoesNotThrow()
    {
        var renderer = new LightingRenderer(null!, 480, 270);
        var lights = new LightData[4];

        renderer.SetLights(lights, 2); // Should not throw
    }

    [Fact]
    public void SetLights__ZeroCount__DoesNotThrow()
    {
        var renderer = new LightingRenderer(null!, 480, 270);
        var lights = new LightData[4];

        renderer.SetLights(lights, 0); // Should not throw
    }
}
