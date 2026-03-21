using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Graphics;

namespace RiverRats.Game.Systems;

/// <summary>
/// Manages a pool of firefly entities that spawn during night, drift organically,
/// emit a soft green glow, and fade out after a short life.
/// Zero-allocation in the hot loop — all fireflies are pre-allocated.
/// </summary>
public sealed class FireflyManager
{
    /// <summary>Minimum night strength before fireflies begin spawning.</summary>
    public const float SpawnNightThreshold = 0.3f;

    /// <summary>Target spawns per second at full night.</summary>
    public const float BaseSpawnRate = 0.5f;

    /// <summary>Minimum firefly life in seconds.</summary>
    public const float MinLife = 1.5f;

    /// <summary>Maximum firefly life in seconds.</summary>
    public const float MaxLife = 3.5f;

    /// <summary>Minimum drift speed in pixels per second.</summary>
    public const float MinSpeed = 15f;

    /// <summary>Maximum drift speed in pixels per second.</summary>
    public const float MaxSpeed = 50f;

    /// <summary>Minimum seconds between direction changes.</summary>
    public const float MinDirectionChangeInterval = 0.2f;

    /// <summary>Maximum seconds between direction changes.</summary>
    public const float MaxDirectionChangeInterval = 0.5f;

    /// <summary>Light radius in world pixels.</summary>
    public const float LightRadius = 25f;

    /// <summary>Peak light intensity (0–1).</summary>
    public const float PeakLightIntensity = 0.5f;

    /// <summary>Pulse speed in radians per second for intensity oscillation.</summary>
    public const float PulseSpeed = 6f;

    /// <summary>Draw scale of the particle texture for each firefly.</summary>
    public const float DrawScale = 0.3f;

    /// <summary>Neon green glow color for firefly lights.</summary>
    public static readonly Color GlowColor = new(57, 255, 20);

    /// <summary>Start color of the firefly visual (bright neon green, semi-transparent).</summary>
    public static readonly Color VisualStartColor = new(100, 255, 50, 220);

    /// <summary>End color of the firefly visual (transparent neon green).</summary>
    public static readonly Color VisualEndColor = new(57, 255, 20, 0);

    private readonly Firefly[] _fireflies;
    private readonly int _maxFireflies;
    private readonly Random _rng;
    private float _spawnAccumulator;

    /// <summary>Gets the number of currently active fireflies.</summary>
    public int ActiveCount { get; private set; }

    /// <summary>
    /// Creates a FireflyManager with a preallocated pool.
    /// </summary>
    /// <param name="maxFireflies">Maximum number of simultaneous fireflies.</param>
    /// <param name="seed">Optional RNG seed for deterministic testing. Null uses default.</param>
    public FireflyManager(int maxFireflies = 32, int? seed = null)
    {
        _maxFireflies = maxFireflies;
        _fireflies = new Firefly[_maxFireflies];
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <summary>
    /// Updates all active fireflies and spawns new ones when night is strong enough.
    /// </summary>
    /// <param name="gameTime">Frame timing.</param>
    /// <param name="nightStrength">Current night strength 0–1 from <see cref="DayNightCycle"/>.</param>
    /// <param name="cameraWorldBounds">Camera-visible area in world space for spawn placement.</param>
    public void Update(GameTime gameTime, float nightStrength, Rectangle cameraWorldBounds)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update active fireflies.
        for (var i = 0; i < _maxFireflies; i++)
        {
            if (!_fireflies[i].IsActive) continue;

            _fireflies[i].LifeRemaining -= dt;
            if (_fireflies[i].LifeRemaining <= 0f)
            {
                _fireflies[i].IsActive = false;
                ActiveCount--;
                continue;
            }

            // Direction change timer.
            _fireflies[i].DirectionTimer -= dt;
            if (_fireflies[i].DirectionTimer <= 0f)
            {
                var angle = (float)(_rng.NextDouble() * MathHelper.TwoPi);
                var speed = MathHelper.Lerp(MinSpeed, MaxSpeed, (float)_rng.NextDouble());
                _fireflies[i].Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed);
                _fireflies[i].DirectionTimer = MathHelper.Lerp(
                    MinDirectionChangeInterval,
                    MaxDirectionChangeInterval,
                    (float)_rng.NextDouble());
            }

            _fireflies[i].Position += _fireflies[i].Velocity * dt;
            _fireflies[i].PulsePhase += PulseSpeed * dt;
        }

