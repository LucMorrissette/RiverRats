using System;
using Microsoft.Xna.Framework;

namespace DogDays.Game.Systems;

/// <summary>
/// Pure-logic helper for the fishing cast / fight state machine.
/// All methods are static and operate on <see cref="FishingCastState"/>;
/// they produce side-effect requests via <see cref="FishingCastEvents"/>
/// rather than calling SFX, particle, or GPU APIs directly.
/// <para>
/// This separation makes the state machine fully testable without a
/// <c>GraphicsDevice</c> or any MonoGame content pipeline.
/// </para>
/// </summary>
public static class FishingCastLogic
{
    // ── Tuning constants (mirrors of the private constants in FishingScreen) ──

    public const float WindupDelaySeconds        = 0.5f;
    public const float GaugeSpeedCyclesPerSecond = 1.2f;
    public const float GaugeGreenHalfClose       = 0.20f;
    public const float GaugeGreenHalfFar         = 0.08f;
    public const float GaugeGreenCenter          = 0.5f;
    public const float AimMinX                   = 96f;
    public const float AimSpeedPxPerSecond       = 60f;
    public const float LureFlightDurationSeconds = 0.6f;
    public const float LureArcHeight             = 30f;
    public const float BadCastMinOffset          = 80f;
    public const float BadCastMaxOffset          = 150f;
    public const float LineSettleDurationSeconds = 0.8f;
    public const float LineMaxSag                = 22f;
    public const float LineRelaxSag              = 14f;
    public const float TwitchDistancePx          = 12f;
    public const float TwitchDurationSeconds     = 0.15f;
    public const float RapidTwitchWindowSeconds  = 0.8f;
    public const int   RapidTwitchSafeCount      = 2;
    public const float ReelSlackSpeed            = 35f;
    public const float ReelLureSpeed             = 50f;
    public const float StrikeBreachHeight        = 10f;
    public const float StrikeDuration            = 0.8f;
    public const float StrikeBreachPeakTime      = 0.3f;
    public const float StrikeDiveStartTime       = 0.4f;
    public const float WiggleSpeed               = 18f;
    public const float WiggleAmplitude           = 0.15f;
    public const float HookedReelSpeed           = 30f;
    public const float HookedDriftBackSpeed      = 2f;
    public const float FightCooldownMin          = 2.0f;
    public const float FightCooldownMax          = 5.0f;
    public const float FightBurstDuration        = 1.2f;
    public const float FightPullSpeed            = 55f;
    public const float TensionReelDuringFight    = 0.8f;
    public const float TensionReelNormal         = 0.15f;
    public const float TensionDecay              = 0.45f;
    public const float TensionSnapThreshold      = 1.0f;
    public const float StaminaDrainPerBurst      = 0.18f;
    public const float StaminaFloor              = 0.15f;
    public const float IdleSwayCyclesPerSecond   = 0.6f;
    public const float IdleSwayAmplitudePx       = 1.5f;
    public const int   WaterSurfaceRow           = 6;
    public const int   TileHeightPx             = 16;

    // Convenience: Y coordinate of the water surface (pixels).
    public static readonly float AimY = WaterSurfaceRow * TileHeightPx;

    // ── Rod-tip offsets (needed by logic for lure retrieval bounds) ───────────

    // Cast rod: sprite-origin → tip
    public static readonly Vector2 FishingRodCastOffset   = new(17f, -8f);
    public static readonly Vector2 RodTipLocalOffset      = new(42f, 10f);

    // Hooked rod: sprite-origin → tip
    public static readonly Vector2 FishingRodHookedOffset    = new(17f, -8f);
    public static readonly Vector2 HookedRodTipLocalOffset   = new(37f, 5f);

    // Twitch rotation used by GetRodTipPosition.
    public const float TwitchRotation = -0.25f;

    // ── Main tick entry point ────────────────────────────────────────────────

