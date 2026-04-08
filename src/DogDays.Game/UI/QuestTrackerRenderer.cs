#nullable enable

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;

namespace DogDays.Game.UI;

/// <summary>
/// Renders a compact HUD panel for the currently tracked quest.
/// </summary>
internal sealed class QuestTrackerRenderer
{
    private const int Margin = 4;
    private const int PaddingX = 6;
    private const int PaddingY = 5;
    private const int LineGap = 3;
    private const int BadgePaddingX = 5;
    private const int BadgePaddingY = 2;
    private const int ShimmerCoreWidth = 8;
    private const int ShimmerGlowWidth = 18;

    private static readonly Color PanelColor = new(0, 0, 0, 150);
    private static readonly Color BorderColor = new(210, 210, 210, 180);
    private static readonly Color TitleColor = new(255, 244, 191);
    private static readonly Color DetailColor = Color.White;
    private static readonly Color CompletedPanelColor = new(24, 48, 26, 190);
    private static readonly Color CompletedFlashPanelColor = new(74, 126, 72, 222);
    private static readonly Color CompletedTitleColor = new(212, 255, 196);
    private static readonly Color CompletedDetailColor = new(232, 255, 236);
    private static readonly Color CompletedBadgeColorA = new(92, 168, 100);
    private static readonly Color CompletedBadgeColorB = new(178, 235, 126);
    private static readonly Color CompletedFlashBorderColor = new(235, 255, 229);
    private static readonly Color EmptyPanelColor = new(22, 26, 32, 150);
    private static readonly Color EmptyBorderColor = new(120, 132, 148, 180);
    private static readonly Color EmptyTitleColor = new(206, 214, 224);
    private static readonly Color EmptyDetailColor = new(164, 172, 184);
    private static readonly Color ShimmerGlowColor = new(224, 255, 214, 26);
    private static readonly Color ShimmerCoreColor = new(255, 255, 255, 76);

    /// <summary>
    /// Draws the active tracked quest panel in screen space.
    /// </summary>
    internal void DrawTrackedQuest(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixelTexture,
        QuestState trackedQuest,
        int sceneScale,
        int topOffset)
    {
        var currentObjective = trackedQuest.CurrentObjective;
        var detailLine = trackedQuest.Status switch
        {
            QuestStatus.Active when currentObjective is not null && trackedQuest.CurrentObjectiveRequiredCount > 1 =>
                $"{currentObjective.Description} ({trackedQuest.CurrentObjectiveProgress}/{trackedQuest.CurrentObjectiveRequiredCount})",
            QuestStatus.Active when currentObjective is not null => currentObjective.Description,
            QuestStatus.Completed => "Completed",
            QuestStatus.Failed => "Failed",
            _ => "Not started",
        };

        DrawPanel(
            spriteBatch,
            font,
            pixelTexture,
            trackedQuest.Definition.Title,
            detailLine,
            panelColor: PanelColor,
            borderColor: BorderColor,
            titleColor: TitleColor,
            detailColor: DetailColor,
            badgeText: null,
            badgeColor: BorderColor,
            panelOffset: Vector2.Zero,
            badgeScale: 1f,
            shimmerProgress: -1f,
            sceneScale,
            topOffset);
    }

    /// <summary>
    /// Draws a temporary completion state in the quest tracker location.
    /// </summary>
    internal void DrawCompletedQuest(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixelTexture,
        QuestDefinition questDefinition,
        float pulse,
        Vector2 panelOffset,
        float flashIntensity,
        float badgeScale,
        float shimmerProgress,
        int sceneScale,
        int topOffset)
    {
        var pulseColor = Color.Lerp(CompletedBadgeColorA, CompletedBadgeColorB, MathHelper.Clamp(pulse, 0f, 1f));
        var badgeColor = Color.Lerp(pulseColor, CompletedFlashBorderColor, MathHelper.Clamp(flashIntensity * 0.75f, 0f, 1f));
        var panelColor = Color.Lerp(CompletedPanelColor, CompletedFlashPanelColor, MathHelper.Clamp(flashIntensity, 0f, 1f));
        var borderColor = Color.Lerp(badgeColor, CompletedFlashBorderColor, MathHelper.Clamp(flashIntensity * 0.45f, 0f, 1f));
        var titleColor = Color.Lerp(CompletedTitleColor, Color.White, MathHelper.Clamp(flashIntensity * 0.65f, 0f, 1f));
        var detailColor = Color.Lerp(CompletedDetailColor, Color.White, MathHelper.Clamp(flashIntensity * 0.35f, 0f, 1f));

        DrawPanel(
            spriteBatch,
            font,
            pixelTexture,
            questDefinition.Title,
            "Quest complete",
            panelColor,
            borderColor,
            titleColor,
            detailColor,
            badgeText: "COMPLETE",
            badgeColor,
            panelOffset,
            badgeScale,
            shimmerProgress,
            sceneScale,
            topOffset);
    }

