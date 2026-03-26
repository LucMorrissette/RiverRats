#nullable enable
using System;
using Microsoft.Xna.Framework;

namespace RiverRats.Components;

/// <summary>
/// Tracks hit points, invincibility, and raises events on damage and death.
/// Reusable by any entity that needs health (player, enemies, etc.).
/// </summary>
public class Health
{
    private float _invincibilityTimer;

    /// <summary>
    /// Creates a new Health component with the specified maximum HP.
    /// </summary>
    /// <param name="maxHp">Maximum (and starting) hit points.</param>
    public Health(int maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = maxHp;
    }

    /// <summary>Current hit points (0 ≤ value ≤ MaxHp).</summary>
    public int CurrentHp { get; private set; }

    /// <summary>Maximum hit points.</summary>
    public int MaxHp { get; private set; }

    /// <summary>True when CurrentHp is greater than zero.</summary>
    public bool IsAlive => CurrentHp > 0;

    /// <summary>When true, <see cref="TakeDamage"/> is ignored.</summary>
    public bool IsInvincible { get; private set; }

    /// <summary>Fired when damage is applied. Parameter is the damage amount.</summary>
    public event Action<int>? OnDamaged;

    /// <summary>Fired when CurrentHp reaches zero.</summary>
    public event Action? OnDied;

    /// <summary>
    /// Reduces CurrentHp by <paramref name="amount"/>, clamped to 0.
    /// Does nothing if invincible or already dead.
    /// Fires <see cref="OnDamaged"/> then <see cref="OnDied"/> if HP reaches 0.
    /// </summary>
    /// <param name="amount">Damage to apply (positive).</param>
    public void TakeDamage(int amount)
    {
        if (!IsAlive || IsInvincible)
            return;

        CurrentHp = Math.Max(0, CurrentHp - amount);
        OnDamaged?.Invoke(amount);

        if (!IsAlive)
            OnDied?.Invoke();
    }

    /// <summary>
    /// Increases CurrentHp by <paramref name="amount"/>, clamped to MaxHp.
    /// Does nothing if already dead.
    /// </summary>
    /// <param name="amount">Amount to heal (positive).</param>
    public void Heal(int amount)
    {
        if (!IsAlive)
            return;

        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// Increases MaxHp and heals CurrentHp by the same amount.
    /// </summary>
    /// <param name="amount">Amount to add to MaxHp and CurrentHp.</param>
    public void IncreaseMax(int amount)
    {
        MaxHp += amount;
        CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
    }

    /// <summary>
    /// Sets or clears the invincibility flag.
    /// </summary>
    /// <param name="invincible">True to enable invincibility.</param>
    public void SetInvincible(bool invincible)
    {
        IsInvincible = invincible;
        if (!invincible)
            _invincibilityTimer = 0f;
    }

    /// <summary>
    /// Enables invincibility for the specified duration.
    /// The timer counts down in <see cref="Update"/>.
    /// </summary>
    /// <param name="seconds">Duration in seconds.</param>
    public void SetInvincibleForDuration(float seconds)
    {
        IsInvincible = true;
        _invincibilityTimer = seconds;
    }

    /// <summary>
    /// Ticks the invincibility timer and auto-clears invincibility when it expires.
    /// </summary>
    /// <param name="gameTime">Current frame timing.</param>
    public void Update(GameTime gameTime)
    {
        if (_invincibilityTimer > 0f)
        {
            _invincibilityTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_invincibilityTimer <= 0f)
            {
                _invincibilityTimer = 0f;
                IsInvincible = false;
            }
        }
    }
}
