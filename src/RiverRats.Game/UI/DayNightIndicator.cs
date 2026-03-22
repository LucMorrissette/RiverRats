using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.UI;

/// <summary>
/// Renders a 16×16 procedural day/night indicator showing the sun or moon
/// arcing across a sky that blends color based on the current game hour.
/// </summary>
public sealed class DayNightIndicator
{
    /// <summary>Size of the indicator widget in pixels.</summary>
    public const int Size = 16;

    /// <summary>Game hour when the sun begins to rise (appears at left horizon).</summary>
    private const float SunriseHour = 5.0f;

    /// <summary>Game hour when the sun finishes setting (disappears at right horizon).</summary>
    private const float SunsetHour = 21.0f;

    /// <summary>Duration of the sun's visible arc in game hours.</summary>
    private const float SunArcDuration = SunsetHour - SunriseHour;

    /// <summary>Game hour when the moon begins to rise.</summary>
    private const float MoonriseHour = 20.0f;

    /// <summary>Game hour when the moon finishes setting (next morning).</summary>
    private const float MoonsetHour = 7.0f;

    /// <summary>Duration of the moon's visible arc in game hours (wraps midnight).</summary>
    private const float MoonArcDuration = (24.0f - MoonriseHour) + MoonsetHour;

    /// <summary>Radius of the sun/moon circle in pixels.</summary>
    private const int CelestialRadius = 2;

    /// <summary>Height of the ground strip at the bottom.</summary>
    private const int GroundHeight = 3;

    /// <summary>Top margin — celestial bodies won't go higher than this.</summary>
    private const int SkyTopMargin = 2;

    // Sky colors for the background gradient
    private static readonly Color SkyDay = new Color(135, 206, 235);      // Light sky blue
    private static readonly Color SkyDawn = new Color(255, 164, 96);      // Warm orange
    private static readonly Color SkyDusk = new Color(255, 128, 80);      // Deep orange-red
    private static readonly Color SkyNight = new Color(15, 15, 50);       // Dark navy
    private static readonly Color GroundDay = new Color(76, 128, 56);     // Green grass
    private static readonly Color GroundNight = new Color(20, 40, 20);    // Dark grass
    private static readonly Color SunColor = new Color(255, 220, 50);     // Warm yellow
    private static readonly Color SunGlowColor = new Color(255, 200, 50, 80); // Subtle glow
    private static readonly Color MoonColor = new Color(220, 230, 255);   // Pale blue-white
    private static readonly Color MoonGlowColor = new Color(180, 200, 255, 60); // Subtle glow
    private static readonly Color StarColor = new Color(255, 255, 255, 180);

    // Pre-computed star positions (pixel offsets within the 16×16 area)
    private static readonly Point[] StarPositions = new[]
    {
        new Point(2, 2), new Point(4, 1), new Point(7, 3), new Point(10, 1),
        new Point(13, 2), new Point(3, 5), new Point(9, 4), new Point(12, 6),
        new Point(5, 7), new Point(14, 4),
    };

    /// <summary>
    /// Draws the day/night indicator at the specified position.
    /// </summary>
    /// <param name="spriteBatch">The active sprite batch.</param>
    /// <param name="pixelTexture">A 1×1 white pixel texture.</param>
    /// <param name="gameHour">Current game hour (0.0–24.0).</param>
    /// <param name="position">Top-left position of the indicator.</param>
    /// <param name="scale">Integer scale factor from virtual to window resolution.</param>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, float gameHour, Vector2 position, int scale = 1)
    {
        int x = (int)position.X;
        int y = (int)position.Y;

        DrawSky(spriteBatch, pixelTexture, gameHour, x, y, scale);
        DrawGround(spriteBatch, pixelTexture, gameHour, x, y, scale);
        DrawStars(spriteBatch, pixelTexture, gameHour, x, y, scale);
        DrawCelestialBodies(spriteBatch, pixelTexture, gameHour, x, y, scale);

        // 1px border
        DrawBorder(spriteBatch, pixelTexture, x, y, scale);
    }

    private static void DrawSky(SpriteBatch spriteBatch, Texture2D pixel, float gameHour, int x, int y, int scale)
    {
        Color skyColor = GetSkyColor(gameHour);
        int size = Size * scale;
        int groundHeight = GroundHeight * scale;
        int skyHeight = size - groundHeight;
        spriteBatch.Draw(pixel, new Rectangle(x, y, size, skyHeight), skyColor);
    }

    private static void DrawGround(SpriteBatch spriteBatch, Texture2D pixel, float gameHour, int x, int y, int scale)
    {
        Color groundColor = GetGroundColor(gameHour);
        int size = Size * scale;
        int groundHeight = GroundHeight * scale;
        int groundY = y + size - groundHeight;
        spriteBatch.Draw(pixel, new Rectangle(x, groundY, size, groundHeight), groundColor);
    }

    private static void DrawStars(SpriteBatch spriteBatch, Texture2D pixel, float gameHour, int x, int y, int scale)
    {
        float nightAmount = GetNightAmount(gameHour);
        if (nightAmount <= 0.05f) return;

        Color starTint = StarColor * nightAmount;
        int size = Size * scale;
        int groundHeight = GroundHeight * scale;
        int skyHeight = size - groundHeight;

        foreach (var star in StarPositions)
        {
            if (star.Y * scale < skyHeight)
            {
                spriteBatch.Draw(pixel, new Rectangle(x + star.X * scale, y + star.Y * scale, scale, scale), starTint);
            }
        }
    }

