using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;

#nullable enable

namespace RiverRats.Game.Entities;

/// <summary>
/// Animated visual effect entity that loops a horizontal sprite sheet with optional smoke particles.
/// Purely visual — no collision. Can be layered on top of other props.
/// </summary>
public sealed class SmallFire
{
    private const float SmokeOffsetX = 8f;   // Center of 16px fire sprite
    private const float SmokeOffsetY = 2f;   // Near the top of the fire

    private readonly Texture2D _spriteSheet;
    private readonly Vector2 _position;
    private readonly LoopAnimator _animator;
    private ParticleEmitter? _smokeEmitter;

    public SmallFire(Vector2 position, Texture2D spriteSheet, LoopAnimator animator)
    {
        _position = position;
        _spriteSheet = spriteSheet;
        _animator = animator;
    }

    public Vector2 Position => _position;

    /// <summary>
    /// Attaches a smoke particle emitter to the fire.
    /// Smoke drifts upward when particles are emitted.
    /// </summary>
    public void AttachSmokeEmitter(ParticleEmitter emitter)
    {
        _smokeEmitter = emitter;
    }

    public void Update(GameTime gameTime)
    {
        _animator.Update(gameTime);

        if (_smokeEmitter != null)
        {
            Vector2 smokePos = new Vector2(
                _position.X + SmokeOffsetX,
                _position.Y + SmokeOffsetY);
            _smokeEmitter.Update(gameTime, smokePos);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _animator.Draw(spriteBatch, _spriteSheet, _position);
    }
}
