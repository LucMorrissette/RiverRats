using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Core;
using RiverRats.Game.Data;
using RiverRats.Game.Input;
using RiverRats.Game.Screens;
using RiverRats.Game.Systems;
using RiverRats.Tests.Helpers;
using Xunit;

namespace RiverRats.Tests.Unit;

public sealed class PauseScreenTests
{
    private static PauseScreen CreatePauseScreen(
        ScreenManager? manager = null,
        FakeMusicManager? music = null,
        QuestManager? quests = null)
    {
        return new PauseScreen(
            manager ?? new ScreenManager(),
            music ?? new FakeMusicManager(),
            quests ?? CreateQuestManager(),
            null!,
            null!,
            480,
            270);
    }

    [Fact]
    public void IsTransparent__ReturnsTrue()
    {
        var screen = CreatePauseScreen();
        Assert.True(screen.IsTransparent);
    }

    [Fact]
    public void Update__PausePressed__PopsTopScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());
        Assert.Equal(2, manager.Count);

        var screen = CreatePauseScreen(manager: manager);
        var input = new FakeInputManager();
        input.Press(InputAction.Pause);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Update__CancelPressed__PopsTopScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());

        var screen = CreatePauseScreen(manager: manager);
        var input = new FakeInputManager();
        input.Press(InputAction.Cancel);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Update__PauseNotPressed__DoesNotPopScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());

        var screen = CreatePauseScreen(manager: manager);
        var input = new FakeInputManager();

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(2, manager.Count);
    }

    [Fact]
    public void Update__MoveDownPressed__AdvancesQuestSelection()
    {
        var screen = CreatePauseScreen(quests: CreateQuestManager());
        var input = new FakeInputManager();
        input.Press(InputAction.MoveDown);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, screen.SelectedQuestIndex);
    }

    [Fact]
    public void Update__MoveUpPressedAtFirstQuest__WrapsToLastQuest()
    {
        var screen = CreatePauseScreen(quests: CreateQuestManager());
        var input = new FakeInputManager();
        input.Press(InputAction.MoveUp);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, screen.SelectedQuestIndex);
    }

    [Fact]
    public void Update__ConfirmPressed__TracksSelectedQuestWithoutClosingPauseScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());

        var quests = CreateQuestManager();
        var screen = CreatePauseScreen(manager: manager, quests: quests);
        var input = new FakeInputManager();
        input.Press(InputAction.MoveDown);
        screen.Update(FakeGameTime.OneFrame(), input);
        input.Update();
        input.Press(InputAction.Confirm);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(2, manager.Count);
        Assert.Equal("quest-b", quests.TrackedQuest!.Definition.Id);
    }

    [Fact]
    public void UnloadContent__RestoresMusicVolumeToFull()
    {
        var music = new FakeMusicManager();
        var screen = CreatePauseScreen(music: music);

        screen.UnloadContent();

        Assert.Equal(1.0f, music.LastVolume);
    }

    [Fact]
    public void UnloadContent__VolumeHistoryShowsRestore()
    {
        var music = new FakeMusicManager();
        music.SetVolume(0.2f);
        var screen = CreatePauseScreen(music: music);

        screen.UnloadContent();

        Assert.Equal(2, music.VolumeHistory.Count);
        Assert.Equal(0.2f, music.VolumeHistory[0]);
        Assert.Equal(1.0f, music.VolumeHistory[1]);
    }

    private static QuestManager CreateQuestManager()
    {
        var bus = new GameEventBus();
        var manager = new QuestManager(bus);
        manager.LoadDefinitions(
        [
            new QuestDefinition
            {
                Id = "quest-a",
                Title = "Quest A",
                Description = "First quest.",
                AutoStart = true,
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        Id = "a-1",
                        Description = "Talk to Mom.",
                        Completion = new QuestEventConditionDefinition
                        {
                            EventType = GameEventType.NpcTalkedTo,
                            TargetId = "mom",
                            RequiredCount = 1,
                        },
                    },
                ],
            },
            new QuestDefinition
            {
                Id = "quest-b",
                Title = "Quest B",
                Description = "Second quest.",
                AutoStart = true,
                Objectives =
                [
                    new ObjectiveDefinition
                    {
                        Id = "b-1",
                        Description = "Talk to Grandpa.",
                        Completion = new QuestEventConditionDefinition
                        {
                            EventType = GameEventType.NpcTalkedTo,
                            TargetId = "grandpa",
                            RequiredCount = 1,
                        },
                    },
                ],
            },
        ]);
        return manager;
    }

    private sealed class StubScreen : IGameScreen
    {
        public bool IsTransparent => false;
        public void LoadContent() { }
        public void UnloadContent() { }
        public void Update(GameTime gameTime, IInputManager input) { }
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
    }
}
