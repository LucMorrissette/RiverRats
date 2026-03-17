using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.World;

/// <summary>
/// Wraps MonoGame.Extended tiled map loading and rendering for the world layer.
/// </summary>
public sealed class TiledWorldRenderer : IMapCollisionData
{
    private const string GrassTerrainType = "Grass";
    private const string SandTerrainType = "Sand";
    private const string WaterTerrainType = "Water";

    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly int[] _tileGlobalIds;
    private readonly bool[] _blockedByTile;
    private readonly int[] _variantIndexByTile;
    private readonly int _tilesetFirstGlobalIdentifier;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D[] _grassVariants;
    private readonly Texture2D[] _sandVariants;
    private readonly int[] _sandVariantIndexByTile;
    private readonly bool[] _isWaterTile;
    private readonly Dictionary<int, TerrainTileInfo> _terrainTiles;
    private float _waterElapsedSeconds;

    /// <summary>Total map width in pixels (tile columns × tile pixel width).</summary>
    public int MapPixelWidth => _mapWidth * _tileWidth;

    /// <summary>Total map height in pixels (tile rows × tile pixel height).</summary>
    public int MapPixelHeight => _mapHeight * _tileHeight;

    /// <summary>
    /// Initializes a world renderer from a tiled map asset in the content pipeline.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="content">The content manager used to load the tiled map.</param>
    /// <param name="assetName">The content asset name for the map (without extension).</param>
    public TiledWorldRenderer(GraphicsDevice graphicsDevice, ContentManager content, string assetName)
    {
        var mapPath = GetMapPath(content, assetName);
        var mapDirectory = Path.GetDirectoryName(mapPath) ?? throw new InvalidOperationException("Map directory is unavailable.");

        var mapDocument = XDocument.Load(mapPath);
        var mapElement = mapDocument.Element("map") ?? throw new InvalidOperationException("TMX map root element was not found.");

        _mapWidth = GetRequiredIntAttribute(mapElement, "width");
        _mapHeight = GetRequiredIntAttribute(mapElement, "height");
        _tileWidth = GetRequiredIntAttribute(mapElement, "tilewidth");
        _tileHeight = GetRequiredIntAttribute(mapElement, "tileheight");

        var tilesetRefElement = mapElement.Element("tileset") ?? throw new InvalidOperationException("TMX tileset reference was not found.");
        _tilesetFirstGlobalIdentifier = GetRequiredIntAttribute(tilesetRefElement, "firstgid");
        var tilesetSource = GetRequiredStringAttribute(tilesetRefElement, "source");
        var tilesetPath = Path.GetFullPath(Path.Combine(mapDirectory, tilesetSource));

        var terrainTiles = LoadTerrainTiles(tilesetPath, content);
        _grassVariants = terrainTiles.GrassVariants;
        _sandVariants = terrainTiles.SandVariants;
        _terrainTiles = terrainTiles.ByLocalIdentifier;

        var groundLayer = mapElement.Element("layer") ?? throw new InvalidOperationException("TMX ground layer was not found.");
        var dataElement = groundLayer.Element("data") ?? throw new InvalidOperationException("TMX ground layer data was not found.");
        _tileGlobalIds = ParseCsvTileData(dataElement.Value, _mapWidth * _mapHeight);

        _blockedByTile = new bool[_tileGlobalIds.Length];
        _variantIndexByTile = new int[_tileGlobalIds.Length];
        _sandVariantIndexByTile = new int[_tileGlobalIds.Length];
        _isWaterTile = new bool[_tileGlobalIds.Length];

        for (var y = 0; y < _mapHeight; y++)
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                var tileIndex = (y * _mapWidth) + x;
                var globalIdentifier = _tileGlobalIds[tileIndex];
                if (globalIdentifier <= 0)
                {
                    continue;
                }

                var localTileIdentifier = globalIdentifier - _tilesetFirstGlobalIdentifier;
                if (terrainTiles.ByLocalIdentifier.TryGetValue(localTileIdentifier, out var terrainTile))
                {
                    _blockedByTile[tileIndex] = terrainTile.Blocked;
                    _isWaterTile[tileIndex] = string.Equals(
                        terrainTile.TerrainType, WaterTerrainType, StringComparison.OrdinalIgnoreCase);
                }

                _variantIndexByTile[tileIndex] = PickWeightedVariantIndex(x, y);
                _sandVariantIndexByTile[tileIndex] = PickSandVariantIndex(x, y);
            }
        }

        _spriteBatch = new SpriteBatch(graphicsDevice);
    }

    /// <summary>
    /// Updates map animation state.
    /// </summary>
    /// <param name="gameTime">Frame timing values.</param>
    public void Update(GameTime gameTime)
    {
        _waterElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    /// <summary>Elapsed seconds used by the water distortion shader.</summary>
    public float WaterElapsedSeconds => _waterElapsedSeconds;

    /// <summary>
    /// Draws non-water terrain tiles using the provided transform matrix.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void DrawTerrain(Matrix transformMatrix)
    {
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: transformMatrix);

        DrawTiles(waterPass: false);

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws water tiles only using the provided transform matrix.
    /// Call this to render water into a separate render target for shader post-processing.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void DrawWater(Matrix transformMatrix)
    {
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: transformMatrix);

        DrawTiles(waterPass: true);

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws all tiles (water and non-water) in a single pass.
    /// Use when no water shader is needed.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void Draw(Matrix transformMatrix)
    {
        DrawTerrain(transformMatrix);
        DrawWater(transformMatrix);
    }

    private void DrawTiles(bool waterPass)
    {
        for (var y = 0; y < _mapHeight; y++)
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                var tileIndex = (y * _mapWidth) + x;

                // Filter: only draw water tiles on water pass, non-water on terrain pass.
                if (_isWaterTile[tileIndex] != waterPass)
                {
                    continue;
                }

                var globalIdentifier = _tileGlobalIds[tileIndex];
                if (globalIdentifier <= 0)
                {
                    continue;
                }

                var localId = globalIdentifier - _tilesetFirstGlobalIdentifier;
                if (!_terrainTiles.TryGetValue(localId, out var terrainTile))
                {
                    continue;
                }

                var destination = new Rectangle(
                    x * _tileWidth,
                    y * _tileHeight,
                    _tileWidth,
                    _tileHeight);

                if (terrainTile.TerrainType == GrassTerrainType)
                {
                    var variantIndex = _variantIndexByTile[tileIndex];
                    _spriteBatch.Draw(_grassVariants[variantIndex], destination, Color.White);
                }
                else if (terrainTile.TerrainType == SandTerrainType)
                {
                    _spriteBatch.Draw(_sandVariants[_sandVariantIndexByTile[tileIndex]], destination, Color.White);
                }
                else
                {
                    _spriteBatch.Draw(terrainTile.Texture, destination, Color.White);
                }
            }
        }
    }

    /// <inheritdoc />
    public bool IsWorldRectangleBlocked(Rectangle worldBounds)
    {
        var clampedBounds = Rectangle.Intersect(worldBounds, new Rectangle(0, 0, MapPixelWidth, MapPixelHeight));
        if (clampedBounds.Width <= 0 || clampedBounds.Height <= 0)
        {
            return false;
        }

        var minTileX = Math.Max(0, clampedBounds.Left / _tileWidth);
        var maxTileX = Math.Min(_mapWidth - 1, (clampedBounds.Right - 1) / _tileWidth);
        var minTileY = Math.Max(0, clampedBounds.Top / _tileHeight);
        var maxTileY = Math.Min(_mapHeight - 1, (clampedBounds.Bottom - 1) / _tileHeight);

        for (var tileY = minTileY; tileY <= maxTileY; tileY++)
        {
            for (var tileX = minTileX; tileX <= maxTileX; tileX++)
            {
                if (_blockedByTile[(tileY * _mapWidth) + tileX])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string GetMapPath(ContentManager content, string assetName)
    {
        var relativeMapPath = assetName.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
            ? assetName
            : $"{assetName}.tmx";

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, content.RootDirectory, relativeMapPath));
    }

    private static int GetRequiredIntAttribute(XElement element, string attributeName)
    {
        var value = GetRequiredStringAttribute(element, attributeName);
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string GetRequiredStringAttribute(XElement element, string attributeName)
    {
        var attribute = element.Attribute(attributeName);
        if (attribute is null || string.IsNullOrWhiteSpace(attribute.Value))
        {
            throw new InvalidOperationException($"Missing required '{attributeName}' attribute on element '{element.Name}'.");
        }

        return attribute.Value;
    }

    private static int[] ParseCsvTileData(string csvData, int expectedTileCount)
    {
        var values = csvData
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length != expectedTileCount)
        {
            throw new InvalidOperationException($"TMX tile data count mismatch. Expected {expectedTileCount}, found {values.Length}.");
        }

        var tiles = new int[expectedTileCount];
        for (var i = 0; i < values.Length; i++)
        {
            tiles[i] = int.Parse(values[i], CultureInfo.InvariantCulture);
        }

        return tiles;
    }

    private static TerrainTilesetData LoadTerrainTiles(string tilesetPath, ContentManager content)
    {
        var tilesetDirectory = Path.GetDirectoryName(tilesetPath) ?? throw new InvalidOperationException("Tileset directory is unavailable.");
        var tilesetDocument = XDocument.Load(tilesetPath);
        var tilesetElement = tilesetDocument.Element("tileset") ?? throw new InvalidOperationException("TSX root element was not found.");

        var tiles = tilesetElement.Elements("tile");
        var byLocalIdentifier = new Dictionary<int, TerrainTileInfo>();
        var grassTextures = new List<Texture2D>();
        var sandTextures = new List<Texture2D>();

        foreach (var tileElement in tiles)
        {
            var localIdentifier = GetRequiredIntAttribute(tileElement, "id");
            var imageElement = tileElement.Element("image") ?? throw new InvalidOperationException("Tile image element is missing in TSX.");
            var imageSource = GetRequiredStringAttribute(imageElement, "source");
            var textureAssetName = GetContentAssetNameFromImageSource(tilesetDirectory, imageSource, content.RootDirectory);
            var texture = LoadTexture2D(content, textureAssetName);

            var terrainType = GetTileProperty(tileElement, "terrainType") ?? GrassTerrainType;
            var blocked = bool.TryParse(GetTileProperty(tileElement, "blocked"), out var parsedBlocked) && parsedBlocked;

            byLocalIdentifier[localIdentifier] = new TerrainTileInfo(terrainType, blocked, texture);

            if (string.Equals(terrainType, GrassTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                grassTextures.Add(texture);
            }
            else if (string.Equals(terrainType, SandTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                sandTextures.Add(texture);
            }
        }

        if (grassTextures.Count == 0)
        {
            throw new InvalidOperationException("Terrain tileset does not define any grass tiles.");
        }

        return new TerrainTilesetData(byLocalIdentifier, grassTextures.ToArray(), sandTextures.ToArray());
    }

    private static string GetTileProperty(XElement tileElement, string propertyName)
    {
        var propertiesElement = tileElement.Element("properties");
        if (propertiesElement is null)
        {
            return null;
        }

        foreach (var propertyElement in propertiesElement.Elements("property"))
        {
            var nameAttribute = propertyElement.Attribute("name");
            if (nameAttribute is not null && string.Equals(nameAttribute.Value, propertyName, StringComparison.Ordinal))
            {
                return propertyElement.Attribute("value")?.Value;
            }
        }

        return null;
    }

    private static string GetContentAssetNameFromImageSource(string tilesetDirectory, string imageSource, string contentRootDirectoryName)
    {
        var fullImagePath = Path.GetFullPath(Path.Combine(tilesetDirectory, imageSource));
        var fullContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, contentRootDirectoryName));

        var relativeImagePath = Path.GetRelativePath(fullContentRoot, fullImagePath)
            .Replace('\\', '/');

        if (relativeImagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            relativeImagePath = relativeImagePath[..^4];
        }

        return relativeImagePath;
    }

    private static Texture2D LoadTexture2D(ContentManager content, string textureAssetName)
    {
        try
        {
            return content.Load<Texture2D>(textureAssetName);
        }
        catch (ContentLoadException)
        {
            // MonoGame's TSX image import can emit texture assets with a _0 suffix.
            return content.Load<Texture2D>($"{textureAssetName}_0");
        }
    }

    private readonly record struct TerrainTileInfo(string TerrainType, bool Blocked, Texture2D Texture);

    private readonly record struct TerrainTilesetData(
        Dictionary<int, TerrainTileInfo> ByLocalIdentifier,
        Texture2D[] GrassVariants,
        Texture2D[] SandVariants);

    private static int PickWeightedVariantIndex(int x, int y)
    {
        // Position-hashed roll: stable for a tile coordinate, varied across the map.
        var roll = GetDeterministicPercentRoll(x, y);

        if (roll < 32)
        {
            return 0;
        }

        if (roll < 54)
        {
            return 1;
        }

        if (roll < 79)
        {
            return 2;
        }

        if (roll < 98)
        {
            return 3;
        }

        if (roll < 99)
        {
            return 4;
        }

        return 5;
    }

    private static int GetDeterministicPercentRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 73856093u) ^ ((uint)y * 19349663u) ^ 0x9E3779B9u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (int)(hash % 100u);
        }
    }

    private static int PickSandVariantIndex(int x, int y)
    {
        // Different prime seeds from grass to avoid identical patterns.
        var roll = GetDeterministicSandRoll(x, y);
        if (roll < 34)
        {
            return 0;
        }

        if (roll < 67)
        {
            return 1;
        }

        return 2;
    }

    private static int GetDeterministicSandRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 83492791u) ^ ((uint)y * 27644437u) ^ 0x517CC1B7u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (int)(hash % 100u);
        }
    }
}
