using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RiverRats.Game.Input;
using Xunit;

namespace RiverRats.Tests.Unit;

/// <summary>
/// Unit tests for gamepad controller support in <see cref="InputManager"/>.
/// Validates NES-style controller (Hyperkin Cadet) bindings.
/// </summary>
public class InputManagerGamepadTests
{
    [Theory]
    [InlineData(Buttons.DPadUp, InputAction.MoveUp)]
    [InlineData(Buttons.DPadDown, InputAction.MoveDown)]
    [InlineData(Buttons.DPadLeft, InputAction.MoveLeft)]
    [InlineData(Buttons.DPadRight, InputAction.MoveRight)]
    [InlineData(Buttons.A, InputAction.Confirm)]
    [InlineData(Buttons.B, InputAction.Cancel)]
    [InlineData(Buttons.Start, InputAction.Pause)]
    public void IsPressed__GamepadButton__ReturnsTrueOnTransition(Buttons button, InputAction expectedAction)
    {
        var keyboard = new FakeKeyboardStateSource(new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(),
            CreateGamePadState(button));

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.True(input.IsPressed(expectedAction));
    }

    [Theory]
    [InlineData(Buttons.DPadUp, InputAction.MoveUp)]
    [InlineData(Buttons.A, InputAction.Confirm)]
    [InlineData(Buttons.Start, InputAction.Pause)]
    public void IsHeld__GamepadButtonDown__ReturnsTrue(Buttons button, InputAction expectedAction)
    {
        var keyboard = new FakeKeyboardStateSource(new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(),
            CreateGamePadState(button));

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.True(input.IsHeld(expectedAction));
    }

    [Fact]
    public void IsReleased__GamepadButtonReleased__ReturnsTrueOnTransition()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(), new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(Buttons.A),
            CreateGamePadState(),
            CreateGamePadState());

        var input = new InputManager(keyboard, gamepad);

        input.Update();
        Assert.True(input.IsReleased(InputAction.Confirm));

        input.Update();
        Assert.False(input.IsReleased(InputAction.Confirm));
    }

    [Fact]
    public void IsPressed__GamepadButtonHeldAcrossFrames__ReturnsFalseOnSecondFrame()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(), new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(),
            CreateGamePadState(Buttons.A),
            CreateGamePadState(Buttons.A));

        var input = new InputManager(keyboard, gamepad);

        input.Update();
        Assert.True(input.IsPressed(InputAction.Confirm));

        input.Update();
        Assert.False(input.IsPressed(InputAction.Confirm));
        Assert.True(input.IsHeld(InputAction.Confirm));
    }

    [Fact]
    public void IsHeld__KeyboardAndGamepadBothSatisfy__ReturnsTrue()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(), new KeyboardState(Keys.Space));
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(), CreateGamePadState(Buttons.A));

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.True(input.IsHeld(InputAction.Confirm));
    }

    [Fact]
    public void IsHeld__KeyboardDownGamepadUp__ReturnsTrue()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(), new KeyboardState(Keys.W));
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(), CreateGamePadState());

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.True(input.IsHeld(InputAction.MoveUp));
    }

    [Fact]
    public void IsHeld__KeyboardUpGamepadDown__ReturnsTrue()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(
            CreateGamePadState(), CreateGamePadState(Buttons.DPadUp));

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.True(input.IsHeld(InputAction.MoveUp));
    }

    [Fact]
    public void IsHeld__DevActionWithNoGamepadBinding__ReturnsFalse()
    {
        // ToggleCollisionDebug has no gamepad binding — should return false from gamepad
        var keyboard = new FakeKeyboardStateSource(new KeyboardState(), new KeyboardState());
        var gamepad = new FakeGamePadStateSource(CreateGamePadState(), CreateGamePadState());

        var input = new InputManager(keyboard, gamepad);
        input.Update();

        Assert.False(input.IsHeld(InputAction.ToggleCollisionDebug));
    }

    /// <summary>
    /// Creates a <see cref="GamePadState"/> with the specified buttons pressed.
    /// </summary>
    private static GamePadState CreateGamePadState(params Buttons[] buttons)
    {
        var combined = (Buttons)0;
        foreach (var b in buttons)
        {
            combined |= b;
        }

        return new GamePadState(
            new GamePadThumbSticks(),
            new GamePadTriggers(),
            new GamePadButtons(combined),
            new GamePadDPad(
                combined.HasFlag(Buttons.DPadUp) ? ButtonState.Pressed : ButtonState.Released,
                combined.HasFlag(Buttons.DPadDown) ? ButtonState.Pressed : ButtonState.Released,
                combined.HasFlag(Buttons.DPadLeft) ? ButtonState.Pressed : ButtonState.Released,
                combined.HasFlag(Buttons.DPadRight) ? ButtonState.Pressed : ButtonState.Released));
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

    private sealed class FakeGamePadStateSource : IGamePadStateSource
    {
        private readonly Queue<GamePadState> _states;
        private GamePadState _lastState;

        public FakeGamePadStateSource(params GamePadState[] states)
        {
            _states = new Queue<GamePadState>(states);
            _lastState = states.Length > 0 ? states[^1] : new GamePadState();
        }

        public GamePadState GetState()
        {
            if (_states.Count > 0)
            {
                _lastState = _states.Dequeue();
            }

            return _lastState;
        }
    }
}
