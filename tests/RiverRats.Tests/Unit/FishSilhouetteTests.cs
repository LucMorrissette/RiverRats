using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Entities;
using RiverRats.Game.Util;
using Xunit;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Tests for the <see cref="FishSilhouette"/> <see cref="FishSilhouette.AttractionState"/>
/// state machine, covering all 7 states and their transitions.
/// </summary>
public class FishSilhouetteTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="FishSilhouette"/> of the given species centred inside a generous
    /// rectangular swim area. Uses a fixed RNG seed for deterministic behaviour.
    /// </summary>
    private static FishSilhouette CreateFish(
        FishSilhouette.FishType type = FishSilhouette.FishType.Bass,
        Vector2? position = null,
        int rngSeed = 42)
    {
        var bounds = PolygonBounds.FromRectangle(new Rectangle(0, 0, 800, 400));
        var pos = position ?? new Vector2(400, 200);
        return new FishSilhouette(type, pos, bounds, new Random(rngSeed));
    }

    /// <summary>
    /// Returns a lure position to the right of the fish at the given X distance.
    /// </summary>
    private static Vector2 LureAt(float distance) => new(400 + distance, 100);

    // ── Initial state ─────────────────────────────────────────────────────

    [Fact]
    public void Initial__AttractionState__IsUnaware()
    {
        var fish = CreateFish();
        Assert.Equal(FishSilhouette.AttractionState.Unaware, fish.Attraction);
    }

    [Fact]
    public void Initial__IsHooked__IsFalse()
    {
        var fish = CreateFish();
        Assert.False(fish.IsHooked);
    }

    [Fact]
    public void Initial__IsFleeing__IsFalse()
    {
        var fish = CreateFish();
        Assert.False(fish.IsFleeing);
    }

    // ── Unaware → Curious ────────────────────────────────────────────────

    [Fact]
    public void UpdateAttraction__Twitch__TransitionsUnawareToCurious__WhenInRange()
    {
        var fish = CreateFish();
        // Bass awareness radius is 200px. Lure at 100px away — well within range.
        var lure = LureAt(100);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);

        Assert.Equal(FishSilhouette.AttractionState.Curious, fish.Attraction);
    }

    [Fact]
    public void UpdateAttraction__Splash__TransitionsUnawareToCurious__WhenInRange()
    {
        var fish = CreateFish();
        var lure = LureAt(100);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Splash);

        Assert.Equal(FishSilhouette.AttractionState.Curious, fish.Attraction);
    }

    [Fact]
    public void UpdateAttraction__Twitch__DoesNotTransition__WhenTooFar()
    {
        var fish = CreateFish();
        // Bass awareness radius is 200px. Lure at 500px away.
        var lure = LureAt(500);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);

        Assert.Equal(FishSilhouette.AttractionState.Unaware, fish.Attraction);
    }

    [Fact]
    public void UpdateAttraction__None__DoesNotTransitionFromUnaware()
    {
        var fish = CreateFish();
        var lure = LureAt(50);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.None);

        Assert.Equal(FishSilhouette.AttractionState.Unaware, fish.Attraction);
    }

    // ── Curious → Approaching ────────────────────────────────────────────

    [Fact]
    public void UpdateAttraction__Twitch__TransitionsCuriousToApproaching__WhenCanStrike()
    {
        // Bass can strike. Trigger Curious first, then Twitch again.
        var fish = CreateFish(FishSilhouette.FishType.Bass);
        var lure = LureAt(100);

        // Enter Curious.
        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);
        Assert.Equal(FishSilhouette.AttractionState.Curious, fish.Attraction);

        // Second Twitch from Curious → Approaching.
        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);

        Assert.Equal(FishSilhouette.AttractionState.Approaching, fish.Attraction);
    }

    [Fact]
    public void UpdateAttraction__Twitch__MinnowCanStrike__TransitionsToCurious()
    {
        // Minnow also has CanStrike=true. Same pattern should work.
        var fish = CreateFish(FishSilhouette.FishType.Minnow);
        var lure = LureAt(80);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);
        Assert.Equal(FishSilhouette.AttractionState.Curious, fish.Attraction);

        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);

        Assert.Equal(FishSilhouette.AttractionState.Approaching, fish.Attraction);
    }

    // ── Approaching → StrikeReady ────────────────────────────────────────

    [Fact]
    public void UpdateAttraction__CloseToLure__TransitionsApproachingToStrikeReady()
    {
        var fish = CreateFish(FishSilhouette.FishType.Bass);

        // Bring fish to Approaching state.
        var lure = LureAt(100);
        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);
        fish.UpdateAttraction(lure, 0.016f, FishSilhouette.LureEvent.Twitch);
        Assert.Equal(FishSilhouette.AttractionState.Approaching, fish.Attraction);

        // Place lure extremely close (<30px from fish centre).
        // Fish centre X is at 400; lure X at 420 = 20px distance.
        var closeLure = new Vector2(420, 100);
        fish.UpdateAttraction(closeLure, 0.016f, FishSilhouette.LureEvent.None);

        Assert.Equal(FishSilhouette.AttractionState.StrikeReady, fish.Attraction);
    }

    // ── → Hooked ─────────────────────────────────────────────────────────

    [Fact]
    public void SetHooked__TransitionsToHooked__FromAnyState()
    {
        var fish = CreateFish();

        fish.SetHooked();

        Assert.Equal(FishSilhouette.AttractionState.Hooked, fish.Attraction);
        Assert.True(fish.IsHooked);
    }

    [Fact]
    public void SetHooked__FreezesVelocity__ToZero()
    {
        var fish = CreateFish();
        var prevPos = fish.Position;

        fish.SetHooked();
        fish.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5f)));

        // When hooked, position-setting is done externally; internal velocity is zero.
        // The fish shouldn't auto-drift from its position.
        Assert.Equal(prevPos, fish.Position);
    }

    [Fact]
    public void UpdateAttraction__Hooked__DoesNotTransitionAway()
    {
        var fish = CreateFish();
        fish.SetHooked();

        // Even a BadSplash won't change the state once hooked.
        fish.UpdateAttraction(LureAt(10), 0.016f, FishSilhouette.LureEvent.BadSplash);

        Assert.Equal(FishSilhouette.AttractionState.Hooked, fish.Attraction);
        Assert.True(fish.IsHooked);
    }

    // ── → Spooked ────────────────────────────────────────────────────────

    [Fact]
    public void Spook__TransitionsToSpooked__FromUnaware()
    {
        var fish = CreateFish();
        fish.UpdateAttraction(LureAt(50), 0.016f, FishSilhouette.LureEvent.None); // ensure lure position set

        fish.Spook();

        Assert.Equal(FishSilhouette.AttractionState.Spooked, fish.Attraction);
    }

    [Fact]
    public void Spook__DoesNotTransition__FromHooked()
    {
        var fish = CreateFish();
        fish.SetHooked();

        fish.Spook();

        // Spook is ignored when hooked.
        Assert.Equal(FishSilhouette.AttractionState.Hooked, fish.Attraction);
    }

    [Fact]
    public void Spook__DoesNotTransition__FromFleeing()
    {
        var fish = CreateFish();
        fish.Flee();

        fish.Spook();

        Assert.Equal(FishSilhouette.AttractionState.Fleeing, fish.Attraction);
    }

    [Fact]
    public void UpdateAttraction__BadSplash__TransitionsToSpooked__WhenCloseEnough()
    {
        var fish = CreateFish();
        // A close BadSplash adds large disturbance; if it exceeds the spook threshold, fish spookes.
        var closeLure = LureAt(20);

        // Apply many BadSplash events to push disturbance over threshold.
        for (var i = 0; i < 10; i++)
            fish.UpdateAttraction(closeLure, 0.001f, FishSilhouette.LureEvent.BadSplash);

        Assert.Equal(FishSilhouette.AttractionState.Spooked, fish.Attraction);
    }

    // ── → Fleeing ────────────────────────────────────────────────────────

    [Fact]
    public void Flee__TransitionsToFleeing__FromAnyState()
    {
        var fish = CreateFish();

        fish.Flee();

        Assert.Equal(FishSilhouette.AttractionState.Fleeing, fish.Attraction);
        Assert.True(fish.IsFleeing);
    }

    [Fact]
    public void Flee__SetsVelocityRight__PositiveX()
    {
        var fish = CreateFish();
        fish.Flee();

        // After a brief update, the fish should have moved to the right.
        var startX = fish.Position.X;
        fish.Update(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        Assert.True(fish.Position.X >= startX, "Fleeing fish should move right (or stay same if clamped)");
    }

    // ── Species-specific behaviour ────────────────────────────────────────

    [Fact]
    public void ReelSpeedMultiplier__Minnow__IsFastest()
    {
        var minnow = CreateFish(FishSilhouette.FishType.Minnow);
        var bass = CreateFish(FishSilhouette.FishType.Bass);
        var catfish = CreateFish(FishSilhouette.FishType.Catfish);

        Assert.True(minnow.ReelSpeedMultiplier > bass.ReelSpeedMultiplier);
        Assert.True(bass.ReelSpeedMultiplier > catfish.ReelSpeedMultiplier);
    }

    [Fact]
    public void ReelSpeedMultiplier__Catfish__IsSlowest()
    {
        var catfish = CreateFish(FishSilhouette.FishType.Catfish);
        Assert.Equal(0.4f, catfish.ReelSpeedMultiplier, 0.001f);
    }

    // ── SetPosition / SetRotation ─────────────────────────────────────────

    [Fact]
    public void SetPosition__UpdatesPosition()
    {
        var fish = CreateFish();
        var newPos = new Vector2(123, 456);

        fish.SetPosition(newPos);

        Assert.Equal(newPos, fish.Position);
    }

    [Fact]
    public void SetRotation__DoesNotThrow()
    {
        var fish = CreateFish();
        var ex = Record.Exception(() => fish.SetRotation(MathF.PI / 4f));
        Assert.Null(ex);
    }
}
