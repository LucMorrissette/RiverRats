#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;

namespace DogDays.Game.World;

/// <summary>
/// Wraps MonoGame.Extended tiled map loading and rendering for the world layer.
/// </summary>
public sealed class TiledWorldRenderer : IMapCollisionData, IDisposable
{
    private const string GrassTerrainType = "Grass";
    private const string WoodFloorTerrainType = "WoodFloor";
    private const string SandTerrainType = "Sand";
    private const string RiverbedTerrainType = "Riverbed";
    private const string ShorelineTerrainType = "Shoreline";
    private const string WaterLayerPrefix = "Water/";
    private const string WaterSurfaceLayerName = "Water/surface";

    private readonly int _mapWidth;
    private readonly int _mapHeight;
    private readonly int _tileWidth;
    private readonly int _tileHeight;
    private readonly MapLayer[] _layers;
    private readonly int[][] _waterLayerTileGlobalIds;
    private readonly bool[] _blockedByTile;
    private readonly int[] _variantIndexByTile;
    private readonly int _tilesetFirstGlobalIdentifier;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D[] _grassVariants;
    private readonly Texture2D[] _woodFloorVariants;
    private readonly Texture2D[] _sandVariants;
    private readonly Texture2D[] _riverbedVariants;
    private readonly Texture2D[] _shorelineVariants;
    private readonly int[] _woodFloorVariantIndexByTile;
    private readonly int[] _sandVariantIndexByTile;
    private readonly int[] _riverbedVariantIndexByTile;
    private readonly int[] _shorelineVariantIndexByTile;
    private readonly Dictionary<int, TerrainTileInfo> _terrainTiles;
    private readonly MapPropPlacement[] _propPlacements;
    private readonly Rectangle[] _colliderBounds;
    private readonly ZoneTriggerData[] _zoneTriggers;
    private readonly SpawnPointData[] _spawnPoints;
    private readonly FishingZoneData[] _fishingZones;
    private readonly IndoorNavGraph? _navGraph;
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
    /// World-space collision rectangles authored in the TMX Colliders object layer.
    /// </summary>
    public IReadOnlyList<Rectangle> ColliderBounds => _colliderBounds;

    /// <summary>
    /// Zone transition triggers authored in the TMX ZoneTriggers object layer.
    /// </summary>
    public IReadOnlyList<ZoneTriggerData> ZoneTriggers => _zoneTriggers;

    /// <summary>
    /// Named spawn points authored in the TMX SpawnPoints object layer.
    /// </summary>
    public IReadOnlyList<SpawnPointData> SpawnPoints => _spawnPoints;

    /// <summary>
    /// Fishable interaction zones authored in the TMX FishingZones object layer.
    /// </summary>
    public IReadOnlyList<FishingZoneData> FishingZones => _fishingZones;

    /// <summary>
    /// Indoor navigation graph for this map, or <c>null</c> if the map does not define navigation data.
    /// </summary>
    public IndoorNavGraph? NavGraph => _navGraph;

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

        _mapWidth = TmxXmlHelpers.GetRequiredIntAttribute(mapElement, "width");
        _mapHeight = TmxXmlHelpers.GetRequiredIntAttribute(mapElement, "height");
        _tileWidth = TmxXmlHelpers.GetRequiredIntAttribute(mapElement, "tilewidth");
        _tileHeight = TmxXmlHelpers.GetRequiredIntAttribute(mapElement, "tileheight");

        var tilesetRefElement = mapElement.Element("tileset") ?? throw new InvalidOperationException("TMX tileset reference was not found.");
        _tilesetFirstGlobalIdentifier = TmxXmlHelpers.GetRequiredIntAttribute(tilesetRefElement, "firstgid");
        var tilesetSource = TmxXmlHelpers.GetRequiredStringAttribute(tilesetRefElement, "source");
        var tilesetPath = Path.GetFullPath(Path.Combine(mapDirectory, tilesetSource));

        var terrainTiles = TmxTilesetLoader.LoadTerrainTiles(tilesetPath, content);
        _grassVariants = terrainTiles.GrassVariants;
        _woodFloorVariants = terrainTiles.WoodFloorVariants;
        _sandVariants = terrainTiles.SandVariants;
        _riverbedVariants = terrainTiles.RiverbedVariants;
        _shorelineVariants = terrainTiles.ShorelineVariants;
        _terrainTiles = terrainTiles.ByLocalIdentifier;

        var propMetadataByGlobalIdentifier = TmxObjectLoader.LoadPropMetadataByGlobalIdentifier(mapElement, mapDirectory);
        _propPlacements = TmxObjectLoader.LoadPropPlacements(mapElement, propMetadataByGlobalIdentifier);
        _colliderBounds = TmxObjectLoader.LoadColliderBounds(mapElement);
        _zoneTriggers = TmxObjectLoader.LoadZoneTriggers(mapElement);
        _spawnPoints = TmxObjectLoader.LoadSpawnPoints(mapElement);
        _fishingZones = TmxObjectLoader.LoadFishingZones(mapElement);

        var navNodes = TmxObjectLoader.LoadNavNodes(mapElement);
        if (navNodes.Length > 0)
        {
            var navLinks = TmxObjectLoader.LoadNavLinks(mapElement, navNodes);
            _navGraph = new IndoorNavGraph(navNodes, navLinks);
        }

