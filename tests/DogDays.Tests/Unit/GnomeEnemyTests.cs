using System;
using Microsoft.Xna.Framework;
using DogDays.Data;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.Systems;
using DogDays.Game.World;
using DogDays.Tests.Helpers;
using Xunit;

namespace DogDays.Tests.Unit;

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
    public void FlowField__Update__TargetOnTopRow__DoesNotThrow()
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
        // Foot bounds Left = Round(pos.X + 3) = 163 at starting position.
        // Block any rect whose Left is not 163 — prevents X movement.
        var wallBlocksX = new DelegateCollisionData(r => r.Left != 163);

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
        // Block any downward movement (Y increases beyond foot bounds starting Top).
        // Foot bounds Top = Round(pos.Y + 10) = 170 at starting position.
        var wallBlocksY = new DelegateCollisionData(r => r.Top > 170);

        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.5f), target, flow, wallBlocksY);

        // With speed redistribution, the gnome should slide X at full speed (60 * 0.5 = 30px)
        // rather than the projected diagonal component (~21px for a 45° flow).
        var distanceX = gnome.Position.X - 160f;
        Assert.True(distanceX > 25f, $"Expected full-speed X slide (≥25px), got {distanceX:F1}px");
        Assert.Equal(160, gnome.Position.Y, 0.5f);
    }

    // -- Attack state machine tests --

    // -- Foot bounds tests --

    [Fact]
    public void FootBounds__UsesStandardizedRatios__ForMovementHull()
    {
        // GnomeEnemy sprite is 16×16. Foot bounds should be:
        // Width  = Round(16 * 0.6)  = 10
        // Height = Round(16 * 0.25) = 4
        // OffsetX = (16 - 10) / 2   = 3
        // OffsetY = 16 - 4 - 2      = 10
        var gnome = new GnomeEnemy(new Vector2(100, 200), 0f);

        var foot = gnome.FootBounds;

        Assert.Equal(103, foot.X);    // 100 + 3
        Assert.Equal(210, foot.Y);    // 200 + 10
        Assert.Equal(10, foot.Width);
        Assert.Equal(4, foot.Height);
    }

    [Fact]
    public void FootBounds__IsSmallerThanBounds()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);

        var foot = gnome.FootBounds;
        var full = gnome.Bounds;

        Assert.True(foot.Width < full.Width);
        Assert.True(foot.Height < full.Height);
    }

    [Fact]
    public void Update__UsesFootBounds__ForMovementCollision()
    {
        // Wall blocks the full-sprite area but NOT the foot bounds area.
        // If the gnome can still move, it's using foot bounds for collision.
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);

        // Block any rectangle that starts at Y < 165. Full sprite starts at Y=160,
        // but foot bounds start at Y=170. So this wall blocks full-sprite checks
        // but NOT foot-bounds checks.
        var wallBlocksFullSpriteOnly = new DelegateCollisionData(r => r.Top < 165);

        UpdateChasing(gnome, FakeGameTime.FromSeconds(0.1f), target, flow, wallBlocksFullSpriteOnly);

        Assert.True(gnome.Position.X > 160, "Gnome should move because foot bounds are below the wall");
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

    // -- Enemy Type Variant Tests --

    [Fact]
    public void SetEnemyType__Standard__DefaultValues()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetEnemyType(EnemyType.Standard);

        Assert.Equal(EnemyType.Standard, gnome.EnemyType);
        Assert.False(gnome.ExplodeOnDeath);
    }

    [Fact]
    public void SetEnemyType__Rusher__HasHigherBaseSpeed()
    {
        var standard = new GnomeEnemy(new Vector2(160, 160), 0f);
        var rusher = new GnomeEnemy(new Vector2(160, 160), 0f);
        rusher.SetEnemyType(EnemyType.Rusher);

        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);

        UpdateChasing(standard, FakeGameTime.FromSeconds(0.5f), target, flow, noWalls);
        UpdateChasing(rusher, FakeGameTime.FromSeconds(0.5f), target, flow, noWalls);

        // Rusher should move further due to higher base speed.
        Assert.True(rusher.Position.X > standard.Position.X,
            $"Rusher ({rusher.Position.X}) should move further right than Standard ({standard.Position.X})");
    }

    [Fact]
    public void SetEnemyType__Brute__HasLowerBaseSpeed()
    {
        var standard = new GnomeEnemy(new Vector2(160, 160), 0f);
        var brute = new GnomeEnemy(new Vector2(160, 160), 0f);
        brute.SetEnemyType(EnemyType.Brute);

        var target = new Vector2(300, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);

        UpdateChasing(standard, FakeGameTime.FromSeconds(0.5f), target, flow, noWalls);
        UpdateChasing(brute, FakeGameTime.FromSeconds(0.5f), target, flow, noWalls);

        // Brute should move less due to lower base speed.
        Assert.True(brute.Position.X < standard.Position.X,
            $"Brute ({brute.Position.X}) should move less than Standard ({standard.Position.X})");
    }

    [Fact]
    public void SetEnemyType__Bomber__HasExplodeOnDeath()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        gnome.SetEnemyType(EnemyType.Bomber);

        Assert.Equal(EnemyType.Bomber, gnome.EnemyType);
        Assert.True(gnome.ExplodeOnDeath);
    }

    [Fact]
    public void SetEnemyType__Bomber__NoExplodeOnDeath_ForOtherTypes()
    {
        foreach (var type in new[] { EnemyType.Standard, EnemyType.Rusher, EnemyType.Brute })
        {
            var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
            gnome.SetEnemyType(type);
            Assert.False(gnome.ExplodeOnDeath, $"{type} should not explode on death");
        }
    }

    // -- JustHitPlayer flag tests --

    [Fact]
    public void JustHitPlayer__IsFalse__InitiallyAndAfterNonHitUpdate()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var flow = CreateOpenFlowField(new Vector2(300, 160));
        var noWalls = new DelegateCollisionData(_ => false);

        gnome.Update(FakeGameTime.OneFrame(), new Vector2(300, 160), flow, noWalls, Vector2.Zero, FarPlayerBounds);

        Assert.False(gnome.JustHitPlayer);
    }

    [Fact]
    public void JustHitPlayer__IsTrueExactlyOneFrame__AfterLungeHitsPlayer()
    {
        // Set up: gnome close to player, get it into Lunging state.
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        // Player sits just to the right so the lunge can intersect it.
        var playerBounds = new Rectangle(176, 155, 16, 16);

        // Enter WindingUp.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
        // Complete wind-up → Lunging.
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, playerBounds);
        Assert.Equal(GnomeState.Lunging, gnome.State);

        // Advance until lunge hits player.
        bool hitSeen = false;
        for (var i = 0; i < 60; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
            if (gnome.JustHitPlayer)
            {
                hitSeen = true;
                break;
            }
        }

        Assert.True(hitSeen, "JustHitPlayer should be true on the frame the lunge connects");
    }

    [Fact]
    public void JustHitPlayer__ClearsToFalse__OnNextUpdate()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        var noWalls = new DelegateCollisionData(_ => false);
        var playerBounds = new Rectangle(176, 155, 16, 16);

        // WindingUp → Lunging.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, noWalls, Vector2.Zero, playerBounds);

        // Drive lunge until hit is detected.
        for (var i = 0; i < 60; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);
            if (gnome.JustHitPlayer)
                break;
        }

        Assert.True(gnome.JustHitPlayer, "Precondition: should have just hit player");

        // One more update — flag must clear.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, noWalls, Vector2.Zero, playerBounds);

        Assert.False(gnome.JustHitPlayer, "JustHitPlayer should clear after one frame");
    }

    [Fact]
    public void JustHitPlayer__IsFalse__WhenLungeHitsWallInsteadOfPlayer()
    {
        var gnome = new GnomeEnemy(new Vector2(160, 160), 0f);
        var target = new Vector2(168, 160);
        var flow = CreateOpenFlowField(target);
        // Wall on the right side.
        var wallOnRight = new DelegateCollisionData(r => r.X > 170);
        var playerBounds = FarPlayerBounds;

        // Enter WindingUp then Lunging.
        gnome.Update(FakeGameTime.OneFrame(), target, flow, wallOnRight, Vector2.Zero, playerBounds);
        gnome.Update(FakeGameTime.FromSeconds(0.5f), target, flow, wallOnRight, Vector2.Zero, playerBounds);

        // Let the lunge run into the wall — gnome enters Stunned from wall hit.
        for (var i = 0; i < 30; i++)
        {
            gnome.Update(FakeGameTime.OneFrame(), target, flow, wallOnRight, Vector2.Zero, playerBounds);
            if (gnome.State == GnomeState.Stunned)
                break;
        }

        Assert.False(gnome.JustHitPlayer, "JustHitPlayer should remain false when lunge hits a wall");
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
