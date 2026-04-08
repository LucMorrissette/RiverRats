using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Systems;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class QuestCompletionSequenceTests
{
    [Fact]
    public void Enqueue__ActivatesSequence__WithFirstQuestImmediately()
    {
        var sequence = new QuestCompletionSequence();
        var quest = CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa.");

        sequence.Enqueue(quest);

        Assert.True(sequence.IsActive);
        Assert.Equal("moms-warning", sequence.CurrentQuest!.Id);
        Assert.Equal(0f, sequence.Progress);
        Assert.True(sequence.FlashIntensity > 0.95f);
        Assert.InRange(sequence.BadgeScale, 0.81f, 0.83f);
        Assert.True(sequence.ShimmerProgress < 0f);
    }

    [Fact]
    public void Update__DuringEntranceWindow__ProducesKickPopAndShimmer()
    {
        var sequence = new QuestCompletionSequence();
        sequence.Enqueue(CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa."));

        sequence.Update(FakeGameTime.FromSeconds(0.16f));

        Assert.True(sequence.IsActive);
        Assert.True(sequence.PanelOffset.Length() > 0.1f);
        Assert.True(sequence.FlashIntensity > 0f);
        Assert.True(sequence.BadgeScale > 1f);
        Assert.InRange(sequence.ShimmerProgress, 0f, 1f);
    }

    [Fact]
    public void Update__AfterEntranceWindow__SettlesJuiceEffects()
    {
        var sequence = new QuestCompletionSequence();
        sequence.Enqueue(CreateQuest("moms-warning", "Mom's Warning", "Talk to Grandpa."));

        sequence.Update(FakeGameTime.FromSeconds(0.7f));

        Assert.True(sequence.IsActive);
        Assert.Equal(Vector2.Zero, sequence.PanelOffset);
        Assert.Equal(0f, sequence.FlashIntensity);
        Assert.Equal(1f, sequence.BadgeScale);
        Assert.True(sequence.ShimmerProgress < 0f);
    }

    [Fact]
    public void Update__AfterDuration__AdvancesToNextQueuedQuest()
    {
        var sequence = new QuestCompletionSequence();
        sequence.Enqueue(CreateQuest("quest-a", "Quest A", "Objective A."));
        sequence.Enqueue(CreateQuest("quest-b", "Quest B", "Objective B."));

        sequence.Update(FakeGameTime.FromSeconds(QuestCompletionSequence.DurationSeconds + 0.01f));

        Assert.True(sequence.IsActive);
        Assert.Equal("quest-b", sequence.CurrentQuest!.Id);
    }

    [Fact]
    public void Update__AfterQueueDrains__EndsSequence()
    {
        var sequence = new QuestCompletionSequence();
        sequence.Enqueue(CreateQuest("quest-a", "Quest A", "Objective A."));

        sequence.Update(FakeGameTime.FromSeconds(QuestCompletionSequence.DurationSeconds + 0.01f));

        Assert.False(sequence.IsActive);
        Assert.Null(sequence.CurrentQuest);
        Assert.Equal(0f, sequence.Progress);
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