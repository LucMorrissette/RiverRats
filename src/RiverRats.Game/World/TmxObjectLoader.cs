#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using RiverRats.Game.Data;

namespace RiverRats.Game.World;

/// <summary>
/// Loads prop placements, colliders, zone triggers, spawn points, and fishing zones
/// from TMX object layers. Extracted from <see cref="TiledWorldRenderer"/> to keep
/// that class focused on map state and rendering.
/// </summary>
internal static class TmxObjectLoader
{
    private const string PropTypePropertyName = "propType";
    private const string IsUnderwaterPropertyName = "isUnderwater";
    private const string ReachesSurfacePropertyName = "reachesSurface";
    private const string SuppressOcclusionPropertyName = "suppressOcclusion";
    private const string ColliderLayerName = "Colliders";
    private const string ZoneTriggerLayerName = "ZoneTriggers";
    private const string SpawnPointLayerName = "SpawnPoints";
    private const string FishingZoneLayerName = "FishingZones";
    private const string TargetMapPropertyName = "targetMap";
    private const string TargetSpawnIdPropertyName = "targetSpawnId";
    private const string FacingDirectionPropertyName = "facingDirection";

    /// <summary>Combined Tiled flip-bit mask (horizontal | vertical | diagonal).</summary>
    private const uint TiledFlipMask = 0xE0000000u;

    // ── Prop placement loading ────────────────────────────────────────────────

    internal static Dictionary<int, PropTileMetadata> LoadPropMetadataByGlobalIdentifier(
        XElement mapElement, string mapDirectory)
    {
        var metadataByGlobalIdentifier = new Dictionary<int, PropTileMetadata>();

        foreach (var tilesetRefElement in mapElement.Elements("tileset"))
        {
            var firstGlobalIdentifier = TmxXmlHelpers.GetRequiredIntAttribute(tilesetRefElement, "firstgid");
            var tilesetSource = tilesetRefElement.Attribute("source")?.Value;
            if (string.IsNullOrWhiteSpace(tilesetSource))
            {
                continue;
            }

            var tilesetPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(mapDirectory, tilesetSource));
            var tilesetDocument = XDocument.Load(tilesetPath);
            var tilesetElement = tilesetDocument.Element("tileset")
                ?? throw new InvalidOperationException("TSX root element was not found.");

            foreach (var tileElement in tilesetElement.Elements("tile"))
            {
                var propType = TmxXmlHelpers.GetTileProperty(tileElement, PropTypePropertyName);
                if (string.IsNullOrWhiteSpace(propType))
                {
                    continue;
                }

                var isUnderwater = bool.TryParse(
                    TmxXmlHelpers.GetTileProperty(tileElement, IsUnderwaterPropertyName),
                    out var parsed) && parsed;
                var reachesSurface = bool.TryParse(
                    TmxXmlHelpers.GetTileProperty(tileElement, ReachesSurfacePropertyName),
                    out var parsedSurface) && parsedSurface;
                var suppressOcclusion = bool.TryParse(
                    TmxXmlHelpers.GetTileProperty(tileElement, SuppressOcclusionPropertyName),
                    out var parsedSuppress) && parsedSuppress;

                var localIdentifier = TmxXmlHelpers.GetRequiredIntAttribute(tileElement, "id");
                metadataByGlobalIdentifier[firstGlobalIdentifier + localIdentifier] =
                    new PropTileMetadata(propType, isUnderwater, reachesSurface, suppressOcclusion);
            }
        }

