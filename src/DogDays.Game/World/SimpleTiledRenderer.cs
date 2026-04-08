using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Util;

namespace DogDays.Game.World;

/// <summary>
/// Lightweight TMX map renderer for scenes that do not require the overworld's
/// terrain-variant system (e.g., the side-view fishing mini-game).
/// Supports standard single-image tilesets and collection-of-images tilesets.
/// Renders all tile layers in order, and exposes named object layer rectangles.
/// </summary>
public sealed class SimpleTiledRenderer : IDisposable
{
    private const uint FlipHorizontalFlag = 0x80000000;
    private const uint FlipVerticalFlag = 0x40000000;
    private const uint FlipDiagonalFlag = 0x20000000;
    private const uint FlipMask = FlipHorizontalFlag | FlipVerticalFlag | FlipDiagonalFlag;

    private readonly int _mapWidthTiles;
    private readonly int _mapHeightTiles;
    private readonly int _mapTileWidth;
    private readonly int _mapTileHeight;
    private readonly TilesetInfo[] _tilesets;
    private readonly TileLayer[] _layers;
    private readonly Dictionary<string, Rectangle[]> _objectLayers;
    private readonly Dictionary<string, PolygonBounds[]> _polygonLayers;
    private readonly SpriteBatch _spriteBatch;

    /// <summary>Total map width in pixels.</summary>
    public int MapPixelWidth => _mapWidthTiles * _mapTileWidth;

    /// <summary>Total map height in pixels.</summary>
    public int MapPixelHeight => _mapHeightTiles * _mapTileHeight;

    /// <summary>Width of a single map tile in pixels.</summary>
    public int TileWidth => _mapTileWidth;

    /// <summary>Height of a single map tile in pixels.</summary>
    public int TileHeight => _mapTileHeight;

    /// <summary>
    /// Loads and parses a TMX map for simple tile rendering.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device for creating the internal sprite batch.</param>
    /// <param name="content">Content manager for loading tileset textures.</param>
    /// <param name="assetName">Map asset name (relative to content root, with or without .tmx extension).</param>
    public SimpleTiledRenderer(GraphicsDevice graphicsDevice, ContentManager content, string assetName)
    {
        var mapPath = GetMapPath(content, assetName);
        var mapDirectory = Path.GetDirectoryName(mapPath)
            ?? throw new InvalidOperationException("Map directory is unavailable.");

        var mapDoc = XDocument.Load(mapPath);
        var mapEl = mapDoc.Element("map")
            ?? throw new InvalidOperationException("TMX map root element was not found.");

        _mapWidthTiles = GetIntAttribute(mapEl, "width");
        _mapHeightTiles = GetIntAttribute(mapEl, "height");
        _mapTileWidth = GetIntAttribute(mapEl, "tilewidth");
        _mapTileHeight = GetIntAttribute(mapEl, "tileheight");

        // Load tilesets.
        var tilesets = new List<TilesetInfo>();
        foreach (var tsRefEl in mapEl.Elements("tileset"))
        {
            var firstGid = GetIntAttribute(tsRefEl, "firstgid");
            var source = tsRefEl.Attribute("source")?.Value;

            if (!string.IsNullOrWhiteSpace(source))
            {
                // External TSX file.
                var tsxPath = Path.GetFullPath(Path.Combine(mapDirectory, source));
                tilesets.Add(LoadTileset(tsxPath, firstGid, content));
            }
            else
            {
                // Embedded tileset (inline in TMX).
                tilesets.Add(LoadTilesetFromElement(tsRefEl, firstGid, mapDirectory, content));
            }
        }

        // Sort by firstGid descending for lookup.
        tilesets.Sort((a, b) => b.FirstGid.CompareTo(a.FirstGid));
        _tilesets = tilesets.ToArray();

        // Parse tile layers.
        var layers = new List<TileLayer>();
        foreach (var layerEl in mapEl.Elements("layer"))
        {
            var name = layerEl.Attribute("name")?.Value ?? "Untitled";
            var dataEl = layerEl.Element("data")
                ?? throw new InvalidOperationException($"TMX layer '{name}' has no data element.");
            var tileData = ParseCsvTileData(dataEl.Value, _mapWidthTiles * _mapHeightTiles);
            layers.Add(new TileLayer(name, tileData));
        }

        _layers = layers.ToArray();

        // Parse object layers (rectangles and polygons).
        _objectLayers = new Dictionary<string, Rectangle[]>(StringComparer.OrdinalIgnoreCase);
        _polygonLayers = new Dictionary<string, PolygonBounds[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var objGroupEl in mapEl.Elements("objectgroup"))
        {
            var groupName = objGroupEl.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(groupName))
            {
                continue;
            }

            var rects = new List<Rectangle>();
            var polygons = new List<PolygonBounds>();
            foreach (var objEl in objGroupEl.Elements("object"))
            {
                // Skip tile objects.
                if (objEl.Attribute("gid") is not null)
                {
                    continue;
                }

                var x = GetFloatAttribute(objEl, "x");
                var y = GetFloatAttribute(objEl, "y");

                // Check for a <polygon> child element.
                var polyEl = objEl.Element("polygon");
                if (polyEl is not null)
                {
                    var points = ParsePolygonPoints(polyEl, x, y);
                    if (points.Length >= 3)
                    {
                        polygons.Add(new PolygonBounds(points));
                    }

                    continue;
                }

                // Plain rectangle.
                var w = GetFloatAttributeOrDefault(objEl, "width", 0f);
                var h = GetFloatAttributeOrDefault(objEl, "height", 0f);

                if (w > 0f && h > 0f)
                {
                    rects.Add(new Rectangle(
                        (int)MathF.Round(x),
                        (int)MathF.Round(y),
                        (int)MathF.Round(w),
                        (int)MathF.Round(h)));
                }
            }

            if (rects.Count > 0)
            {
                _objectLayers[groupName] = rects.ToArray();
            }

            if (polygons.Count > 0)
            {
                _polygonLayers[groupName] = polygons.ToArray();
            }
        }