    /// <summary>
    /// Advances the cast/fight state machine by one frame.
    /// </summary>
    /// <param name="state">Mutable state snapshot (modified in place).</param>
    /// <param name="dt">Frame delta time in seconds.</param>
    /// <param name="confirmHeld">Whether the Confirm button is currently held.</param>
    /// <param name="confirmPressed">Whether the Confirm button was pressed this frame.</param>
    /// <param name="moveLeftPressed">Whether MoveLeft was pressed this frame.</param>
    /// <param name="moveRightHeld">Whether MoveRight is held.</param>
    /// <param name="moveLeftHeld">Whether MoveLeft is held.</param>
    /// <param name="cancelPressed">Whether Cancel was pressed this frame (only used by
    ///   the screen; this parameter is unused here — Cancel is handled at the screen level
    ///   before <see cref="Tick"/> is called).</param>
    /// <param name="playerPosition">World-space top-left of the player character sprite.</param>
    /// <param name="virtualWidth">Virtual-resolution width (for clamping fight movement).</param>
    /// <param name="virtualHeight">Virtual-resolution height.</param>
    /// <param name="rng">Shared random instance (for fight cooldown randomisation).</param>
    /// <param name="hookedFishCenter">Current center of the hooked fish (Vector2.Zero if none).</param>
    /// <param name="hookedFishReelSpeedMultiplier">Reel-speed multiplier from the hooked fish (1.0 if none).</param>
    /// <param name="fishCount">Number of fish currently in the scene.</param>
    /// <param name="getFishAttractionState">
    ///   Callback that returns the attraction state index of fish[i].
    ///   0=Unaware, 1=Curious, 2=Approaching, 3=StrikeReady, 4=Spooked, 5=Hooked, 6=Fleeing.
    /// </param>
    /// <param name="getFishCenter">Callback that returns the center of fish[i].</param>
    /// <param name="events">Output: side-effect requests for this frame.</param>
    public static void Tick(
        FishingCastState state,
        float dt,
        bool confirmHeld,
        bool confirmPressed,
        bool moveLeftPressed,
        bool moveRightHeld,
        bool moveLeftHeld,
        Vector2 playerPosition,
        int virtualWidth,
        int virtualHeight,
        Random rng,
        Vector2 hookedFishCenter,
        float hookedFishReelSpeedMultiplier,
        int fishCount,
        Func<int, int> getFishAttractionState,
        Func<int, Vector2> getFishCenter,
        out FishingCastEvents events)
    {
        events = default;
        events.LureEvent = FishLureEvent.None;

        TickCastStateMachine(
            state, dt,
            confirmHeld, moveLeftPressed,
            playerPosition, virtualWidth, virtualHeight,
            rng, hookedFishCenter, hookedFishReelSpeedMultiplier,
            fishCount, getFishAttractionState, getFishCenter,
            ref events);

        TickAim(state, dt, moveRightHeld, moveLeftHeld);

        // Feed lure events to fish attraction while lure is in water.
        if (state.State is CastState.CastComplete or CastState.ReelingSlack or CastState.ReelingLure
            && state.LurePosition.Y >= AimY)
        {
            // Screen will call fish[i].UpdateAttraction using events.LureEvent.
        }

        // Wiggle timer.
        if (state.State == CastState.FishHooked)
            state.WiggleTimer += dt;
        else
            state.WiggleTimer = 0f;
    }

    // ── Aim ─────────────────────────────────────────────────────────────────

    /// <summary>Updates the aim cursor position.</summary>
    public static void TickAim(
        FishingCastState state,
        float dt,
        bool moveRightHeld,
        bool moveLeftHeld)
    {
        // Lock the aim once the power gauge is active or the lure is in flight/reeling.
        if (state.State is CastState.Charging or CastState.LureFlying or CastState.CastComplete
            or CastState.ReelingSlack or CastState.ReelingLure
            or CastState.FishStrike or CastState.FishHooked)
        {
            return;
        }

        if (moveRightHeld)
            state.AimX += AimSpeedPxPerSecond * dt;

        if (moveLeftHeld)
            state.AimX -= AimSpeedPxPerSecond * dt;

        state.AimX = MathHelper.Clamp(state.AimX, AimMinX, state.AimMaxX);
    }

