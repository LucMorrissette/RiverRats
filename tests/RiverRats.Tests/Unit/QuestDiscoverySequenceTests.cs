using RiverRats.Game.Data;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;
using Microsoft.Xna.Framework;

namespace RiverRats.Tests.Unit;

public sealed class QuestDiscoverySequenceTests
{
    [Fact]
    public void Enqueue__ActivatesSequence__WithFirstQuestImmediately()
    {
        var sequence = new QuestDiscoverySequence();
        var quest = CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa.");

        sequence.Enqueue(quest);

        Assert.True(sequence.IsActive);
        Assert.Equal("moms-warning", sequence.CurrentQuest!.Id);
        Assert.Equal(0f, sequence.Progress);
        Assert.Equal(0f, sequence.Opacity);
        Assert.True(sequence.FlashIntensity > 0.95f);
        Assert.InRange(sequence.BadgeScale, 0.83f, 0.85f);
        Assert.True(sequence.ShimmerProgress < 0f);
    }

    [Fact]
    public void Update__DuringFadeIn__RaisesOpacity()
    {
        var sequence = new QuestDiscoverySequence();
        sequence.Enqueue(CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa."));

        sequence.Update(FakeGameTime.FromSeconds(0.11f));

        Assert.True(sequence.IsActive);
        Assert.InRange(sequence.Opacity, 0.45f, 0.55f);
    }

    [Fact]
    public void Update__DuringEntranceWindow__ProducesKickPopAndShimmer()
    {
        var sequence = new QuestDiscoverySequence();
        sequence.Enqueue(CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa."));

        sequence.Update(FakeGameTime.FromSeconds(0.18f));

        Assert.True(sequence.IsActive);
        Assert.True(sequence.PanelOffset.Length() > 0.1f);
        Assert.True(sequence.FlashIntensity > 0f);
        Assert.True(sequence.BadgeScale > 1f);
        Assert.InRange(sequence.ShimmerProgress, 0f, 1f);
    }

    [Fact]
    public void Update__AfterEntranceWindow__SettlesJuiceEffects()
    {
        var sequence = new QuestDiscoverySequence();
        sequence.Enqueue(CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa."));

        sequence.Update(FakeGameTime.FromSeconds(0.8f));

        Assert.True(sequence.IsActive);
        Assert.Equal(Vector2.Zero, sequence.PanelOffset);
        Assert.Equal(0f, sequence.FlashIntensity);
        Assert.Equal(1f, sequence.BadgeScale);
        Assert.True(sequence.ShimmerProgress < 0f);
    }

    [Fact]
    public void Update__AfterFullDuration__AdvancesToNextQueuedQuest()
    {
        var sequence = new QuestDiscoverySequence();
        sequence.Enqueue(CreateQuest("quest-a", "Quest A", "Objective A."));
        sequence.Enqueue(CreateQuest("quest-b", "Quest B", "Objective B."));

        sequence.Update(FakeGameTime.FromSeconds(QuestDiscoverySequence.TotalDurationSeconds + 0.01f));

        Assert.True(sequence.IsActive);
        Assert.Equal("quest-b", sequence.CurrentQuest!.Id);
    }

    [Fact]
    public void Update__AfterQueueDrains__EndsSequence()
    {
        var sequence = new QuestDiscoverySequence();
        sequence.Enqueue(CreateQuest("quest-a", "Quest A", "Objective A."));

        sequence.Update(FakeGameTime.FromSeconds(QuestDiscoverySequence.TotalDurationSeconds + 0.01f));

        Assert.False(sequence.IsActive);
        Assert.Null(sequence.CurrentQuest);
        Assert.Equal(0f, sequence.Opacity);
    }

    private static QuestDefinition CreateQuest(string id, string title, string objectiveText)
    {
        return new QuestDefinition
        {
            Id = id,
            Title = title,
            Description = title,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    Id = $"{id}-objective",
                    Description = objectiveText,
                    Completion = new QuestEventConditionDefinition(),
                },
            ],
        };
    }
}