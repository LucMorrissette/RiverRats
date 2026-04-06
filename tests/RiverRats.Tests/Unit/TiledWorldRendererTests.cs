using RiverRats.Game.World;
using Microsoft.Xna.Framework;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for TMX water layer grouping rules.
/// </summary>
public class TiledWorldRendererTests
{
    private const int RiverbedVariantCount = 16;
    private const int ShorelineVariantCount = 8;

    [Theory]
    [InlineData("Water/Bottom")]
    [InlineData("water/Surface")]
    [InlineData("WATER/Fish")]
    public void IsWaterLayerName__WaterPrefixMatch__ReturnsTrue(string layerName)
    {
        var isWaterLayer = TiledWorldRenderer.IsWaterLayerName(layerName);

        Assert.True(isWaterLayer);
    }

    [Theory]
    [InlineData("Ground")]
    [InlineData("Props")]
    [InlineData("UnderWater")]
    public void IsWaterLayerName__NoWaterPrefix__ReturnsFalse(string layerName)
    {
        var isWaterLayer = TiledWorldRenderer.IsWaterLayerName(layerName);

        Assert.False(isWaterLayer);
    }

    [Fact]
    public void HasAnyWaterTileAt__WhenAnyWaterLayerContainsTile__ReturnsTrue()
    {
        var waterLayers = new[]
        {
            new[] { 0, 0, 0, 0 },
            new[] { 0, 24, 0, 0 },
        };

        var hasWater = TiledWorldRenderer.HasAnyWaterTileAt(waterLayers, 1);

        Assert.True(hasWater);
    }

    [Fact]
    public void HasAnyWaterTileAt__WhenAllWaterLayersEmpty__ReturnsFalse()
    {
        var waterLayers = new[]
        {
            new[] { 0, 0, 0, 0 },
            new[] { 0, 0, 0, 0 },
        };

        var hasWater = TiledWorldRenderer.HasAnyWaterTileAt(waterLayers, 2);

        Assert.False(hasWater);
    }

    [Fact]
    public void HasAnyWaterTileAt__WhenTileIndexOutOfRange__ReturnsFalse()
    {
        var waterLayers = new[]
        {
            new[] { 24, 0 },
        };

        var hasWater = TiledWorldRenderer.HasAnyWaterTileAt(waterLayers, 5);

        Assert.False(hasWater);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 7)]
    [InlineData(9, 4)]
    [InlineData(15, 12)]
    public void PickRiverbedVariantIndex__SameCoordinates__ReturnsStableInRangeVariant(int x, int y)
    {
        var first = TiledWorldRenderer.PickRiverbedVariantIndex(x, y, RiverbedVariantCount);
        var second = TiledWorldRenderer.PickRiverbedVariantIndex(x, y, RiverbedVariantCount);

        Assert.Equal(first, second);
        Assert.InRange(first, 0, RiverbedVariantCount - 1);
    }

    [Fact]
    public void PickRiverbedVariantIndex__SampleGrid__UsesAllRiverbedVariants()
    {
        var seenIndices = new HashSet<int>();

        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                seenIndices.Add(TiledWorldRenderer.PickRiverbedVariantIndex(x, y, RiverbedVariantCount));
            }
        }

        Assert.Equal(RiverbedVariantCount, seenIndices.Count);
    }

    [Fact]
    public void PickRiverbedVariantIndex__TilesWithinSameMacroRegion__FavorSameVariantCategory()
    {
        var categories = new HashSet<int>();

        for (var y = 6; y < 9; y++)
        {
            for (var x = 8; x < 12; x++)
            {
                var variantIndex = TiledWorldRenderer.PickRiverbedVariantIndex(x, y, RiverbedVariantCount);
                categories.Add(variantIndex % 4);
            }
        }

        Assert.InRange(categories.Count, 1, 2);
    }

    [Theory]
    [InlineData(384f, 352f, 32f, 384f, 320f)]
    [InlineData(128f, 576f, 64f, 128f, 512f)]
    [InlineData(609.333f, 619.333f, 64f, 609f, 555f)]
    public void GetTileObjectTopLeft__BottomAlignedTileObject__ReturnsTopLeft(float x, float y, float height, float expectedX, float expectedY)
    {
        var topLeft = TiledWorldRenderer.GetTileObjectTopLeft(x, y, height);

        Assert.Equal(new Vector2(expectedX, expectedY), topLeft);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(3, 7)]
    [InlineData(9, 4)]
    [InlineData(15, 12)]
    public void PickShorelineVariantIndex__SameCoordinates__ReturnsStableInRangeVariant(int x, int y)
    {
        var first = TiledWorldRenderer.PickShorelineVariantIndex(x, y, ShorelineVariantCount);
        var second = TiledWorldRenderer.PickShorelineVariantIndex(x, y, ShorelineVariantCount);

        Assert.Equal(first, second);
        Assert.InRange(first, 0, ShorelineVariantCount - 1);
    }

    [Fact]
    public void PickShorelineVariantIndex__SampleGrid__UsesAllShorelineVariants()
    {
        var seenIndices = new HashSet<int>();

        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                seenIndices.Add(TiledWorldRenderer.PickShorelineVariantIndex(x, y, ShorelineVariantCount));
            }
        }

        Assert.Equal(ShorelineVariantCount, seenIndices.Count);
    }
}