    // ── Gauge helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current gauge needle position (0–1 ping-pong triangle wave).
    /// </summary>
    public static float GaugeValue(FishingCastState state)
    {
        var t = state.GaugePhase % 1f;
        return t <= 0.5f ? t * 2f : 2f - t * 2f;
    }

    /// <summary>
    /// Returns the green zone [start, end] on the gauge based on cast distance.
    /// Close casts are easy (wide green), far casts are hard (narrow green).
    /// </summary>
    public static (float start, float end) GaugeGreenZone(FishingCastState state)
    {
        var range = state.AimMaxX - AimMinX;
        var distanceFraction = range > 1f
            ? MathHelper.Clamp((state.AimX - AimMinX) / range, 0f, 1f)
            : 0f;
        var half = MathHelper.Lerp(GaugeGreenHalfClose, GaugeGreenHalfFar, distanceFraction);
        return (GaugeGreenCenter - half, GaugeGreenCenter + half);
    }

    // ── Rod-tip position ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the cast-rod tip in world space, accounting for active twitch rotation.
    /// </summary>
    public static Vector2 GetCastRodTipPosition(FishingCastState state, Vector2 playerPosition)
    {
        var handleWorld  = playerPosition + FishingRodCastOffset;
        var handleOrigin = new Vector2(5f, 30f);
        var tipLocal     = RodTipLocalOffset;

        if (state.TwitchTimer <= 0f)
            return handleWorld + tipLocal;

        var twitchT  = state.TwitchTimer / TwitchDurationSeconds;
        var rotation = TwitchRotation * twitchT;
        var offset   = tipLocal - handleOrigin;
        var cos      = MathF.Cos(rotation);
        var sin      = MathF.Sin(rotation);
        var rotated  = new Vector2(
            offset.X * cos - offset.Y * sin,
            offset.X * sin + offset.Y * cos);
        return handleWorld + handleOrigin + rotated;
    }

    /// <summary>Returns the hooked-rod tip in world space.</summary>
    public static Vector2 GetHookedRodTipPosition(Vector2 playerPosition)
        => playerPosition + FishingRodHookedOffset + HookedRodTipLocalOffset;

    // ── Character positions ───────────────────────────────────────────────────

    /// <summary>
    /// Computes the top-left sprite positions for the player and follower characters
    /// standing on the grass shelf, given the map tile dimensions.
    /// </summary>
    /// <param name="grassShelfRow">Row index of the top grass shelf.</param>
    /// <param name="characterFramePixels">Sprite height in pixels.</param>
    /// <param name="grassShelfLeftMarginPx">Left-margin offset in pixels.</param>
    /// <param name="characterSpacingPx">Horizontal gap between follower and player sprites.</param>
    /// <param name="followerPosition">Output: top-left of the follower sprite.</param>
    /// <param name="playerPosition">Output: top-left of the player sprite.</param>
    public static void CalculateCharacterPositions(
        int grassShelfRow,
        int characterFramePixels,
        float grassShelfLeftMarginPx,
        float characterSpacingPx,
        out Vector2 followerPosition,
        out Vector2 playerPosition)
    {
        var standingBaselineY = grassShelfRow * TileHeightPx + 3f;
        var characterTopY = standingBaselineY - characterFramePixels;

        followerPosition = new Vector2(grassShelfLeftMarginPx, characterTopY);
        playerPosition   = new Vector2(
            grassShelfLeftMarginPx + characterFramePixels + characterSpacingPx,
            characterTopY);
    }

    // ── State machine (internal) ─────────────────────────────────────────────