    private static void DrawCelestialBodies(SpriteBatch spriteBatch, Texture2D pixel, float gameHour, int x, int y, int scale)
    {
        float sunProgress = GetSunProgress(gameHour);
        float moonProgress = GetMoonProgress(gameHour);

        int size = Size * scale;
        int groundHeight = GroundHeight * scale;
        int celestialRadius = CelestialRadius * scale;
        int skyTopMargin = SkyTopMargin * scale;

        int skyHeight = size - groundHeight;
        int arcBottom = skyHeight - celestialRadius;
        int arcTop = skyTopMargin + celestialRadius;
        int arcHeight = arcBottom - arcTop;

        // Draw moon behind sun
        if (moonProgress >= 0f && moonProgress <= 1f)
        {
            Vector2 moonPos = ComputeArcPosition(moonProgress, x, y, arcTop, arcHeight, size, celestialRadius);
            DrawFilledCircle(spriteBatch, pixel, moonPos, celestialRadius - scale, MoonGlowColor);
            DrawFilledCircle(spriteBatch, pixel, moonPos, celestialRadius - scale * 2, MoonColor);
        }

        if (sunProgress >= 0f && sunProgress <= 1f)
        {
            Vector2 sunPos = ComputeArcPosition(sunProgress, x, y, arcTop, arcHeight, size, celestialRadius);
            DrawFilledCircle(spriteBatch, pixel, sunPos, celestialRadius, SunGlowColor);
            DrawFilledCircle(spriteBatch, pixel, sunPos, celestialRadius - scale, SunColor);
        }
    }

    /// <summary>
    /// Computes a position along a parabolic arc.
    /// Progress 0 = left horizon, 0.5 = zenith (top), 1 = right horizon.
    /// </summary>
    private static Vector2 ComputeArcPosition(float progress, int originX, int originY, int arcTop, int arcHeight, int size, int celestialRadius)
    {
        float px = originX + celestialRadius + progress * (size - celestialRadius * 2);

        // Parabolic arc: peaks at progress 0.5
        float arcFactor = 4f * progress * (1f - progress);
        float py = originY + arcTop + arcHeight * (1f - arcFactor);

        return new Vector2(px, py);
    }

    /// <summary>
    /// Returns the sun's progress (0–1) across its arc, or -1 if below horizon.
    /// </summary>
    private static float GetSunProgress(float gameHour)
    {
        if (gameHour < SunriseHour || gameHour > SunsetHour)
            return -1f;

        return (gameHour - SunriseHour) / SunArcDuration;
    }

    /// <summary>
    /// Returns the moon's progress (0–1) across its arc, or -1 if below horizon.
    /// </summary>
    private static float GetMoonProgress(float gameHour)
    {
        float hoursIntoArc;

        if (gameHour >= MoonriseHour)
        {
            hoursIntoArc = gameHour - MoonriseHour;
        }
        else if (gameHour < MoonsetHour)
        {
            hoursIntoArc = (24f - MoonriseHour) + gameHour;
        }
        else
        {
            return -1f;
        }

        return hoursIntoArc / MoonArcDuration;
    }

    /// <summary>
    /// Returns a 0–1 value indicating how "night-like" the current hour is.
    /// Used for star visibility.
    /// </summary>
    private static float GetNightAmount(float gameHour)
    {
        // Full night: 22–4, transition: 19–22 (dusk), 4–7 (dawn)
        if (gameHour >= 22f || gameHour < 4f) return 1f;
        if (gameHour >= 7f && gameHour < 19f) return 0f;

        if (gameHour >= 19f)
            return (gameHour - 19f) / 3f; // Dusk fade in

        // 4–7: Dawn fade out
        return 1f - (gameHour - 4f) / 3f;
    }

    private static Color GetSkyColor(float gameHour)
    {
        // Dawn: 5–7, Day: 7–19, Dusk: 19–21, Night: 21–5
        if (gameHour >= 7f && gameHour < 19f) return SkyDay;
        if (gameHour >= 21f || gameHour < 5f) return SkyNight;

        if (gameHour >= 5f && gameHour < 6f)
            return Color.Lerp(SkyNight, SkyDawn, gameHour - 5f);
        if (gameHour >= 6f && gameHour < 7f)
            return Color.Lerp(SkyDawn, SkyDay, gameHour - 6f);
        if (gameHour >= 19f && gameHour < 20f)
            return Color.Lerp(SkyDay, SkyDusk, gameHour - 19f);

        // 20–21
        return Color.Lerp(SkyDusk, SkyNight, gameHour - 20f);
    }

    private static Color GetGroundColor(float gameHour)
    {
        float nightAmount = GetNightAmount(gameHour);
        return Color.Lerp(GroundDay, GroundNight, nightAmount);
    }

    /// <summary>
    /// Draws a filled circle using pixel rectangles (diamond/cross approximation for small sizes).
    /// </summary>
    private static void DrawFilledCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, int radius, Color color)
    {
        int cx = (int)center.X;
        int cy = (int)center.Y;

        for (int dy = -radius; dy <= radius; dy++)
        {
            int halfWidth = (int)MathF.Sqrt(radius * radius - dy * dy);
            spriteBatch.Draw(pixel,
                new Rectangle(cx - halfWidth, cy + dy, halfWidth * 2 + 1, 1),
                color);
        }
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, int x, int y, int scale)
    {
        Color borderColor = new Color(0, 0, 0, 160);
        int size = Size * scale;
        int thickness = scale;
        // Top
        spriteBatch.Draw(pixel, new Rectangle(x, y, size, thickness), borderColor);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(x, y + size - thickness, size, thickness), borderColor);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(x, y, thickness, size), borderColor);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(x + size - thickness, y, thickness, size), borderColor);
    }
}
