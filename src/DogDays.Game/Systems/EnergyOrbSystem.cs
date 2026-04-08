using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Systems;

/// <summary>
/// A single energy orb in the pool. All fields are public for direct struct mutation
/// inside the fixed-size <see cref="EnergyOrbSystem"/> pool; there is no heap allocation
/// per orb.
/// </summary>
public struct EnergyOrb
{
    public Vector2 Position;
    public Vector2 Velocity;
    /// <summary>Elapsed time since spawn — drives the pulse animation.</summary>
    public float Age;
    /// <summary>Random phase offset so orbs don't all pulse in sync.</summary>
    public float PulseOffset;
    public bool IsActive;
    public bool IsRed;
}

/// <summary>
/// Manages a fixed-size pool of <see cref="EnergyOrb"/> pickups that drop from gnome
/// deaths. Handles physics (magnet + drag), pickup detection, XP award, and drawing.
/// Replaces the 6 parallel arrays that previously lived in <c>GameplayScreen</c>.
/// </summary>
public sealed class EnergyOrbSystem
{
    private const float PickupRadius = 14f;
    private const float PickupRadiusSq = PickupRadius * PickupRadius;
    private const float MagnetRadius = 72f;
    private const float MagnetRadiusSq = MagnetRadius * MagnetRadius;
    private const float MagnetForce = 1400f;
    private const float DragPerSecond = 2f;
    private const float TerminalSpeed = 280f;
    private const float PulseSpeed = 6f;
    private const float PulseAmount = 0.18f;
    private const float BaseScale = 0.70f;
    private const float MaxSpawnSpeed = 60f;
    private const float RedEnergyOrbChance = 0.10f;

    private readonly EnergyOrb[] _orbs;
    private readonly int _maxOrbs;

    /// <summary>
    /// Creates the system with a fixed pool capacity.
    /// </summary>
    /// <param name="maxOrbs">Maximum simultaneous orbs (must match the content budget).</param>
    public EnergyOrbSystem(int maxOrbs)
    {
        _maxOrbs = maxOrbs;
        _orbs = new EnergyOrb[maxOrbs];
    }

    /// <summary>Attempts to activate an orb at <paramref name="centre"/>. Silently drops if pool is full.</summary>
    public void Spawn(Vector2 centre, Random rng, bool forceRed = false)
    {
        for (var i = 0; i < _maxOrbs; i++)
        {
            if (_orbs[i].IsActive)
                continue;

            var angle = (float)(rng.NextDouble() * MathHelper.TwoPi);
            var speed = (float)rng.NextDouble() * MaxSpawnSpeed;
            _orbs[i].IsActive = true;
            _orbs[i].Position = centre;
            _orbs[i].Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _orbs[i].Age = 0f;
            _orbs[i].PulseOffset = (float)(rng.NextDouble() * MathHelper.TwoPi);
            _orbs[i].IsRed = forceRed || rng.NextDouble() < RedEnergyOrbChance;
            return;
        }
    }