    private static void TickCastStateMachine(
        FishingCastState state,
        float dt,
        bool confirmHeld,
        bool moveLeftPressed,
        Vector2 playerPosition,
        int virtualWidth,
        int virtualHeight,
        Random rng,
        Vector2 hookedFishCenter,
        float hookedFishReelSpeedMultiplier,
        int fishCount,
        Func<int, int> getFishAttractionState,
        Func<int, Vector2> getFishCenter,
        ref FishingCastEvents events)
    {
        switch (state.State)
        {
            case CastState.Idle:
                state.SwayTimer += dt;
                if (confirmHeld)
                {
                    state.State       = CastState.WindingUp;
                    state.WindupTimer = 0f;
                    state.SwayTimer   = 0f;
                    state.LureSwayOffset = 0f;
                }
                break;

            case CastState.WindingUp:
                if (!confirmHeld)
                {
                    state.State = CastState.Idle;
                    break;
                }

                state.WindupTimer += dt;
                if (state.WindupTimer >= WindupDelaySeconds)
                {
                    state.State      = CastState.Charging;
                    state.GaugePhase = 0f;
                }
                break;

            case CastState.Charging:
                state.GaugePhase += dt * GaugeSpeedCyclesPerSecond;

                if (!confirmHeld)
                {
                    var gaugeValue = GaugeValue(state);
                    var (greenStart, greenEnd) = GaugeGreenZone(state);
                    state.LastCastGood = gaugeValue >= greenStart && gaugeValue <= greenEnd;
                    BeginLureFlight(state, playerPosition, gaugeValue, greenStart, greenEnd);
                    events.PlayCastSfx = true;
                }
                break;

            case CastState.LureFlying:
                state.LureFlightTime += dt;
                if (state.LureFlightTime >= LureFlightDurationSeconds)
                {
                    state.LureFlightTime = LureFlightDurationSeconds;
                    state.LurePosition   = state.LureEnd;
                    state.LineSettleTimer = 0f;
                    state.State = CastState.CastComplete;
                    events.PlayPlopSfx = true;

                    if (state.LastCastGood)
                    {
                        events.SpawnLureLandingSplash = true;
                        events.LureEvent    = FishLureEvent.Splash;
                    }
                    else
                    {
                        events.SpawnBadCastSplash = true;
                        events.LureEvent          = FishLureEvent.BadSplash;
                    }
                    events.SplashPosition = new Vector2(state.LurePosition.X, AimY);
                }
                else
                {
                    var t = state.LureFlightTime / LureFlightDurationSeconds;
                    state.LurePosition = Vector2.Lerp(state.LureStart, state.LureEnd, t);
                    state.LurePosition.Y -= LureArcHeight * MathF.Sin(MathF.PI * t);
                }
                break;

            case CastState.CastComplete:
                TickCastComplete(state, dt, confirmHeld, moveLeftPressed,
                    playerPosition, virtualHeight, fishCount, getFishAttractionState, getFishCenter,
                    ref events);
                break;

            case CastState.ReelingSlack:
                if (!confirmHeld)
                {
                    state.CurrentSag = MathHelper.Max(state.CurrentSag, LineRelaxSag);
                    state.State      = CastState.CastComplete;
                    break;
                }

                events.LureEvent = FishLureEvent.ReelTick;
                events.PlayReelTick = true;

                state.CurrentSag -= ReelSlackSpeed * dt;
                if (state.CurrentSag <= 0f)
                {
                    state.CurrentSag = 0f;
                    state.State      = CastState.ReelingLure;
                }
                break;

            case CastState.ReelingLure:
                TickReelingLure(state, dt, confirmHeld, playerPosition, ref events);
                break;

            case CastState.FishStrike:
                TickFishStrike(state, dt, hookedFishCenter, rng, ref events);
                break;

            case CastState.FishHooked:
                TickFishHooked(state, dt, confirmHeld, playerPosition,
                    virtualWidth, hookedFishCenter, hookedFishReelSpeedMultiplier,
                    rng, ref events);
                break;
        }
    }

