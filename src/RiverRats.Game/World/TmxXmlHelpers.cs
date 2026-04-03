#nullable enable
using System;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.Xna.Framework;

namespace RiverRats.Game.World;

/// <summary>
/// Static helpers for parsing TMX/TSX XML attributes and common TMX conventions.
/// Extracted from <see cref="TiledWorldRenderer"/> to keep that class focused on
/// map state and rendering.
/// </summary>
internal static class TmxXmlHelpers
{
    private const string WaterLayerPrefix = "Water/";

    // ── Attribute readers ────────────────────────────────────────────────────

    internal static int GetRequiredIntAttribute(XElement element, string attributeName)
    {
        var value = GetRequiredStringAttribute(element, attributeName);
        return int.Parse(value, CultureInfo.InvariantCulture);
    }

    internal static string GetRequiredStringAttribute(XElement element, string attributeName)
    {
        var attribute = element.Attribute(attributeName);
        if (attribute is null || string.IsNullOrWhiteSpace(attribute.Value))
        {
            throw new InvalidOperationException(
                $"Missing required '{attributeName}' attribute on element '{element.Name}'.");
        }

        return attribute.Value;
    }

    /// <summary>
    /// Reads a named custom property from a Tiled &lt;properties&gt; block.
    /// Returns <c>null</c> if the property is absent.
    /// </summary>
    internal static string? GetTileProperty(XElement tileElement, string propertyName)
    {
        var propertiesElement = tileElement.Element("properties");
        if (propertiesElement is null)
        {
            return null;
        }

        foreach (var propertyElement in propertiesElement.Elements("property"))
        {
            var nameAttribute = propertyElement.Attribute("name");
            if (nameAttribute is not null &&
                string.Equals(nameAttribute.Value, propertyName, StringComparison.Ordinal))
            {
                return propertyElement.Attribute("value")?.Value;
            }
        }

        return null;
    }

    // ── TMX conventions ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the layer name begins with the "Water/" prefix
    /// (case-insensitive), indicating it belongs to the water render stack.
    /// </summary>
    internal static bool IsWaterLayerName(string layerName) =>
        layerName.StartsWith(WaterLayerPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Converts a Tiled tile-object position (bottom-left origin, Tiled convention)
    /// to world-space top-left, rounding to the nearest pixel.
    /// </summary>
    internal static Vector2 GetTileObjectTopLeft(float x, float y, float height) =>
        new(MathF.Round(x), MathF.Round(y - height));

    /// <summary>
    /// Parses the CSV tile-data string from a TMX &lt;data&gt; element into a flat
    /// array of raw global tile identifiers (including flip bits).
    /// </summary>
    internal static int[] ParseCsvTileData(string csvData, int expectedTileCount)
    {
        var values = csvData
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length != expectedTileCount)
        {
            throw new InvalidOperationException(
                $"TMX tile data count mismatch. Expected {expectedTileCount}, found {values.Length}.");
        }

        var tiles = new int[expectedTileCount];
        for (var i = 0; i < values.Length; i++)
        {
            tiles[i] = int.Parse(values[i], CultureInfo.InvariantCulture);
        }

        return tiles;
    }
}
