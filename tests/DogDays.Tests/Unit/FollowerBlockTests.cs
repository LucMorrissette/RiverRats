using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for breadcrumb trail follower positioning.
/// </summary>
public sealed class FollowerBlockTests
{
    private static readonly Rectangle WorldBounds = new(0, 0, 512, 512);
    private static readonly Point FollowerSize = new(32, 32);
    private static readonly FollowerMovementConfig TestConfig = new()
    {
        FollowDistancePixels = 32f,
        IdleFollowDistancePixels = 32f,
        TrailSampleDistancePixels = 8f,
        FacingDeadZonePixels = 0f,
        DistanceEasePerSecond = 100f,
        PositionEasePerSecond = 100f,
        RestPositionEasePerSecond = 100f,
        PositionSnapDistancePixels = 0.01f,
        AnimationMoveThresholdPixels = 0.01f
    };

    private static readonly FollowerMovementConfig EasedConfig = new()
    {
        FollowDistancePixels = 100f,
        IdleFollowDistancePixels = 80f,
        TrailSampleDistancePixels = 8f,
        FacingDeadZonePixels = 0f,
        DistanceEasePerSecond = 4f,
        PositionEasePerSecond = 4f,
        RestPositionEasePerSecond = 4f
    };

    private static readonly FollowerMovementConfig SettlingConfig = new()
    {
        FollowDistancePixels = 40f,
        IdleFollowDistancePixels = 20f,
        TrailSampleDistancePixels = 8f,
        FacingDeadZonePixels = 0f,
        DistanceEasePerSecond = 6f,
        PositionEasePerSecond = 10f,
        RestPositionEasePerSecond = 3f,
        PositionSnapDistancePixels = 0.5f,
        AnimationMoveThresholdPixels = 0.35f,
        SideRestOffsetPixels = 32f,
        RestExitDistancePixels = 20f
    };

    [Fact]
    public void Update__LeaderStationaryAtSpawn__KeepsFollowerAtInitialTrailingPosition()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = CreateFollower(new Vector2(100f, 132f));

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Down);

        Assert.Equal(new Vector2(100f, 132f), follower.Position);
        Assert.False(follower.IsMoving);
        Assert.Equal(FacingDirection.Down, follower.Facing);
    }

    [Fact]
    public void Update__LeaderMovesRightAcrossSeveralFrames__FollowerTrailsAlongRecordedPath()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = CreateFollower(new Vector2(100f, 132f));
        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Down);

        for (var step = 1; step <= 8; step++)
        {
            leaderPosition = new Vector2(100f + step * 8f, 100f);
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        }

        Assert.InRange(follower.Position.X, 129f, 133f);
        Assert.InRange(follower.Position.Y, 99f, 101f);
        Assert.True(follower.IsMoving);
        Assert.Equal(FacingDirection.Right, follower.Facing);
    }

    [Fact]
    public void Update__LeaderTurnsCorner__FollowerPassesThroughCornerWithoutCutting()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = CreateFollower(new Vector2(100f, 132f));
        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Down);

        for (var step = 1; step <= 4; step++)
        {
            leaderPosition = new Vector2(100f + step * 8f, 100f);
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        }

        for (var step = 1; step <= 4; step++)
        {
            leaderPosition = new Vector2(132f, 100f + step * 8f);
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Down);
        }

        Assert.InRange(follower.Position.X, 129f, 133f);
        Assert.InRange(follower.Position.Y, 99f, 101f);
        Assert.True(follower.IsMoving);
        Assert.Equal(FacingDirection.Right, follower.Facing);
    }

    [Fact]
    public void Update__LeaderMovesWithEasingConfig__FollowerDoesNotSnapDirectlyToTarget()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = new FollowerBlock(new Vector2(100f, 200f), FollowerSize, WorldBounds, EasedConfig);

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Down);
        leaderPosition = new Vector2(220f, 100f);

        follower.Update(FakeGameTime.FromSeconds(0.1f), leaderPosition, FacingDirection.Right);

        Assert.True(follower.Position.X > 100f);
        Assert.True(follower.Position.X < 120f);
    }

    [Fact]
    public void Update__LeaderStops__FollowerSpacingEasesTighterOverTime()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = new FollowerBlock(new Vector2(0f, 100f), FollowerSize, WorldBounds, EasedConfig);

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        leaderPosition = new Vector2(260f, 100f);
        follower.Update(FakeGameTime.FromSeconds(1f), leaderPosition, FacingDirection.Right);

        var distanceWhileMoving = leaderPosition.X - follower.Position.X;

        follower.Update(FakeGameTime.FromSeconds(1f), leaderPosition, FacingDirection.Right);

        var distanceWhileIdle = leaderPosition.X - follower.Position.X;

        Assert.True(distanceWhileIdle < distanceWhileMoving);
        Assert.True(distanceWhileIdle > EasedConfig.IdleFollowDistancePixels - 10f);
    }

    [Fact]
    public void Update__FollowerSettlesNearTarget__StopsReportingMovementForAnimation()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var follower = new FollowerBlock(new Vector2(60f, 100f), FollowerSize, WorldBounds, SettlingConfig);

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        leaderPosition = new Vector2(180f, 100f);
        follower.Update(FakeGameTime.FromSeconds(0.5f), leaderPosition, FacingDirection.Right);

        for (var step = 0; step < 30; step++)
        {
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        }

        Assert.False(follower.IsMoving);
    }

    [Fact]
    public void Update__IdleRestPositionProvided__FollowerSettlesBesideLeader()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var restPosition = new Vector2(132f, 100f);
        var follower = new FollowerBlock(new Vector2(60f, 100f), FollowerSize, WorldBounds, SettlingConfig);

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right, restPosition);

        for (var step = 0; step < 120; step++)
        {
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right, restPosition);
        }

        Assert.InRange(follower.Position.X, 131.5f, 132.5f);
        Assert.InRange(follower.Position.Y, 99.5f, 100.5f);
    }

    [Fact]
    public void Update__IdleRestPositionProvided__FollowerDoesNotSnapImmediatelyBesideLeader()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var restPosition = new Vector2(132f, 100f);
        var follower = new FollowerBlock(new Vector2(60f, 100f), FollowerSize, WorldBounds, SettlingConfig);

        follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right, restPosition);

        Assert.True(follower.Position.X < 66f);
    }

    [Fact]
    public void Update__LeaderMakesSmallMovementAfterRest__FollowerKeepsRestTarget()
    {
        var leaderPosition = new Vector2(100f, 100f);
        var restPosition = new Vector2(132f, 100f);
        var follower = new FollowerBlock(new Vector2(60f, 100f), FollowerSize, WorldBounds, SettlingConfig);

        for (var step = 0; step < 45; step++)
        {
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right, restPosition);
        }

        leaderPosition = new Vector2(112f, 100f);

        for (var step = 0; step < 10; step++)
        {
            follower.Update(FakeGameTime.OneFrame(), leaderPosition, FacingDirection.Right);
        }

        Assert.InRange(follower.Position.X, 125f, 133f);
        Assert.InRange(follower.Position.Y, 99f, 101f);
    }

    private static FollowerBlock CreateFollower(Vector2 startPosition)
    {
        return new FollowerBlock(startPosition, FollowerSize, WorldBounds, TestConfig);
    }
}