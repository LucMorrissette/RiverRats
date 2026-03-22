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
        var cycle = new DayNightCycle(120f, startProgress: 0f);

        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtMidDay__ReturnWhiteTint()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.50f);

        Assert.Equal(Color.White, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtDayBeginning__ReturnWhiteTint()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.30f);

        Assert.Equal(Color.White, cycle.CurrentTint);
    }

    [Fact]
    public void Constructor__StartAtLateNight__ReturnNightTint()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.90f);

        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    // ── CycleProgress ───────────────────────────────────────────

    [Fact]
    public void CycleProgress__AtStart__ReturnsStartProgress()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.30f);

        Assert.Equal(0.30f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void CycleProgress__AfterHalfCycle__ReturnsCorrectProgress()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0f);
        cycle.Update(FakeGameTime.FromSeconds(60f));

        Assert.Equal(0.50f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void CycleProgress__AfterFullCycle__WrapsToZero()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0f);
        cycle.Update(FakeGameTime.FromSeconds(120f));

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
        var cycle = new DayNightCycle(120f, startProgress: 0f);

        for (int i = 0; i < 60; i++)
            cycle.Update(FakeGameTime.OneFrame());

        // 60 frames at 1/60s each = 1 second. 1/120 = 0.00833...
        Assert.Equal(1f / 120f, cycle.CycleProgress, precision: 4);
    }

    // ── Clamp start progress ────────────────────────────────────

    [Fact]
    public void Constructor__StartProgressAboveOne__ClampedToOne()
    {
        var cycle = new DayNightCycle(120f, startProgress: 1.5f);

        // Clamped to 1.0 → wraps to 0 via modulo → night
        Assert.Equal(0f, cycle.CycleProgress, precision: 3);
    }

    [Fact]
    public void Constructor__NegativeStartProgress__ClampedToZero()
    {
        var cycle = new DayNightCycle(120f, startProgress: -0.5f);

        Assert.Equal(0f, cycle.CycleProgress, precision: 3);
        Assert.Equal(NightTint, cycle.CurrentTint);
    }

    // ── NightStrength ────────────────────────────────────────────

    [Fact]
    public void NightStrength__AtFullNight__ReturnsOne()
    {
        // progress = 0.10 → deep night (before NightEnd 0.20)
        var cycle = new DayNightCycle(100f, startProgress: 0.10f);

        Assert.Equal(1f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__AtFullDay__ReturnsZero()
    {
        // progress = 0.50 → full day (between DawnEnd 0.30 and DayEnd 0.70)
        var cycle = new DayNightCycle(100f, startProgress: 0.50f);

        Assert.Equal(0f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__AtDawnMidpoint__ReturnsHalf()
    {
        // progress = 0.25 → midway through dawn (0.20–0.30), so strength = 0.5
        var cycle = new DayNightCycle(100f, startProgress: 0.25f);

        Assert.Equal(0.5f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__AtDuskMidpoint__ReturnsHalf()
    {
        // progress = 0.75 → midway through dusk (0.70–0.80), so strength = 0.5
        var cycle = new DayNightCycle(100f, startProgress: 0.75f);

        Assert.Equal(0.5f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__AtLateNight__ReturnsOne()
    {
        // progress = 0.90 → late night (after DuskEnd 0.80)
        var cycle = new DayNightCycle(100f, startProgress: 0.90f);

        Assert.Equal(1f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__UpdateAdvancesFromNightToDay__StrengthDropsToZero()
    {
        // Start just before end of night (progress 0.19), advance into full day
        var cycle = new DayNightCycle(100f, startProgress: 0.19f);
        Assert.Equal(1f, cycle.NightStrength, precision: 3);

        // Advance +0.15 → progress 0.34 = full day
        cycle.Update(FakeGameTime.FromSeconds(15f));

        Assert.Equal(0f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__IsBetweenZeroAndOne__AtDawn()
    {
        // Any dawn progress (0.20–0.30) must yield a value strictly between 0 and 1.
        var cycle = new DayNightCycle(100f, startProgress: 0.22f);

        Assert.InRange(cycle.NightStrength, 0f, 1f);
    }

    [Fact]
    public void NightStrength__IsBetweenZeroAndOne__AtDusk()
    {
        // Any dusk progress (0.70–0.80) must yield a value strictly between 0 and 1.
        var cycle = new DayNightCycle(100f, startProgress: 0.78f);

        Assert.InRange(cycle.NightStrength, 0f, 1f);
    }

    // ── Edge cases ───────────────────────────────────────────────

    [Fact]
    public void Constructor__VerySmallDuration__DoesNotThrow()
    {
        // Extremely small but non-zero duration should not crash.
        var cycle = new DayNightCycle(0.001f, startProgress: 0f);
        Assert.Equal(0f, cycle.CycleProgress, precision: 1);
    }

    [Fact]
    public void Update__VerySmallDuration__WrapsCorrectly()
    {
        var cycle = new DayNightCycle(0.01f, startProgress: 0f);

        // Advance well past one full cycle.
        cycle.Update(FakeGameTime.FromSeconds(1.0f));

        // Should wrap, progress should be between 0 and 1.
        Assert.InRange(cycle.CycleProgress, 0f, 1f);
    }

    [Fact]
    public void NightStrength__AtFullNight__ReturnsOne_120sCycle()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.10f);

        Assert.Equal(1f, cycle.NightStrength, precision: 3);
    }

    [Fact]
    public void NightStrength__AtFullDay__ReturnsZero_120sCycle()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.50f);

        Assert.Equal(0f, cycle.NightStrength, precision: 3);
    }

    // ── GameHour ─────────────────────────────────────────────

    [Fact]
    public void GameHour__AtMidnight__ReturnsZero()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0f);

        Assert.Equal(0f, cycle.GameHour, precision: 2);
    }

    [Fact]
    public void GameHour__AtNoon__ReturnsTwelve()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0.50f);

        Assert.Equal(12f, cycle.GameHour, precision: 2);
    }

    [Fact]
    public void GameHour__AtDayStart__ReturnsSevenTwoZero()
    {
        // Progress 0.30 = start of day = 7.2 hours
        var cycle = new DayNightCycle(120f, startProgress: 0.30f);

        Assert.Equal(7.2f, cycle.GameHour, precision: 2);
    }

    [Fact]
    public void GameHour__AfterUpdate__ReflectsNewProgress()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0f);
        // Advance 60 seconds = halfway through 120s cycle = progress 0.5 = noon
        cycle.Update(FakeGameTime.FromSeconds(60f));

        Assert.Equal(12f, cycle.GameHour, precision: 2);
    }

    [Fact]
    public void GameHour__AfterFullCycle__WrapsToZero()
    {
        var cycle = new DayNightCycle(120f, startProgress: 0f);
        cycle.Update(FakeGameTime.FromSeconds(120f));

        Assert.Equal(0f, cycle.GameHour, precision: 2);
    }
}
