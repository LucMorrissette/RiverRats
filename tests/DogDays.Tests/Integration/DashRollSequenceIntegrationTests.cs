using Microsoft.Xna.Framework;
using DogDays.Game.Entities;
using DogDays.Game.Systems;
using DogDays.Game.World;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Integration;

/// <summary>
/// Integration tests for multi-frame dash-roll movement against world collision.
/// </summary>
public sealed class DashRollSequenceIntegrationTests
{
    [Fact]
    public void Update__AcrossMultipleFrames__StopsBeforeWallAndEndsDash()
    {
        var wall = new Rectangle(170, 0, 32, 256);
        var collision = new DelegateCollisionData(bounds => bounds.Intersects(wall));
        var worldBounds = new Rectangle(0, 0, 512, 256);
        var player = new PlayerBlock(new Vector2(100f, 80f), new Point(32, 32), 96f, worldBounds);
        var sequence = new DashRollSequence();

        Assert.True(sequence.TryBegin(new Vector2(1f, 0f), player, health: null));

        for (var i = 0; i < 30; i++)
        {
            sequence.Update(FakeGameTime.OneFrame(), player, collision, health: null);
        }

        Assert.False(sequence.IsActive);
        Assert.True(player.FootBounds.Right <= wall.Left);
        Assert.False(player.FootBounds.Intersects(wall));
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