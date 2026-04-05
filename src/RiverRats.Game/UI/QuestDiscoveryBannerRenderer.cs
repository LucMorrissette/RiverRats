#nullable enable

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Data;
using RiverRats.Game.Systems;

namespace RiverRats.Game.UI;

/// <summary>
/// Renders a high-visibility quest discovery banner in screen space.
/// </summary>
internal sealed class QuestDiscoveryBannerRenderer
{
    private const int PanelWidth = 340;
    private const int PanelHeight = 72;
    private const int TopMargin = 16;
    private const int AccentHeight = 6;
    private const int PaddingX = 16;
    private const int PaddingY = 12;
    private const int BadgePaddingX = 8;
    private const int BadgePaddingY = 4;
    private const int ShimmerCoreWidth = 20;
    private const int ShimmerGlowWidth = 44;

    private static readonly Color PanelColor = new(12, 16, 20, 235);
    private static readonly Color BorderColor = new(248, 224, 142, 240);
    private static readonly Color AccentColor = new(246, 186, 62, 255);
    private static readonly Color LabelColor = new(255, 220, 132, 255);
    private static readonly Color TitleColor = Color.White;
    private static readonly Color ObjectiveColor = new(220, 228, 236, 255);
    private static readonly Color FlashPanelColor = new(56, 41, 18, 245);
    private static readonly Color FlashBorderColor = new(255, 245, 214, 255);
    private static readonly Color FlashAccentColor = new(255, 223, 118, 255);
    private static readonly Color FlashLabelColor = new(255, 239, 184, 255);
    private static readonly Color FlashObjectiveColor = new(244, 238, 224, 255);
    private static readonly Color BadgeColorA = new(244, 184, 68, 255);
    private static readonly Color BadgeColorB = new(255, 230, 146, 255);
    private static readonly Color BadgeTextColor = new(20, 14, 8, 255);
    private static readonly Color ShimmerGlowColor = new(255, 220, 140, 28);
    private static readonly Color ShimmerCoreColor = new(255, 248, 226, 76);

    /// <summary>
    /// Draws the current quest discovery banner.
    /// </summary>
    internal static Rectangle CalculatePanelRectangle(QuestDiscoverySequence sequence, Viewport viewport, int sceneScale)
    {
        var emphasis = sequence.Emphasis;
        var panelWidth = PanelWidth * sceneScale;
        var panelHeight = PanelHeight * sceneScale;
        var topMargin = TopMargin * sceneScale;
        var slideOffset = (int)((1f - emphasis) * 18f * sceneScale);
        var panelOffsetX = (int)MathF.Round(sequence.PanelOffset.X * sceneScale);
        var panelOffsetY = (int)MathF.Round(sequence.PanelOffset.Y * sceneScale);

        return new Rectangle(
            ((viewport.Width - panelWidth) / 2) + panelOffsetX,
            topMargin - slideOffset + panelOffsetY,
            panelWidth,
            panelHeight);
    }

