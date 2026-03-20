using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.Graphics;

#nullable enable

namespace RiverRats.Game.Entities;

/// <summary>
/// Animated visual effect entity that loops a horizontal sprite sheet with optional smoke and spark particles.
/// Purely visual — no collision. Can be layered on top of other props.
/// Emits a warm flickering light that is read by the lighting renderer during night.
/// </summary>
public sealed class SmallFire
{
    private const float SmokeOffsetX = 8f;   // Center of 16px fire sprite
    private const float SmokeOffsetY = 2f;   // Near the top of the fire

    /// <summary>Base light radius in world pixels.</summary>
    public const float BaseLightRadius = 110f;

    /// <summary>Maximum radius deviation for flicker.</summary>
    public const float FlickerRadiusVariance = 3f;

    /// <summary>Base light intensity (0–1).</summary>
    public const float BaseLightIntensity = 0.85f;

    /// <summary>Maximum intensity deviation for flicker.</summary>
    public const float FlickerIntensityVariance = 0.02f;

    /// <summary>Speed of the flicker sine oscillation (radians per second).</summary>
    public const float FlickerSpeed = 10f;

    private readonly Texture2D? _spriteSheet;
    private readonly Vector2 _position;
    private readonly LoopAnimator _animator;
    private ParticleEmitter? _smokeEmitter;
    private ParticleEmitter? _sparkEmitter;
    private float _flickerTime;

    /// <summary>
    /// Creates a logic-only small fire for tests or non-rendering scenarios.
    /// </summary>
    /// <param name="position">Top-left world position of the fire sprite.</param>
    /// <param name="animator">Looping animator for the fire sprite sheet.</param>
    public SmallFire(Vector2 position, LoopAnimator animator)
    {
        _position = position;
        _animator = animator;
        LightRadius = BaseLightRadius;
        LightIntensity = BaseLightIntensity;
    }

    /// <summary>
    /// Creates a drawable small fire with a sprite sheet.
    /// </summary>
    public SmallFire(Vector2 position, Texture2D spriteSheet, LoopAnimator animator)
        : this(position, animator)
    {
        _spriteSheet = spriteSheet;
    }

    /// <summary>Top-left world position of the fire sprite.</summary>
    public Vector2 Position => _position;

    /// <summary>
    /// World-space position of the light origin — horizontally centered on the sprite,
    /// positioned near the flame tip (top quarter of the sprite).
    /// </summary>
    public Vector2 LightPosition => new(
        _position.X + 8f,        // center of 16 px sprite
        _position.Y + 4f);       // flame tip (~top quarter)

    /// <summary>Current light radius in world pixels (fluctuates with flicker).</summary>
    public float LightRadius { get; private set; }

    /// <summary>Current light intensity 0–1 (fluctuates with flicker).</summary>
    public float LightIntensity { get; private set; }

    /// <summary>Warm orange fire colour.</summary>
    public Color LightColor { get; } = new Color(255, 180, 70);

    /// <summary>
    /// Returns a snapshot of this fire's current light properties for use by the
    /// <see cref="LightingRenderer"/> this frame.
    /// </summary>
    public LightData GetLightData() => new LightData(LightPosition, LightRadius, LightColor, LightIntensity);

    /// <summary>
    /// Attaches a smoke particle emitter to the fire.
    /// Smoke drifts upward when particles are emitted.
    /// </summary>
    public void AttachSmokeEmitter(ParticleEmitter emitter)
    {
        _smokeEmitter = emitter;
    }

    /// <summary>
    /// Attaches a spark particle emitter to the fire.
    /// Sparks fly upward when the fire is active.
    /// </summary>
    public void AttachSparkEmitter(ParticleEmitter emitter)
    {
        _sparkEmitter = emitter;
    }

    public void Update(GameTime gameTime)
    {
        _animator.Update(gameTime);

        // Flicker: sine oscillation on both radius and intensity.
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _flickerTime += FlickerSpeed * dt;
        var flicker = (float)Math.Sin(_flickerTime);
        LightRadius = BaseLightRadius + flicker * FlickerRadiusVariance;
        LightIntensity = BaseLightIntensity + flicker * FlickerIntensityVariance;

        if (_smokeEmitter != null)
        {
            Vector2 smokePos = new Vector2(
                _position.X + SmokeOffsetX,
                _position.Y + SmokeOffsetY);
            _smokeEmitter.Update(gameTime, smokePos);
        }

        if (_sparkEmitter != null)
        {
            Vector2 sparkPos = new Vector2(
                _position.X + SmokeOffsetX,   // Same X center as smoke
                _position.Y + SmokeOffsetY + 2f);  // Slightly lower than smoke (closer to flame center)
            _sparkEmitter.Update(gameTime, sparkPos);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_spriteSheet is null)
        {
            throw new InvalidOperationException("A sprite sheet is required to draw SmallFire.");
        }

        _animator.Draw(spriteBatch, _spriteSheet, _position);
    }
}