    private static void TickCastComplete(
        FishingCastState state,
        float dt,
        bool confirmHeld,
        bool moveLeftPressed,
        Vector2 playerPosition,
        int virtualHeight,
        int fishCount,
        Func<int, int> getFishAttractionState,
        Func<int, Vector2> getFishCenter,
        ref FishingCastEvents events)
    {
        // Line settle.
        if (state.LineSettleTimer < LineSettleDurationSeconds)
        {
            state.LineSettleTimer += dt;
            var settleT = MathHelper.Clamp(state.LineSettleTimer / LineSettleDurationSeconds, 0f, 1f);
            state.CurrentSag = LineMaxSag * (1f - (1f - settleT) * (1f - settleT));
        }

        // Rapid-twitch window.
        if (state.RapidTwitchWindow > 0f)
            state.RapidTwitchWindow -= dt;

        // Twitch timer.
        if (state.TwitchTimer > 0f)
        {
            state.TwitchTimer -= dt;
            if (state.TwitchTimer <= 0f)
            {
                state.TwitchTimer = 0f;
                state.CurrentSag  = LineRelaxSag;
            }
        }

        // Tap MoveLeft to twitch.
        if (moveLeftPressed)
        {
            var rodTip = GetCastRodTipPosition(state, playerPosition);
            state.LurePosition.X = MathHelper.Max(state.LurePosition.X - TwitchDistancePx, rodTip.X);
            state.CurrentSag     = MathHelper.Max(state.CurrentSag - 3f, 0f);
            state.TwitchTimer    = TwitchDurationSeconds;
            events.PlayTwitchSfx = true;

            if (state.RapidTwitchWindow <= 0f)
                state.RapidTwitchCount = 0;
            state.RapidTwitchCount++;
            state.RapidTwitchWindow = RapidTwitchWindowSeconds;

            var twitchPos = new Vector2(state.LurePosition.X, AimY);
            events.SplashPosition = twitchPos;

            if (state.RapidTwitchCount > RapidTwitchSafeCount)
            {
                events.LureEvent      = FishLureEvent.BadSplash;
                events.SpawnSpookRings = true;
            }
            else
            {
                events.LureEvent        = FishLureEvent.Twitch;
                events.SpawnTwitchRipple = true;
            }

            // Check for strike — a twitch while a fish is StrikeReady (attraction index 3) triggers a bite.
            for (var i = 0; i < fishCount; i++)
            {
                if (getFishAttractionState(i) == 3) // StrikeReady
                {
                    var strikePos = new Vector2(state.LurePosition.X, AimY);
                    state.State         = CastState.FishStrike;
                    state.StrikeStartPos = getFishCenter(i);
                    state.StrikeTimer   = 0f;
                    state.TwitchTimer   = 0f;
                    // Scale dive depth by how far from shore the lure is,
                    // so fish near the diagonal slope don't clip into ground.
                    var depthFraction = MathHelper.Clamp(
                        (state.LurePosition.X - AimMinX) / (state.AimMaxX - AimMinX), 0f, 1f);
                    var groundAtX = AimY + (virtualHeight - AimY) * depthFraction;
                    state.HookTarget    = new Vector2(
                        state.LurePosition.X,
                        AimY + (groundAtX - AimY) * 0.5f);

                    events.FishHooked         = true;
                    events.HookedFishIndex    = i;
                    events.HookStrikeStartPos = state.StrikeStartPos;
                    events.HookTarget         = state.HookTarget;
                    events.TriggerShake       = true;
                    events.SpawnSplash        = true;
                    events.SplashPosition     = strikePos;
                    events.SpawnStrikeSpookRings   = true;
                    events.StrikeRipplePosition    = strikePos;
                    events.PlayStrikeSfx           = true;

                    // Gather indices of fish to spook.
                    var attracted = new System.Collections.Generic.List<int>();
                    var nearby    = new System.Collections.Generic.List<int>();
                    for (var j = 0; j < fishCount; j++)
                    {
                        if (j == i) continue;
                        var attractionJ = getFishAttractionState(j);
                        if (attractionJ is 1 or 2 or 3)
                        {
                            attracted.Add(j);
                        }
                        else
                        {
                            var dist = Vector2.Distance(getFishCenter(j), state.StrikeStartPos);
                            if (dist < 80f)
                                nearby.Add(j);
                        }
                    }
                    events.StrikeSpooked     = attracted.Count > 0 ? attracted.ToArray() : Array.Empty<int>();
                    events.StrikeNearbySpook = nearby.Count > 0 ? nearby.ToArray() : Array.Empty<int>();
                    break;
                }
            }
        }

        // Idle sway when lure is past the shoreline (in the air / water).
        if (state.LurePosition.Y < AimY)
        {
            state.SwayTimer     += dt;
            state.LureSwayOffset = MathF.Sin(state.SwayTimer * IdleSwayCyclesPerSecond * MathHelper.TwoPi) * IdleSwayAmplitudePx;
        }
        else
        {
            state.LureSwayOffset = 0f;
        }

        // Hold Confirm to reel.
        if (confirmHeld)
        {
            state.LureSwayOffset = 0f;
            state.SwayTimer      = 0f;
            events.LureEvent     = FishLureEvent.ReelTick;
            state.State          = state.CurrentSag > 0f ? CastState.ReelingSlack : CastState.ReelingLure;
            events.PlayReelTick  = true;
        }
    }

