using System;

namespace RiverRats.Game.Util;

/// <summary>
/// Generates 2D Perlin noise with tileable output.
/// Based on Ken Perlin's improved noise algorithm, extended with periodic wrapping
/// so the output tiles seamlessly at the specified period.
/// </summary>
public static class PerlinNoise
{
    private static readonly int[] Permutation =
    {
        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,
        140,36,103,30,69,142,8,99,37,240,21,10,23,190,6,148,
        247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,
        57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,
        74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,
        60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
        65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,
        200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,
        52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,
        207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,
        119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,
        129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,
        218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,
        81,51,145,235,249,14,239,107,49,192,214,31,181,199,106,157,
        184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,
        222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    // Doubled permutation table to avoid wrapping index lookups.
    private static readonly int[] P = new int[512];

    static PerlinNoise()
    {
        for (var i = 0; i < 512; i++)
        {
            P[i] = Permutation[i & 255];
        }
    }

    /// <summary>
    /// Samples 2D Perlin noise that tiles at the specified period.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="period">
    /// Tile period in grid cells. The output repeats every <paramref name="period"/> units
    /// in both X and Y. Must be greater than zero.
    /// </param>
    /// <returns>A noise value roughly in the range [-1, 1].</returns>
    public static float Sample(float x, float y, int period)
    {
        // Grid cell coordinates, wrapped to period.
        var xi = ((int)MathF.Floor(x)) % period;
        var yi = ((int)MathF.Floor(y)) % period;
        if (xi < 0) xi += period;
        if (yi < 0) yi += period;

        // Fractional position inside the cell.
        var xf = x - MathF.Floor(x);
        var yf = y - MathF.Floor(y);

        // Fade curves for smooth interpolation.
        var u = Fade(xf);
        var v = Fade(yf);

        // Wrapped neighbor indices.
        var x1 = (xi + 1) % period;
        var y1 = (yi + 1) % period;

        // Hash corners.
        var aa = P[P[xi] + yi];
        var ab = P[P[xi] + y1];
        var ba = P[P[x1] + yi];
        var bb = P[P[x1] + y1];

        // Gradient dot products and bilinear interpolation.
        var lerpX1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        var lerpX2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

        return Lerp(lerpX1, lerpX2, v);
    }

    /// <summary>
    /// Generates a tileable 2D noise map using fractional Brownian motion (fBm).
    /// Multiple octaves of tileable Perlin noise produce natural, detailed patterns.
    /// </summary>
    /// <param name="width">Output map width in pixels.</param>
    /// <param name="height">Output map height in pixels.</param>
    /// <param name="baseFrequency">
    /// Base frequency (number of noise cells across the texture). Determines the size of
    /// the largest features. For clouds, 3–5 creates realistic large formations.
    /// </param>
    /// <param name="octaves">
    /// Number of noise layers to combine. Each adds finer detail at half the amplitude.
    /// 4–5 octaves gives fluffy cloud edges.
    /// </param>
    /// <param name="persistence">
    /// Amplitude multiplier per octave (0–1). Higher values make fine detail more prominent.
    /// 0.5 is typical for natural patterns.
    /// </param>
    /// <returns>
    /// A flat array of size <paramref name="width"/> × <paramref name="height"/> with noise
    /// values normalized to the [0, 1] range.
    /// </returns>
    public static float[] GenerateTileableNoiseMap(
        int width,
        int height,
        int baseFrequency,
        int octaves,
        float persistence)
    {
        var map = new float[width * height];
        var maxAmplitude = 0f;
        var amplitude = 1f;

        // Accumulate max possible amplitude for normalization.
        for (var o = 0; o < octaves; o++)
        {
            maxAmplitude += amplitude;
            amplitude *= persistence;
        }

        for (var py = 0; py < height; py++)
        {
            for (var px = 0; px < width; px++)
            {
                var noiseValue = 0f;
                amplitude = 1f;
                var frequency = baseFrequency;

                for (var o = 0; o < octaves; o++)
                {
                    var sampleX = (float)px / width * frequency;
                    var sampleY = (float)py / height * frequency;

                    noiseValue += Sample(sampleX, sampleY, frequency) * amplitude;

                    amplitude *= persistence;
                    frequency *= 2;
                }

                // Normalize from [-maxAmplitude, maxAmplitude] to [0, 1].
                map[(py * width) + px] = (noiseValue / maxAmplitude + 1f) * 0.5f;
            }
        }

        return map;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float Grad(int hash, float x, float y)
    {
        return (hash & 3) switch
        {
            0 => x + y,
            1 => -x + y,
            2 => x - y,
            _ => -x - y
        };
    }
}