        var layers = new List<MapLayer>();
        var waterLayerTileGlobalIds = new List<int[]>();
        foreach (var layerElement in mapElement.Elements("layer"))
        {
            var layerName = TmxXmlHelpers.GetRequiredStringAttribute(layerElement, "name");
            var dataElement = layerElement.Element("data") ?? throw new InvalidOperationException($"TMX layer '{layerName}' data was not found.");
            var tileGlobalIds = TmxXmlHelpers.ParseCsvTileData(dataElement.Value, _mapWidth * _mapHeight);
            var isWaterLayer = TmxXmlHelpers.IsWaterLayerName(layerName);
            layers.Add(new MapLayer(layerName, tileGlobalIds, isWaterLayer));

            if (isWaterLayer)
            {
                waterLayerTileGlobalIds.Add(tileGlobalIds);
            }
        }

        if (layers.Count == 0)
        {
            throw new InvalidOperationException("TMX map does not define any tile layers.");
        }

        _layers = layers.ToArray();
    _waterLayerTileGlobalIds = waterLayerTileGlobalIds.ToArray();

        var tileCount = _mapWidth * _mapHeight;
        _blockedByTile = new bool[tileCount];
        _variantIndexByTile = new int[tileCount];
        _woodFloorVariantIndexByTile = new int[tileCount];
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

                _variantIndexByTile[tileIndex] = TileVariantPicker.PickGrassVariantIndex(x, y);
                _woodFloorVariantIndexByTile[tileIndex] = TileVariantPicker.PickWoodFloorVariantIndex(x, y, _woodFloorVariants.Length);
                _sandVariantIndexByTile[tileIndex] = TileVariantPicker.PickSandVariantIndex(x, y);
                _riverbedVariantIndexByTile[tileIndex] = TileVariantPicker.PickRiverbedVariantIndex(x, y, _riverbedVariants.Length);
                _shorelineVariantIndexByTile[tileIndex] = TileVariantPicker.PickShorelineVariantIndex(x, y, _shorelineVariants.Length);
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
                else if (terrainTile.TerrainType == WoodFloorTerrainType && _woodFloorVariants.Length > 0)
                {
                    _spriteBatch.Draw(_woodFloorVariants[_woodFloorVariantIndexByTile[tileIndex]], destination, Color.White);
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

    /// <summary>
    /// Returns true when the provided world-space point lies on any authored water layer tile.
    /// </summary>
    public bool IsWorldPointInWater(Point worldPoint)
    {
        if (worldPoint.X < 0
            || worldPoint.Y < 0
            || worldPoint.X >= MapPixelWidth
            || worldPoint.Y >= MapPixelHeight)
        {
            return false;
        }

        var tileX = worldPoint.X / _tileWidth;
        var tileY = worldPoint.Y / _tileHeight;
        var tileIndex = (tileY * _mapWidth) + tileX;
        return HasAnyWaterTileAt(_waterLayerTileGlobalIds, tileIndex);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _spriteBatch.Dispose();
    }

    private static string GetMapPath(ContentManager content, string assetName)
    {
        var relativeMapPath = assetName.EndsWith(".tmx", StringComparison.OrdinalIgnoreCase)
            ? assetName
            : $"{assetName}.tmx";

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, content.RootDirectory, relativeMapPath));
    }

    /// <summary>
    /// Returns <c>true</c> when the layer name begins with the "Water/" prefix (case-insensitive).
    /// </summary>
    internal static bool IsWaterLayerName(string layerName) =>
        TmxXmlHelpers.IsWaterLayerName(layerName);

    /// <summary>
    /// Returns true when any supplied water layer has a non-empty tile at the given index.
    /// </summary>
    internal static bool HasAnyWaterTileAt(IReadOnlyList<int[]> waterLayerTileGlobalIds, int tileIndex)
    {
        if (tileIndex < 0)
        {
            return false;
        }

        for (var i = 0; i < waterLayerTileGlobalIds.Count; i++)
        {
            var tileIds = waterLayerTileGlobalIds[i];
            if (tileIndex >= tileIds.Length)
            {
                continue;
            }

            if (tileIds[tileIndex] > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a Tiled tile-object position (bottom-left origin) to world-space top-left.
    /// </summary>
    internal static Vector2 GetTileObjectTopLeft(float x, float y, float height) =>
        TmxXmlHelpers.GetTileObjectTopLeft(x, y, height);

    /// <summary>
    /// Picks a deterministic riverbed tile variant for the given tile coordinate.
    /// </summary>
    internal static int PickRiverbedVariantIndex(int x, int y, int variantCount) =>
        TileVariantPicker.PickRiverbedVariantIndex(x, y, variantCount);

    /// <summary>
    /// Picks a deterministic shoreline tile variant for the given tile coordinate.
    /// </summary>
    internal static int PickShorelineVariantIndex(int x, int y, int variantCount) =>
        TileVariantPicker.PickShorelineVariantIndex(x, y, variantCount);

    private readonly record struct MapLayer(string Name, int[] TileGlobalIds, bool IsWaterLayer);

    /// <summary>
    /// A prop instance placed by TMX object data.
    /// </summary>
    /// <param name="PropType">Prop identifier from TSX tile property <c>propType</c>.</param>
    /// <param name="Position">World-space top-left position in pixels.</param>
    /// <param name="IsUnderwater">When true the prop is drawn into the water render target so the distortion shader affects it.</param>
    /// <param name="SuppressOcclusion">When true the reveal lens will not activate when a character walks behind this prop.</param>
    /// <param name="RotationRadians">Clockwise rotation in radians as authored in Tiled (converted from degrees).</param>
    public readonly record struct MapPropPlacement(string PropType, Vector2 Position, bool IsUnderwater, bool ReachesSurface, bool SuppressOcclusion, float RotationRadians = 0f);
}