    /// <summary>
    /// Draws an empty quest tracker state when there are no active quests.
    /// </summary>
    internal void DrawEmptyState(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixelTexture,
        int sceneScale,
        int topOffset)
    {
        DrawPanel(
            spriteBatch,
            font,
            pixelTexture,
            "Quest Tracker",
            "No active quest",
            panelColor: EmptyPanelColor,
            borderColor: EmptyBorderColor,
            titleColor: EmptyTitleColor,
            detailColor: EmptyDetailColor,
            badgeText: null,
            badgeColor: EmptyBorderColor,
            panelOffset: Vector2.Zero,
            badgeScale: 1f,
            shimmerProgress: -1f,
            sceneScale,
            topOffset);
    }

    private void DrawPanel(
        SpriteBatch spriteBatch,
        SpriteFontBase font,
        Texture2D pixelTexture,
        string titleLine,
        string detailLine,
        Color panelColor,
        Color borderColor,
        Color titleColor,
        Color detailColor,
        string? badgeText,
        Color badgeColor,
        Vector2 panelOffset,
        float badgeScale,
        float shimmerProgress,
        int sceneScale,
        int topOffset)
    {
        var paddingX = PaddingX * sceneScale;
        var paddingY = PaddingY * sceneScale;
        var lineGap = LineGap * sceneScale;
        var margin = Margin * sceneScale;
        var panelOffsetX = (int)MathF.Round(panelOffset.X * sceneScale);
        var panelOffsetY = (int)MathF.Round(panelOffset.Y * sceneScale);

        var titleSize = font.MeasureString(titleLine);
        var detailSize = font.MeasureString(detailLine);
        var contentWidth = (int)MathF.Max(titleSize.X, detailSize.X);
        var contentHeight = (int)titleSize.Y + lineGap + (int)detailSize.Y;
        Vector2 badgeLabelSize = Vector2.Zero;
        var badgeWidth = 0;
        var scaledBadgeWidth = 0;

        if (!string.IsNullOrWhiteSpace(badgeText))
        {
            badgeLabelSize = font.MeasureString(badgeText);
            badgeWidth = (int)badgeLabelSize.X + (BadgePaddingX * sceneScale * 2);
            scaledBadgeWidth = (int)MathF.Ceiling(badgeWidth * Math.Max(1f, badgeScale));
            contentWidth = Math.Max(contentWidth, (int)titleSize.X + (BadgePaddingX * sceneScale) + scaledBadgeWidth);
        }

        var panelRect = new Rectangle(
            margin + panelOffsetX,
            topOffset + panelOffsetY,
            contentWidth + paddingX * 2,
            contentHeight + paddingY * 2);

        DrawPanelBox(spriteBatch, pixelTexture, panelRect, panelColor, borderColor);
        if (shimmerProgress >= 0f)
        {
            DrawShimmer(spriteBatch, pixelTexture, panelRect, shimmerProgress, sceneScale);
        }

        var textX = panelRect.X + paddingX;
        var textY = panelRect.Y + paddingY;
        spriteBatch.DrawString(font, titleLine, new Vector2(textX, textY), titleColor);
        spriteBatch.DrawString(
            font,
            detailLine,
            new Vector2(textX, textY + titleSize.Y + lineGap),
            detailColor);

        if (string.IsNullOrWhiteSpace(badgeText))
        {
            return;
        }

        var badgeHeight = Math.Max((int)titleSize.Y, (int)badgeLabelSize.Y + (BadgePaddingY * sceneScale * 2));
        var scaledBadgeHeight = (int)MathF.Ceiling(badgeHeight * Math.Max(1f, badgeScale));
        var badgeRect = new Rectangle(
            panelRect.Right - paddingX - Math.Max(badgeWidth, scaledBadgeWidth),
            panelRect.Y + paddingY - ((scaledBadgeHeight - badgeHeight) / 2),
            Math.Max(badgeWidth, scaledBadgeWidth),
            scaledBadgeHeight);
        spriteBatch.Draw(pixelTexture, badgeRect, badgeColor);

        var badgeTextX = badgeRect.X + ((badgeRect.Width - badgeLabelSize.X) / 2f);
        var badgeTextY = badgeRect.Y + ((badgeRect.Height - badgeLabelSize.Y) / 2f) - (sceneScale * 0.5f);
        spriteBatch.DrawString(font, badgeText, new Vector2(badgeTextX, badgeTextY), Color.Black);
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
        var glowWidth = Math.Max(ShimmerGlowWidth * sceneScale, panelRect.Width / 5);
        var coreWidth = Math.Max(ShimmerCoreWidth * sceneScale, glowWidth / 3);
        var shimmerCenter = MathHelper.Lerp(panelRect.Left - glowWidth, panelRect.Right + glowWidth, shimmerProgress);

        var glowRect = Rectangle.Intersect(
            new Rectangle(
                (int)MathF.Round(shimmerCenter) - (glowWidth / 2),
                panelRect.Y + 1,
                glowWidth,
                Math.Max(4 * sceneScale, panelRect.Height / 2)),
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
                Math.Max(2 * sceneScale, panelRect.Height / 3)),
            panelRect);
        if (!coreRect.IsEmpty)
        {
            spriteBatch.Draw(pixelTexture, coreRect, ShimmerCoreColor);
        }
    }
}