#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.Data;

namespace DogDays.Game.Systems;

/// <summary>
/// Queues short quest-completion HUD states so completed quests remain visible briefly before the tracker advances.
/// </summary>
internal sealed class QuestCompletionSequence
{
    private const float DurationSecondsValue = 3.0f;
    private const float KickDurationSeconds = 0.32f;
    private const float FlashDurationSeconds = 0.22f;
    private const float BadgePopDurationSeconds = 0.28f;
    private const float ShimmerDelaySeconds = 0.10f;
    private const float ShimmerDurationSeconds = 0.26f;
    private const float KickAmplitudeX = 4.75f;
    private const float KickAmplitudeY = 2.4f;
    private readonly Queue<QuestDefinition> _pendingDefinitions = new();
    private float _elapsedSeconds;

    /// <summary>Total duration of one quest-completion HUD state.</summary>
    internal const float DurationSeconds = DurationSecondsValue;

    /// <summary>Whether a quest completion HUD state is currently active.</summary>
    internal bool IsActive { get; private set; }

    /// <summary>The quest definition currently being displayed as completed.</summary>
    internal QuestDefinition? CurrentQuest { get; private set; }

    /// <summary>Normalized progress through the current completion state.</summary>
    internal float Progress => !IsActive
        ? 0f
        : MathHelper.Clamp(_elapsedSeconds / DurationSecondsValue, 0f, 1f);

    /// <summary>Normalized pulse value used by the HUD renderer to emphasize completion.</summary>
    internal float Pulse => !IsActive
        ? 0f
        : 0.5f + (0.5f * MathF.Sin(Progress * MathHelper.TwoPi * 2f));

    /// <summary>Animated panel offset used for the initial completion kick.</summary>
    internal Vector2 PanelOffset => !IsActive
        ? Vector2.Zero
        : new Vector2(
            ComputeKickOffset(KickAmplitudeX, 0.35f),
            ComputeKickOffset(KickAmplitudeY, 0.8f));

    /// <summary>Initial green flash intensity for the completion panel.</summary>
    internal float FlashIntensity => !IsActive
        ? 0f
        : 1f - EaseOutCubic(MathHelper.Clamp(_elapsedSeconds / FlashDurationSeconds, 0f, 1f));

    /// <summary>Badge scale used to overshoot and settle the COMPLETE marker.</summary>
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
                return MathHelper.Lerp(0.82f, 1.18f, EaseOutCubic(normalized / 0.45f));
            }

            return MathHelper.Lerp(1.18f, 1f, EaseOutCubic((normalized - 0.45f) / 0.55f));
        }
    }

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
    /// Queues a completed quest for temporary HUD display.
    /// </summary>
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
    /// Advances the completion timer and consumes queued completions.
    /// </summary>
    internal void Update(GameTime gameTime)
    {
        if (!IsActive)
        {
            return;
        }

        _elapsedSeconds += Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_elapsedSeconds < DurationSecondsValue)
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
        var oscillation = MathF.Sin((normalized * MathHelper.TwoPi * 1.45f) + phaseOffset);
        return oscillation * amplitude * envelope;
    }

    private static float EaseOutCubic(float value)
    {
        var inverse = 1f - MathHelper.Clamp(value, 0f, 1f);
        return 1f - (inverse * inverse * inverse);
    }
}