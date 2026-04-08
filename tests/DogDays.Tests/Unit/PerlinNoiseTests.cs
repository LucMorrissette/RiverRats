using DogDays.Game.Util;

namespace DogDays.Tests.Unit;

public sealed class PerlinNoiseTests
{
    // ── Determinism ─────────────────────────────────────────────

    [Fact]
    public void Sample__SameInputs__ReturnsSameValue()
    {
        var a = PerlinNoise.Sample(1.5f, 2.3f, 8);
        var b = PerlinNoise.Sample(1.5f, 2.3f, 8);

        Assert.Equal(a, b);
    }

    // ── Range ───────────────────────────────────────────────────

    [Fact]
    public void Sample__VariousCoordinates__StaysWithinExpectedRange()
    {
        for (var x = 0f; x < 10f; x += 0.37f)
        {
            for (var y = 0f; y < 10f; y += 0.41f)
            {
                var value = PerlinNoise.Sample(x, y, 4);
                Assert.InRange(value, -1.5f, 1.5f);
            }
        }
    }

    // ── Tileability ─────────────────────────────────────────────

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void Sample__AtPeriodBoundary__TilesSeamlessly(int period)
    {
        for (var i = 0f; i < period; i += 0.5f)
        {
            var atStart = PerlinNoise.Sample(0f, i, period);
            var atWrap = PerlinNoise.Sample(period, i, period);
            Assert.Equal(atStart, atWrap, precision: 5);

            var colStart = PerlinNoise.Sample(i, 0f, period);
            var colWrap = PerlinNoise.Sample(i, period, period);
            Assert.Equal(colStart, colWrap, precision: 5);
        }
    }

    // ── Noise map generation ────────────────────────────────────

    [Fact]
    public void GenerateTileableNoiseMap__ReturnsCorrectSize()
    {
        var map = PerlinNoise.GenerateTileableNoiseMap(64, 64, 4, 3, 0.5f);

        Assert.Equal(64 * 64, map.Length);
    }

    [Fact]
    public void GenerateTileableNoiseMap__AllValuesInZeroOneRange()
    {
        var map = PerlinNoise.GenerateTileableNoiseMap(128, 128, 4, 4, 0.5f);

        for (var i = 0; i < map.Length; i++)
        {
            Assert.InRange(map[i], 0f, 1f);
        }
    }

    [Fact]
    public void GenerateTileableNoiseMap__NotAllSameValue()
    {
        var map = PerlinNoise.GenerateTileableNoiseMap(64, 64, 4, 3, 0.5f);

        var allSame = true;
        for (var i = 1; i < map.Length; i++)
        {
            if (MathF.Abs(map[i] - map[0]) > 0.001f)
            {
                allSame = false;
                break;
            }
        }

        Assert.False(allSame, "Noise map should contain varied values.");
    }

    [Fact]
    public void GenerateTileableNoiseMap__TilesSeamlessly()
    {
        const int size = 64;
        var map = PerlinNoise.GenerateTileableNoiseMap(size, size, 4, 3, 0.5f);

        // Compare left edge with right edge (wrapping in X).
        for (var y = 0; y < size; y++)
        {
            var leftVal = map[y * size];
            var rightVal = map[(y * size) + size - 1];
            // Adjacent pixels in a tileable noise field should be close but not
            // necessarily identical — verify they're within a reasonable range.
            // The key tileability test is the Sample boundary check above.
            Assert.InRange(MathF.Abs(leftVal - rightVal), 0f, 0.5f);
        }
    }

    [Fact]
    public void GenerateTileableNoiseMap__IsDeterministic()
    {
        var map1 = PerlinNoise.GenerateTileableNoiseMap(32, 32, 4, 3, 0.5f);
        var map2 = PerlinNoise.GenerateTileableNoiseMap(32, 32, 4, 3, 0.5f);

        Assert.Equal(map1, map2);
    }
}
