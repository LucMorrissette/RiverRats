using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Entities;

/// <summary>
/// Purely visual underwater prop that simulates shore depth using a 1×3 tile sprite.
/// </summary>
public sealed class FlatShoreDepthSimulator
{
    private readonly Texture2D _texture;
    private readonly Vector2 _position;

    /// <summary>
    /// Creates a flat shore depth simulator at a world position.
    /// </summary>
    /// <param name="position">Top-left world position in pixels.</param>
    /// <param name="texture">Texture used for drawing.</param>
    public FlatShoreDepthSimulator(Vector2 position, Texture2D texture)
    {
        _position = position;
        _texture = texture;
    }

    /// <summary>Top-left world position in pixels.</summary>
    public Vector2 Position => _position;

    /// <summary>
    /// Draws the shore depth simulator in world space.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for the current render pass.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, _position, Color.White);
    }
}
