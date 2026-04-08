#nullable enable

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Systems;

namespace DogDays.Game.UI;

/// <summary>
/// Renders the NPC dialogue box using a 9-slice panel texture with typewriter-
/// revealed body text and a speaker name plate.
/// </summary>
/// <remarks>
/// The dialog box is drawn in screen space (no camera transform).  It sits
/// at the bottom of the virtual screen (480×270).
///
/// 9-slice layout for the 24×24 <c>dialog_box_9slice</c> texture:
/// <code>
///   corner = 6px  ←  SliceBorderPx constant
///   ┌──────┬──────────┬──────┐
///   │  TL  │ top edge │  TR  │  6 px
///   ├──────┼──────────┼──────┤
///   │  L   │  center  │  R   │  stretch
///   ├──────┼──────────┼──────┤
///   │  BL  │ bot edge │  BR  │  6 px
///   └──────┴──────────┴──────┘
///    6 px    stretch     6 px
/// </code>
/// </remarks>
public sealed class DialogBoxRenderer
{
    // ── Layout constants (virtual-pixel units, 480×270) ────────────────────
    private const int ScreenMarginX = 16;
    private const int ScreenMarginBottom = 10;
    private const int BoxHeight = 52;
    private const int TextPaddingX = 8;
    private const int TextPaddingTop = 7;
    private const int NamePlatePaddingX = 4;
    private const int NamePlatePaddingY = 1;
    private const int NamePlateOffsetX = 6;
    private const int ContinueIndicatorPaddingPx = 4;

    /// <summary>Source-texture corner size in texels.</summary>
    private const int SliceBorderPx = 6;

    private static readonly Color NamePlateColor = new(50, 35, 18, 230);
    private static readonly Color NamePlateTextColor = new(255, 235, 180);
    private static readonly Color BodyTextColor = new(48, 36, 22);
    private static readonly Color ContinueHintColor = new(120, 90, 50, 200);

    private readonly Texture2D _sliceTexture;
    private readonly Texture2D _pixelTexture;

