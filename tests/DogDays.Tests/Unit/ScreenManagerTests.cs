using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DogDays.Game.Input;
using DogDays.Game.Screens;
using DogDays.Tests.Helpers;
using Xunit;

namespace DogDays.Tests.Unit;

public sealed class ScreenManagerTests
{
    [Fact]
    public void Push__AddsScreenToStack__CountIncreases()
    {
        var manager = new ScreenManager();
        manager.Push(new StubScreen());

        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void Push__MultipleTimes__ActiveScreenIsLastPushed()
    {
        var manager = new ScreenManager();
        var first = new StubScreen("First");
        var second = new StubScreen("Second");
        manager.Push(first);
        manager.Push(second);

        Assert.Equal(2, manager.Count);
        Assert.Same(second, manager.ActiveScreen);
    }

    [Fact]
    public void Pop__RemovesTopScreen__PreviousBecomesActive()
    {
        var manager = new ScreenManager();
        var first = new StubScreen("First");
        var second = new StubScreen("Second");
        manager.Push(first);
        manager.Push(second);

        manager.Pop();

        Assert.Equal(1, manager.Count);
        Assert.Same(first, manager.ActiveScreen);
    }

    [Fact]
    public void Pop__EmptyStack__NoOp()
    {
        var manager = new ScreenManager();
        manager.Pop();

        Assert.Equal(0, manager.Count);
    }

    [Fact]
    public void Pop__CallsUnloadContentOnRemovedScreen()
    {
        var manager = new ScreenManager();
        var screen = new StubScreen();
        manager.Push(screen);

        manager.Pop();

        Assert.True(screen.UnloadCalled);
    }

    [Fact]
    public void Push__CallsLoadContentOnNewScreen()
    {
        var manager = new ScreenManager();
        var screen = new StubScreen();

        manager.Push(screen);

        Assert.True(screen.LoadCalled);
    }

    [Fact]
    public void Replace__RemovesAllScreensAndPushesNew()
    {
        var manager = new ScreenManager();
        var a = new StubScreen("A");
        var b = new StubScreen("B");
        var replacement = new StubScreen("Replacement");
        manager.Push(a);
        manager.Push(b);

        manager.Replace(replacement);

        Assert.Equal(1, manager.Count);
        Assert.Same(replacement, manager.ActiveScreen);
        Assert.True(a.UnloadCalled);
        Assert.True(b.UnloadCalled);
        Assert.True(replacement.LoadCalled);
    }

    [Fact]
    public void Update__OnlyCallsTopScreen()
    {
        var manager = new ScreenManager();
        var bottom = new StubScreen("Bottom");
        var top = new StubScreen("Top");
        manager.Push(bottom);
        manager.Push(top);

        var input = new FakeInputManager();
        manager.Update(FakeGameTime.OneFrame(), input);

        Assert.Equal(1, top.UpdateCount);
        Assert.Equal(0, bottom.UpdateCount);
    }

    [Fact]
    public void Draw__OpaqueTopScreen__OnlyDrawsTopScreen()
    {
        var manager = new ScreenManager();
        var bottom = new StubScreen("Bottom") { Transparent = false };
        var top = new StubScreen("Top") { Transparent = false };
        manager.Push(bottom);
        manager.Push(top);

        manager.Draw(FakeGameTime.OneFrame(), null);

        Assert.Equal(1, top.DrawCount);
        Assert.Equal(0, bottom.DrawCount);
    }

    [Fact]
    public void Draw__TransparentTopScreen__DrawsBothScreens()
    {
        var manager = new ScreenManager();
        var bottom = new StubScreen("Bottom") { Transparent = false };
        var top = new StubScreen("Top") { Transparent = true };
        manager.Push(bottom);
        manager.Push(top);

        manager.Draw(FakeGameTime.OneFrame(), null);

        Assert.Equal(1, bottom.DrawCount);
        Assert.Equal(1, top.DrawCount);
    }

    [Fact]
    public void Draw__DrawsBottomToTop()
    {
        var drawOrder = new List<string>();
        var manager = new ScreenManager();
        var bottom = new StubScreen("Bottom") { Transparent = false, OnDraw = name => drawOrder.Add(name) };
        var top = new StubScreen("Top") { Transparent = true, OnDraw = name => drawOrder.Add(name) };
        manager.Push(bottom);
        manager.Push(top);

        manager.Draw(FakeGameTime.OneFrame(), null);

        Assert.Equal(new[] { "Bottom", "Top" }, drawOrder);
    }

    [Fact]
    public void ActiveScreen__EmptyStack__ReturnsNull()
    {
        var manager = new ScreenManager();
        Assert.Null(manager.ActiveScreen);
    }

    [Fact]
    public void Update__EmptyStack__NoException()
    {
        var manager = new ScreenManager();
        manager.Update(FakeGameTime.OneFrame(), new FakeInputManager());
    }

    [Fact]
    public void Draw__EmptyStack__NoException()
    {
        var manager = new ScreenManager();
        manager.Draw(FakeGameTime.OneFrame(), null);
    }

    [Fact]
    public void Push__DuringUpdate__DefersUntilUpdateComplete()
    {
        var manager = new ScreenManager();
        var deferred = new StubScreen("Deferred");
        var trigger = new StubScreen("Trigger")
        {
            OnUpdate = () => manager.Push(deferred)
        };
        manager.Push(trigger);

        manager.Update(FakeGameTime.OneFrame(), new FakeInputManager());

        Assert.Equal(2, manager.Count);
        Assert.Same(deferred, manager.ActiveScreen);
        Assert.True(deferred.LoadCalled);
    }

    [Fact]
    public void Pop__DuringUpdate__DefersUntilUpdateComplete()
    {
        var manager = new ScreenManager();
        var bottom = new StubScreen("Bottom");
        var top = new StubScreen("Top")
        {
            OnUpdate = () => manager.Pop()
        };
        manager.Push(bottom);
        manager.Push(top);

        manager.Update(FakeGameTime.OneFrame(), new FakeInputManager());

        Assert.Equal(1, manager.Count);
        Assert.Same(bottom, manager.ActiveScreen);
        Assert.True(top.UnloadCalled);
    }

    [Fact]
    public void Replace__CalledDuringUpdate__DefersReplacementUntilAfterUpdate()
    {
        var manager = new ScreenManager();
        var originalScreen = new StubScreen();
        var replacementScreen = new StubScreen();
        manager.Push(originalScreen);

        // Create a screen that calls Replace during its own Update
        var triggerScreen = new ReplaceOnUpdateScreen(manager, replacementScreen);
        manager.Push(triggerScreen);

        // This Update should NOT crash despite Replace being called inside it
        manager.Update(new GameTime(), new FakeInputManager());

        // After Update completes, the replacement should have been applied
        Assert.Equal(1, manager.Count);
        Assert.Same(replacementScreen, manager.ActiveScreen);
        Assert.True(originalScreen.UnloadCalled);
        Assert.True(triggerScreen.UnloadCalled);
        Assert.True(replacementScreen.LoadCalled);
    }

    /// <summary>
    /// Minimal screen stub that records lifecycle calls for assertions.
    /// </summary>
    private sealed class StubScreen : IGameScreen
    {
        private readonly string _name;

        public StubScreen(string name = "Stub")
        {
            _name = name;
        }

        public bool Transparent { get; set; }
        public bool LoadCalled { get; private set; }
        public bool UnloadCalled { get; private set; }
        public int UpdateCount { get; private set; }
        public int DrawCount { get; private set; }
        public System.Action OnUpdate { get; set; } = null!;
        public System.Action<string> OnDraw { get; set; } = null!;

        public bool IsTransparent => Transparent;

        public void LoadContent() => LoadCalled = true;

        public void UnloadContent() => UnloadCalled = true;

        public void Update(GameTime gameTime, IInputManager input)
        {
            UpdateCount++;
            OnUpdate?.Invoke();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            DrawCount++;
            OnDraw?.Invoke(_name);
        }
    }

    private sealed class ReplaceOnUpdateScreen : IGameScreen
    {
        private readonly ScreenManager _manager;
        private readonly IGameScreen _replacement;

        public ReplaceOnUpdateScreen(ScreenManager manager, IGameScreen replacement)
        {
            _manager = manager;
            _replacement = replacement;
        }

        public bool IsTransparent => false;
        public bool LoadCalled { get; private set; }
        public bool UnloadCalled { get; private set; }

        public void LoadContent() => LoadCalled = true;
        public void UnloadContent() => UnloadCalled = true;

        public void Update(GameTime gameTime, IInputManager input)
        {
            _manager.Replace(_replacement);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
    }
}
