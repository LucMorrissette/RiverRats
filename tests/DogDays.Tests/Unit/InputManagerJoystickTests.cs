using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using DogDays.Game.Input;
using Xunit;

namespace DogDays.Tests.Unit;

/// <summary>
/// Unit tests for raw joystick fallback support in <see cref="InputManager"/>.
/// Validates Hat0 D-pad and raw button bindings for unmapped USB controllers
/// (Hyperkin Cadet NES controller).
/// </summary>
public class InputManagerJoystickTests
{
    // ── D-pad via Hat0 ──────────────────────────────────────────────────

    [Theory]
    [InlineData(InputAction.MoveUp)]
    [InlineData(InputAction.MoveDown)]
    [InlineData(InputAction.MoveLeft)]
    [InlineData(InputAction.MoveRight)]
    public void IsHeld__JoystickHatDirection__ReturnsTrue(InputAction action)
    {
        var joystick = new FakeJoystickStateSource(
            JoystickSnapshot.Disconnected,
            MakeHatSnapshot(action));

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.True(input.IsHeld(action));
    }

    [Theory]
    [InlineData(InputAction.MoveUp)]
    [InlineData(InputAction.MoveDown)]
    [InlineData(InputAction.MoveLeft)]
    [InlineData(InputAction.MoveRight)]
    public void IsPressed__JoystickHatTransition__ReturnsTrueOnFirstFrame(InputAction action)
    {
        var joystick = new FakeJoystickStateSource(
            new JoystickSnapshot(true),
            MakeHatSnapshot(action),
            MakeHatSnapshot(action));

        var input = CreateInputManager(joystick);

        input.Update();
        Assert.True(input.IsPressed(action));

        input.Update();
        Assert.False(input.IsPressed(action));
        Assert.True(input.IsHeld(action));
    }

    [Fact]
    public void IsReleased__JoystickHatReleased__ReturnsTrueOnTransition()
    {
        var joystick = new FakeJoystickStateSource(
            MakeHatSnapshot(InputAction.MoveLeft),
            new JoystickSnapshot(true),
            new JoystickSnapshot(true));

        var input = CreateInputManager(joystick);

        input.Update();
        Assert.True(input.IsReleased(InputAction.MoveLeft));

        input.Update();
        Assert.False(input.IsReleased(InputAction.MoveLeft));
    }

    // ── Buttons ─────────────────────────────────────────────────────────

    [Fact]
    public void IsPressed__JoystickButton1_Confirm__ReturnsTrue()
    {
        // A button = index 1 → Confirm
        var joystick = new FakeJoystickStateSource(
            new JoystickSnapshot(true, buttons: new bool[10]),
            MakeButtonSnapshot(1));

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.True(input.IsPressed(InputAction.Confirm));
    }

    [Fact]
    public void IsPressed__JoystickButton0_Cancel__ReturnsTrue()
    {
        // B button = index 0 → Cancel
        var joystick = new FakeJoystickStateSource(
            new JoystickSnapshot(true, buttons: new bool[10]),
            MakeButtonSnapshot(0));

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.True(input.IsPressed(InputAction.Cancel));
    }

    [Fact]
    public void IsPressed__JoystickButton9_Pause__ReturnsTrue()
    {
        // Start button = index 9 → Pause
        var joystick = new FakeJoystickStateSource(
            new JoystickSnapshot(true, buttons: new bool[10]),
            MakeButtonSnapshot(9));

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.True(input.IsPressed(InputAction.Pause));
    }

    [Fact]
    public void IsHeld__JoystickButtonDown__ReturnsTrue()
    {
        var joystick = new FakeJoystickStateSource(
            new JoystickSnapshot(true, buttons: new bool[10]),
            MakeButtonSnapshot(1));

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.True(input.IsHeld(InputAction.Confirm));
    }

    [Fact]
    public void IsReleased__JoystickButtonReleased__ReturnsTrueOnTransition()
    {
        var joystick = new FakeJoystickStateSource(
            MakeButtonSnapshot(1),
            new JoystickSnapshot(true, buttons: new bool[10]),
            new JoystickSnapshot(true, buttons: new bool[10]));

        var input = CreateInputManager(joystick);

        input.Update();
        Assert.True(input.IsReleased(InputAction.Confirm));

        input.Update();
        Assert.False(input.IsReleased(InputAction.Confirm));
    }

    // ── Disconnected joystick ───────────────────────────────────────────

    [Fact]
    public void IsHeld__JoystickDisconnected__ReturnsFalse()
    {
        var joystick = new FakeJoystickStateSource(
            JoystickSnapshot.Disconnected,
            JoystickSnapshot.Disconnected);

        var input = CreateInputManager(joystick);
        input.Update();

        Assert.False(input.IsHeld(InputAction.MoveUp));
        Assert.False(input.IsHeld(InputAction.Confirm));
    }

    // ── Keyboard still works alongside joystick ─────────────────────────

    [Fact]
    public void IsHeld__KeyboardDownJoystickDisconnected__ReturnsTrue()
    {
        var keyboard = new FakeKeyboardStateSource(
            new KeyboardState(),
            new KeyboardState(Keys.W));
        var joystick = new FakeJoystickStateSource(
            JoystickSnapshot.Disconnected,
            JoystickSnapshot.Disconnected);

        var input = new InputManager(keyboard, null, joystick);
        input.Update();

        Assert.True(input.IsHeld(InputAction.MoveUp));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static InputManager CreateInputManager(FakeJoystickStateSource joystick)
    {
        var keyboard = new FakeKeyboardStateSource(new KeyboardState(), new KeyboardState());
        return new InputManager(keyboard, null, joystick);
    }

    private static JoystickSnapshot MakeHatSnapshot(InputAction action)
    {
        return new JoystickSnapshot(
            true,
            hatUp: action == InputAction.MoveUp,
            hatDown: action == InputAction.MoveDown,
            hatLeft: action == InputAction.MoveLeft,
            hatRight: action == InputAction.MoveRight);
    }

    private static JoystickSnapshot MakeButtonSnapshot(int buttonIndex)
    {
        var buttons = new bool[10];
        buttons[buttonIndex] = true;
        return new JoystickSnapshot(true, buttons: buttons);
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

    private sealed class FakeJoystickStateSource : IJoystickStateSource
    {
        private readonly Queue<JoystickSnapshot> _states;
        private JoystickSnapshot _lastState;

        public FakeJoystickStateSource(params JoystickSnapshot[] states)
        {
            _states = new Queue<JoystickSnapshot>(states);
            _lastState = states.Length > 0 ? states[^1] : JoystickSnapshot.Disconnected;
        }

        public JoystickSnapshot GetState()
        {
            if (_states.Count > 0)
            {
                _lastState = _states.Dequeue();
            }

            return _lastState;
        }
    }
}
