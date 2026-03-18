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
    private const string RiverbedTerrainType = "Riverbed";
    private const string ShorelineTerrainType = "Shoreline";
    private const string WaterLayerPrefix = "Water/";
    private const string WaterSurfaceLayerName = "Water/surface";
    private const string PropTypePropertyName = "propType";
    private const string IsUnderwaterPropertyName = "isUnderwater";
    private const uint TiledHorizontalFlipFlag = 0x80000000;
    private const uint TiledVerticalFlipFlag = 0x40000000;
    private const uint TiledDiagonalFlipFlag = 0x20000000;
    private const uint TiledFlipMask = TiledHorizontalFlipFlag | TiledVerticalFlipFlag | TiledDiagonalFlipFlag;

    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly MapLayer[] _layers;
    private readonly bool[] _blockedByTile;
    private readonly int[] _variantIndexByTile;
    private readonly int _tilesetFirstGlobalIdentifier;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D[] _grassVariants;
    private readonly Texture2D[] _sandVariants;
    private readonly Texture2D[] _riverbedVariants;
    private readonly Texture2D[] _shorelineVariants;
    private readonly int[] _sandVariantIndexByTile;
    private readonly int[] _riverbedVariantIndexByTile;
    private readonly int[] _shorelineVariantIndexByTile;
    private readonly Dictionary<int, TerrainTileInfo> _terrainTiles;
    private readonly MapPropPlacement[] _propPlacements;
    private float _waterElapsedSeconds;

    /// <summary>Total map width in pixels (tile columns × tile pixel width).</summary>
    public int MapPixelWidth => _mapWidth * _tileWidth;

    /// <summary>Total map height in pixels (tile rows × tile pixel height).</summary>
    public int MapPixelHeight => _mapHeight * _tileHeight;

    /// <summary>Width of an individual map tile in pixels.</summary>
    public int TileWidthPixels => _tileWidth;

    /// <summary>Height of an individual map tile in pixels.</summary>
    public int TileHeightPixels => _tileHeight;

    /// <summary>
    /// Prop instances placed through TMX object layers.
    /// </summary>
    public IReadOnlyList<MapPropPlacement> PropPlacements => _propPlacements;

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
        _riverbedVariants = terrainTiles.RiverbedVariants;
        _shorelineVariants = terrainTiles.ShorelineVariants;
        _terrainTiles = terrainTiles.ByLocalIdentifier;

        var propMetadataByGlobalIdentifier = LoadPropMetadataByGlobalIdentifier(mapElement, mapDirectory);
        _propPlacements = LoadPropPlacements(mapElement, propMetadataByGlobalIdentifier);

        var layers = new List<MapLayer>();
        foreach (var layerElement in mapElement.Elements("layer"))
        {
            var layerName = GetRequiredStringAttribute(layerElement, "name");
            var dataElement = layerElement.Element("data") ?? throw new InvalidOperationException($"TMX layer '{layerName}' data was not found.");
            var tileGlobalIds = ParseCsvTileData(dataElement.Value, _mapWidth * _mapHeight);
            layers.Add(new MapLayer(layerName, tileGlobalIds, IsWaterLayerName(layerName)));
        }

        if (layers.Count == 0)
        {
            throw new InvalidOperationException("TMX map does not define any tile layers.");
        }

        _layers = layers.ToArray();

        var tileCount = _mapWidth * _mapHeight;
        _blockedByTile = new bool[tileCount];
        _variantIndexByTile = new int[tileCount];
        _sandVariantIndexByTile = new int[tileCount];
        _riverbedVariantIndexByTile = new int[tileCount];
        _shorelineVariantIndexByTile = new int[tileCount];

        for (var y = 0; y < _mapHeight; y++)
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                var tileIndex = (y * _mapWidth) + x;
                for (var layerIndex = 0; layerIndex < _layers.Length; layerIndex++)
                {
                    var globalIdentifier = _layers[layerIndex].TileGlobalIds[tileIndex];
                    if (globalIdentifier <= 0)
                    {
                        continue;
                    }

                    var localTileIdentifier = globalIdentifier - _tilesetFirstGlobalIdentifier;
                    if (terrainTiles.ByLocalIdentifier.TryGetValue(localTileIdentifier, out var terrainTile))
                    {
                        _blockedByTile[tileIndex] |= terrainTile.Blocked;
                    }
                }

                _variantIndexByTile[tileIndex] = PickWeightedVariantIndex(x, y);
                _sandVariantIndexByTile[tileIndex] = PickSandVariantIndex(x, y);
                _riverbedVariantIndexByTile[tileIndex] = PickRiverbedVariantIndex(x, y, _riverbedVariants.Length);
                _shorelineVariantIndexByTile[tileIndex] = PickShorelineVariantIndex(x, y, _shorelineVariants.Length);
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
    /// Draws all non-water tile layers using the provided transform matrix.
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
    /// Draws all water-prefixed tile layers using the provided transform matrix.
    /// Call this to render a composite water stack into a separate render target for shader post-processing.
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
    /// Draws water layers below the surface (e.g. "Water/Bottom").
    /// Call this before drawing underwater props so they sit on the riverbed.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void DrawWaterBottom(Matrix transformMatrix)
    {
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: transformMatrix);

        DrawWaterLayers(isSurface: false);

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws the water surface layer ("Water/surface") on top.
    /// Call this after drawing underwater props so the surface renders over them.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void DrawWaterSurface(Matrix transformMatrix)
    {
        _spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: transformMatrix);

        DrawWaterLayers(isSurface: true);

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws all tile layers in two passes with water below terrain.
    /// Use when no water shader is needed.
    /// </summary>
    /// <param name="transformMatrix">World transform matrix.</param>
    public void Draw(Matrix transformMatrix)
    {
        DrawWater(transformMatrix);
        DrawTerrain(transformMatrix);
    }

    private void DrawTiles(bool waterPass)
    {
        for (var layerIndex = 0; layerIndex < _layers.Length; layerIndex++)
        {
            var layer = _layers[layerIndex];
            if (layer.IsWaterLayer != waterPass)
            {
                continue;
            }

            DrawLayerTiles(layer.TileGlobalIds);
        }
    }

    private void DrawWaterLayers(bool isSurface)
    {
        for (var layerIndex = 0; layerIndex < _layers.Length; layerIndex++)
        {
            var layer = _layers[layerIndex];
            if (!layer.IsWaterLayer)
            {
                continue;
            }

            var layerIsSurface = string.Equals(layer.Name, WaterSurfaceLayerName, StringComparison.OrdinalIgnoreCase);
            if (layerIsSurface != isSurface)
            {
                continue;
            }

            DrawLayerTiles(layer.TileGlobalIds);
        }
    }

    private void DrawLayerTiles(int[] tileGlobalIds)
    {
        for (var y = 0; y < _mapHeight; y++)
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                var tileIndex = (y * _mapWidth) + x;
                var globalIdentifier = tileGlobalIds[tileIndex];
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
                else if (terrainTile.TerrainType == RiverbedTerrainType && _riverbedVariants.Length > 0)
                {
                    _spriteBatch.Draw(_riverbedVariants[_riverbedVariantIndexByTile[tileIndex]], destination, Color.White);
                }
                else if (terrainTile.TerrainType == ShorelineTerrainType && _shorelineVariants.Length > 0)
                {
                    _spriteBatch.Draw(_shorelineVariants[_shorelineVariantIndexByTile[tileIndex]], destination, Color.White);
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

    internal static bool IsWaterLayerName(string layerName)
    {
        return layerName.StartsWith(WaterLayerPrefix, StringComparison.OrdinalIgnoreCase);
    }

    internal static Vector2 GetTileObjectTopLeft(float x, float y, float height)
    {
        return new Vector2(
            MathF.Round(x),
            MathF.Round(y - height));
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

    private static Dictionary<int, PropTileMetadata> LoadPropMetadataByGlobalIdentifier(XElement mapElement, string mapDirectory)
    {
        var metadataByGlobalIdentifier = new Dictionary<int, PropTileMetadata>();

        foreach (var tilesetRefElement in mapElement.Elements("tileset"))
        {
            var firstGlobalIdentifier = GetRequiredIntAttribute(tilesetRefElement, "firstgid");
            var tilesetSource = tilesetRefElement.Attribute("source")?.Value;
            if (string.IsNullOrWhiteSpace(tilesetSource))
            {
                continue;
            }

            var tilesetPath = Path.GetFullPath(Path.Combine(mapDirectory, tilesetSource));
            var tilesetDocument = XDocument.Load(tilesetPath);
            var tilesetElement = tilesetDocument.Element("tileset") ?? throw new InvalidOperationException("TSX root element was not found.");

            foreach (var tileElement in tilesetElement.Elements("tile"))
            {
                var propType = GetTileProperty(tileElement, PropTypePropertyName);
                if (string.IsNullOrWhiteSpace(propType))
                {
                    continue;
                }

                var isUnderwater = bool.TryParse(GetTileProperty(tileElement, IsUnderwaterPropertyName), out var parsed) && parsed;
                var localIdentifier = GetRequiredIntAttribute(tileElement, "id");
                metadataByGlobalIdentifier[firstGlobalIdentifier + localIdentifier] = new PropTileMetadata(propType, isUnderwater);
            }
        }

        return metadataByGlobalIdentifier;
    }

    private static MapPropPlacement[] LoadPropPlacements(XElement mapElement, Dictionary<int, PropTileMetadata> metadataByGlobalIdentifier)
    {
        if (metadataByGlobalIdentifier.Count == 0)
        {
            return Array.Empty<MapPropPlacement>();
        }

        var props = new List<MapPropPlacement>();

        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                var gidAttribute = objectElement.Attribute("gid");
                if (gidAttribute is null)
                {
                    continue;
                }

                if (!uint.TryParse(gidAttribute.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawGlobalIdentifier))
                {
                    continue;
                }

                var globalIdentifier = (int)(rawGlobalIdentifier & ~TiledFlipMask);
                if (!metadataByGlobalIdentifier.TryGetValue(globalIdentifier, out var metadata))
                {
                    continue;
                }

                var x = float.Parse(GetRequiredStringAttribute(objectElement, "x"), CultureInfo.InvariantCulture);
                var y = float.Parse(GetRequiredStringAttribute(objectElement, "y"), CultureInfo.InvariantCulture);
                var height = objectElement.Attribute("height") is { } heightAttribute
                    ? float.Parse(heightAttribute.Value, CultureInfo.InvariantCulture)
                    : 0f;
                var topLeft = GetTileObjectTopLeft(x, y, height);
                props.Add(new MapPropPlacement(metadata.PropType, topLeft, metadata.IsUnderwater));
            }
        }

        return props.ToArray();
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
        var riverbedTextures = new List<Texture2D>();
        var shorelineTextures = new List<Texture2D>();

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
            else if (string.Equals(terrainType, RiverbedTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                riverbedTextures.Add(texture);
            }
            else if (string.Equals(terrainType, ShorelineTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                shorelineTextures.Add(texture);
            }
        }

        if (grassTextures.Count == 0)
        {
            throw new InvalidOperationException("Terrain tileset does not define any grass tiles.");
        }

        return new TerrainTilesetData(byLocalIdentifier, grassTextures.ToArray(), sandTextures.ToArray(), riverbedTextures.ToArray(), shorelineTextures.ToArray());
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

    private readonly record struct MapLayer(string Name, int[] TileGlobalIds, bool IsWaterLayer);

    /// <summary>
    /// A prop instance placed by TMX object data.
    /// </summary>
    /// <param name="PropType">Prop identifier from TSX tile property <c>propType</c>.</param>
    /// <param name="Position">World-space top-left position in pixels.</param>
    /// <param name="IsUnderwater">When true the prop is drawn into the water render target so the distortion shader affects it.</param>
    public readonly record struct MapPropPlacement(string PropType, Vector2 Position, bool IsUnderwater);

    private readonly record struct PropTileMetadata(string PropType, bool IsUnderwater);

    private readonly record struct TerrainTilesetData(
        Dictionary<int, TerrainTileInfo> ByLocalIdentifier,
        Texture2D[] GrassVariants,
        Texture2D[] SandVariants,
        Texture2D[] RiverbedVariants,
        Texture2D[] ShorelineVariants);

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

    internal static int PickRiverbedVariantIndex(int x, int y, int variantCount)
    {
        if (variantCount <= 0)
        {
            return 0;
        }

        var regionCategory = GetDeterministicRiverbedRegionCategory(x, y);
        var categoryCount = 4;
        var localVariantCount = (variantCount + categoryCount - 1) / categoryCount;
        var preferredVariant = (GetDeterministicRiverbedLocalRoll(x, y) % localVariantCount) * categoryCount;
        preferredVariant += regionCategory;

        if (preferredVariant >= variantCount)
        {
            preferredVariant = regionCategory % variantCount;
        }

        var accentRoll = GetDeterministicRiverbedAccentRoll(x, y);
        if (accentRoll < 72)
        {
            return preferredVariant;
        }

        var neighboringCategory = (regionCategory + 1 + (accentRoll % 3)) % categoryCount;
        var neighboringVariant = (GetDeterministicRiverbedNeighborRoll(x, y) % localVariantCount) * categoryCount;
        neighboringVariant += neighboringCategory;

        if (neighboringVariant >= variantCount)
        {
            neighboringVariant = neighboringCategory % variantCount;
        }

        return neighboringVariant;
    }

    internal static int PickShorelineVariantIndex(int x, int y, int variantCount)
    {
        if (variantCount <= 0)
        {
            return 0;
        }

        var roll = GetDeterministicShorelineRoll(x, y);
        return roll % variantCount;
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

    private static int GetDeterministicRiverbedRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 961748927u) ^ ((uint)y * 982451653u) ^ 0x68E31DA4u;
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (int)(hash % 100u);
        }
    }

    private static int GetDeterministicRiverbedRegionCategory(int x, int y)
    {
        unchecked
        {
            var regionX = x / 4;
            var regionY = y / 3;
            var hash = ((uint)regionX * 2246822519u) ^ ((uint)regionY * 3266489917u) ^ 0xC2B2AE35u;
            hash ^= hash >> 15;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (int)(hash % 4u);
        }
    }

    private static int GetDeterministicRiverbedLocalRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 668265263u) ^ ((uint)y * 2147483647u) ^ 0x165667B1u;
            hash ^= hash >> 13;
            hash *= 2246822519u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static int GetDeterministicRiverbedAccentRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 374761393u) ^ ((uint)y * 1103515245u) ^ 0x85EBCA77u;
            hash ^= hash >> 15;
            hash *= 1274126177u;
            hash ^= hash >> 14;
            return (int)(hash % 100u);
        }
    }

    private static int GetDeterministicRiverbedNeighborRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 1597334677u) ^ ((uint)y * 3812015801u) ^ 0x27D4EB2Fu;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }

    private static int GetDeterministicShorelineRoll(int x, int y)
    {
        unchecked
        {
            var hash = ((uint)x * 2654435761u) ^ ((uint)y * 2246822519u) ^ 0xA136AAABu;
            hash ^= hash >> 13;
            hash *= 3266489917u;
            hash ^= hash >> 16;
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
