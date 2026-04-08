#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.Data;

namespace DogDays.Game.Systems;

/// <summary>
/// Queues and times high-visibility quest discovery notifications.
/// </summary>
internal sealed class QuestDiscoverySequence
{
    private const float FadeInSeconds = 0.22f;
    private const float HoldSeconds = 2.6f;
    private const float FadeOutSeconds = 0.42f;
    private const float KickDurationSeconds = 0.34f;
    private const float FlashDurationSeconds = 0.24f;
    private const float BadgePopDurationSeconds = 0.30f;
    private const float ShimmerDelaySeconds = 0.14f;
    private const float ShimmerDurationSeconds = 0.34f;
    private const float KickAmplitudeX = 4.25f;
    private const float KickAmplitudeY = 3.2f;

    private readonly Queue<QuestDefinition> _pendingDefinitions = new();
    private float _elapsedSeconds;

    /// <summary>Total duration of one discovery banner cycle.</summary>
    internal const float TotalDurationSeconds = FadeInSeconds + HoldSeconds + FadeOutSeconds;

    /// <summary>Whether a quest discovery banner is currently active.</summary>
    internal bool IsActive { get; private set; }

    /// <summary>The quest definition currently being displayed.</summary>
    internal QuestDefinition? CurrentQuest { get; private set; }

    /// <summary>Normalized progress through the current discovery banner cycle.</summary>
    internal float Progress => !IsActive
        ? 0f
        : MathHelper.Clamp(_elapsedSeconds / TotalDurationSeconds, 0f, 1f);

    /// <summary>
    /// Normalized banner opacity for the current discovery card.
    /// </summary>
    internal float Opacity
    {
        get
        {
            if (!IsActive)
            {
                return 0f;
            }

            if (_elapsedSeconds < FadeInSeconds)
            {
                return MathHelper.Clamp(_elapsedSeconds / FadeInSeconds, 0f, 1f);
            }

            if (_elapsedSeconds < FadeInSeconds + HoldSeconds)
            {
                return 1f;
            }

            var fadeOutElapsed = _elapsedSeconds - FadeInSeconds - HoldSeconds;
            return 1f - MathHelper.Clamp(fadeOutElapsed / FadeOutSeconds, 0f, 1f);
        }
    }

    /// <summary>
    /// Normalized emphasis curve used to drive motion and accent sizing.
    /// </summary>
    internal float Emphasis => MathF.Sin(Opacity * MathHelper.PiOver2);

    /// <summary>Animated panel offset used for the initial discovery kick.</summary>
    internal Vector2 PanelOffset => !IsActive
        ? Vector2.Zero
        : new Vector2(
            ComputeKickOffset(KickAmplitudeX, 0.45f),
            ComputeKickOffset(KickAmplitudeY, 1.05f));

    /// <summary>Warm flash intensity used to brighten the discovery banner on arrival.</summary>
    internal float FlashIntensity => !IsActive
        ? 0f
        : 1f - EaseOutCubic(MathHelper.Clamp(_elapsedSeconds / FlashDurationSeconds, 0f, 1f));

    /// <summary>Badge scale used to overshoot and settle the NEW marker.</summary>
    internal float BadgeScale
    {
        get
        {
            if (!IsActive)
            {
                return 1f;
            }

            var normalized = MathHelper.Clamp(_elapsedSeconds / BadgePopDurationSeconds, 0f, 1f);
            if (normalized < 0.45f)
            {
                return MathHelper.Lerp(0.84f, 1.14f, EaseOutCubic(normalized / 0.45f));
            }

            return MathHelper.Lerp(1.14f, 1f, EaseOutCubic((normalized - 0.45f) / 0.55f));
        }
    }

    /// <summary>Normalized pulse value used to modulate the discovery accent colors.</summary>
    internal float Pulse => !IsActive
        ? 0f
        : 0.5f + (0.5f * MathF.Sin(Progress * MathHelper.TwoPi * 1.75f));

    /// <summary>Normalized shimmer progress, or -1 when the shimmer is inactive.</summary>
    internal float ShimmerProgress
    {
        get
        {
            if (!IsActive)
            {
                return -1f;
            }

            var normalized = (_elapsedSeconds - ShimmerDelaySeconds) / ShimmerDurationSeconds;
            return normalized is < 0f or > 1f
                ? -1f
                : MathHelper.Clamp(normalized, 0f, 1f);
        }
    }

    /// <summary>
    /// Queues a quest discovery notification.
    /// </summary>
    /// <param name="questDefinition">Quest to display when its turn arrives.</param>
    internal void Enqueue(QuestDefinition questDefinition)
    {
        ArgumentNullException.ThrowIfNull(questDefinition);

        _pendingDefinitions.Enqueue(questDefinition);
        if (!IsActive)
        {
            AdvanceQueue();
        }
    }

    /// <summary>
    /// Advances the banner timer and consumes queued discoveries.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    internal void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        _elapsedSeconds += Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_elapsedSeconds < TotalDurationSeconds)
        {
            return;
        }

        AdvanceQueue();
    }

    private void AdvanceQueue()
    {
        if (_pendingDefinitions.Count == 0)
        {
            IsActive = false;
            CurrentQuest = null;
            _elapsedSeconds = 0f;
            return;
        }

        CurrentQuest = _pendingDefinitions.Dequeue();
        _elapsedSeconds = 0f;
        IsActive = true;
    }

    private float ComputeKickOffset(float amplitude, float phaseOffset)
    {
        var normalized = MathHelper.Clamp(_elapsedSeconds / KickDurationSeconds, 0f, 1f);
        if (normalized >= 1f)
        {
            return 0f;
        }

        var envelope = 1f - EaseOutCubic(normalized);
        var oscillation = MathF.Sin((normalized * MathHelper.TwoPi * 1.3f) + phaseOffset);
        return oscillation * amplitude * envelope;
    }

    private static float EaseOutCubic(float value)
    {
        var inverse = 1f - MathHelper.Clamp(value, 0f, 1f);
        return 1f - (inverse * inverse * inverse);
    }
}