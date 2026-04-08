using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using DogDays.Game.Input;
using Xunit;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for keyboard action mapping and frame transition behavior in <see cref="InputManager"/>.
/// </summary>
public class InputManagerTests
{
    [Fact]
    public void IsPressed__OnTransitionUpToDown__ReturnsTrueForOneFrame()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.F12),
            new KeyboardState(Keys.F12));

        var input = new InputManager(source);

        input.Update();
        Assert.True(input.IsPressed(InputAction.Exit));

        input.Update();
        Assert.False(input.IsPressed(InputAction.Exit));
        Assert.True(input.IsHeld(InputAction.Exit));
    }

    [Fact]
    public void IsReleased__OnTransitionDownToUp__ReturnsTrueForOneFrame()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(Keys.F12),
            new KeyboardState(),
            new KeyboardState());

        var input = new InputManager(source);

        input.Update();
        Assert.True(input.IsReleased(InputAction.Exit));

        input.Update();
        Assert.False(input.IsReleased(InputAction.Exit));
        Assert.False(input.IsHeld(InputAction.Exit));
    }

    [Fact]
    public void IsHeld__WhileAnyBoundKeyDown__ReturnsTrue()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.W));

        var input = new InputManager(source);
        input.Update();

        Assert.True(input.IsHeld(InputAction.MoveUp));
    }

    [Fact]
    public void IsPressed__SecondaryBindingKey__ReturnsTrue()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.Up));

        var input = new InputManager(source);
        input.Update();

        Assert.True(input.IsPressed(InputAction.MoveUp));
    }

    [Fact]
    public void SetBinding__ReplacesKeysForAction()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.F));

        var input = new InputManager(source);
        input.SetBinding(InputAction.Confirm, Keys.F);
        input.Update();

        Assert.True(input.IsPressed(InputAction.Confirm));
    }

    [Fact]
    public void SetBinding__WhenNoKeysProvided__ThrowsArgumentException()
    {
        var source = new FakeKeyboardStateSource(new KeyboardState());
        var input = new InputManager(source);

        Assert.Throws<ArgumentException>(() => input.SetBinding(InputAction.Confirm));
    }

    [Fact]
    public void IsPressed__ScreenshotBindingUsesPKey__ReturnsTrue()
    {
        var source = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.P));

        var input = new InputManager(source);
        input.Update();

        Assert.True(input.IsPressed(InputAction.CopyScreenshotToClipboard));
    }

    private sealed class FakeKeyboardStateSource : IKeyboardStateSource
    {
        private readonly Queue<KeyboardState> _states;
        private KeyboardState _lastState;

        public FakeKeyboardStateSource(params KeyboardState[] states)
        {
            _states = new Queue<KeyboardState>(states);
            _lastState = states.Length > 0 ? states[^1] : new KeyboardState();
        }

        public KeyboardState GetState()
        {
            if (_states.Count > 0)
            {
                _lastState = _states.Dequeue();
            }

            return _lastState;
        }
    }
}
