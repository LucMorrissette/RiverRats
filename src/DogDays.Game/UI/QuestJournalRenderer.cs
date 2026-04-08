#nullable enable

using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Data;

namespace DogDays.Game.UI;

/// <summary>
/// Renders the pause-screen quest journal and tracked-quest picker.
/// </summary>
internal sealed class QuestJournalRenderer
{
    private const int OuterMargin = 18;
    private const int PanelPadding = 12;
    private const int RowHeight = 18;
    private const int SectionGap = 12;
    private const string HeaderText = "Paused";
    private const string ControlsText = "Up/Down: Select   Confirm: Track   Pause/Cancel: Resume";
    private const string EmptyStateText = "No quests available yet.";

    private static readonly Color PanelColor = new(10, 10, 14, 220);
    private static readonly Color BorderColor = new(220, 220, 220, 180);
    private static readonly Color HeaderColor = Color.White;
    private static readonly Color ControlsColor = new(190, 190, 190);
    private static readonly Color SelectedRowColor = new(255, 246, 214);
    private static readonly Color SelectedRowFillColor = new(96, 72, 28, 184);
    private static readonly Color SelectedRowAccentColor = new(255, 222, 148, 220);
    private static readonly Color UnselectedRowColor = new(190, 190, 190);
    private static readonly Color TrackedMarkerColor = new(255, 232, 120);
    private static readonly Color DescriptionColor = new(220, 220, 220);
    private static readonly Color DetailLabelColor = new(160, 160, 160);