    /// <summary>
    /// Updates all active orbs: physics, magnet pull toward player, pickup collection.
    /// </summary>
    /// <param name="dt">Delta time in seconds.</param>
    /// <param name="playerCenter">Player centre in world space (magnet/pickup target).</param>
    /// <param name="xpSystem">Optional XP system — receives XP when an orb is collected.</param>
    /// <param name="orbCollectSfx">Sound effect pool for normal orb collection (null-safe).</param>
    /// <param name="redOrbCollectSfx">Sound effect pool for red orb collection (null-safe).</param>
    /// <param name="orbSfxVolume">Volume for normal orb SFX.</param>
    /// <param name="redOrbSfxVolume">Volume for red orb SFX.</param>
    /// <param name="rng">Random used for SFX variation selection.</param>
    public void Update(
        float dt,
        Vector2 playerCenter,
        XpLevelSystem xpSystem,
        SoundEffect[] orbCollectSfx,
        SoundEffect[] redOrbCollectSfx,
        float orbSfxVolume,
        float redOrbSfxVolume,
        Random rng)
    {
        for (var i = 0; i < _maxOrbs; i++)
        {
            if (!_orbs[i].IsActive)
                continue;

            _orbs[i].Age += dt;
            var toPlayer = playerCenter - _orbs[i].Position;
            var distanceSq = toPlayer.LengthSquared();

            if (distanceSq <= PickupRadiusSq)
            {
                if (_orbs[i].IsRed && redOrbCollectSfx?.Length > 0 && redOrbCollectSfx[0] != null)
                {
                    redOrbCollectSfx[rng.Next(redOrbCollectSfx.Length)].Play(redOrbSfxVolume, 0f, 0f);
                }
                else if (orbCollectSfx?.Length > 0 && orbCollectSfx[0] != null)
                {
                    orbCollectSfx[rng.Next(orbCollectSfx.Length)].Play(orbSfxVolume, 0f, 0f);
                }

                xpSystem?.AddXp(_orbs[i].IsRed ? 5 : 1);
                _orbs[i].IsActive = false;
                _orbs[i].IsRed = false;
                continue;
            }

            if (distanceSq <= MagnetRadiusSq && distanceSq > 0.001f)
            {
                var distance = MathF.Sqrt(distanceSq);
                var direction = toPlayer / distance;
                var pullStrength = 1f - MathHelper.Clamp(distance / MagnetRadius, 0f, 1f);
                _orbs[i].Velocity += direction * (MagnetForce * pullStrength * dt);
            }

            var drag = MathF.Exp(-DragPerSecond * dt);
            _orbs[i].Velocity *= drag;
            var speed = _orbs[i].Velocity.Length();
            if (speed > TerminalSpeed)
                _orbs[i].Velocity = _orbs[i].Velocity / speed * TerminalSpeed;
            _orbs[i].Position += _orbs[i].Velocity * dt;
        }
    }

    /// <summary>
    /// Draws all active orbs. Assumes the sprite batch is already begun with the correct state.
    /// </summary>
    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D orbTexture,
        Texture2D orbRedTexture,
        float mapHeight,
        float mapWidth,
        float playerDepth,
        EntityDepthFilter filter)
    {
        if (orbTexture == null)
            return;

        for (var i = 0; i < _maxOrbs; i++)
        {
            if (!_orbs[i].IsActive)
                continue;

            var tex = _orbs[i].IsRed ? (orbRedTexture ?? orbTexture) : orbTexture;
            if (tex == null)
                continue;

            var position = _orbs[i].Position;
            var halfW = tex.Width * 0.5f;
            var halfH = tex.Height * 0.5f;
            var bounds = new Rectangle((int)(position.X - halfW), (int)(position.Y - halfH), tex.Width, tex.Height);
            var depth = SortDepth(bounds, mapHeight, mapWidth);
            if (!PassesDepthFilter(depth, playerDepth, filter))
                continue;

            var pulse = MathF.Sin(_orbs[i].Age * PulseSpeed + _orbs[i].PulseOffset);
            var scale = BaseScale + ((pulse * 0.5f) + 0.5f) * PulseAmount;
            var alpha = 0.78f + ((pulse * 0.5f) + 0.5f) * 0.22f;

            spriteBatch.Draw(
                tex,
                position,
                null,
                Color.White * alpha,
                0f,
                new Vector2(halfW, halfH),
                scale,
                SpriteEffects.None,
                depth);
        }
    }

    // ── depth helpers (duplicated from GameplayScreen to avoid coupling) ──────

    private static float SortDepth(Rectangle bounds, float mapHeight, float mapWidth, float anchorOffset = 0f)
    {
        var yDepth = (bounds.Bottom - anchorOffset) / mapHeight;
        var tieBreakerRange = 1f / mapHeight;
        var yScaled = yDepth * (1f - tieBreakerRange);
        var xTie = bounds.Left / (mapWidth * mapHeight);
        return MathHelper.Clamp(yScaled + xTie, 0f, 0.9999f);
    }

    private static bool PassesDepthFilter(float depth, float playerDepth, EntityDepthFilter filter)
    {
        return filter switch
        {
            EntityDepthFilter.BehindOrAtPlayer => depth <= playerDepth,
            EntityDepthFilter.InFrontOfPlayer => depth > playerDepth,
            _ => true
        };
    }
}
