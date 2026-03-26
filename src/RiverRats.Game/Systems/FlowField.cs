using System;
using Microsoft.Xna.Framework;
using RiverRats.Game.World;

namespace RiverRats.Game.Systems;

/// <summary>
/// BFS-based flow field that computes a direction vector per tile pointing toward a target.
/// All enemies share a single field. Recomputed periodically to amortize cost.
/// </summary>
internal sealed class FlowField
{
    private const int TileSize = 32;
    private const int RecomputeIntervalFrames = 4;

    private static readonly float InvSqrt2 = 0.70710678f;

    /// <summary>Offsets to reach each of the 8 neighbors (N, S, W, E, NW, NE, SW, SE).</summary>
    private static readonly int[] Dx = { 0, 0, -1, 1, -1, 1, -1, 1 };
    private static readonly int[] Dy = { -1, 1, 0, 0, -1, -1, 1, 1 };

    /// <summary>
    /// Direction from a neighbor cell back toward the BFS parent that discovered it.
    /// This is the reverse of the offset, pre-normalized for diagonals.
    /// Index matches <see cref="Dx"/>/<see cref="Dy"/>.
    /// </summary>
    private static readonly Vector2[] ReverseDir =
    {
        new(0, 1),                       // N neighbor → go S
        new(0, -1),                      // S neighbor → go N
        new(1, 0),                       // W neighbor → go E
        new(-1, 0),                      // E neighbor → go W
        new(InvSqrt2, InvSqrt2),         // NW neighbor → go SE
        new(-InvSqrt2, InvSqrt2),        // NE neighbor → go SW
        new(InvSqrt2, -InvSqrt2),        // SW neighbor → go NE
        new(-InvSqrt2, -InvSqrt2),       // SE neighbor → go NW
    };

    /// <summary>
    /// Cardinal index pairs that must both be walkable for a diagonal move to be valid.
    /// Prevents corner-cutting through diagonal walls.
    /// Indices 0–3 are cardinal (N, S, W, E); diagonals start at index 4.
    /// Diagonal index 4 (NW) requires N(0) and W(2), etc.
    /// </summary>
    private static readonly int[] DiagCardinal1 = { 0, 0, 1, 1 }; // for diag indices 4,5,6,7
    private static readonly int[] DiagCardinal2 = { 2, 3, 2, 3 }; // for diag indices 4,5,6,7

    private readonly int _gridWidth;
    private readonly int _gridHeight;
    private readonly bool[] _walkable;
    private readonly bool[] _walkableUnpadded;
    private readonly Vector2[] _directions;
    private readonly int[] _distance;
    private int _frameCounter;

    // Pre-allocated circular BFS queue.
    private readonly int[] _queueBuffer;

    // Scratch buffer for tiles temporarily forced walkable during BFS (max 9: target + 8 neighbors).
    private readonly int[] _tempForcedIndices = new int[9];
    private int _tempForcedCount;