    /// <summary>
    /// Draws the pause-screen quest journal in screen space.
    /// </summary>
    internal void Draw(
        SpriteBatch spriteBatch,
        SpriteFontBase headerFont,
        SpriteFontBase bodyFont,
        Texture2D pixelTexture,
        IReadOnlyList<QuestState> availableQuests,
        QuestState? trackedQuest,
        int selectedIndex,
        Viewport viewport,
        int sceneScale)
    {
        var margin = OuterMargin * sceneScale;
        var panelPadding = PanelPadding * sceneScale;
        var sectionGap = SectionGap * sceneScale;
        var rowHeight = RowHeight * sceneScale;

        var panelRect = new Rectangle(
            margin,
            margin,
            Math.Max(1, viewport.Width - margin * 2),
            Math.Max(1, viewport.Height - margin * 2));

        DrawPanel(spriteBatch, pixelTexture, panelRect);

        var contentX = panelRect.X + panelPadding;
        var contentY = panelRect.Y + panelPadding;
        var contentWidth = panelRect.Width - panelPadding * 2;

        spriteBatch.DrawString(headerFont, HeaderText, new Vector2(contentX, contentY), HeaderColor);
        contentY += (int)headerFont.LineHeight + (4 * sceneScale);
        spriteBatch.DrawString(bodyFont, ControlsText, new Vector2(contentX, contentY), ControlsColor);
        contentY += (int)bodyFont.LineHeight + sectionGap;

        if (availableQuests.Count == 0)
        {
            spriteBatch.DrawString(bodyFont, EmptyStateText, new Vector2(contentX, contentY), DescriptionColor);
            return;
        }

        var detailPanelHeight = (int)bodyFont.LineHeight * 4 + sectionGap * 2;
        var listBottom = panelRect.Bottom - panelPadding - detailPanelHeight - sectionGap;
        var listVisibleRows = Math.Max(1, (listBottom - contentY) / rowHeight);

        var firstVisibleIndex = 0;
        if (selectedIndex >= listVisibleRows)
        {
            firstVisibleIndex = selectedIndex - listVisibleRows + 1;
        }

        var rowY = contentY;
        var statusRight = panelRect.Right - panelPadding;
        var rowCount = Math.Min(listVisibleRows, availableQuests.Count - firstVisibleIndex);
        for (var row = 0; row < rowCount; row++)
        {
            var questIndex = firstVisibleIndex + row;
            var quest = availableQuests[questIndex];
            var isSelected = questIndex == selectedIndex;
            var isTracked = trackedQuest != null && trackedQuest.Definition.Id == quest.Definition.Id;
            var textColor = isSelected ? SelectedRowColor : UnselectedRowColor;

            if (isSelected)
            {
                var horizontalInset = 4 * sceneScale;
                var selectedRowRect = new Rectangle(contentX - horizontalInset, rowY - (2 * sceneScale), contentWidth + (horizontalInset * 2), rowHeight);
                var accentWidth = Math.Max(1, sceneScale);
                spriteBatch.Draw(pixelTexture, selectedRowRect, SelectedRowFillColor);
                spriteBatch.Draw(pixelTexture, new Rectangle(selectedRowRect.X, selectedRowRect.Y, accentWidth, selectedRowRect.Height), SelectedRowAccentColor);
                spriteBatch.Draw(pixelTexture, new Rectangle(selectedRowRect.X, selectedRowRect.Bottom - 1, selectedRowRect.Width, 1), SelectedRowAccentColor * 0.65f);
            }

            if (isTracked)
            {
                spriteBatch.DrawString(bodyFont, "*", new Vector2(contentX, rowY), TrackedMarkerColor);
            }

            var titleX = contentX + (12 * sceneScale);
            spriteBatch.DrawString(bodyFont, quest.Definition.Title, new Vector2(titleX, rowY), textColor);

            var statusText = GetStatusLabel(quest.Status);
            var statusSize = bodyFont.MeasureString(statusText);
            spriteBatch.DrawString(bodyFont, statusText, new Vector2(statusRight - statusSize.X, rowY), textColor);

            rowY += rowHeight;
        }

        var selectedQuest = availableQuests[selectedIndex];
        var detailTop = panelRect.Bottom - panelPadding - detailPanelHeight;
        spriteBatch.Draw(pixelTexture, new Rectangle(contentX, detailTop - sectionGap / 2, contentWidth, 1), BorderColor);

        spriteBatch.DrawString(bodyFont, "Quest", new Vector2(contentX, detailTop), DetailLabelColor);
        spriteBatch.DrawString(bodyFont, selectedQuest.Definition.Title, new Vector2(contentX + (48 * sceneScale), detailTop), HeaderColor);

        var descriptionY = detailTop + (int)bodyFont.LineHeight + (4 * sceneScale);
        spriteBatch.DrawString(bodyFont, "About", new Vector2(contentX, descriptionY), DetailLabelColor);
        spriteBatch.DrawString(bodyFont, selectedQuest.Definition.Description, new Vector2(contentX + (48 * sceneScale), descriptionY), DescriptionColor);

        var objectiveY = descriptionY + (int)bodyFont.LineHeight + (4 * sceneScale);
        spriteBatch.DrawString(bodyFont, "Track", new Vector2(contentX, objectiveY), DetailLabelColor);
        spriteBatch.DrawString(bodyFont, BuildDetailLine(selectedQuest), new Vector2(contentX + (48 * sceneScale), objectiveY), DescriptionColor);
    }

    private static string BuildDetailLine(QuestState quest)
    {
        if (quest.Status == QuestStatus.Active && quest.CurrentObjective is { } objective)
        {
            if (quest.CurrentObjectiveRequiredCount > 1)
            {
                return $"{objective.Description} ({quest.CurrentObjectiveProgress}/{quest.CurrentObjectiveRequiredCount})";
            }

            return objective.Description;
        }

        return GetStatusLabel(quest.Status);
    }

    private static string GetStatusLabel(QuestStatus status)
    {
        return status switch
        {
            QuestStatus.Active => "Active",
            QuestStatus.Completed => "Completed",
            QuestStatus.Failed => "Failed",
            _ => "Hidden",
        };
    }

    private static void DrawPanel(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle panelRect)
    {
        spriteBatch.Draw(pixelTexture, panelRect, PanelColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 1), BorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Bottom - 1, panelRect.Width, 1), BorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.X, panelRect.Y, 1, panelRect.Height), BorderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(panelRect.Right - 1, panelRect.Y, 1, panelRect.Height), BorderColor);
    }
}