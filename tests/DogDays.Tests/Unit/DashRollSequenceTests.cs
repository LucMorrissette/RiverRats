using Microsoft.Xna.Framework;
using DogDays.Components;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Systems;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="DashRollSequence"/>.
/// </summary>
public sealed class DashRollSequenceTests
{
    private static readonly Rectangle WorldBounds = new(0, 0, 2048, 2048);
    private static readonly IMapCollisionData NoBlockedTiles = new DelegateCollisionData(_ => false);

    [Fact]
    public void TryBegin__WithMovementInput__StartsDashAndSetsFacing()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        var started = sequence.TryBegin(new Vector2(1f, 0f), player, health: null);

        Assert.True(started);
        Assert.True(sequence.IsActive);
        Assert.Equal(FacingDirection.Right, player.Facing);
        Assert.Equal(DashRollSequence.CooldownSeconds, sequence.CooldownRemainingSeconds, precision: 3);
    }

    [Fact]
    public void TryBegin__WithoutMovementInput__DoesNotStartDash()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        var started = sequence.TryBegin(Vector2.Zero, player, health: null);

        Assert.False(started);
        Assert.False(sequence.IsActive);
        Assert.True(sequence.IsReady);
    }

    [Fact]
    public void TryBegin__AppliesImmediateInvulnerability()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));
        var health = new Health(maxHp: 3);

        sequence.TryBegin(new Vector2(0f, -1f), player, health);
        health.TakeDamage(1);

        Assert.True(health.IsInvincible);
        Assert.Equal(3, health.CurrentHp);
    }

    [Fact]
    public void Update__WhileActive__AdvancesPlayerAndAnimationFrame()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        sequence.TryBegin(new Vector2(1f, 0f), player, health: null);
        sequence.Update(FakeGameTime.FromSeconds(0.12f), player, NoBlockedTiles, health: null);

        Assert.True(player.Position.X > 100f);
        Assert.True(sequence.CurrentFrameIndex > 0);
    }

    [Fact]
    public void Update__WhenBlockedImmediately__EndsDash()
    {
        var blockedBounds = new Rectangle(118, 90, 40, 40);
        var collision = new DelegateCollisionData(bounds => bounds.Intersects(blockedBounds));
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        sequence.TryBegin(new Vector2(1f, 0f), player, health: null);
        sequence.Update(FakeGameTime.OneFrame(), player, collision, health: null);

        Assert.False(sequence.IsActive);
        Assert.True(player.Position.X <= 100f);
    }

    [Fact]
    public void Cooldown__PreventsRestartUntilTimerExpires()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        Assert.True(sequence.TryBegin(new Vector2(1f, 0f), player, health: null));

        sequence.Update(FakeGameTime.FromSeconds(0.5f), player, NoBlockedTiles, health: null);

        Assert.False(sequence.TryBegin(new Vector2(1f, 0f), player, health: null));

        for (var i = 0; i < 4; i++)
        {
            sequence.Update(FakeGameTime.FromSeconds(1f), player, NoBlockedTiles, health: null);
        }

        Assert.True(sequence.IsReady);
        Assert.True(sequence.TryBegin(new Vector2(0f, 1f), player, health: null));
    }

    [Fact]
    public void Cooldown__TicksWhileInactive()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        sequence.TryBegin(new Vector2(1f, 0f), player, health: null);
        sequence.Update(FakeGameTime.FromSeconds(1f), player, NoBlockedTiles, health: null);

        Assert.False(sequence.IsActive);

        sequence.Update(FakeGameTime.FromSeconds(2f), player, NoBlockedTiles, health: null);

        Assert.Equal(1f, sequence.CooldownRemainingSeconds, precision: 2);
        Assert.False(sequence.IsReady);
    }

    [Fact]
    public void CooldownFraction__ShrinksAsTimeElapses()
    {
        var sequence = new DashRollSequence();
        var player = CreatePlayer(new Vector2(100f, 100f));

        sequence.TryBegin(new Vector2(-1f, 0f), player, health: null);
        sequence.Update(FakeGameTime.FromSeconds(2f), player, NoBlockedTiles, health: null);

        Assert.Equal(0.5f, sequence.CooldownFraction, precision: 2);
    }

    [Fact]
    public void TryBegin__WithReducedCooldownMultiplier__StartsShorterCooldown()
    {
        var sequence = new DashRollSequence
        {
            CooldownMultiplier = 0.92f,
        };
        var player = CreatePlayer(new Vector2(100f, 100f));

        sequence.TryBegin(new Vector2(1f, 0f), player, health: null);

        Assert.Equal(DashRollSequence.CooldownSeconds * 0.92f, sequence.CooldownRemainingSeconds, precision: 3);
    }

    private static PlayerBlock CreatePlayer(Vector2 position)
    {
        return new PlayerBlock(position, new Point(32, 32), 96f, WorldBounds);
    }

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;

        public DelegateCollisionData(Func<Rectangle, bool> isBlocked)
        {
            _isBlocked = isBlocked;
        }

        public bool IsWorldRectangleBlocked(Rectangle worldBounds)
        {
            return _isBlocked(worldBounds);
        }
    }
}