    /// <summary>
    /// Creates a flow field for the given map dimensions.
    /// Builds the walkability grid once by querying the collision map.
    /// </summary>
    /// <param name="mapPixelWidth">Total map width in pixels.</param>
    /// <param name="mapPixelHeight">Total map height in pixels.</param>
    /// <param name="collisionMap">Collision data used to determine tile walkability.</param>
    /// <param name="agentRadius">
    /// Half-size of the agents that will use this field (pixels). Each tile's walkability
    /// check is inflated by this amount so the flow field routes agents with physical clearance
    /// around obstacles (Minkowski sum padding).
    /// </param>
    public FlowField(int mapPixelWidth, int mapPixelHeight, IMapCollisionData collisionMap, int agentRadius = 0)
    {
        _gridWidth = mapPixelWidth / TileSize;
        _gridHeight = mapPixelHeight / TileSize;
        var totalCells = _gridWidth * _gridHeight;
        _walkable = new bool[totalCells];
        _walkableUnpadded = new bool[totalCells];
        _directions = new Vector2[totalCells];
        _distance = new int[totalCells];
        _queueBuffer = new int[totalCells];

        // Build both unpadded (true walkability) and padded (agent-clearance) grids.
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                var idx = y * _gridWidth + x;
                var baseBounds = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                _walkableUnpadded[idx] = !collisionMap.IsWorldRectangleBlocked(baseBounds);

                if (agentRadius > 0)
                {
                    var paddedBounds = new Rectangle(
                        x * TileSize - agentRadius,
                        y * TileSize - agentRadius,
                        TileSize + agentRadius * 2,
                        TileSize + agentRadius * 2);
                    _walkable[idx] = !collisionMap.IsWorldRectangleBlocked(paddedBounds);
                }
                else
                {
                    _walkable[idx] = _walkableUnpadded[idx];
                }
            }
        }

        // Trigger BFS on the very first Update call.
        _frameCounter = RecomputeIntervalFrames;
    }

    /// <summary>
    /// Returns the flow direction at the given world position, or <see cref="Vector2.Zero"/> if
    /// the position is out of bounds or unreachable.
    /// </summary>
    public Vector2 GetDirection(Vector2 worldPosition)
    {
        var tileX = (int)(worldPosition.X / TileSize);
        var tileY = (int)(worldPosition.Y / TileSize);
        if (tileX < 0 || tileX >= _gridWidth || tileY < 0 || tileY >= _gridHeight)
            return Vector2.Zero;
        return _directions[tileY * _gridWidth + tileX];
    }

    /// <summary>
    /// Updates the flow field if enough frames have elapsed since the last recomputation.
    /// </summary>
    /// <param name="targetWorldPosition">World position of the target (e.g. player centre).</param>
    public void Update(Vector2 targetWorldPosition)
    {
        _frameCounter++;
        if (_frameCounter >= RecomputeIntervalFrames)
        {
            _frameCounter = 0;
            Recompute(targetWorldPosition);
        }
    }

    private void Recompute(Vector2 targetWorldPosition)
    {
        var totalCells = _gridWidth * _gridHeight;

        // Convert target to tile coords, clamped to grid.
        var targetTileX = Math.Clamp((int)(targetWorldPosition.X / TileSize), 0, _gridWidth - 1);
        var targetTileY = Math.Clamp((int)(targetWorldPosition.Y / TileSize), 0, _gridHeight - 1);

        // Reset arrays.
        Array.Fill(_distance, int.MaxValue);
        Array.Clear(_directions, 0, totalCells);

        var targetIndex = targetTileY * _gridWidth + targetTileX;

        // Create a "connection zone" around the target: temporarily force walkable any
        // padded-blocked tile that is walkable without padding. The player uses fine-grained
        // collision and can stand in tiles the padded grid rejects. Without this, BFS
        // seeds the target but can't expand when all neighbors are padded-blocked.
        _tempForcedCount = 0;
        ForceWalkableIfUnpadded(targetIndex);
        for (var i = 0; i < 8; i++)
        {
            var nx = targetTileX + Dx[i];
            var ny = targetTileY + Dy[i];
            if (nx >= 0 && nx < _gridWidth && ny >= 0 && ny < _gridHeight)
                ForceWalkableIfUnpadded(ny * _gridWidth + nx);
        }

        _distance[targetIndex] = 0;

        // Circular queue.
        var head = 0;
        var tail = 0;
        _queueBuffer[tail++] = targetIndex;

        while (head != tail)
        {
            var currentIndex = _queueBuffer[head++];
            if (head >= totalCells) head = 0;

            var cx = currentIndex % _gridWidth;
            var cy = currentIndex / _gridWidth;
            var nextDist = _distance[currentIndex] + 1;

            // Check all 8 neighbors.
            for (var i = 0; i < 8; i++)
            {
                var nx = cx + Dx[i];
                var ny = cy + Dy[i];

                if (nx < 0 || nx >= _gridWidth || ny < 0 || ny >= _gridHeight)
                    continue;

                var neighborIndex = ny * _gridWidth + nx;

                if (!_walkable[neighborIndex] || _distance[neighborIndex] <= nextDist)
                    continue;

                // Diagonal corner-cutting check: both adjacent cardinals must be walkable.
                if (i >= 4)
                {
                    var diagIdx = i - 4;
                    var card1X = cx + Dx[DiagCardinal1[diagIdx]];
                    var card1Y = cy + Dy[DiagCardinal1[diagIdx]];
                    var card2X = cx + Dx[DiagCardinal2[diagIdx]];
                    var card2Y = cy + Dy[DiagCardinal2[diagIdx]];

                    if (!_walkable[card1Y * _gridWidth + card1X] ||
                        !_walkable[card2Y * _gridWidth + card2X])
                        continue;
                }

                _distance[neighborIndex] = nextDist;
                _directions[neighborIndex] = ReverseDir[i];
                _queueBuffer[tail++] = neighborIndex;
                if (tail >= totalCells) tail = 0;
            }
        }

        // Restore original padded walkability for all temporarily forced tiles.
        for (var i = 0; i < _tempForcedCount; i++)
            _walkable[_tempForcedIndices[i]] = false;
    }

    /// <summary>
    /// If the tile at <paramref name="index"/> is blocked by padding but walkable without it,
    /// temporarily force it walkable and record it for later restoration.
    /// </summary>
    private void ForceWalkableIfUnpadded(int index)
    {
        if (!_walkable[index] && _walkableUnpadded[index])
        {
            _walkable[index] = true;
            _tempForcedIndices[_tempForcedCount++] = index;
        }
    }
}
