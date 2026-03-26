using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Systems;
using RiverRats.Game.World;
using RiverRats.Tests.Helpers;
using Xunit;

namespace RiverRats.Tests.Unit;

public class GnomeEnemyTests
{
    private const int MapSize = 320; // 10×10 tiles of 32px

    /// <summary>Player bounds placed far away so gnomes stay in Chasing state.</summary>
    private static readonly Rectangle FarPlayerBounds = new(9000, 9000, 16, 16);

    /// <summary>
    /// Creates a FlowField with all tiles walkable, pointed toward the given target.
    /// </summary>
    private static FlowField CreateOpenFlowField(Vector2 target)
    {
        var collision = new DelegateCollisionData(_ => false);
        var field = new FlowField(MapSize, MapSize, collision);
        field.Update(target);
        return field;
    }

    private static void UpdateChasing(GnomeEnemy gnome, GameTime gt, Vector2 target,
        FlowField flow, IMapCollisionData collision, Vector2 separation = default)
    {
        gnome.Update(gt, target, flow, collision, separation, FarPlayerBounds);
    }

    [Fact]
    public void FlowField_Update__TargetOnTopRow__DoesNotThrow()
    {
        var collision = new DelegateCollisionData(_ => false);
        var flow = new FlowField(MapSize, MapSize, collision);
        var topRowTarget = new Vector2(5 * 32 + 16, 16);

        var ex = Record.Exception(() => flow.Update(topRowTarget));

        Assert.Null(ex);
    }

    // -- Separation force tests --

