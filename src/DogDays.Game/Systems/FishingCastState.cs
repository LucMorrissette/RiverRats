using Microsoft.Xna.Framework;

namespace DogDays.Game.Systems;

/// <summary>
/// Discriminated-union state for the fishing cast sequence.
/// </summary>
public enum CastState
{
    /// <summary>Rod at rest in front with bobber.</summary>
    Idle,

    /// <summary>Rod behind head, waiting for delay before gauge.</summary>
    WindingUp,

    /// <summary>Power gauge oscillating — release to cast.</summary>
    Charging,

    /// <summary>Lure is flying through the air toward the target.</summary>
    LureFlying,

    /// <summary>Rod in front with slight droop, lure has landed.</summary>
    CastComplete,

    /// <summary>Line slack is being reeled in (sag decreasing, lure stationary).</summary>
    ReelingSlack,

    /// <summary>Line is taut, lure is being retrieved toward the rod tip.</summary>
    ReelingLure,

    /// <summary>Fish lunging + breach animation.</summary>
    FishStrike,

    /// <summary>Fish on the line, player reels in.</summary>
    FishHooked,
}

/// <summary>
/// Pure-data snapshot of the entire fishing cast + fight machine.
/// Contains no MonoGame GPU types — fully testable without a <c>GraphicsDevice</c>.
/// <para>
/// Side-effecting operations (SFX, particles, ripples, fish attraction) are communicated
/// back to <c>FishingScreen</c> via <see cref="FishingCastEvents"/> after each
/// <see cref="FishingCastLogic.Tick"/> call.
/// </para>
/// </summary>
public sealed class FishingCastState
{
    // ── Aim ─────────────────────────────────────────────────────────────────
    /// <summary>Current horizontal aim position on the water surface (pixels).</summary>
    public float AimX;

    /// <summary>Maximum aim X (right edge of water, set once map is loaded).</summary>
    public float AimMaxX;

    // ── Wind-up / charge ─────────────────────────────────────────────────────
    /// <summary>Elapsed time since the player started holding Confirm.</summary>
    public float WindupTimer;

    /// <summary>Current phase of the oscillating power gauge (cycles, unbounded).</summary>
    public float GaugePhase;

    /// <summary>Whether the last cast was in the green zone.</summary>
    public bool LastCastGood;

    // ── Lure flight ───────────────────────────────────────────────────────────
    /// <summary>World-space position from which the lure was launched.</summary>
    public Vector2 LureStart;

    /// <summary>World-space position where the lure will land.</summary>
    public Vector2 LureEnd;

    /// <summary>Current world-space position of the lure (interpolated during flight).</summary>
    public Vector2 LurePosition;

    /// <summary>Elapsed time in the current lure-flight arc.</summary>
    public float LureFlightTime;

    // ── Line settling ─────────────────────────────────────────────────────────
    /// <summary>Elapsed time since the lure landed, driving the slack settle animation.</summary>
    public float LineSettleTimer;

    /// <summary>Current sag depth of the fishing line (pixels).</summary>
    public float CurrentSag;

    // ── Twitch / rapid twitch ────────────────────────────────────────────────
    /// <summary>Remaining seconds of the current rod-flip twitch animation.</summary>
    public float TwitchTimer;

    /// <summary>Rolling window for rapid-twitch counting.</summary>
    public float RapidTwitchWindow;

    /// <summary>Number of twitches performed within the current window.</summary>
    public int RapidTwitchCount;

    // ── Idle sway ─────────────────────────────────────────────────────────────
    /// <summary>Accumulated sway phase timer.</summary>
    public float SwayTimer;

    /// <summary>Current horizontal lure-sway offset (pixels).</summary>
    public float LureSwayOffset;

    // ── Fight / tension ───────────────────────────────────────────────────────
    /// <summary>Current line tension (0 = slack, 1 = snap).</summary>
    public float LineTension;

    /// <summary>Countdown (seconds) until the next fight burst begins.</summary>
    public float FightCooldown;

    /// <summary>Remaining duration (seconds) of the active fight burst (0 = not fighting).</summary>
    public float FightBurstTimer;

    /// <summary>Fish stamina (1 = fresh, 0 = exhausted).</summary>
    public float FishStamina;

    // ── Strike animation ──────────────────────────────────────────────────────
    /// <summary>Elapsed time in the fish-strike animation.</summary>
    public float StrikeTimer;

    /// <summary>World-space position of the hooked fish at the moment it struck.</summary>
    public Vector2 StrikeStartPos;

    /// <summary>Target dive position after the strike breach (halfway depth).</summary>
    public Vector2 HookTarget;

    // ── Wiggle ────────────────────────────────────────────────────────────────
    /// <summary>Accumulated wiggle phase timer (used for sinusoidal fish rotation).</summary>
    public float WiggleTimer;

    // ── Cast state ────────────────────────────────────────────────────────────
    /// <summary>Current state of the cast / fight state machine.</summary>
    public CastState State = CastState.Idle;

    /// <summary>Convenience: whether the fish is currently in a fight burst.</summary>
    public bool IsFighting => FightBurstTimer > 0f;
}
