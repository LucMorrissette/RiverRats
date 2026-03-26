using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Components;
using RiverRats.Data;
using RiverRats.Game.Systems;

#nullable enable

namespace RiverRats.Game.UI;

/// <summary>
/// Renders the forest survival HUD: health hearts, XP bar, level, wave counter,
/// and wave-status banners. Drawn in screen-space (no camera transform).
/// </summary>
internal sealed class ForestHudRenderer
{
    private const int Padding = 8;
    private const int BarHeight = 8;
    private const int BarWidth = 100;
    private const int HeartSize = 12;
    private const int HeartSpacing = 2;

    private static readonly Color HeartFilled = new(220, 40, 40);
    private static readonly Color HeartEmpty = new(80, 20, 20);
    private static readonly Color XpBarBackground = new(50, 50, 50);
    private static readonly Color XpBarFill = new(240, 200, 40);
    private static readonly Color BannerColor = new(255, 255, 200);

    /// <summary>
    /// Draws the forest survival HUD.
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch (already Begin'd).</param>
    /// <param name="font">Font for text rendering (FontStashSharp SpriteFontBase).</param>
    /// <param name="pixel">1×1 white pixel texture for drawing rectangles.</param>
    /// <param name="health">Player health component.</param>
    /// <param name="stats">Player combat stats (XP, level).</param>
    /// <param name="waveNumber">Current wave number (1-based).</param>
    /// <param name="waveState">Current wave state for display.</param>
    /// <param name="sceneScale">Scene scaling factor.</param>
    /// <param name="screenWidth">Window width in pixels.</param>
    /// <param name="screenHeight">Window height in pixels.</param>
    internal void Draw(SpriteBatch spriteBatch, SpriteFontBase font, Texture2D pixel,
        Health health, PlayerCombatStats stats, int waveNumber, WaveState waveState,
        int sceneScale, int screenWidth, int screenHeight)
    {
        int pad = Padding * sceneScale;
        int heartSize = HeartSize * sceneScale;
        int heartSpacing = HeartSpacing * sceneScale;

        // --- Row 1: Health hearts ---
        int heartY = pad;
        for (var i = 0; i < health.MaxHp; i++)
        {
            int heartX = pad + i * (heartSize + heartSpacing);
            var color = i < health.CurrentHp ? HeartFilled : HeartEmpty;
            spriteBatch.Draw(pixel, new Rectangle(heartX, heartY, heartSize, heartSize), color);
        }

        // --- Row 2: XP bar with level label ---
        int barHeight = BarHeight * sceneScale;
        int barWidth = BarWidth * sceneScale;
        int barY = heartY + heartSize + pad / 2;

        // Background
        spriteBatch.Draw(pixel, new Rectangle(pad, barY, barWidth, barHeight), XpBarBackground);

        // Fill
        float xpFraction = stats.XpToNextLevel > 0
            ? MathHelper.Clamp((float)stats.Xp / stats.XpToNextLevel, 0f, 1f)
            : 0f;
        int fillWidth = (int)(barWidth * xpFraction);
        if (fillWidth > 0)
            spriteBatch.Draw(pixel, new Rectangle(pad, barY, fillWidth, barHeight), XpBarFill);

        // Level label
        string levelText = $"Lv.{stats.Level}";
        int labelX = pad + barWidth + pad / 2;
        int labelY = barY + (barHeight - (int)font.LineHeight) / 2;
        spriteBatch.DrawString(font, levelText, new Vector2(labelX, labelY), Color.White);

        // --- Top-right: Wave counter ---
        string waveText = $"Wave {waveNumber}/{WaveManager.TotalWaves}";
        var waveSize = font.MeasureString(waveText);
        int waveX = screenWidth - (int)waveSize.X - pad;
        int waveY = pad;
        spriteBatch.DrawString(font, waveText, new Vector2(waveX, waveY), Color.White);

        // --- Center: Wave status banner ---
        string? bannerText = waveState switch
        {
            WaveState.Cleared => $"Wave {waveNumber} Complete!",
            WaveState.Intermission => $"Wave {waveNumber} Complete!",
            WaveState.AllWavesComplete => "Victory!",
            _ => null
        };

        if (bannerText != null)
        {
            var bannerSize = font.MeasureString(bannerText);
            int bannerX = (screenWidth - (int)bannerSize.X) / 2;
            int bannerY = (screenHeight - (int)bannerSize.Y) / 2;
            spriteBatch.DrawString(font, bannerText, new Vector2(bannerX, bannerY), BannerColor);
        }
    }
}
