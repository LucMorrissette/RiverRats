using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Systems;

/// <summary>
/// A single explosion frame in the pool.
/// </summary>
public struct Explosion
{
    public Vector2 Position;
    /// <summary>Current animation frame index (0 to FrameCount-1).</summary>
    public int Frame;
    /// <summary>Elapsed time within the current frame.</summary>
    public float Elapsed;
    public bool IsActive;
}

/// <summary>
/// Manages a fixed-size pool of sprite-sheet explosion animations (gnome death bursts).
/// Replaces the 4 parallel arrays that previously lived in <c>GameplayScreen</c>.
/// </summary>
public sealed class ExplosionSystem
{
    private const int FrameCount = 4;
    private const int CellSize = 32;
    private const float FrameDuration = 0.05f;

    private readonly Explosion[] _explosions;
    private readonly int _maxExplosions;

    /// <summary>Creates the system with a fixed pool capacity.</summary>
    public ExplosionSystem(int maxExplosions)
    {
        _maxExplosions = maxExplosions;
        _explosions = new Explosion[maxExplosions];
    }

    /// <summary>Activates an explosion at <paramref name="centre"/>. Silently drops if pool is full.</summary>
    public void Spawn(Vector2 centre)
    {
        for (var i = 0; i < _maxExplosions; i++)
        {
            if (!_explosions[i].IsActive)
            {
                _explosions[i].IsActive = true;
                _explosions[i].Position = centre;
                _explosions[i].Frame = 0;
                _explosions[i].Elapsed = 0f;
                return;
            }
        }
    }

    /// <summary>Advances all active explosion animations.</summary>
    public void Update(float dt)
    {
        for (var i = 0; i < _maxExplosions; i++)
        {
            if (!_explosions[i].IsActive)
                continue;

            _explosions[i].Elapsed += dt;
            if (_explosions[i].Elapsed >= FrameDuration)
            {
                _explosions[i].Elapsed -= FrameDuration;
                _explosions[i].Frame++;
                if (_explosions[i].Frame >= FrameCount)
                    _explosions[i].IsActive = false;
            }
        }
    }

    /// <summary>
    /// Draws all active explosions. Assumes the sprite batch is already begun.
    /// Explosions always sort in front of the player (drawn in the InFrontOfPlayer pass).
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D explosionTexture, float mapHeight, float mapWidth)
    {
        if (explosionTexture == null)
            return;

        var half = CellSize * 0.5f;
        var origin = new Vector2(half, half);

        for (var i = 0; i < _maxExplosions; i++)
        {
            if (!_explosions[i].IsActive)
                continue;

            var sourceRect = new Rectangle(
                _explosions[i].Frame * CellSize, 0,
                CellSize, CellSize);

            var pos = _explosions[i].Position;
            var sortBounds = new Rectangle((int)(pos.X - half), (int)(pos.Y - half), CellSize, CellSize);
            var depth = SortDepth(sortBounds, mapHeight, mapWidth);

            spriteBatch.Draw(
                explosionTexture,
                pos,
                sourceRect,
                Color.White,
                0f,
                origin,
                1f,
                SpriteEffects.None,
                depth);
        }
    }

    private static float SortDepth(Rectangle bounds, float mapHeight, float mapWidth, float anchorOffset = 0f)
    {
        var yDepth = (bounds.Bottom - anchorOffset) / mapHeight;
        var tieBreakerRange = 1f / mapHeight;
        var yScaled = yDepth * (1f - tieBreakerRange);
        var xTie = bounds.Left / (mapWidth * mapHeight);
        return MathHelper.Clamp(yScaled + xTie, 0f, 0.9999f);
    }
}
