using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Input;
using DogDays.Game.Screens;
using DogDays.Tests.Helpers;
using Xunit;

namespace DogDays.Tests.Unit;

public sealed class ConfirmationScreenTests
{
    private static readonly string[] YesNoOptions = { "Yes", "No" };

    private static ConfirmationScreen CreateScreen(
        ScreenManager manager = null,
        string prompt = "Test?",
        string[] options = null,
        Action<int> onSelect = null,
        Action onCancel = null,
        int defaultSelection = 1)
    {
        return new ConfirmationScreen(
            manager ?? new ScreenManager(),
            null!, // GraphicsDevice not needed for logic tests
            null!, // ContentManager not needed for logic tests
            prompt,
            options ?? YesNoOptions,
            onSelect ?? (_ => { }),
            onCancel,
            defaultSelection);
    }

    [Fact]
    public void IsTransparent__ReturnsTrue()
    {
        var screen = CreateScreen();
        Assert.True(screen.IsTransparent);
    }

    [Fact]
    public void SelectedIndex__DefaultsToProvidedDefault()
    {
        var screen = CreateScreen(defaultSelection: 1);
        Assert.Equal(1, screen.SelectedIndex);
    }

    [Fact]
    public void SelectedIndex__DefaultsToZero__WhenDefaultSelectionIsZero()
    {
        var screen = CreateScreen(defaultSelection: 0);
        Assert.Equal(0, screen.SelectedIndex);
    }

    [Fact]
    public void Update__MoveDownPressed__SelectionWrapsToFirst()
    {
        var screen = CreateScreen(defaultSelection: 1);
        var input = new FakeInputManager();
        input.Press(InputAction.MoveDown);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(0, screen.SelectedIndex);
    }

    [Fact]
    public void Update__MoveUpPressed__SelectionWrapsToLast()
    {
        var screen = CreateScreen(defaultSelection: 0);
        var input = new FakeInputManager();
        input.Press(InputAction.MoveUp);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, screen.SelectedIndex);
    }

    [Fact]
    public void Update__MoveUpPressed__SelectionMovesUp()
    {
        var screen = CreateScreen(defaultSelection: 1);
        var input = new FakeInputManager();
        input.Press(InputAction.MoveUp);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(0, screen.SelectedIndex);
    }

    [Fact]
    public void Update__ConfirmPressed__CallsOnSelectWithSelectedIndex()
    {
        var selectedIndex = -1;
        var screen = CreateScreen(
            onSelect: idx => selectedIndex = idx,
            defaultSelection: 0);
        var input = new FakeInputManager();
        input.Press(InputAction.Confirm);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(0, selectedIndex);
    }

    [Fact]
    public void Update__ConfirmPressed__PopsScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());

        var screen = CreateScreen(manager: manager);
        var input = new FakeInputManager();
        input.Press(InputAction.Confirm);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Update__CancelPressed__CallsOnCancel()
    {
        var cancelCalled = false;
        var screen = CreateScreen(onCancel: () => cancelCalled = true);
        var input = new FakeInputManager();
        input.Press(InputAction.Cancel);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.True(cancelCalled);
    }

    [Fact]
    public void Update__CancelPressed__NoOnCancel__SelectsLastOption()
    {
        var selectedIndex = -1;
        var screen = CreateScreen(
            onSelect: idx => selectedIndex = idx,
            onCancel: null);
        var input = new FakeInputManager();
        input.Press(InputAction.Cancel);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, selectedIndex);
    }

    [Fact]
    public void Update__CancelPressed__PopsScreen()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());
        manager.Push(new StubScreen());

        var screen = CreateScreen(manager: manager);
        var input = new FakeInputManager();
        input.Press(InputAction.Cancel);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Update__NavigateThenConfirm__SelectsNavigatedOption()
    {
        var selectedIndex = -1;
        var screen = CreateScreen(
            onSelect: idx => selectedIndex = idx,
            defaultSelection: 1);
        var input = new FakeInputManager();

        // Navigate up to select "Yes" (index 0).
        input.Press(InputAction.MoveUp);
        screen.Update(FakeGameTime.OneFrame(), input);
        input.Update();

        // Confirm selection.
        input.Press(InputAction.Confirm);
        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(0, selectedIndex);
    }

    [Theory]
    [InlineData(3, 2, 1)]
    [InlineData(3, 0, 2)]
    public void Update__ThreeOptions__WrapsCorrectly(int optionCount, int start, int expectedAfterUp)
    {
        var options = new string[optionCount];
        for (var i = 0; i < optionCount; i++)
        {
            options[i] = $"Option {i}";
        }

        var screen = CreateScreen(options: options, defaultSelection: start);
        var input = new FakeInputManager();
        input.Press(InputAction.MoveUp);

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(expectedAfterUp, screen.SelectedIndex);
    }

    [Fact]
    public void Update__NoInput__SelectionDoesNotChange()
    {
        var screen = CreateScreen(defaultSelection: 1);
        var input = new FakeInputManager();

        screen.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, screen.SelectedIndex);
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
