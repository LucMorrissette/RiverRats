using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.UI;

/// <summary>
/// Renders the HUD overlay with a semi-transparent panel showing
/// the day/night indicator and 12-hour formatted time text.
/// </summary>
public sealed class HudRenderer
{
    /// <summary>Horizontal padding inside the HUD panel.</summary>
    private const int PanelPaddingX = 4;

    /// <summary>Vertical padding inside the HUD panel.</summary>
    private const int PanelPaddingY = 3;

    /// <summary>Margin from screen edges to the panel edges.</summary>
    private const int ScreenMargin = 3;

    /// <summary>Corner radius for the rounded panel background.</summary>
    private const int CornerRadius = 2;

    /// <summary>Horizontal gap between the indicator and the time text.</summary>
    private const int IndicatorTextGap = 3;

    private static readonly Color PanelColor = new(0, 0, 0, 140);
    private static readonly Color BorderColor = new(200, 200, 200, 180);

    private readonly DayNightIndicator _dayNightIndicator = new();

    /// <summary>
    /// Draws the time-of-day HUD panel.
    /// </summary>
    /// <param name="spriteBatch">The active sprite batch (screen-space, no camera transform).</param>
    /// <param name="font">Font for the time text, pre-scaled for window resolution.</param>
    /// <param name="pixelTexture">A 1×1 white pixel texture for drawing rectangles.</param>
    /// <param name="gameHour">Current game hour (0.0–24.0).</param>
    /// <param name="scale">Integer scale factor from virtual to window resolution.</param>
    public void Draw(SpriteBatch spriteBatch, SpriteFontBase font, Texture2D pixelTexture, float gameHour, int scale = 1)
    {
        int lineHeight = (int)font.LineHeight;
        int paddingX = PanelPaddingX * scale;
        int paddingY = PanelPaddingY * scale;
        int margin = ScreenMargin * scale;
        int gap = IndicatorTextGap * scale;
        int indicatorSize = DayNightIndicator.Size * scale;

        // Measure content: indicator + gap + widest possible time string
        var maxTimeSize = font.MeasureString("12:30 PM");
        int contentWidth = indicatorSize + gap + (int)maxTimeSize.X;
        int contentHeight = Math.Max(indicatorSize, lineHeight);

        int panelWidth = contentWidth + paddingX * 2;
        int panelHeight = contentHeight + paddingY * 2;

        var panelRect = new Rectangle(margin, margin, panelWidth, panelHeight);
        DrawPanel(spriteBatch, pixelTexture, panelRect, scale);

        int contentX = panelRect.X + paddingX;
        int contentY = panelRect.Y + paddingY;

        _dayNightIndicator.Draw(spriteBatch, pixelTexture, gameHour, new Vector2(contentX, contentY), scale);

        string timeText = FormatTime(gameHour);
        int textX = contentX + indicatorSize + gap;
        int textY = contentY + (indicatorSize - lineHeight) / 2;
        spriteBatch.DrawString(font, timeText, new Vector2(textX, textY), Color.White);
    }

    /// <summary>
    /// Formats a game hour float (0.0–24.0) into a 12-hour time string
    /// with 30-minute granularity (e.g., "6:00 AM", "6:30 AM").
    /// </summary>
    public static string FormatTime(float gameHour)
    {
        var totalMinutes = (int)(gameHour * 60);
        var hour24 = totalMinutes / 60;
        var minute = (totalMinutes % 60 / 30) * 30;

        var amPm = hour24 < 12 ? "AM" : "PM";
        var hour12 = hour24 % 12;

        if (hour12 == 0)
        {
            hour12 = 12;
        }

        return $"{hour12}:{minute:D2} {amPm}";
    }

    /// <summary>
    /// Draws a semi-transparent rounded panel background with border.
    /// </summary>
    private static void DrawPanel(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int scale)
    {
        int r = CornerRadius * scale;

        // Main body (full width, inset vertically by corner radius)
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X, rect.Y + r, rect.Width, rect.Height - r * 2),
            PanelColor);

        // Top strip (inset horizontally by corner radius)
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, r),
            PanelColor);

        // Bottom strip (inset horizontally by corner radius)
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + r, rect.Bottom - r, rect.Width - r * 2, r),
            PanelColor);

        // Corner fills (1px inset for a subtle rounding effect)
        int inset = 1 * scale;
        // Top-left corner
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Y + inset, r - inset, r - inset),
            PanelColor);
        // Top-right corner
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - r, rect.Y + inset, r - inset, r - inset),
            PanelColor);
        // Bottom-left corner
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Bottom - r, r - inset, r - inset),
            PanelColor);
        // Bottom-right corner
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - r, rect.Bottom - r, r - inset, r - inset),
            PanelColor);

        // Border
        DrawBorder(spriteBatch, pixelTexture, rect, BorderColor, scale);
    }

    /// <summary>
    /// Draws a 1px solid border that follows the rounded panel shape.
    /// </summary>
    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture,
        Rectangle rect, Color color, int scale)
    {
        int r = CornerRadius * scale;
        int inset = 1 * scale;

        // Top edge (inset by corner radius)
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, inset), color);
        // Bottom edge
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + r, rect.Bottom - inset, rect.Width - r * 2, inset), color);
        // Left edge (inset by corner radius)
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X, rect.Y + r, inset, rect.Height - r * 2), color);
        // Right edge
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - inset, rect.Y + r, inset, rect.Height - r * 2), color);

        // Corner edge pixels (scaled inset to match the rounded fill)
        // Top-left
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Y + inset, r - inset, inset), color);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Y + inset, inset, r - inset), color);
        // Top-right
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - r, rect.Y + inset, r - inset, inset), color);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - inset - inset + inset, rect.Y + inset, inset, r - inset), color);
        // Bottom-left
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Bottom - inset - inset + inset, r - inset, inset), color);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.X + inset, rect.Bottom - r, inset, r - inset), color);
        // Bottom-right
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - r, rect.Bottom - inset - inset + inset, r - inset, inset), color);
        spriteBatch.Draw(pixelTexture,
            new Rectangle(rect.Right - inset - inset + inset, rect.Bottom - r, inset, r - inset), color);
    }
}