    private static void TickReelingLure(
        FishingCastState state,
        float dt,
        bool confirmHeld,
        Vector2 playerPosition,
        ref FishingCastEvents events)
    {
        if (!confirmHeld)
        {
            state.CurrentSag = LineRelaxSag;
            state.State      = CastState.CastComplete;
            return;
        }

        events.LureEvent    = FishLureEvent.ReelTick;
        events.PlayReelTick = true;

        var rodTip = GetCastRodTipPosition(state, playerPosition);
        var shoreX = rodTip.X;

        if (state.LurePosition.X > shoreX)
        {
            state.LurePosition.X -= ReelLureSpeed * dt;
            state.LurePosition.Y  = AimY;

            if (state.LurePosition.X <= shoreX)
                state.LurePosition.X = shoreX;
        }
        else
        {
            var toTip = rodTip - state.LurePosition;
            var dist  = toTip.Length();
            if (dist <= ReelLureSpeed * dt)
            {
                state.LurePosition = rodTip;
                state.State        = CastState.Idle;
            }
            else
            {
                state.LurePosition += Vector2.Normalize(toTip) * ReelLureSpeed * dt;
            }
        }
    }

    private static void TickFishStrike(
        FishingCastState state,
        float dt,
        Vector2 hookedFishCenter,
        Random rng,
        ref FishingCastEvents events)
    {
        state.StrikeTimer += dt;

        Vector2 newFishPos;

        if (state.StrikeTimer < StrikeBreachPeakTime)
        {
            var t = state.StrikeTimer / StrikeBreachPeakTime;
            newFishPos = new Vector2(
                MathHelper.Lerp(state.StrikeStartPos.X, state.LurePosition.X, t),
                MathHelper.Lerp(state.StrikeStartPos.Y, AimY - StrikeBreachHeight, t));
        }
        else if (state.StrikeTimer < StrikeDiveStartTime)
        {
            newFishPos = new Vector2(state.LurePosition.X, AimY - StrikeBreachHeight);
        }
        else if (state.StrikeTimer < StrikeDuration)
        {
            var t = (state.StrikeTimer - StrikeDiveStartTime) / (StrikeDuration - StrikeDiveStartTime);
            t = 1f - (1f - t) * (1f - t);
            var diveStart = new Vector2(state.LurePosition.X, AimY - StrikeBreachHeight);
            newFishPos = Vector2.Lerp(diveStart, state.HookTarget, t);
        }
        else
        {
            // Strike complete — transition to hooked.
            newFishPos = state.HookTarget;
            state.State          = CastState.FishHooked;
            state.WiggleTimer    = 0f;
            state.LineTension    = 0f;
            state.FightBurstTimer = 0f;
            state.FishStamina    = 1f;
            state.FightCooldown  = FightCooldownMin + (float)rng.NextDouble() * (FightCooldownMax - FightCooldownMin);
        }

        events.UpdateHookedFishPosition = true;
        events.HookedFishNewPosition    = newFishPos;
    }