        return metadataByGlobalIdentifier;
    }

    internal static TiledWorldRenderer.MapPropPlacement[] LoadPropPlacements(
        XElement mapElement, Dictionary<int, PropTileMetadata> metadataByGlobalIdentifier)
    {
        if (metadataByGlobalIdentifier.Count == 0)
        {
            return Array.Empty<TiledWorldRenderer.MapPropPlacement>();
        }

        var props = new List<TiledWorldRenderer.MapPropPlacement>();

        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                var gidAttribute = objectElement.Attribute("gid");
                if (gidAttribute is null)
                {
                    continue;
                }

                if (!uint.TryParse(
                    gidAttribute.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var rawGlobalIdentifier))
                {
                    continue;
                }

                var globalIdentifier = (int)(rawGlobalIdentifier & ~TiledFlipMask);
                if (!metadataByGlobalIdentifier.TryGetValue(globalIdentifier, out var metadata))
                {
                    continue;
                }

                var x = float.Parse(
                    TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "x"),
                    CultureInfo.InvariantCulture);
                var y = float.Parse(
                    TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "y"),
                    CultureInfo.InvariantCulture);
                var height = objectElement.Attribute("height") is { } heightAttribute
                    ? float.Parse(heightAttribute.Value, CultureInfo.InvariantCulture)
                    : 0f;
                var topLeft = TmxXmlHelpers.GetTileObjectTopLeft(x, y, height);

                var rotationRadians = 0f;
                if (objectElement.Attribute("rotation") is { } rotationAttribute
                    && float.TryParse(
                        rotationAttribute.Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var rotationDegrees))
                {
                    rotationRadians = MathHelper.ToRadians(rotationDegrees);
                }

                // Per-object properties in the TMX override tile-level defaults from the TSX.
                var suppressOcclusion = metadata.SuppressOcclusion;
                var objectSuppressValue = TmxXmlHelpers.GetTileProperty(objectElement, SuppressOcclusionPropertyName);
                if (objectSuppressValue is not null)
                {
                    suppressOcclusion = bool.TryParse(objectSuppressValue, out var parsedObjectSuppress) && parsedObjectSuppress;
                }

                props.Add(new TiledWorldRenderer.MapPropPlacement(
                    metadata.PropType,
                    topLeft,
                    metadata.IsUnderwater,
                    metadata.ReachesSurface,
                    suppressOcclusion,
                    rotationRadians));
            }
        }

        return props.ToArray();
    }

    // ── Colliders ─────────────────────────────────────────────────────────────

    internal static Rectangle[] LoadColliderBounds(XElement mapElement)
    {
        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            var groupName = objectGroupElement.Attribute("name")?.Value;
            if (!string.Equals(groupName, ColliderLayerName, StringComparison.Ordinal))
            {
                continue;
            }

            var rects = new List<Rectangle>();
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                if (objectElement.Attribute("gid") is not null)
                {
                    continue;
                }

                var x = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "x"), CultureInfo.InvariantCulture);
                var y = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "y"), CultureInfo.InvariantCulture);
                var width = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "width"), CultureInfo.InvariantCulture);
                var height = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "height"), CultureInfo.InvariantCulture);

                rects.Add(new Rectangle(
                    (int)MathF.Round(x),
                    (int)MathF.Round(y),
                    (int)MathF.Round(width),
                    (int)MathF.Round(height)));
            }

            return rects.ToArray();
        }

        return Array.Empty<Rectangle>();
    }

    // ── Zone triggers ─────────────────────────────────────────────────────────

    internal static ZoneTriggerData[] LoadZoneTriggers(XElement mapElement)
    {
        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            var groupName = objectGroupElement.Attribute("name")?.Value;
            if (!string.Equals(groupName, ZoneTriggerLayerName, StringComparison.Ordinal))
            {
                continue;
            }

            var triggers = new List<ZoneTriggerData>();
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                // Zone triggers are plain rectangles (no gid).
                if (objectElement.Attribute("gid") is not null)
                {
                    continue;
                }

                var targetMap = TmxXmlHelpers.GetTileProperty(objectElement, TargetMapPropertyName);
                var targetSpawnId = TmxXmlHelpers.GetTileProperty(objectElement, TargetSpawnIdPropertyName);
                if (targetMap is null)
                {
                    continue;
                }

                var x = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "x"), CultureInfo.InvariantCulture);
                var y = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "y"), CultureInfo.InvariantCulture);
                var width = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "width"), CultureInfo.InvariantCulture);
                var height = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "height"), CultureInfo.InvariantCulture);

                triggers.Add(new ZoneTriggerData(
                    new Rectangle(
                        (int)MathF.Round(x),
                        (int)MathF.Round(y),
                        (int)MathF.Round(width),
                        (int)MathF.Round(height)),
                    targetMap,
                    targetSpawnId ?? "default"));
            }

            return triggers.ToArray();
        }

        return Array.Empty<ZoneTriggerData>();
    }

    // ── Spawn points ──────────────────────────────────────────────────────────

    internal static SpawnPointData[] LoadSpawnPoints(XElement mapElement)
    {
        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            var groupName = objectGroupElement.Attribute("name")?.Value;
            if (!string.Equals(groupName, SpawnPointLayerName, StringComparison.Ordinal))
            {
                continue;
            }

            var points = new List<SpawnPointData>();
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                // Spawn points are named points (no gid, no width/height required).
                if (objectElement.Attribute("gid") is not null)
                {
                    continue;
                }

                var name = objectElement.Attribute("name")?.Value;
                if (name is null)
                {
                    continue;
                }

                var x = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "x"), CultureInfo.InvariantCulture);
                var y = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "y"), CultureInfo.InvariantCulture);

                points.Add(new SpawnPointData(name, new Vector2(x, y)));
            }

            return points.ToArray();
        }

        return Array.Empty<SpawnPointData>();
    }

    // ── Fishing zones ─────────────────────────────────────────────────────────

    internal static FishingZoneData[] LoadFishingZones(XElement mapElement)
    {
        foreach (var objectGroupElement in mapElement.Elements("objectgroup"))
        {
            var groupName = objectGroupElement.Attribute("name")?.Value;
            if (!string.Equals(groupName, FishingZoneLayerName, StringComparison.Ordinal))
            {
                continue;
            }

            var zones = new List<FishingZoneData>();
            foreach (var objectElement in objectGroupElement.Elements("object"))
            {
                if (objectElement.Attribute("gid") is not null)
                {
                    continue;
                }

                var x = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "x"), CultureInfo.InvariantCulture);
                var y = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "y"), CultureInfo.InvariantCulture);
                var width = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "width"), CultureInfo.InvariantCulture);
                var height = float.Parse(TmxXmlHelpers.GetRequiredStringAttribute(objectElement, "height"), CultureInfo.InvariantCulture);

                var facingStr = TmxXmlHelpers.GetTileProperty(objectElement, FacingDirectionPropertyName);
                var facing = ParseFacingDirection(facingStr);

                zones.Add(new FishingZoneData(
                    new Rectangle(
                        (int)MathF.Round(x),
                        (int)MathF.Round(y),
                        (int)MathF.Round(width),
                        (int)MathF.Round(height)),
                    facing));
            }

            return zones.ToArray();
        }

        return Array.Empty<FishingZoneData>();
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    internal static FacingDirection ParseFacingDirection(string? value)
    {
        if (value is null)
        {
            return FacingDirection.Down;
        }

        return value.ToLowerInvariant() switch
        {
            "down" => FacingDirection.Down,
            "up" => FacingDirection.Up,
            "left" => FacingDirection.Left,
            "right" => FacingDirection.Right,
            _ => FacingDirection.Down,
        };
    }
}

/// <summary>
/// TSX-level metadata for a single prop tile, used only during map loading.
/// </summary>
internal readonly record struct PropTileMetadata(
    string PropType,
    bool IsUnderwater,
    bool ReachesSurface,
    bool SuppressOcclusion);
