#nullable enable

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;
using DogDays.Game.Systems;

namespace DogDays.Game.UI;

/// <summary>
/// Renders short-lived additive glint bursts around the quest discovery banner.
/// </summary>
internal sealed class QuestDiscoveryGlintPass
{
    private const int MaxParticleCount = 96;
    private const int MaxScheduledBursts = 4;

    private static readonly ParticleProfile HaloProfile = new()
    {
        SpawnRate = 1f,
        MinLife = 0.20f,
        MaxLife = 0.46f,
        MinSpeed = 24f,
        MaxSpeed = 68f,
        MinScale = 0.10f,
        MaxScale = 0.24f,
        StartColor = new Color(255, 245, 222, 210),
        EndColor = new Color(255, 188, 96, 0),
        SpreadRadians = MathHelper.ToRadians(18f),
        Gravity = 18f,
    };

    private static readonly ParticleProfile SparkProfile = new()
    {
        SpawnRate = 1f,
        MinLife = 0.14f,
        MaxLife = 0.32f,
        MinSpeed = 58f,
        MaxSpeed = 132f,
        MinScale = 0.05f,
        MaxScale = 0.12f,
        StartColor = new Color(255, 250, 236, 255),
        EndColor = new Color(255, 214, 128, 0),
        SpreadRadians = MathHelper.ToRadians(12f),
        Gravity = 44f,
    };

    private readonly ParticleManager _particleManager = new(MaxParticleCount);
    private readonly ScheduledBurst[] _scheduledBursts = new ScheduledBurst[MaxScheduledBursts];
    private int _scheduledBurstCount;

    /// <summary>Whether any active or pending glints remain.</summary>
    internal bool HasActiveParticles => _particleManager.ActiveCount > 0 || _scheduledBurstCount > 0;

    /// <summary>Active particle count exposed for unit tests.</summary>
    internal int ActiveParticleCount => _particleManager.ActiveCount;

    /// <summary>Pending scheduled-burst count exposed for unit tests.</summary>
    internal int PendingBurstCount => _scheduledBurstCount;

    /// <summary>
    /// Triggers a fresh glint burst around the supplied banner rectangle.
    /// </summary>
    internal void Trigger(Rectangle panelRect, int sceneScale)
    {
        _scheduledBurstCount = 0;

        var topCenter = new Vector2(panelRect.Center.X, panelRect.Y + (10f * sceneScale));
        var topLeft = new Vector2(panelRect.X + (30f * sceneScale), panelRect.Y + (14f * sceneScale));
        var topRight = new Vector2(panelRect.Right - (30f * sceneScale), panelRect.Y + (14f * sceneScale));
        var badgeAnchor = new Vector2(panelRect.Right - (44f * sceneScale), panelRect.Y + (20f * sceneScale));

        EmitFan(topCenter, particleCount: 10, minAngle: -0.95f, maxAngle: 0.95f, useHaloProfile: true);
        EmitFan(topLeft, particleCount: 6, minAngle: 0.08f, maxAngle: 1.05f, useHaloProfile: false);
        EmitFan(topRight, particleCount: 6, minAngle: -1.05f, maxAngle: -0.08f, useHaloProfile: false);
        EmitRing(badgeAnchor, particleCount: 5, useHaloProfile: true);

        ScheduleBurst(
            new Vector2(panelRect.X + (72f * sceneScale), panelRect.Y + (24f * sceneScale)),
            delaySeconds: 0.08f,
            particleCount: 5,
            kind: ScheduledBurstKind.SparkFan,
            minAngle: -0.18f,
            maxAngle: 0.72f);
        ScheduleBurst(
            new Vector2(panelRect.Right - (86f * sceneScale), panelRect.Y + (26f * sceneScale)),
            delaySeconds: 0.16f,
            particleCount: 5,
            kind: ScheduledBurstKind.SparkFan,
            minAngle: -0.72f,
            maxAngle: 0.18f);
        ScheduleBurst(
            new Vector2(panelRect.Center.X, panelRect.Y + (18f * sceneScale)),
            delaySeconds: 0.24f,
            particleCount: 6,
            kind: ScheduledBurstKind.HaloRing,
            minAngle: 0f,
            maxAngle: MathHelper.TwoPi);
        ScheduleBurst(
            new Vector2(panelRect.Right - (52f * sceneScale), panelRect.Y + (16f * sceneScale)),
            delaySeconds: 0.30f,
            particleCount: 4,
            kind: ScheduledBurstKind.HaloRing,
            minAngle: 0f,
            maxAngle: MathHelper.TwoPi);
    }