    /// <summary>
    /// Creates a dialog box renderer.
    /// </summary>
    /// <param name="sliceTexture">The 24×24 nine-slice dialog box texture.</param>
    /// <param name="pixelTexture">A 1×1 white pixel texture used for solid colour fills.</param>
    public DialogBoxRenderer(Texture2D sliceTexture, Texture2D pixelTexture)
    {
        _sliceTexture = sliceTexture ?? throw new ArgumentNullException(nameof(sliceTexture));
        _pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));
    }

    /// <summary>
    /// Draws the dialog box if the supplied <paramref name="sequence"/> is active.
    /// Call this in screen-space (no camera transform applied to the SpriteBatch).
    /// </summary>
    /// <param name="spriteBatch">Active sprite batch without camera matrix.</param>
    /// <param name="sequence">The dialog sequence whose state drives the rendering.</param>
    /// <param name="font">Pre-scaled font for body text.</param>
    /// <param name="virtualWidth">Virtual screen width in pixels.</param>
    /// <param name="virtualHeight">Virtual screen height in pixels.</param>
    /// <param name="scale">Integer pixel scale (window px / virtual px).</param>
    public void Draw(SpriteBatch spriteBatch, DialogSequence sequence,
        SpriteFontBase font, int virtualWidth, int virtualHeight, int scale = 1)
    {
        if (!sequence.IsActive) return;

        var line = sequence.CurrentLine;
        if (line == null) return;

        int s = scale;
        int marginX = ScreenMarginX * s;
        int marginBottom = ScreenMarginBottom * s;
        int boxH = BoxHeight * s;
        int boxW = virtualWidth * s - marginX * 2;
        int boxX = marginX;
        int boxY = virtualHeight * s - marginBottom - boxH;

        var boxRect = new Rectangle(boxX, boxY, boxW, boxH);

        // 1. Draw 9-slice background panel.
        //    The displayed border width is the source border scaled up to match
        //    the window's pixel scale so the frame stays proportional.
        int displayedBorder = SliceBorderPx * s;
        Draw9Slice(spriteBatch, _sliceTexture, boxRect, displayedBorder);

        // 2. Draw speaker name plate above top-left of the box.
        var speakerName = line.Value.SpeakerName;
        if (!string.IsNullOrEmpty(speakerName))
        {
            DrawNamePlate(spriteBatch, font, speakerName, boxX, boxY, s);
        }

        // 3. Draw typewriter body text.
        var fullText = line.Value.Text;
        var visibleCount = Math.Min(sequence.VisibleCharCount, fullText.Length);
        var visibleText = fullText[..visibleCount];

        int textX = boxX + TextPaddingX * s;
        int textY = boxY + TextPaddingTop * s;
        spriteBatch.DrawString(font, visibleText,
            new Vector2(textX, textY), BodyTextColor);

        // 4. Draw ">" continue indicator when waiting for confirm.
        if (sequence.IsWaitingForConfirm)
        {
            const string indicator = ">";
            var indSize = font.MeasureString(indicator);
            int indX = boxX + boxW - (int)indSize.X - ContinueIndicatorPaddingPx * s;
            int indY = boxY + boxH - (int)indSize.Y - ContinueIndicatorPaddingPx * s;
            spriteBatch.DrawString(font, indicator, new Vector2(indX, indY), ContinueHintColor);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void DrawNamePlate(SpriteBatch sb, SpriteFontBase font,
        string name, int boxX, int boxY, int s)
    {
        var textSize = font.MeasureString(name);
        int padX = NamePlatePaddingX * s;
        int padY = NamePlatePaddingY * s;
        int plateW = (int)textSize.X + padX * 2;
        int plateH = (int)textSize.Y + padY * 2;
        int plateX = boxX + NamePlateOffsetX * s;
        int plateY = boxY - plateH;

        // Draw solid dark plate
        sb.Draw(_pixelTexture, new Rectangle(plateX, plateY, plateW, plateH), NamePlateColor);

        sb.DrawString(font, name,
            new Vector2(plateX + padX, plateY + padY),
            NamePlateTextColor);
    }

    /// <summary>
    /// Draws a 9-slice rectangle using the source texture.  The source corner
    /// size is always <see cref="SliceBorderPx"/> texels; <paramref name="borderPx"/>
    /// is the displayed size (already scaled).
    /// </summary>
    private static void Draw9Slice(SpriteBatch sb, Texture2D texture,
        Rectangle dest, int borderPx)
    {
        int tw = texture.Width;
        int th = texture.Height;
        int b = borderPx;
        int srcB = SliceBorderPx;

        var srcTL = new Rectangle(0,         0,         srcB,            srcB);
        var srcTM = new Rectangle(srcB,      0,         tw - srcB * 2,   srcB);
        var srcTR = new Rectangle(tw - srcB, 0,         srcB,            srcB);

        var srcML = new Rectangle(0,         srcB,      srcB,            th - srcB * 2);
        var srcMM = new Rectangle(srcB,      srcB,      tw - srcB * 2,   th - srcB * 2);
        var srcMR = new Rectangle(tw - srcB, srcB,      srcB,            th - srcB * 2);

        var srcBL = new Rectangle(0,         th - srcB, srcB,            srcB);
        var srcBM = new Rectangle(srcB,      th - srcB, tw - srcB * 2,   srcB);
        var srcBR = new Rectangle(tw - srcB, th - srcB, srcB,            srcB);

        int cx = dest.X + b;
        int cy = dest.Y + b;
        int cw = dest.Width - b * 2;
        int ch = dest.Height - b * 2;

        // Corners
        sb.Draw(texture, new Rectangle(dest.X,         dest.Y,          b,  b),  srcTL, Color.White);
        sb.Draw(texture, new Rectangle(dest.Right - b, dest.Y,          b,  b),  srcTR, Color.White);
        sb.Draw(texture, new Rectangle(dest.X,         dest.Bottom - b, b,  b),  srcBL, Color.White);
        sb.Draw(texture, new Rectangle(dest.Right - b, dest.Bottom - b, b,  b),  srcBR, Color.White);

        // Edges
        sb.Draw(texture, new Rectangle(cx,             dest.Y,          cw, b),  srcTM, Color.White);
        sb.Draw(texture, new Rectangle(cx,             dest.Bottom - b, cw, b),  srcBM, Color.White);
        sb.Draw(texture, new Rectangle(dest.X,         cy,              b,  ch), srcML, Color.White);
        sb.Draw(texture, new Rectangle(dest.Right - b, cy,              b,  ch), srcMR, Color.White);

        // Center fill
        sb.Draw(texture, new Rectangle(cx,             cy,              cw, ch), srcMM, Color.White);
    }
}