        // Spawn new fireflies when night is strong enough.
        if (nightStrength >= SpawnNightThreshold && cameraWorldBounds.Width > 0 && cameraWorldBounds.Height > 0)
        {
            var effectiveRate = BaseSpawnRate * ((nightStrength - SpawnNightThreshold) / (1f - SpawnNightThreshold));
            _spawnAccumulator += effectiveRate * dt;

            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                TrySpawn(cameraWorldBounds);
            }
        }
        else
        {
            _spawnAccumulator = 0f;
        }
    }

    /// <summary>
    /// Writes active firefly light data into the provided array starting at <paramref name="offset"/>.
    /// Returns the number of lights written.
    /// </summary>
    /// <param name="lights">Destination array to fill.</param>
    /// <param name="offset">Starting index in the array.</param>
    /// <returns>Number of light entries written.</returns>
    public int WriteLightData(LightData[] lights, int offset)
    {
        var count = 0;
        for (var i = 0; i < _maxFireflies; i++)
        {
            if (!_fireflies[i].IsActive) continue;

            var normalizedLife = _fireflies[i].LifeRemaining / _fireflies[i].InitialLife;
            // Fade in quickly at start, fade out at end.
            var lifeFade = FadeInOut(normalizedLife);
            var pulse = 0.7f + 0.3f * (float)Math.Sin(_fireflies[i].PulsePhase);
            var intensity = PeakLightIntensity * lifeFade * pulse;

            lights[offset + count] = new LightData(
                _fireflies[i].Position,
                LightRadius,
                GlowColor,
                intensity);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Draws all active fireflies as small glowing dots.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch in an active Begin/End block with camera transform.</param>
    /// <param name="texture">A small soft texture (e.g. the existing smoke puff or radial gradient).</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D texture)
    {
        if (texture == null) return;

        var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

        for (var i = 0; i < _maxFireflies; i++)
        {
            if (!_fireflies[i].IsActive) continue;

            var normalizedLife = _fireflies[i].LifeRemaining / _fireflies[i].InitialLife;
            var lifeFade = FadeInOut(normalizedLife);
            var color = Color.Lerp(VisualEndColor, VisualStartColor, lifeFade);

            spriteBatch.Draw(
                texture,
                _fireflies[i].Position,
                null,
                color,
                0f,
                origin,
                DrawScale,
                SpriteEffects.None,
                0f);
        }
    }

    /// <summary>
    /// Gets the position of a firefly at the given pool index. For testing.
    /// </summary>
    public Vector2 GetPosition(int index) => _fireflies[index].Position;

    /// <summary>
    /// Gets whether a firefly at the given pool index is active. For testing.
    /// </summary>
    public bool IsActive(int index) => _fireflies[index].IsActive;

    private void TrySpawn(Rectangle cameraBounds)
    {
        // Find a free slot.
        for (var i = 0; i < _maxFireflies; i++)
        {
            if (_fireflies[i].IsActive) continue;

            var life = MathHelper.Lerp(MinLife, MaxLife, (float)_rng.NextDouble());
            var angle = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            var speed = MathHelper.Lerp(MinSpeed, MaxSpeed, (float)_rng.NextDouble());

            _fireflies[i].Position = new Vector2(
                cameraBounds.X + (float)_rng.NextDouble() * cameraBounds.Width,
                cameraBounds.Y + (float)_rng.NextDouble() * cameraBounds.Height);
            _fireflies[i].Velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed);
            _fireflies[i].InitialLife = life;
            _fireflies[i].LifeRemaining = life;
            _fireflies[i].PulsePhase = (float)(_rng.NextDouble() * MathHelper.TwoPi);
            _fireflies[i].DirectionTimer = MathHelper.Lerp(
                MinDirectionChangeInterval,
                MaxDirectionChangeInterval,
                (float)_rng.NextDouble());
            _fireflies[i].IsActive = true;
            ActiveCount++;
            return;
        }
    }

    /// <summary>
    /// Smoothstep-style fade: ramps up in the first 20% of life, ramps down in the last 30%.
    /// </summary>
    private static float FadeInOut(float normalizedLife)
    {
        // normalizedLife goes from 1 (just spawned) to 0 (about to die).
        // Fade in when normalizedLife is near 1 (first 20% of total life):
        var fadeIn = MathHelper.Clamp((1f - normalizedLife) / 0.2f, 0f, 1f);
        // Fade out when normalizedLife is near 0 (last 30% of total life):
        var fadeOut = MathHelper.Clamp(normalizedLife / 0.3f, 0f, 1f);
        return fadeIn * fadeOut;
    }

    private struct Firefly
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float InitialLife;
        public float LifeRemaining;
        public float PulsePhase;
        public float DirectionTimer;
        public bool IsActive;
    }
}