    /// <summary>
    /// Advances active glints and fires any scheduled follow-up bursts.
    /// </summary>
    internal void Update(GameTime gameTime)
    {
        var dt = Math.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
        for (var i = 0; i < _scheduledBurstCount;)
        {
            _scheduledBursts[i].DelaySeconds -= dt;
            if (_scheduledBursts[i].DelaySeconds > 0f)
            {
                i++;
                continue;
            }

            EmitScheduledBurst(_scheduledBursts[i]);
            RemoveScheduledBurstAt(i);
        }

        _particleManager.Update(gameTime);
    }

    /// <summary>
    /// Draws the active glints with the supplied particle texture.
    /// </summary>
    internal void Draw(SpriteBatch spriteBatch, Texture2D particleTexture)
    {
        _particleManager.Draw(spriteBatch, particleTexture);
    }

    private void ScheduleBurst(
        Vector2 position,
        float delaySeconds,
        int particleCount,
        ScheduledBurstKind kind,
        float minAngle,
        float maxAngle)
    {
        if (_scheduledBurstCount >= MaxScheduledBursts)
        {
            return;
        }

        _scheduledBursts[_scheduledBurstCount++] = new ScheduledBurst
        {
            Position = position,
            DelaySeconds = delaySeconds,
            ParticleCount = particleCount,
            Kind = kind,
            MinAngle = minAngle,
            MaxAngle = maxAngle,
        };
    }

    private void EmitScheduledBurst(in ScheduledBurst burst)
    {
        switch (burst.Kind)
        {
            case ScheduledBurstKind.HaloRing:
                EmitRing(burst.Position, burst.ParticleCount, useHaloProfile: true);
                break;

            case ScheduledBurstKind.SparkFan:
                EmitFan(burst.Position, burst.ParticleCount, burst.MinAngle, burst.MaxAngle, useHaloProfile: false);
                break;
        }
    }

    private void EmitRing(Vector2 position, int particleCount, bool useHaloProfile)
    {
        var profile = useHaloProfile ? HaloProfile : SparkProfile;
        for (var i = 0; i < particleCount; i++)
        {
            var angle = (MathHelper.TwoPi * i) / Math.Max(1, particleCount);
            _particleManager.Emit(profile, position, 1, angle);
        }
    }

    private void EmitFan(Vector2 position, int particleCount, float minAngle, float maxAngle, bool useHaloProfile)
    {
        var profile = useHaloProfile ? HaloProfile : SparkProfile;
        if (particleCount <= 1)
        {
            _particleManager.Emit(profile, position, 1, (minAngle + maxAngle) * 0.5f);
            return;
        }

        for (var i = 0; i < particleCount; i++)
        {
            var normalized = i / (float)(particleCount - 1);
            var angle = MathHelper.Lerp(minAngle, maxAngle, normalized);
            _particleManager.Emit(profile, position, 1, angle);
        }
    }

    private void RemoveScheduledBurstAt(int index)
    {
        _scheduledBurstCount--;
        if (index < _scheduledBurstCount)
        {
            _scheduledBursts[index] = _scheduledBursts[_scheduledBurstCount];
        }

        _scheduledBursts[_scheduledBurstCount] = default;
    }

    private enum ScheduledBurstKind : byte
    {
        HaloRing,
        SparkFan,
    }

    private struct ScheduledBurst
    {
        public Vector2 Position;
        public float DelaySeconds;
        public int ParticleCount;
        public ScheduledBurstKind Kind;
        public float MinAngle;
        public float MaxAngle;
    }
}