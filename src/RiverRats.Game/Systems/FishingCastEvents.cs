using Microsoft.Xna.Framework;

namespace RiverRats.Game.Systems;

/// <summary>
/// Side-effect events produced by <see cref="FishingCastLogic.Tick"/> for a single frame.
/// <para>
/// The screen reads these flags after each tick and triggers SFX, particles, and ripples.
/// Keeping events separate from state makes the logic testable without any GPU types.
/// </para>
/// </summary>
public struct FishingCastEvents
{
    // ── Audio ────────────────────────────────────────────────────────────────

    /// <summary>Play a random cast SFX this frame.</summary>
    public bool PlayCastSfx;

    /// <summary>Play a random plop SFX this frame.</summary>
    public bool PlayPlopSfx;

    /// <summary>Play a random twitch SFX this frame.</summary>
    public bool PlayTwitchSfx;

    /// <summary>Play a reel-tick SFX this frame (if cooldown permits).</summary>
    public bool PlayReelTick;

    /// <summary>Play a random strike SFX this frame.</summary>
    public bool PlayStrikeSfx;

    /// <summary>Play a random catch SFX this frame.</summary>
    public bool PlayCatchSfx;

    // ── Particles / ripples ───────────────────────────────────────────────────

    /// <summary>Spawn a normal splash at <see cref="SplashPosition"/>.</summary>
    public bool SpawnSplash;

    /// <summary>Spawn a heavier lure-landing splash at <see cref="SplashPosition"/>.</summary>
    public bool SpawnLureLandingSplash;

    /// <summary>Spawn a bad-cast (red) splash at <see cref="SplashPosition"/>.</summary>
    public bool SpawnBadCastSplash;

    /// <summary>
    /// Spawn a lure twitch ripple at <see cref="SplashPosition"/>.
    /// Also triggers a white attract ring.
    /// </summary>
    public bool SpawnTwitchRipple;

    /// <summary>Spawn red spook rings at <see cref="SplashPosition"/> (rapid-twitch overload).</summary>
    public bool SpawnSpookRings;

    /// <summary>World-space position for any splash/ripple events that fire this frame.</summary>
    public Vector2 SplashPosition;

    /// <summary>Spawn the strike spook ring cluster at <see cref="StrikeRipplePosition"/>.</summary>
    public bool SpawnStrikeSpookRings;

    /// <summary>World-space position for strike ripple cluster (lure X, water surface Y).</summary>
    public Vector2 StrikeRipplePosition;

    /// <summary>Spawn a fight-burst spook ring at <see cref="FightRipplePosition"/>.</summary>
    public bool SpawnFightSpookRing;

    /// <summary>Fish center at the moment a fight burst starts.</summary>
    public Vector2 FightRipplePosition;

    // ── Catch toast ───────────────────────────────────────────────────────────

    /// <summary>A fish was successfully caught this frame.</summary>
    public bool FishCaught;

    // ── Fish attraction ───────────────────────────────────────────────────────

    /// <summary>Lure event to feed each fish's attraction state machine this frame.</summary>
    public FishLureEvent LureEvent;

    // ── Screen shake ──────────────────────────────────────────────────────────

    /// <summary>Trigger a screen shake this frame.</summary>
    public bool TriggerShake;

    // ── Fish hook / flee ──────────────────────────────────────────────────────

    /// <summary>The fish at <see cref="HookedFishIndex"/> just got hooked — transition to FishStrike.</summary>
    public bool FishHooked;

    /// <summary>Index into the fish list of the fish that just got hooked.</summary>
    public int HookedFishIndex;

    /// <summary>
    /// Positions of the hooked fish at hook time, needed to set up the strike animation.
    /// </summary>
    public Vector2 HookStrikeStartPos;

    /// <summary>World-space target position the fish dives to after the strike breach.</summary>
    public Vector2 HookTarget;

    /// <summary>
    /// The hooked fish's line snapped or Cancel was pressed — fish should flee.
    /// </summary>
    public bool FishFled;

    /// <summary>The hooked fish was caught (reached the rod tip) — remove it from the list.</summary>
    public bool FishReachedRod;

    /// <summary>World-space position of the rod tip when the catch completed (for spawn-catch-splash).</summary>
    public Vector2 CatchRodTipPosition;

    /// <summary>Indices of other fish (besides the hooked one) that should be spooked by a strike.</summary>
    public int[] StrikeSpooked;

    /// <summary>Indices of other fish within 80 px of the hooked fish that should be spooked.</summary>
    public int[] StrikeNearbySpook;

    // ── Fish positions (for the fight phase) ─────────────────────────────────

    /// <summary>Desired new position for the hooked fish this frame (fight / reel logic).</summary>
    public bool UpdateHookedFishPosition;

    /// <summary>New world-space position for the hooked fish sprite origin.</summary>
    public Vector2 HookedFishNewPosition;

    /// <summary>Desired rotation (radians) for the hooked fish sprite.</summary>
    public float HookedFishRotation;

    /// <summary>Whether the hooked fish should face left.</summary>
    public bool HookedFishFaceLeft;
}

/// <summary>
/// Lure event category fed to each fish's attraction state machine each frame.
/// Mirrors <c>FishSilhouette.LureEvent</c> but lives in the Systems layer so that
/// <see cref="FishingCastLogic"/> has no dependency on <c>FishSilhouette</c>.
/// <c>FishingScreen</c> translates this to the entity-layer enum before forwarding.
/// </summary>
public enum FishLureEvent
{
    None,
    Splash,
    BadSplash,
    Twitch,
    ReelTick,
}
