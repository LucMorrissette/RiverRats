using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Input;
using RiverRats.Game.Screens;
using RiverRats.Tests.Helpers;
using Xunit;

namespace RiverRats.Tests.Unit;

public sealed class PauseScreenTests
{
    private static PauseScreen CreatePauseScreen(
        ScreenManager? manager = null,
        FakeMusicManager? music = null)
    {
        return new PauseScreen(
            manager ?? new ScreenManager(),
            music ?? new FakeMusicManager(),
            null!, // GraphicsDevice not needed for logic tests (LoadContent/Draw not called)
            null!, // ContentManager not needed for logic tests (LoadContent not called)
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
        // Push two stub screens to simulate gameplay + pause on the stack
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());
        Assert.Equal(2, manager.Count);

        var screen = CreatePauseScreen(manager: manager);
        var input = new FakeInputManager();
        input.Press(InputAction.Pause);

        // Calling Update directly (not via manager) — Pop executes immediately
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
        // No Pause press

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(2, manager.Count);
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
        // Simulate what LoadContent would do (set volume to 0.2)
        music.SetVolume(0.2f);
        var screen = CreatePauseScreen(music: music);

        screen.UnloadContent();

        Assert.Equal(2, music.VolumeHistory.Count);
        Assert.Equal(0.2f, music.VolumeHistory[0]);
        Assert.Equal(1.0f, music.VolumeHistory[1]);
    }

    /// <summary>Minimal stub screen for stack setup in tests.</summary>
    private sealed class StubScreen : IGameScreen
    {
        public bool IsTransparent => false;
        public void LoadContent() { }
        public void UnloadContent() { }
        public void Update(GameTime gameTime, IInputManager input) { }
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
    }
}