    private static void TickFishHooked(
        FishingCastState state,
        float dt,
        bool confirmHeld,
        Vector2 playerPosition,
        int virtualWidth,
        Vector2 hookedFishCenter,
        float hookedFishReelSpeedMultiplier,
        Random rng,
        ref FishingCastEvents events)
    {
        var fishInWater = hookedFishCenter.X > AimMinX;

        // ── Fight burst management ────────────────────────────────────────────
        if (!fishInWater)
        {
            state.FightBurstTimer = 0f;
        }
        else if (state.FightBurstTimer > 0f)
        {
            state.FightBurstTimer -= dt;
            if (state.FightBurstTimer <= 0f)
            {
                state.FightBurstTimer = 0f;
                state.FightCooldown   = FightCooldownMin + (float)rng.NextDouble() * (FightCooldownMax - FightCooldownMin);
            }
        }
        else
        {
            state.FightCooldown -= dt;
            if (state.FightCooldown <= 0f)
            {
                var staminaFactor     = MathHelper.Max(state.FishStamina, StaminaFloor);
                state.FightBurstTimer = FightBurstDuration * staminaFactor;
                state.FishStamina     = MathHelper.Max(state.FishStamina - StaminaDrainPerBurst, 0f);
                events.SpawnFightSpookRing   = true;
                events.FightRipplePosition   = hookedFishCenter;
            }
        }

        // ── Line tension ──────────────────────────────────────────────────────
        if (confirmHeld)
            state.LineTension += (state.IsFighting ? TensionReelDuringFight : TensionReelNormal) * dt;
        else
            state.LineTension -= TensionDecay * dt;

        state.LineTension = MathHelper.Clamp(state.LineTension, 0f, TensionSnapThreshold);

        // ── Snap check ────────────────────────────────────────────────────────
        if (state.LineTension >= TensionSnapThreshold)
        {
            state.State       = CastState.Idle;
            state.LineTension = 0f;
            events.FishFled   = true;
            events.TriggerShake = true;
            return;
        }

        // ── Fight burst: fish pulls hard to the right ─────────────────────────
        if (state.IsFighting)
        {
            var staminaFactor = MathHelper.Max(state.FishStamina, StaminaFloor);
            var fightWiggle   = MathF.Sin(state.WiggleTimer * WiggleSpeed * 2.5f) * WiggleAmplitude * 1.8f * staminaFactor;

            var pullDir = new Vector2(1f, MathF.Sin(state.WiggleTimer * 4f) * 0.4f);
            pullDir = Vector2.Normalize(pullDir);
            var newCenter = hookedFishCenter + pullDir * FightPullSpeed * staminaFactor * dt;

            newCenter.Y = MathHelper.Clamp(newCenter.Y, AimY, state.HookTarget.Y);
            newCenter.X = MathHelper.Min(newCenter.X, virtualWidth - 16f);

            events.UpdateHookedFishPosition = true;
            events.HookedFishNewPosition    = newCenter;
            events.HookedFishRotation       = fightWiggle;
            events.HookedFishFaceLeft       = false;

            if (confirmHeld)
            {
                events.PlayReelTick = true;
                var reelSpeed = HookedReelSpeed * hookedFishReelSpeedMultiplier * 0.3f;
                newCenter = hookedFishCenter;
                if (newCenter.X > AimMinX)
                {
                    newCenter.X -= reelSpeed * dt;
                    // Snap to shore surface when crossing the shoreline so the
                    // fish doesn't end up underground while fighting mid-reel.
                    if (newCenter.X <= AimMinX)
                    {
                        newCenter.X = AimMinX;
                        newCenter.Y = AimY;
                    }
                    events.HookedFishNewPosition = newCenter;
                }
            }
        }
        else if (confirmHeld)
        {
            events.PlayReelTick     = true;
            events.HookedFishFaceLeft = true;

            var reelSpeed = HookedReelSpeed * hookedFishReelSpeedMultiplier;
            var rodTip    = GetHookedRodTipPosition(playerPosition);
            var shoreX    = AimMinX;

            if (hookedFishCenter.X > shoreX)
            {
                var newX = hookedFishCenter.X - reelSpeed * dt;
                if (newX <= shoreX) newX = shoreX;

                var totalDist = state.HookTarget.X - shoreX;
                var progress  = totalDist > 1f
                    ? 1f - MathHelper.Clamp((newX - shoreX) / totalDist, 0f, 1f)
                    : 1f;
                var newY  = MathHelper.Lerp(state.HookTarget.Y, AimY, progress);
                newY      = MathHelper.Max(newY, AimY);
                var rotTarget = MathHelper.PiOver2;
                var wiggle    = MathF.Sin(state.WiggleTimer * WiggleSpeed) * WiggleAmplitude;

                events.UpdateHookedFishPosition = true;
                events.HookedFishNewPosition    = new Vector2(newX, newY);
                events.HookedFishRotation       = MathHelper.Lerp(0f, rotTarget, progress) + wiggle;
            }
            else
            {
                // Fish in the air — reel to rod tip.
                var airWiggle = MathF.Sin(state.WiggleTimer * WiggleSpeed) * WiggleAmplitude;
                var toRod     = rodTip - hookedFishCenter;
                var dist      = toRod.Length();

                if (dist <= reelSpeed * dt)
                {
                    // Caught!
                    state.State       = CastState.Idle;
                    state.LineTension = 0f;
                    events.FishReachedRod    = true;
                    events.FishCaught        = true;
                    events.TriggerShake      = true;
                    events.PlayCatchSfx      = true;
                    events.SpawnSplash       = true;
                    events.SplashPosition    = new Vector2(rodTip.X, AimY);
                    events.CatchRodTipPosition = rodTip;
                }
                else
                {
                    var newCenter = hookedFishCenter + Vector2.Normalize(toRod) * reelSpeed * dt;
                    // Fish is airborne above shore — keep it at or above the water surface.
                    newCenter.Y = MathHelper.Min(newCenter.Y, AimY);
                    events.UpdateHookedFishPosition = true;
                    events.HookedFishNewPosition    = newCenter;
                    events.HookedFishRotation       = MathHelper.PiOver2 + airWiggle;
                }
            }
        }
        else
        {
            // Not reeling, not fighting — fish drifts back slowly.
            var rodTipIdle = GetHookedRodTipPosition(playerPosition);
            if (hookedFishCenter.X > AimMinX && hookedFishCenter.X > rodTipIdle.X)
            {
                var driftDir  = Vector2.Normalize(new Vector2(1f, 0.3f));
                var newCenter = hookedFishCenter + driftDir * HookedDriftBackSpeed * dt;
                newCenter.Y   = MathHelper.Min(newCenter.Y, state.HookTarget.Y);
                newCenter.X   = MathHelper.Min(newCenter.X, virtualWidth - 16f);

                events.UpdateHookedFishPosition = true;
                events.HookedFishNewPosition    = newCenter;
                events.HookedFishRotation       = 0f;
                events.HookedFishFaceLeft       = false;
            }
        }
    }

    private static void BeginLureFlight(
        FishingCastState state,
        Vector2 playerPosition,
        float gaugeValue,
        float greenStart,
        float greenEnd)
    {
        state.LureStart = playerPosition + FishingRodCastOffset + RodTipLocalOffset;
        var targetX     = state.AimX + 5f / 2f; // AimArrowSize / 2

        if (!state.LastCastGood)
        {
            float offset;
            if (gaugeValue < greenStart)
            {
                var miss = 1f - (gaugeValue / greenStart);
                offset   = -MathHelper.Lerp(BadCastMinOffset, BadCastMaxOffset, miss);
            }
            else
            {
                var miss = (gaugeValue - greenEnd) / (1f - greenEnd);
                offset   = MathHelper.Lerp(BadCastMinOffset, BadCastMaxOffset, miss);
            }
            targetX += offset;
            targetX  = MathHelper.Clamp(targetX, 0f, state.AimMaxX + 5f);
        }

        state.LureEnd        = new Vector2(targetX, AimY);
        state.LureFlightTime = 0f;
        state.LurePosition   = state.LureStart;
        state.State          = CastState.LureFlying;
    }
}