    [Fact]
    public void Update__SeparationForce__PushesGnomeAwayFromFlowDirection()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);

        var separation = new Vector2(0, -1f);
        var gt = FakeGameTime.FromSeconds(0.5f);

        UpdateChasing(gnome, gt, target, flow, noWalls, separation);

        Assert.True(gnome.Position.X > 160, "Should move right from flow field");
        Assert.True(gnome.Position.Y < 160, "Should move up from separation force");
    }

    [Fact]
    public void Update__ZeroSeparation__MovesAlongFlowOnly()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);

        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.5f), target, flow, noWalls);

        Assert.True(gnome.Position.X > 160);
    }

    // -- Wall sliding tests --

    [Fact]
    public void Update__BlockedBothAxes__SlidesAlongWallOnFreeAxis()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 300);
        var flow = CreateOpenFlowField(target);
        var wallOnRight = new DelegateCollisionData(r => r.Right > 176 && r.Bottom > 176);

        var startPos = gnome.Position;
        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.1f), target, flow, wallOnRight);

        var moved = gnome.Position != startPos;
        Assert.True(moved, "Gnome should wall-slide instead of stopping when both axes blocked diagonally");
    }

    [Fact]
    public void Update__WallBlocksXAxis__SlidesAlongYAxis()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 300);
        var flow = CreateOpenFlowField(target);
        var wallBlocksX = new DelegateCollisionData(r => r.Left != 160);

        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.5f), target, flow, wallBlocksX);

        Assert.Equal(160, gnome.Position.X, 0.5f);
        Assert.True(gnome.Position.Y > 160, "Should slide along Y when X is blocked");
    }

    [Fact]
    public void Update__WallBlocksYAxis__SlidesXAtFullSpeed()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        // Target is diagonal — flow will have both X and Y components.
        var target = new Vector2(300, 300);
        var flow = CreateOpenFlowField(target);
        // Block any downward movement (Y increases).
        var wallBlocksY = new DelegateCollisionData(r => r.Top > 160);

        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.5f), target, flow, wallBlocksY);

        // With speed redistribution, the gnome should slide X at full speed (60 * 0.5 = 30px)
        // rather than the projected diagonal component (~21px for a 45° flow).
        var distanceX = gnome.Position.X - 160f;
        Assert.True(distanceX > 25f, $"Expected full-speed X slide (≥25px), got {distanceX:F1}px");
        Assert.Equal(160, gnome.Position.Y, 0.5f);
    }

    // -- Attack state machine tests --

    [Fact]
    public void Update__PhasesThrough__AfterStuckFor2Seconds()
    {
        // Place gnome boxed in on all sides.
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var wallEverywhere = new DelegateCollisionData(_ => true);

        var startPos = gnome.Position;

        // Simulate 1.05 seconds stuck (63 frames at 60fps).
        for (var i = 0; i < 63; i++)
            UpdateChasing(gnome, FakeGameTime.OneFrame(), target, flow, wallEverywhere);

        Assert.True(gnome.IsPhasing, "Gnome should start phasing after 2s stuck");

        // Next frame should move through the wall.
        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.1f), target, flow, wallEverywhere);

        Assert.True(gnome.Position.X > startPos.X, "Phasing gnome should move through obstacles");
    }

    [Fact]
    public void Update__ClearsPhasing__AfterMoving16px()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var wallEverywhere = new DelegateCollisionData(_ => true);

        // Get stuck for 1s to trigger phasing.
        for (var i = 0; i < 63; i++)
            UpdateChasing(gnome, FakeGameTime.OneFrame(), target, flow, wallEverywhere);

        Assert.True(gnome.IsPhasing);

        // Move through walls until 16px clear — then phasing should end.
        var noWalls = new DelegateCollisionData(_ => false);
        for (var i = 0; i < 60; i++)
        {
            UpdateChasing(gnome, FakeGameTime.OneFrame(), target, flow, noWalls);
            if (!gnome.IsPhasing)
                break;
        }

        Assert.False(gnome.IsPhasing, "Phasing should clear after moving 16px from stuck point");
        Assert.True(gnome.Position.X > 172, "Gnome should have moved at least 16px from start");
    }

    // -- Attack state machine tests --

    [Fact]
    public void Update__EntersWindingUp__WhenWithinAttackRange()
    {
        // Place gnome very close to target so it enters attack range.
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160); // 8px away — well within 48px trigger range
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        var playerBounds = new Rectangle(160, 152, 16, 16);

        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);

        Assert.Equal(GnomeState.WindingUp, gnome.State);
    }

    [Fact]
    public void Update__StaysChasing__WhenFarFromPlayer()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160); // ~140px away — outside 48px range
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);

        UpdateChasing(gnome, FakeGameTime.OneFrame(), target, flow, noWalls);

        Assert.Equal(GnomeState.Chasing, gnome.State);
    }

    [Fact]
    public void Update__TransitionsToLunging__AfterWindUpCompletes()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        var playerBounds = new Rectangle(160, 152, 16, 16);

        // Enter wind-up.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
        Assert.Equal(GnomeState.WindingUp, gnome.State);

        // Advance past wind-up duration (0.4s).
        // Move player away so gnome doesn't instantly hit during lunge.
        var farBounds = new Rectangle(9000, 9000, 16, 16);
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, farBounds);

        Assert.Equal(GnomeState.Lunging, gnome.State);
    }

    [Fact]
    public void Update__LungeHitsPlayer__EntersStunned()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        // Player right in front of gnome.
        var playerBounds = new Rectangle(176, 155, 16, 16);

        // Enter wind-up.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
        // Complete wind-up.
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, playerBounds);
        Assert.Equal(GnomeState.Lunging, gnome.State);

        // Lunge into the player.
        for (var i = 0; i < 30; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
            if (gnome.State == GnomeState.Stunned)
                break;
        }

        Assert.Equal(GnomeState.Stunned, gnome.State);
    }

    [Fact]
    public void Update__LungeMissesPlayer__EntersStunnedAfterFullDistance()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        var playerBounds = new Rectangle(160, 152, 16, 16);

        // Enter wind-up.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
        // Complete wind-up — move player far away so lunge misses.
        var farBounds = new Rectangle(9000, 9000, 16, 16);
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, farBounds);
        Assert.Equal(GnomeState.Lunging, gnome.State);

        // Advance until lunge completes (80px at 220px/s ≈ 0.36s).
        for (var i = 0; i < 60; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, farBounds);
            if (gnome.State == GnomeState.Stunned)
                break;
        }

        Assert.Equal(GnomeState.Stunned, gnome.State);
    }

    [Fact]
    public void Update__ReturnsToChasing__AfterStunExpires()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        var farBounds = new Rectangle(9000, 9000, 16, 16);

        // Wind-up -> Lunge -> Miss -> Stunned.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, new Rectangle(160, 152, 16, 16));
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, farBounds);
        for (var i = 0; i < 60; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, farBounds);
            if (gnome.State == GnomeState.Stunned)
                break;
        }

        Assert.Equal(GnomeState.Stunned, gnome.State);

        // Advance past miss recovery duration (0.6s).
        gnome.Update(FakeGameTime.FromSeconds(0.7f), target, flow, noWalls, Vector2.Zero, farBounds);

        Assert.Equal(GnomeState.Chasing, gnome.State);
    }

    // -- Dying state tests --

    [Fact]
    public void Die__FromChasing__TransitionsToDyingState()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.Die();
        Assert.Equal(GnomeState.Dying, gnome.State);
        Assert.False(gnome.IsDead);
    }

    [Fact]
    public void Die__CalledTwice__DoesNotResetTimer()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var noWalls = new DelegateCollisionData(_ => false);
        var flow = CreateOpenFlowField(new Vector2(300, 160));

        gnome.Die();
        // Advance half the dying duration.
        gnome.Update(FakeGameTime.FromSeconds(0.1f), new Vector2(300, 160), flow, noWalls, Vector2.Zero, FarPlayerBounds);
        Assert.False(gnome.IsDead);

        // Call Die again — should not reset.
        gnome.Die();
        gnome.Update(FakeGameTime.FromSeconds(0.15f), new Vector2(300, 160), flow, noWalls, Vector2.Zero, FarPlayerBounds);
        Assert.True(gnome.IsDead);
    }

    [Fact]
    public void Update__DyingState__SetsIsDeadAfterDuration()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var noWalls = new DelegateCollisionData(_ => false);
        var flow = CreateOpenFlowField(new Vector2(300, 160));

        gnome.Die();

        // Not dead yet halfway through.
        gnome.Update(FakeGameTime.FromSeconds(0.1f), new Vector2(300, 160), flow, noWalls, Vector2.Zero, FarPlayerBounds);
        Assert.False(gnome.IsDead);

        // Dead after full duration (0.2s total).
        gnome.Update(FakeGameTime.FromSeconds(0.15f), new Vector2(300, 160), flow, noWalls, Vector2.Zero, FarPlayerBounds);
        Assert.True(gnome.IsDead);
    }

    // -- HP / TakeDamage tests --

    [Fact]
    public void SetHp__SetsHpToValue__OnSpawn()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetHp(3);
        Assert.Equal(3, gnome.Hp);
    }

    [Fact]
    public void TakeDamage__ReducesHp__ByDamageAmount()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetHp(3);

        gnome.TakeDamage(1);

        Assert.Equal(2, gnome.Hp);
    }

    [Fact]
    public void TakeDamage__TransitionsToDying__WhenHpReachesZero()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetHp(1);

        gnome.TakeDamage(1);

        Assert.Equal(GnomeState.Dying, gnome.State);
    }

    [Fact]
    public void TakeDamage__TransitionsToStunned__WhenHpAboveZero()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetHp(2);

        gnome.TakeDamage(1);

        Assert.Equal(GnomeState.Stunned, gnome.State);
        Assert.NotEqual(GnomeState.Dying, gnome.State);
    }

    // -- Helper --

    private sealed class DelegateCollisionData : IMapCollisionData
    {
        private readonly Func<Rectangle, bool> _isBlocked;

        public DelegateCollisionData(Func<Rectangle, bool> isBlocked)
        {
            _isBlocked = isBlocked;
        }

        public bool IsWorldRectangleBlocked(Rectangle worldBounds) => _isBlocked(worldBounds);
    }
}