        _spriteBatch = new SpriteBatch(graphicsDevice);
    }

    /// <summary>
    /// Gets the rectangles defined in a named object layer.
    /// </summary>
    /// <param name="layerName">Object layer name (case-insensitive).</param>
    /// <returns>Array of rectangles, or empty if the layer doesn't exist.</returns>
    public IReadOnlyList<Rectangle> GetObjectRectangles(string layerName)
    {
        return _objectLayers.TryGetValue(layerName, out var rects)
            ? rects
            : Array.Empty<Rectangle>();
    }

    /// <summary>
    /// Gets the polygon bounds defined in a named object layer.
    /// </summary>
    /// <param name="layerName">Object layer name (case-insensitive).</param>
    /// <returns>Array of polygon bounds, or empty if the layer doesn't exist.</returns>
    public IReadOnlyList<PolygonBounds> GetObjectPolygons(string layerName)
    {
        return _polygonLayers.TryGetValue(layerName, out var polys)
            ? polys
            : Array.Empty<PolygonBounds>();
    }

    /// <summary>
    /// Draws all tile layers using the provided transform matrix.
    /// </summary>
    /// <param name="transformMatrix">World-to-screen transform (use <see cref="Matrix.Identity"/> for 1:1).</param>
    public void Draw(Matrix transformMatrix)
    {
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: transformMatrix);

        for (var layerIndex = 0; layerIndex < _layers.Length; layerIndex++)
        {
            DrawLayer(_layers[layerIndex].TileData);
        }

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws a single named tile layer. Does nothing if the layer name is not found.
    /// </summary>
    /// <param name="layerName">Layer name (case-insensitive).</param>
    /// <param name="transformMatrix">World-to-screen transform.</param>
    public void DrawLayer(string layerName, Matrix transformMatrix)
    {
        for (var i = 0; i < _layers.Length; i++)
        {
            if (string.Equals(_layers[i].Name, layerName, StringComparison.OrdinalIgnoreCase))
            {
                _spriteBatch.Begin(
                    sortMode: SpriteSortMode.Deferred,
                    blendState: BlendState.AlphaBlend,
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: transformMatrix);

                DrawLayer(_layers[i].TileData);

                _spriteBatch.End();
                return;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _spriteBatch?.Dispose();
    }

    private void DrawLayer(uint[] tileData)
    {
        for (var y = 0; y < _mapHeightTiles; y++)
        {
            for (var x = 0; x < _mapWidthTiles; x++)
            {
                var rawGid = tileData[(y * _mapWidthTiles) + x];
                if (rawGid == 0)
                {
                    continue;
                }

                var flipH = (rawGid & FlipHorizontalFlag) != 0;
                var flipV = (rawGid & FlipVerticalFlag) != 0;
                var globalId = (int)(rawGid & ~FlipMask);

                if (!TryFindTileset(globalId, out var tileset))
                {
                    continue;
                }

                var localId = globalId - tileset.FirstGid;
                var sourceRect = tileset.GetSourceRectangle(localId);
                if (sourceRect is null)
                {
                    continue;
                }

                var effects = SpriteEffects.None;
                if (flipH) effects |= SpriteEffects.FlipHorizontally;
                if (flipV) effects |= SpriteEffects.FlipVertically;

                var dest = new Rectangle(
                    x * _mapTileWidth,
                    y * _mapTileHeight,
                    _mapTileWidth,
                    _mapTileHeight);

                _spriteBatch.Draw(
                    tileset.GetTexture(localId),
                    dest,
                    sourceRect.Value,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    effects,
                    0f);
            }
        }
    }

    private bool TryFindTileset(int globalId, out TilesetInfo tileset)
    {
        // Tilesets are sorted by firstGid descending, so the first match is correct.
        for (var i = 0; i < _tilesets.Length; i++)
        {
            if (globalId >= _tilesets[i].FirstGid)
            {
                tileset = _tilesets[i];
                return true;
            }
        }

        tileset = null;
        return false;
    }

    private static TilesetInfo LoadTileset(string tsxPath, int firstGid, ContentManager content)
    {
        var tsxDirectory = Path.GetDirectoryName(tsxPath)
            ?? throw new InvalidOperationException("Tileset directory is unavailable.");
        var tsxDoc = XDocument.Load(tsxPath);
        var tsEl = tsxDoc.Element("tileset")
            ?? throw new InvalidOperationException("TSX root element was not found.");

        return LoadTilesetFromElement(tsEl, firstGid, tsxDirectory, content);
    }

    private static TilesetInfo LoadTilesetFromElement(
        XElement tsEl, int firstGid, string baseDirectory, ContentManager content)
    {
        var tileWidth = GetIntAttribute(tsEl, "tilewidth");
        var tileHeight = GetIntAttribute(tsEl, "tileheight");
        var tileCount = tsEl.Attribute("tilecount") is { } tcAttr
            ? int.Parse(tcAttr.Value, CultureInfo.InvariantCulture) : 0;
        var columns = tsEl.Attribute("columns") is { } colAttr
            ? int.Parse(colAttr.Value, CultureInfo.InvariantCulture) : 0;

        // Check for single-image tileset.
        var imageEl = tsEl.Element("image");
        if (imageEl is not null)
        {
            var imageSource = imageEl.Attribute("source")?.Value
                ?? throw new InvalidOperationException("Tileset image element has no source.");
            var assetName = ResolveContentAssetName(baseDirectory, imageSource, content.RootDirectory);
            var texture = content.Load<Texture2D>(assetName);
            return new SingleImageTilesetInfo(firstGid, tileWidth, tileHeight, tileCount, columns, texture);
        }

        // Collection-of-images tileset.
        var tileTextures = new Dictionary<int, Texture2D>();
        foreach (var tileEl in tsEl.Elements("tile"))
        {
            var localId = GetIntAttribute(tileEl, "id");
            var tileImageEl = tileEl.Element("image");
            if (tileImageEl is null)
            {
                continue;
            }

            var tileImageSource = tileImageEl.Attribute("source")?.Value;
            if (string.IsNullOrWhiteSpace(tileImageSource))
            {
                continue;
            }

            var tileAssetName = ResolveContentAssetName(baseDirectory, tileImageSource, content.RootDirectory);
            tileTextures[localId] = LoadTexture(content, tileAssetName);
        }

        return new CollectionTilesetInfo(firstGid, tileWidth, tileHeight, tileTextures);
    }

    private static string ResolveContentAssetName(string baseDirectory, string imageSource, string contentRootName)
    {
        var fullImagePath = Path.GetFullPath(Path.Combine(baseDirectory, imageSource));
        var fullContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, contentRootName));
        var relative = Path.GetRelativePath(fullContentRoot, fullImagePath).Replace('\\', '/');

        if (relative.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            relative = relative[..^4];
        }

        return relative;
    }

    private static Texture2D LoadTexture(ContentManager content, string assetName)
    {
        try
        {
            return content.Load<Texture2D>(assetName);
        }
        catch (ContentLoadException)
        {
            return content.Load<Texture2D>($"{assetName}_0");
        }
    }

    private static string GetMapPath(ContentManager content, string assetName)
    {
        var relative = assetName.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
            ? assetName
            : $"{assetName}.tmx";
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, content.RootDirectory, relative));
    }

    private static int GetIntAttribute(XElement el, string name)
    {
        var attr = el.Attribute(name)
            ?? throw new InvalidOperationException($"Missing required '{name}' attribute on element '{el.Name}'.");
        return int.Parse(attr.Value, CultureInfo.InvariantCulture);
    }

    private static float GetFloatAttribute(XElement el, string name)
    {
        var attr = el.Attribute(name)
            ?? throw new InvalidOperationException($"Missing required '{name}' attribute on element '{el.Name}'.");
        return float.Parse(attr.Value, CultureInfo.InvariantCulture);
    }

    private static float GetFloatAttributeOrDefault(XElement el, string name, float defaultValue)
    {
        var attr = el.Attribute(name);
        return attr is not null
            ? float.Parse(attr.Value, CultureInfo.InvariantCulture)
            : defaultValue;
    }

    private static uint[] ParseCsvTileData(string csvData, int expectedCount)
    {
        var values = csvData.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length != expectedCount)
        {
            throw new InvalidOperationException(
                $"TMX tile data count mismatch. Expected {expectedCount}, found {values.Length}.");
        }

        var tiles = new uint[expectedCount];
        for (var i = 0; i < values.Length; i++)
        {
            tiles[i] = uint.Parse(values[i], CultureInfo.InvariantCulture);
        }

        return tiles;
    }

    /// <summary>
    /// Parses a Tiled polygon points string ("x1,y1 x2,y2 ...") and offsets
    /// each point by the object's position to produce absolute coordinates.
    /// </summary>
    private static Vector2[] ParsePolygonPoints(XElement polyEl, float objX, float objY)
    {
        var pointsAttr = polyEl.Attribute("points")?.Value;
        if (string.IsNullOrWhiteSpace(pointsAttr))
            return Array.Empty<Vector2>();

        var pairs = pointsAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = new Vector2[pairs.Length];

        for (var i = 0; i < pairs.Length; i++)
        {
            var parts = pairs[i].Split(',');
            var x = float.Parse(parts[0], CultureInfo.InvariantCulture);
            var y = float.Parse(parts[1], CultureInfo.InvariantCulture);
            result[i] = new Vector2(objX + x, objY + y);
        }

        return result;
    }

    // --- Tileset abstractions ---

    private abstract class TilesetInfo
    {
        public int FirstGid { get; }

        protected TilesetInfo(int firstGid)
        {
            FirstGid = firstGid;
        }

        public abstract Texture2D GetTexture(int localId);
        public abstract Rectangle? GetSourceRectangle(int localId);
    }

    private sealed class SingleImageTilesetInfo : TilesetInfo
    {
        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private readonly int _tileCount;
        private readonly int _columns;
        private readonly Texture2D _texture;

        public SingleImageTilesetInfo(
            int firstGid, int tileWidth, int tileHeight, int tileCount, int columns, Texture2D texture)
            : base(firstGid)
        {
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _tileCount = tileCount;
            _columns = columns > 0 ? columns : texture.Width / tileWidth;
            _texture = texture;
        }

        public override Texture2D GetTexture(int localId) => _texture;

        public override Rectangle? GetSourceRectangle(int localId)
        {
            if (localId < 0 || (_tileCount > 0 && localId >= _tileCount))
            {
                return null;
            }

            var col = localId % _columns;
            var row = localId / _columns;
            return new Rectangle(col * _tileWidth, row * _tileHeight, _tileWidth, _tileHeight);
        }
    }

    private sealed class CollectionTilesetInfo : TilesetInfo
    {
        private readonly int _tileWidth;
        private readonly int _tileHeight;
        private readonly Dictionary<int, Texture2D> _textures;

        public CollectionTilesetInfo(
            int firstGid, int tileWidth, int tileHeight, Dictionary<int, Texture2D> textures)
            : base(firstGid)
        {
            _tileWidth = tileWidth;
            _tileHeight = tileHeight;
            _textures = textures;
        }

        public override Texture2D GetTexture(int localId)
        {
            return _textures.TryGetValue(localId, out var tex) ? tex : null!;
        }

        public override Rectangle? GetSourceRectangle(int localId)
        {
            if (!_textures.TryGetValue(localId, out var tex))
            {
                return null;
            }

            return new Rectangle(0, 0, tex.Width, tex.Height);
        }
    }

    private readonly record struct TileLayer(string Name, uint[] TileData);
}
