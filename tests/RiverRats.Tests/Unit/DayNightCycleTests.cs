using Microsoft.Xna.Framework;
using RiverRats.Game.Graphics;
using RiverRats.Tests.Helpers;

namespace RiverRats.Tests.Unit;

public sealed class DayNightCycleTests
{
    private static readonly Color NightTint = new(60, 60, 120);

    // ── Constructor / start progress ────────────────────────────

    [Fact]
    public void Constructor__StartAtNight__ReturnNightTint()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0f);

        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtMidDay__ReturnWhiteTint()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0.50f);

        Assert.Equal(Color.White, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtDayBeginning__ReturnWhiteTint()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0.30f);

        Assert.Equal(Color.White, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtLateNight__ReturnNightTint()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0.90f);

        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    // ── CycleProgress ───────────────────────────────────────────

    [Fact]
    public void CycleProgress__AtStart__ReturnsStartProgress()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0.30f);

        Assert.Equal(0.30f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void CycleProgress__AfterHalfCycle__ReturnsCorrectProgress()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0f);
        cycle.Update(FakeGameTime.FromSeconds(150f));

        Assert.Equal(0.50f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void CycleProgress__AfterFullCycle__WrapsToZero()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0f);
        cycle.Update(FakeGameTime.FromSeconds(300f));

        Assert.Equal(0f, cycle.CycleProgress, precision: 3);
    }

    // ── Dawn transition ─────────────────────────────────────────

    [Fact]
    public void Update__DuringDawn__LerpsBetweenNightAndDay()
    {
        // Progress 0.25 = midway through dawn (0.20–0.30)
        var cycle = new DayNightCycle(100f, startProgress: 0.25f);
        var expected = Color.Lerp(NightTint, Color.White, 0.5f);

        Assert.Equal(expected, cycle.CurrentTint);
    }

    // ── Dusk transition ─────────────────────────────────────────

    [Fact]
    public void Update__DuringDusk__LerpsBetweenDayAndNight()
    {
        // Progress 0.75 = midway through dusk (0.70–0.80)
        var cycle = new DayNightCycle(100f, startProgress: 0.75f);
        var expected = Color.Lerp(Color.White, NightTint, 0.5f);

        Assert.Equal(expected, cycle.CurrentTint);
    }

    // ── Update advances time ────────────────────────────────────

    [Fact]
    public void Update__FromDayIntoNight__TintChanges()
    {
        // Start at end of day (0.69), advance past dusk into night
        var cycle = new DayNightCycle(100f, startProgress: 0.69f);
        Assert.Equal(Color.White, cycle.CurrentTint);

        // Advance 15 seconds on a 100s cycle = +0.15 progress → 0.84 (night)
        cycle.Update(FakeGameTime.FromSeconds(15f));

        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    [Fact]
    public void Update__MultipleFrames__ProgressAccumulates()
    {
        var cycle = new DayNightCycle(300f, startProgress: 0f);

        for (int i = 0; i < 60; i++)
            cycle.Update(FakeGameTime.OneFrame());

        // 60 frames at 1/60s each = 1 second. 1/300 = 0.00333...
        Assert.Equal(1f / 300f, cycle.CycleProgress, precision: 4);
    }

    // ── Clamp start progress ────────────────────────────────────

    [Fact]
    public void Constructor__StartProgressAboveOne__ClampedToOne()
    {
        var cycle = new DayNightCycle(300f, startProgress: 1.5f);

        // Clamped to 1.0 → wraps to 0 via modulo → night
        Assert.Equal(0f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void Constructor__NegativeStartProgress__ClampedToZero()
    {
        var cycle = new DayNightCycle(300f, startProgress: -0.5f);

        Assert.Equal(0f, cycle.CycleProgress, precision: 3);
        Assert.Equal(NightTint, cycle.CurrentTint);
    }
}
