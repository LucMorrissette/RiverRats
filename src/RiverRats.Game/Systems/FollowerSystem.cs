using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.World;

namespace RiverRats.Game.Systems;

/// <summary>
/// Computes the leader-target position and the optional rest position for the follower
/// character, including gnome-cluster attraction steering used in the forest combat map.
/// Extracted from <c>GameplayScreen</c>.
/// </summary>
internal sealed class FollowerSystem
{
    // ── Cluster attraction constants ────────────────────────────────────────
    private const int AttractionMinClusterSize = 3;
    private const float AttractionClusterRadius = 48f;
    private const float AttractionClusterRadiusSq = AttractionClusterRadius * AttractionClusterRadius;
    private const float AttractionSearchRadius = 200f;
    private const float AttractionSearchRadiusSq = AttractionSearchRadius * AttractionSearchRadius;
    private const float AttractionStrength = 0.35f;
    private const float AttractionMaxOffset = 28f;

    private readonly FollowerMovementConfig _config;
    private readonly int _playerFramePixels;
    private readonly bool _useClusterAttraction;

    /// <summary>
    /// Creates the follower system.
    /// </summary>
    /// <param name="config">Follower movement configuration for the current map.</param>
    /// <param name="playerFramePixels">Player sprite size in pixels (used for rest position bounds).</param>
    /// <param name="useClusterAttraction">
    /// When true, the leader target is biased toward gnome clusters (forest combat map only).
    /// </param>
    public FollowerSystem(
        FollowerMovementConfig config,
        int playerFramePixels,
        bool useClusterAttraction)
    {
        _config = config;
        _playerFramePixels = playerFramePixels;
        _useClusterAttraction = useClusterAttraction;
    }

    /// <summary>
    /// Computes the world-space position the follower should steer toward as the leader target.
    /// In combat maps this is biased toward the nearest gnome cluster; otherwise it is the
    /// player's position.
    /// </summary>
    public Vector2 GetLeaderTargetPosition(
        PlayerBlock player,
        FollowerBlock follower,
        GnomeSpawner gnomeSpawner)
    {
        if (!_useClusterAttraction || gnomeSpawner == null)
            return player.Position;

        var gnomes = gnomeSpawner.Gnomes;
        if (gnomes.Count < AttractionMinClusterSize)
            return player.Position;

        var followerCenter = follower.Center;
        var bestScore = 0f;
        var bestClusterCenter = Vector2.Zero;
        var foundCluster = false;

        for (var i = 0; i < gnomes.Count; i++)
        {
            var origin = gnomes[i].Position + new Vector2(8f, 8f);
            var followerDistanceSq = Vector2.DistanceSquared(followerCenter, origin);
            if (followerDistanceSq > AttractionSearchRadiusSq)
                continue;

            var clusterSum = origin;
            var clusterSize = 1;
            for (var j = 0; j < gnomes.Count; j++)
            {
                if (i == j) continue;
                var other = gnomes[j].Position + new Vector2(8f, 8f);
                if (Vector2.DistanceSquared(origin, other) <= AttractionClusterRadiusSq)
                {
                    clusterSum += other;
                    clusterSize++;
                }
            }

            if (clusterSize < AttractionMinClusterSize)
                continue;

            var proximity = 1f - MathHelper.Clamp(followerDistanceSq / AttractionSearchRadiusSq, 0f, 1f);
            var score = clusterSize * proximity;
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestClusterCenter = clusterSum / clusterSize;
            foundCluster = true;
        }

        if (!foundCluster)
            return player.Position;

        var toCluster = bestClusterCenter - player.Center;
        var distance = toCluster.Length();
        if (distance <= 0.001f)
            return player.Position;

        var offsetDistance = Math.Min(distance * AttractionStrength, AttractionMaxOffset);
        var offset = toCluster / distance * offsetDistance;
        return player.Position + offset;
    }

    /// <summary>
    /// Computes the preferred rest position for the follower when the player is stationary,
    /// or returns <c>null</c> if neither side candidate is open.
    /// </summary>
    public Vector2? GetRestPosition(
        PlayerBlock player,
        FollowerBlock follower,
        WorldCollisionMap collisionMap,
        int mapPixelWidth,
        int mapPixelHeight)
    {
        if (player.IsMoving || follower is null || collisionMap is null)
            return null;

        var (first, second) = GetRestOffsets(player.Facing);
        var firstPos = player.Position + first;
        var secondPos = player.Position + second;
        var firstOpen = IsPositionOpen(firstPos, player, collisionMap, mapPixelWidth, mapPixelHeight);
        var secondOpen = IsPositionOpen(secondPos, player, collisionMap, mapPixelWidth, mapPixelHeight);

        if (!firstOpen && !secondOpen)
            return null;

        if (firstOpen && secondOpen)
        {
            var d1 = Vector2.DistanceSquared(follower.Position, firstPos);
            var d2 = Vector2.DistanceSquared(follower.Position, secondPos);
            return d1 <= d2 ? firstPos : secondPos;
        }

        return firstOpen ? firstPos : secondPos;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private (Vector2 First, Vector2 Second) GetRestOffsets(FacingDirection facing)
    {
        var side = _config.SideRestOffsetPixels;
        return facing switch
        {
            FacingDirection.Left or FacingDirection.Right =>
                (new Vector2(0f, -side), new Vector2(0f, side)),
            _ =>
                (new Vector2(-side, 0f), new Vector2(side, 0f))
        };
    }

    private bool IsPositionOpen(
        Vector2 candidatePos,
        PlayerBlock player,
        WorldCollisionMap collisionMap,
        int mapPixelWidth,
        int mapPixelHeight)
    {
        var candidateBounds = new Rectangle(
            (int)MathF.Round(candidatePos.X),
            (int)MathF.Round(candidatePos.Y),
            _playerFramePixels,
            _playerFramePixels);

        var worldBounds = new Rectangle(0, 0, mapPixelWidth, mapPixelHeight);

        if (!worldBounds.Contains(candidateBounds))
            return false;

        if (candidateBounds.Intersects(player.Bounds))
            return false;

        return !collisionMap.IsWorldRectangleBlocked(candidateBounds);
    }
}
