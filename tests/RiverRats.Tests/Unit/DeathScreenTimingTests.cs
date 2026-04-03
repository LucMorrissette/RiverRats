using RiverRats.Game.Screens;
using Xunit;

namespace RiverRats.Tests.Unit;

public class DeathScreenTimingTests
{
    // ── Initial state ──────────────────────────────────────────────────────

    [Fact]
    public void Elapsed__StartsAtZero__OnConstruction()
    {
        var logic = new DeathScreenTimingLogic();
        Assert.Equal(0f, logic.Elapsed);
    }

    [Fact]
    public void IsFadingOut__FalseAtStart()
    {
        var logic = new DeathScreenTimingLogic();
        Assert.False(logic.IsFadingOut);
    }

    [Fact]
    public void IsTransitioning__FalseAtStart()
    {
        var logic = new DeathScreenTimingLogic();
        Assert.False(logic.IsTransitioning);
    }

    [Fact]
    public void ShowPrompt__FalseBeforePromptDelay()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(1.0f, inputPressed: false);
        Assert.False(logic.ShowPrompt);
    }

    // ── Fade-in alpha ──────────────────────────────────────────────────────

    [Fact]
    public void FadeInAlpha__IsOne__AtElapsedZero()
    {
        var logic = new DeathScreenTimingLogic();
        // No ticks yet — elapsed = 0.
        Assert.Equal(1f, logic.FadeInAlpha);
    }

    [Fact]
    public void FadeInAlpha__IsZero__AfterFadeInDuration()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.FadeInDuration + 0.1f, inputPressed: false);
        Assert.Equal(0f, logic.FadeInAlpha);
    }

    [Fact]
    public void FadeInAlpha__IsHalf__AtHalfFadeInDuration()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.FadeInDuration * 0.5f, inputPressed: false);
        Assert.Equal(0.5f, logic.FadeInAlpha, precision: 5);
    }

    // ── Prompt delay ───────────────────────────────────────────────────────

    [Fact]
    public void ShowPrompt__TrueAfterPromptDelay()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.PromptDelay + 0.01f, inputPressed: false);
        Assert.True(logic.ShowPrompt);
    }

    [Fact]
    public void ShowPrompt__FalseExactlyAtBoundary()
    {
        // elapsed = PromptDelay - epsilon should still be false.
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.PromptDelay - 0.01f, inputPressed: false);
        Assert.False(logic.ShowPrompt);
    }

    // ── Input-triggered fade-out ───────────────────────────────────────────

    [Fact]
    public void Tick__InputBeforePromptDelay__DoesNotBeginFadeOut()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(1.0f, inputPressed: true); // 1s < PromptDelay (3s)
        Assert.False(logic.IsFadingOut);
    }

    [Fact]
    public void Tick__InputAfterPromptDelay__BeginsFadeOut()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.PromptDelay + 0.1f, inputPressed: false);
        logic.Tick(0.01f, inputPressed: true);
        Assert.True(logic.IsFadingOut);
    }

    [Fact]
    public void ShowPrompt__FalseOnceFadingOut()
    {
        var logic = new DeathScreenTimingLogic();
        // Advance past prompt delay then trigger fade-out via input.
        logic.Tick(DeathScreenTimingLogic.PromptDelay + 0.1f, inputPressed: true);
        // Even though elapsed > PromptDelay, IsFadingOut suppresses ShowPrompt.
        Assert.False(logic.ShowPrompt);
    }

    // ── Auto-transition ────────────────────────────────────────────────────

    [Fact]
    public void Tick__AutoTransition__BeginsFadeOutAfterAutoTransitionDelay()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.AutoTransitionDelay + 0.01f, inputPressed: false);
        Assert.True(logic.IsFadingOut);
    }

    [Fact]
    public void Tick__NoInput__DoesNotFadeOut__BeforeAutoTransitionDelay()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(DeathScreenTimingLogic.AutoTransitionDelay - 0.5f, inputPressed: false);
        Assert.False(logic.IsFadingOut);
    }

    // ── Fade-out alpha and completion ──────────────────────────────────────

    [Fact]
    public void FadeOutAlpha__IsZero__WhenNotFadingOut()
    {
        var logic = new DeathScreenTimingLogic();
        Assert.Equal(0f, logic.FadeOutAlpha);
    }

    [Fact]
    public void FadeOutAlpha__RampsToOne__OverFadeOutDuration()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(DeathScreenTimingLogic.FadeOutDuration, inputPressed: false);
        Assert.Equal(1f, logic.FadeOutAlpha, precision: 5);
    }

    [Fact]
    public void FadeOutAlpha__IsHalf__AtHalfFadeOutDuration()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(DeathScreenTimingLogic.FadeOutDuration * 0.5f, inputPressed: false);
        Assert.Equal(0.5f, logic.FadeOutAlpha, precision: 5);
    }

    [Fact]
    public void Tick__ReturnsFalse__DuringFadeOut()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        var result = logic.Tick(DeathScreenTimingLogic.FadeOutDuration * 0.5f, inputPressed: false);
        Assert.False(result);
    }

    [Fact]
    public void Tick__ReturnsTrue__WhenFadeOutCompletes()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        var result = logic.Tick(DeathScreenTimingLogic.FadeOutDuration + 0.01f, inputPressed: false);
        Assert.True(result);
    }

    [Fact]
    public void Tick__SetsIsTransitioning__WhenFadeOutCompletes()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(DeathScreenTimingLogic.FadeOutDuration + 0.01f, inputPressed: false);
        Assert.True(logic.IsTransitioning);
    }

    [Fact]
    public void Tick__DoesNotReturnTrueTwice__OnSubsequentTicksAfterTransition()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(DeathScreenTimingLogic.FadeOutDuration + 0.01f, inputPressed: false);
        // Second tick after fade complete — should NOT return true again.
        var secondResult = logic.Tick(0.1f, inputPressed: false);
        Assert.False(secondResult);
    }

    // ── BeginFadeOut idempotency ───────────────────────────────────────────

    [Fact]
    public void BeginFadeOut__IsIdempotent__CalledTwice()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(DeathScreenTimingLogic.FadeOutDuration * 0.5f, inputPressed: false);
        var timerAfterHalf = logic.FadeOutTimer;

        // Call BeginFadeOut again — should not reset the timer.
        logic.BeginFadeOut();
        Assert.Equal(timerAfterHalf, logic.FadeOutTimer, precision: 5);
    }

    // ── FadeOutTimer accumulation ──────────────────────────────────────────

    [Fact]
    public void FadeOutTimer__AccumulatesAcrossMultipleTicks()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(0.5f, inputPressed: false);
        logic.Tick(0.5f, inputPressed: false);
        Assert.Equal(1.0f, logic.FadeOutTimer, precision: 5);
    }

    // ── Elapsed accumulation ───────────────────────────────────────────────

    [Fact]
    public void Elapsed__AccumulatesAcrossMultipleTicks()
    {
        var logic = new DeathScreenTimingLogic();
        logic.Tick(1.0f, inputPressed: false);
        logic.Tick(2.0f, inputPressed: false);
        Assert.Equal(3.0f, logic.Elapsed, precision: 5);
    }

    [Fact]
    public void Elapsed__ContinuesAccumulating__WhileFadingOut()
    {
        var logic = new DeathScreenTimingLogic();
        logic.BeginFadeOut();
        logic.Tick(1.0f, inputPressed: false);
        Assert.Equal(1.0f, logic.Elapsed, precision: 5);
    }
}