    /// <summary>
    /// Draws the current quest discovery banner.
    /// </summary>
    internal void Draw(
        SpriteBatch spriteBatch,
        SpriteFontBase titleFont,
        SpriteFontBase bodyFont,
        Texture2D pixelTexture,
        QuestDiscoverySequence sequence,
        Viewport viewport,
        int sceneScale)
    {
        if (!sequence.IsActive || sequence.CurrentQuest is null)
        {
            return;
        }

        var opacity = sequence.Opacity;
        var emphasis = sequence.Emphasis;
        var flashIntensity = sequence.FlashIntensity;
        var panelRect = CalculatePanelRectangle(sequence, viewport, sceneScale);

        var pulse = MathHelper.Clamp(sequence.Pulse, 0f, 1f);
        var badgeScale = sequence.BadgeScale;
        var panelColor = Color.Lerp(PanelColor, FlashPanelColor, flashIntensity) * opacity;
        var borderColor = Color.Lerp(BorderColor, FlashBorderColor, MathHelper.Clamp(flashIntensity * 0.8f, 0f, 1f)) * opacity;
        var accentBaseColor = Color.Lerp(AccentColor, FlashAccentColor, MathHelper.Clamp((pulse * 0.45f) + (flashIntensity * 0.55f), 0f, 1f));
        var accentColor = accentBaseColor * opacity;
        var labelColor = Color.Lerp(LabelColor, FlashLabelColor, MathHelper.Clamp(flashIntensity * 0.65f, 0f, 1f)) * opacity;
        var titleColor = Color.Lerp(TitleColor, FlashBorderColor, MathHelper.Clamp(flashIntensity * 0.4f, 0f, 1f)) * opacity;
        var objectiveColor = Color.Lerp(ObjectiveColor, FlashObjectiveColor, MathHelper.Clamp(flashIntensity * 0.55f, 0f, 1f)) * opacity;
        var accentHeight = (AccentHeight * sceneScale) + (int)MathF.Round(flashIntensity * 2f * sceneScale);
        var paddingX = PaddingX * sceneScale;
        var paddingY = PaddingY * sceneScale;
        var accentExtraWidth = (int)MathF.Round(((12f * emphasis) + (10f * flashIntensity)) * sceneScale);

        DrawPanelBox(spriteBatch, pixelTexture, panelRect, panelColor, borderColor);
        if (sequence.ShimmerProgress >= 0f)
        {
            DrawShimmer(spriteBatch, pixelTexture, panelRect, sequence.ShimmerProgress, sceneScale);
        }

        spriteBatch.Draw(
            pixelTexture,
            new Rectangle(panelRect.X - accentExtraWidth / 2, panelRect.Y, panelRect.Width + accentExtraWidth, accentHeight),
            accentColor);

        var labelText = "NEW QUEST DISCOVERED";
        var questTitle = sequence.CurrentQuest.Title;
        var objectiveText = sequence.CurrentQuest.Objectives.Length > 0
            ? sequence.CurrentQuest.Objectives[0].Description
            : sequence.CurrentQuest.Description;
        var badgeText = "NEW";
        var badgeLabelSize = bodyFont.MeasureString(badgeText);
        var badgeBaseWidth = (int)MathF.Ceiling(badgeLabelSize.X) + (BadgePaddingX * sceneScale * 2);
        var badgeBaseHeight = (int)MathF.Ceiling(badgeLabelSize.Y) + (BadgePaddingY * sceneScale * 2);
        var scaledBadgeWidth = (int)MathF.Ceiling(badgeBaseWidth * Math.Max(1f, badgeScale));
        var scaledBadgeHeight = (int)MathF.Ceiling(badgeBaseHeight * Math.Max(1f, badgeScale));
        var badgeColor = Color.Lerp(BadgeColorA, BadgeColorB, pulse);
        badgeColor = Color.Lerp(badgeColor, FlashBorderColor, MathHelper.Clamp(flashIntensity * 0.45f, 0f, 1f)) * opacity;
        var badgeBorderColor = Color.Lerp(BorderColor, FlashBorderColor, MathHelper.Clamp(0.5f + (flashIntensity * 0.3f), 0f, 1f)) * opacity;
        var badgeTextColor = BadgeTextColor * opacity;

        var labelPosition = new Vector2(panelRect.X + paddingX, panelRect.Y + paddingY - (2 * sceneScale));
        var badgeRect = new Rectangle(
            panelRect.Right - paddingX - scaledBadgeWidth,
            panelRect.Y + paddingY - (2 * sceneScale) - ((scaledBadgeHeight - badgeBaseHeight) / 2),
            scaledBadgeWidth,
            scaledBadgeHeight);
        var titlePosition = new Vector2(panelRect.X + paddingX, labelPosition.Y + titleFont.LineHeight * 0.75f);
        var objectivePosition = new Vector2(panelRect.X + paddingX, titlePosition.Y + titleFont.LineHeight + (4 * sceneScale));

        spriteBatch.DrawString(bodyFont, labelText, labelPosition, labelColor);
        DrawPanelBox(spriteBatch, pixelTexture, badgeRect, badgeColor, badgeBorderColor);

        var badgeTextX = badgeRect.X + ((badgeRect.Width - badgeLabelSize.X) / 2f);
        var badgeTextY = badgeRect.Y + ((badgeRect.Height - badgeLabelSize.Y) / 2f) - (sceneScale * 0.5f);
        spriteBatch.DrawString(bodyFont, badgeText, new Vector2(badgeTextX, badgeTextY), badgeTextColor);
        spriteBatch.DrawString(titleFont, questTitle, titlePosition, titleColor);
        spriteBatch.DrawString(bodyFont, objectiveText, objectivePosition, objectiveColor);
    }

    private static void DrawPanelBox(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        Rectangle panelRect,
        Color panelColor,
        Color borderColor)
    {
        spriteBatch.Draw(pixelTexture, panelRect, panelColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 1), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Bottom - 1, panelRect.Width, 1), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Y, 1, panelRect.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.Right - 1, panelRect.Y, 1, panelRect.Height), borderColor);
    }

    private static void DrawShimmer(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        Rectangle panelRect,
        float shimmerProgress,
        int sceneScale)
    {
        var glowWidth = Math.Max(ShimmerGlowWidth * sceneScale, panelRect.Width / 4);
        var coreWidth = Math.Max(ShimmerCoreWidth * sceneScale, glowWidth / 3);
        var shimmerCenter = MathHelper.Lerp(panelRect.Left - glowWidth, panelRect.Right + glowWidth, shimmerProgress);

        var glowRect = Rectangle.Intersect(
            new Rectangle(
                (int)MathF.Round(shimmerCenter) - (glowWidth / 2),
                panelRect.Y + 1,
                glowWidth,
                Math.Max(6 * sceneScale, panelRect.Height / 2)),
            panelRect);
        if (!glowRect.IsEmpty)
        {
            spriteBatch.Draw(pixelTexture, glowRect, ShimmerGlowColor);
        }

        var coreRect = Rectangle.Intersect(
            new Rectangle(
                (int)MathF.Round(shimmerCenter) - (coreWidth / 2),
                panelRect.Y,
                coreWidth,
                Math.Max(3 * sceneScale, panelRect.Height / 3)),
            panelRect);
        if (!coreRect.IsEmpty)
        {
            spriteBatch.Draw(pixelTexture, coreRect, ShimmerCoreColor);
        }
    }
}