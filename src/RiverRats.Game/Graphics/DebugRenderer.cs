using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Graphics;

/// <summary>
/// Utility for drawing debug overlays (collision bounds, tile grid).
/// Requires an active SpriteBatch session to be already started.
/// </summary>
internal sealed class DebugRenderer
{
    private static readonly Color TileGridColor = new(255, 255, 255, 40);

    private readonly Texture2D _pixelTexture;

    /// <summary>
    /// Creates a debug renderer with the given pixel texture.
    /// </summary>
    /// <param name="pixelTexture">A 1x1 white pixel texture for drawing primitives.</param>
    public DebugRenderer(Texture2D pixelTexture)
    {
        _pixelTexture = pixelTexture;
    }

    /// <summary>
    /// Draws a tile grid overlay over the map.
    /// Must be called inside an active SpriteBatch session.
    /// </summary>
    public void DrawTileGrid(SpriteBatch spriteBatch, int mapWidth, int mapHeight, int tileWidth, int tileHeight)
    {
        for (var x = 0; x <= mapWidth; x += tileWidth)
        {
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(x, 0, 1, mapHeight),
                TileGridColor);
        }

        for (var y = 0; y <= mapHeight; y += tileHeight)
        {
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, y, mapWidth, 1),
                TileGridColor);
        }
    }

    /// <summary>
    /// Draws a rectangle outline.
    /// Must be called inside an active SpriteBatch session.
    /// </summary>
    public void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness = 1)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
    }
}
