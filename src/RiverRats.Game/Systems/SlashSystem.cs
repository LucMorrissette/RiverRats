using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages always-active hatchet sweep attacks for two slashers (player and follower).
/// Each slasher cycles between a 180° sweep phase (centred on facing) and a cooldown phase.
/// Gnomes within the wedge during a sweep are killed on contact (once per sweep).
/// </summary>
internal sealed class SlashSystem
{
    private const float SweepDuration = 0.6f;
    private const float CooldownDuration = 1.2f;
    private const float SlashRadius = 30f;
    private const float WedgeHalfWidth = MathF.PI / 6f;
    private const float SweepArc = MathF.Tau; // 360° sweep
    private const float SweepSpeed = SweepArc / SweepDuration;

    /// <summary>Slash radius used for hatchet positioning and collision.</summary>
    internal const float Radius = SlashRadius;

    // Player slasher state.
    private float _playerAngle;
    private float _playerSweepProgress;
    private float _playerCooldownTimer;
    private bool _playerIsSweeping;
    private readonly HashSet<GnomeEnemy> _playerHitGnomes = new();

    // Follower slasher state.
    private float _followerAngle;
    private float _followerSweepProgress;
    private float _followerCooldownTimer;
    private bool _followerIsSweeping;
    private readonly HashSet<GnomeEnemy> _followerHitGnomes = new();

    /// <summary>
    /// Creates a slash system with offset timing so slashers alternate.
    /// </summary>
    public SlashSystem()
    {
        // Player starts sweeping immediately.
        _playerIsSweeping = true;
        _playerSweepProgress = 0f;

        // Follower starts in cooldown so they alternate.
        _followerIsSweeping = false;
        _followerCooldownTimer = CooldownDuration;
    }

    /// <summary>Current world-space angle of the player hatchet (radians).</summary>
    internal float PlayerAngle => _playerAngle;

    /// <summary>Whether the player slasher is currently in sweep phase.</summary>
    internal bool PlayerIsSweeping => _playerIsSweeping;

    /// <summary>Current world-space angle of the follower hatchet (radians).</summary>
    internal float FollowerAngle => _followerAngle;

    /// <summary>Whether the follower slasher is currently in sweep phase.</summary>
    internal bool FollowerIsSweeping => _followerIsSweeping;

    /// <summary>
    /// Advances both slash cycles and kills gnomes in the wedge.
    /// </summary>
    public void Update(GameTime gameTime, Vector2 playerCenter, Vector2 followerCenter,
        FacingDirection playerFacing, FacingDirection followerFacing, GnomeSpawner gnomeSpawner)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var gnomes = gnomeSpawner.Gnomes;

        UpdateSlasher(dt, playerCenter, playerFacing, gnomes, gnomeSpawner,
            ref _playerAngle, ref _playerSweepProgress, ref _playerCooldownTimer,
            ref _playerIsSweeping, _playerHitGnomes);

        UpdateSlasher(dt, followerCenter, followerFacing, gnomes, gnomeSpawner,
            ref _followerAngle, ref _followerSweepProgress, ref _followerCooldownTimer,
            ref _followerIsSweeping, _followerHitGnomes);
    }

    /// <summary>
    /// Tests whether a point falls inside the slash wedge. Exposed for unit testing.
    /// </summary>
    /// <param name="ownerCenter">Centre of the slasher.</param>
    /// <param name="targetCenter">Centre of the potential target.</param>
    /// <param name="currentAngle">Current sweep angle in radians.</param>
    /// <param name="radius">Slash radius.</param>
    /// <param name="halfWidth">Half-width of the wedge in radians.</param>
    /// <returns>True if the target is within the wedge.</returns>
    internal static bool IsInWedge(Vector2 ownerCenter, Vector2 targetCenter, float currentAngle, float radius, float halfWidth)
    {
        var diff = targetCenter - ownerCenter;
        var distSq = diff.LengthSquared();
        if (distSq > radius * radius)
            return false;

        var targetAngle = MathF.Atan2(diff.Y, diff.X);
        return AngleDiff(targetAngle, currentAngle) < halfWidth;
    }

    /// <summary>
    /// Returns the absolute angular distance between two angles, normalized to [0, π].
    /// </summary>
    internal static float AngleDiff(float a, float b)
    {
        var diff = MathF.Abs(a - b) % MathF.Tau;
        if (diff > MathF.PI)
            diff = MathF.Tau - diff;
        return diff;
    }

    private void UpdateSlasher(float dt, Vector2 center, FacingDirection facing,
        IReadOnlyList<GnomeEnemy> gnomes, GnomeSpawner gnomeSpawner,
        ref float angle, ref float sweepProgress, ref float cooldownTimer,
        ref bool isSweeping, HashSet<GnomeEnemy> hitGnomes)
    {
        if (isSweeping)
        {
            sweepProgress += SweepSpeed * dt;

            // Compute world angle: full 360° sweep centred on facing direction.
            var facingAngle = FacingToAngle(facing);
            angle = facingAngle - SweepArc * 0.5f + sweepProgress;

            // Check gnome collisions (iterate backwards for safe removal).
            for (var i = gnomes.Count - 1; i >= 0; i--)
            {
                var gnome = gnomes[i];
                if (gnome.State == GnomeState.Dying || hitGnomes.Contains(gnome))
                    continue;

                var gnomeBounds = gnome.Bounds;
                var gnomeCenter = new Vector2(
                    gnomeBounds.X + gnomeBounds.Width * 0.5f,
                    gnomeBounds.Y + gnomeBounds.Height * 0.5f);

                // Use gnome half-size as extra reach for the radius check.
                var gnomeHalfSize = gnomeBounds.Width * 0.5f;
                if (IsInWedge(center, gnomeCenter, angle, SlashRadius + gnomeHalfSize, WedgeHalfWidth))
                {
                    hitGnomes.Add(gnome);
                    gnome.Die();
                }
            }

            // Check if sweep arc complete.
            if (sweepProgress >= SweepArc)
            {
                isSweeping = false;
                cooldownTimer = CooldownDuration;
                sweepProgress = 0f;
                hitGnomes.Clear();
            }
        }
        else
        {
            cooldownTimer -= dt;
            if (cooldownTimer <= 0f)
            {
                isSweeping = true;
                sweepProgress = 0f;
                hitGnomes.Clear();
            }
        }
    }

    /// <summary>
    /// Converts a <see cref="FacingDirection"/> to a world angle in radians.
    /// </summary>
    internal static float FacingToAngle(FacingDirection facing) => facing switch
    {
        FacingDirection.Right => 0f,
        FacingDirection.Down => MathF.PI * 0.5f,
        FacingDirection.Left => MathF.PI,
        FacingDirection.Up => -MathF.PI * 0.5f,
        _ => 0f,
    };
}
