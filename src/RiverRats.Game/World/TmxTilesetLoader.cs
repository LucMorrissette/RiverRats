using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.World;

/// <summary>
/// Loads terrain tilesets from TSX files and resolves MonoGame content asset names.
/// Extracted from <see cref="TiledWorldRenderer"/> to keep that class focused on
/// map state and rendering.
/// </summary>
internal static class TmxTilesetLoader
{
    private const string GrassTerrainType = "Grass";
    private const string WoodFloorTerrainType = "WoodFloor";
    private const string SandTerrainType = "Sand";
    private const string RiverbedTerrainType = "Riverbed";
    private const string ShorelineTerrainType = "Shoreline";

    internal static TerrainTilesetData LoadTerrainTiles(string tilesetPath, ContentManager content)
    {
        var tilesetDirectory = Path.GetDirectoryName(tilesetPath)
            ?? throw new InvalidOperationException("Tileset directory is unavailable.");
        var tilesetDocument = XDocument.Load(tilesetPath);
        var tilesetElement = tilesetDocument.Element("tileset")
            ?? throw new InvalidOperationException("TSX root element was not found.");

        var tiles = tilesetElement.Elements("tile");
        var byLocalIdentifier = new Dictionary<int, TerrainTileInfo>();
        var grassTextures = new List<Texture2D>();
        var woodFloorTextures = new List<Texture2D>();
        var sandTextures = new List<Texture2D>();
        var riverbedTextures = new List<Texture2D>();
        var shorelineTextures = new List<Texture2D>();

        foreach (var tileElement in tiles)
        {
            var localIdentifier = TmxXmlHelpers.GetRequiredIntAttribute(tileElement, "id");
            var imageElement = tileElement.Element("image")
                ?? throw new InvalidOperationException("Tile image element is missing in TSX.");
            var imageSource = TmxXmlHelpers.GetRequiredStringAttribute(imageElement, "source");
            var textureAssetName = GetContentAssetNameFromImageSource(
                tilesetDirectory, imageSource, content.RootDirectory);
            var texture = LoadTexture2D(content, textureAssetName);

            var terrainType = TmxXmlHelpers.GetTileProperty(tileElement, "terrainType") ?? GrassTerrainType;
            var blocked = bool.TryParse(TmxXmlHelpers.GetTileProperty(tileElement, "blocked"), out var parsedBlocked) && parsedBlocked;

            byLocalIdentifier[localIdentifier] = new TerrainTileInfo(terrainType, blocked, texture);

            if (string.Equals(terrainType, GrassTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                grassTextures.Add(texture);
            }
            else if (string.Equals(terrainType, WoodFloorTerrainType, StringComparison.OrdinalIgnoreCase))
            {
                woodFloorTextures.Add(texture);
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

        if (byLocalIdentifier.Count == 0)
        {
            throw new InvalidOperationException("Terrain tileset does not define any tiles.");
        }

        return new TerrainTilesetData(
            byLocalIdentifier,
            grassTextures.ToArray(),
            woodFloorTextures.ToArray(),
            sandTextures.ToArray(),
            riverbedTextures.ToArray(),
            shorelineTextures.ToArray());
    }

    internal static string GetContentAssetNameFromImageSource(
        string tilesetDirectory, string imageSource, string contentRootDirectoryName)
    {
        var fullImagePath = Path.GetFullPath(Path.Combine(tilesetDirectory, imageSource));
        var fullContentRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, contentRootDirectoryName));

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
}

/// <summary>
/// Parsed data for a single terrain tile from a TSX tileset.
/// </summary>
internal readonly record struct TerrainTileInfo(string TerrainType, bool Blocked, Texture2D Texture);

/// <summary>
/// All terrain tile data loaded from a TSX file, organised by variant type for fast render-time lookup.
/// </summary>
internal readonly record struct TerrainTilesetData(
    Dictionary<int, TerrainTileInfo> ByLocalIdentifier,
    Texture2D[] GrassVariants,
    Texture2D[] WoodFloorVariants,
    Texture2D[] SandVariants,
    Texture2D[] RiverbedVariants,
    Texture2D[] ShorelineVariants);
