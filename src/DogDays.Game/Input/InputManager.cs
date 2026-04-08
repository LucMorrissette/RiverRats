using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Input manager that tracks keyboard, gamepad, and raw joystick state each frame
/// and exposes action-based hold/press/release queries.
/// Keyboard, gamepad, and joystick inputs are OR-merged — if any device satisfies
/// a binding, the action is considered active.
/// When <c>GamePadState.IsConnected</c> is false, the manager falls back to raw
/// joystick polling via <see cref="JoystickSnapshot"/> for unmapped USB controllers
/// (e.g., Hyperkin Cadet).
/// Uses an SDL2 event listener to reliably detect fast mouse clicks on macOS,
/// where <c>Mouse.GetState()</c> polling misses press+release cycles that
/// complete between two consecutive polls.
/// </summary>
public sealed class InputManager : IInputManager, IDisposable
{
    private readonly Dictionary<InputAction, Keys[]> _bindings;
    private readonly Dictionary<InputAction, Buttons[]> _gamepadBindings;
    private readonly Dictionary<InputAction, int[]> _joystickButtonBindings;
    private readonly Dictionary<InputAction, JoystickHatDirection> _joystickHatBindings;
    private readonly IKeyboardStateSource _keyboardStateSource;
    private readonly IGamePadStateSource _gamePadStateSource;
    private readonly IJoystickStateSource _joystickStateSource;
    private readonly Sdl2MouseListener _sdl2Mouse = new();

    private KeyboardState _previousState;
    private KeyboardState _currentState;
    private GamePadState _previousGamePadState;
    private GamePadState _currentGamePadState;
    private JoystickSnapshot _previousJoystickState;
    private JoystickSnapshot _currentJoystickState;
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    /// <summary>
    /// Creates an input manager with default bindings and production input sources.
    /// </summary>
    public InputManager()
        : this(new KeyboardStateSource(), new GamePadStateSource(), new JoystickStateSource())
    {
    }

    /// <summary>
    /// Creates an input manager with default bindings and custom input sources.
    /// </summary>
    /// <param name="keyboardStateSource">Keyboard source used to sample current state each frame.</param>
    /// <param name="gamePadStateSource">Gamepad source used to sample current state each frame.</param>
    /// <param name="joystickStateSource">Joystick source for unmapped USB controllers. Falls back to production source if null.</param>
    public InputManager(
        IKeyboardStateSource keyboardStateSource,
        IGamePadStateSource gamePadStateSource = null,
        IJoystickStateSource joystickStateSource = null)
    {
        _keyboardStateSource = keyboardStateSource ?? throw new ArgumentNullException(nameof(keyboardStateSource));
        _gamePadStateSource = gamePadStateSource ?? new GamePadStateSource();
        _joystickStateSource = joystickStateSource ?? new JoystickStateSource();
        _bindings = CreateDefaultBindings();
        _gamepadBindings = CreateDefaultGamepadBindings();
        _joystickButtonBindings = CreateDefaultJoystickButtonBindings();
        _joystickHatBindings = CreateDefaultJoystickHatBindings();
        _previousState = _keyboardStateSource.GetState();
        _currentState = _previousState;
        _previousGamePadState = _gamePadStateSource.GetState();
        _currentGamePadState = _previousGamePadState;
        _previousJoystickState = _joystickStateSource.GetState();
        _currentJoystickState = _previousJoystickState;
        _previousMouseState = Mouse.GetState();
        _currentMouseState = _previousMouseState;

        // Install the SDL2 event watcher so fast clicks are never lost.
        _sdl2Mouse.Install();
    }

    /// <inheritdoc />
    public void Update()
    {
        _previousState = _currentState;
        _currentState = _keyboardStateSource.GetState();
        _previousGamePadState = _currentGamePadState;
        _currentGamePadState = _gamePadStateSource.GetState();
        _previousJoystickState = _currentJoystickState;
        _currentJoystickState = _joystickStateSource.GetState();
        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();
    }

    /// <inheritdoc />
    public bool IsHeld(InputAction action)
    {
        var keys = GetBoundKeys(action);
        for (var i = 0; i < keys.Length; i++)
        {
            if (_currentState.IsKeyDown(keys[i]))
            {
                return true;
            }
        }

        return IsGamepadButtonHeld(action) || IsJoystickHeld(action);
    }

    /// <inheritdoc />
    public bool IsPressed(InputAction action)
    {
        var keys = GetBoundKeys(action);
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (_currentState.IsKeyDown(key) && _previousState.IsKeyUp(key))
            {
                return true;
            }
        }

        return IsGamepadButtonPressed(action) || IsJoystickPressed(action);
    }

    /// <inheritdoc />
    public bool IsReleased(InputAction action)
    {
        var keys = GetBoundKeys(action);
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            if (_currentState.IsKeyUp(key) && _previousState.IsKeyDown(key))
            {
                return true;
            }
        }

        return IsGamepadButtonReleased(action) || IsJoystickReleased(action);
    }

    /// <summary>
    /// Replaces the full binding list for an action.
    /// </summary>
    /// <param name="action">Action to update.</param>
    /// <param name="keys">One or more keys that will trigger the action.</param>
    public void SetBinding(InputAction action, params Keys[] keys)
    {
        if (keys is null || keys.Length == 0)
        {
            throw new ArgumentException("At least one key is required for an input binding.", nameof(keys));
        }

        _bindings[action] = keys;
    }

    /// <inheritdoc />
    public bool IsMouseLeftPressed()
    {
        // Polling-based detection (works for slow clicks).
        var polled = _currentMouseState.LeftButton == ButtonState.Pressed
            && _previousMouseState.LeftButton == ButtonState.Released;

        // SDL2 event-based detection (catches fast clicks that polling misses on macOS).
        return polled || _sdl2Mouse.WasLeftClickedThisFrame;
    }

    /// <inheritdoc />
    public bool IsMouseLeftReleased()
    {
        // Polling-based detection (works for slow clicks).
        var polled = _currentMouseState.LeftButton == ButtonState.Released
            && _previousMouseState.LeftButton == ButtonState.Pressed;

        // SDL2 event-based detection (catches fast clicks that polling misses on macOS).
        return polled || _sdl2Mouse.WasLeftReleasedThisFrame;
    }

    /// <inheritdoc />
    public Point GetMousePosition()
    {
        // If the SDL2 listener captured a click event, use its position
        // (it's more accurate for fast clicks where GetState() missed the press).
        if (_sdl2Mouse.WasLeftClickedThisFrame || _sdl2Mouse.WasLeftReleasedThisFrame)
        {
            return _sdl2Mouse.LastEventPosition;
        }

        return _currentMouseState.Position;
    }

    /// <summary>
    /// Clears the buffered SDL2 mouse events. Must be called at the end of
    /// each frame's input processing so events don't carry over.
    /// </summary>
    public void EndFrame()
    {
        _sdl2Mouse.ConsumeFrame();
    }

    private Keys[] GetBoundKeys(InputAction action)
    {
        if (_bindings.TryGetValue(action, out var keys))
        {
            return keys;
        }

        throw new InvalidOperationException($"No key binding exists for action '{action}'.");
    }

    private static Dictionary<InputAction, Keys[]> CreateDefaultBindings()
    {
        return new Dictionary<InputAction, Keys[]>
        {
            [InputAction.MoveUp] = new[] { Keys.W, Keys.Up },
            [InputAction.MoveDown] = new[] { Keys.S, Keys.Down },
            [InputAction.MoveLeft] = new[] { Keys.A, Keys.Left },
            [InputAction.MoveRight] = new[] { Keys.D, Keys.Right },
            [InputAction.Confirm] = new[] { Keys.Space, Keys.Enter },
            [InputAction.Cancel] = new[] { Keys.Back },
            [InputAction.Exit] = new[] { Keys.F12 },
            [InputAction.ToggleCollisionDebug] = new[] { Keys.U },
            [InputAction.ToggleCrtFilter] = new[] { Keys.F9 },
            [InputAction.CopyScreenshotToClipboard] = new[] { Keys.P },
            [InputAction.Pause] = new[] { Keys.Escape },
            [InputAction.QuickSave] = new[] { Keys.K },
            [InputAction.QuickLoad] = new[] { Keys.L }
        };
    }

    /// <summary>
    /// Default gamepad bindings for an NES-style controller (Hyperkin Cadet).
    /// D-pad AND left-thumbstick for movement (USB retro controllers often report
    /// the D-pad as thumbstick axis values rather than hat buttons), A = Confirm,
    /// B = Cancel, Start = Pause. Debug/dev actions remain keyboard-only.
    /// </summary>
    private static Dictionary<InputAction, Buttons[]> CreateDefaultGamepadBindings()
    {
        return new Dictionary<InputAction, Buttons[]>
        {
            [InputAction.MoveUp] = new[] { Buttons.DPadUp, Buttons.LeftThumbstickUp },
            [InputAction.MoveDown] = new[] { Buttons.DPadDown, Buttons.LeftThumbstickDown },
            [InputAction.MoveLeft] = new[] { Buttons.DPadLeft, Buttons.LeftThumbstickLeft },
            [InputAction.MoveRight] = new[] { Buttons.DPadRight, Buttons.LeftThumbstickRight },
            [InputAction.Confirm] = new[] { Buttons.A },
            [InputAction.Cancel] = new[] { Buttons.B },
            [InputAction.Pause] = new[] { Buttons.Start }
        };
    }

    private bool IsGamepadButtonHeld(InputAction action)
    {
        if (!_gamepadBindings.TryGetValue(action, out var buttons))
        {
            return false;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            if (_currentGamePadState.IsButtonDown(buttons[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGamepadButtonPressed(InputAction action)
    {
        if (!_gamepadBindings.TryGetValue(action, out var buttons))
        {
            return false;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (_currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsGamepadButtonReleased(InputAction action)
    {
        if (!_gamepadBindings.TryGetValue(action, out var buttons))
        {
            return false;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (_currentGamePadState.IsButtonUp(button) && _previousGamePadState.IsButtonDown(button))
            {
                return true;
            }
        }

        return false;
    }

    // ── Joystick fallback (unmapped USB controllers) ──────────────────

    /// <summary>
    /// Hat direction flags used by joystick hat bindings.
    /// </summary>
    private enum JoystickHatDirection { Up, Down, Left, Right }

    /// <summary>
    /// Default joystick button bindings for the Hyperkin Cadet NES controller.
    /// Button indices discovered via <see cref="JoystickDiagnostic"/>:
    /// A = B1, B = B0, Start = B9.
    /// </summary>
    private static Dictionary<InputAction, int[]> CreateDefaultJoystickButtonBindings()
    {
        return new Dictionary<InputAction, int[]>
        {
            [InputAction.Confirm] = new[] { 1 },  // A button
            [InputAction.Cancel] = new[] { 0 },   // B button
            [InputAction.Pause] = new[] { 9 }     // Start button
        };
    }

    /// <summary>
    /// Default joystick hat bindings. D-pad = Hat0 for all USB NES controllers.
    /// </summary>
    private static Dictionary<InputAction, JoystickHatDirection> CreateDefaultJoystickHatBindings()
    {
        return new Dictionary<InputAction, JoystickHatDirection>
        {
            [InputAction.MoveUp] = JoystickHatDirection.Up,
            [InputAction.MoveDown] = JoystickHatDirection.Down,
            [InputAction.MoveLeft] = JoystickHatDirection.Left,
            [InputAction.MoveRight] = JoystickHatDirection.Right
        };
    }

    private bool IsJoystickHeld(InputAction action)
    {
        if (!_currentJoystickState.IsConnected)
        {
            return false;
        }

        if (_joystickHatBindings.TryGetValue(action, out var hatDir)
            && IsHatActive(_currentJoystickState, hatDir))
        {
            return true;
        }

        if (_joystickButtonBindings.TryGetValue(action, out var buttonIndices))
        {
            for (var i = 0; i < buttonIndices.Length; i++)
            {
                var idx = buttonIndices[i];
                if (idx < _currentJoystickState.Buttons.Length
                    && _currentJoystickState.Buttons[idx])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsJoystickPressed(InputAction action)
    {
        if (!_currentJoystickState.IsConnected)
        {
            return false;
        }

        if (_joystickHatBindings.TryGetValue(action, out var hatDir))
        {
            if (IsHatActive(_currentJoystickState, hatDir)
                && !IsHatActive(_previousJoystickState, hatDir))
            {
                return true;
            }
        }

        if (_joystickButtonBindings.TryGetValue(action, out var buttonIndices))
        {
            for (var i = 0; i < buttonIndices.Length; i++)
            {
                var idx = buttonIndices[i];
                if (idx < _currentJoystickState.Buttons.Length
                    && _currentJoystickState.Buttons[idx]
                    && idx < _previousJoystickState.Buttons.Length
                    && !_previousJoystickState.Buttons[idx])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsJoystickReleased(InputAction action)
    {
        if (!_currentJoystickState.IsConnected)
        {
            return false;
        }

        if (_joystickHatBindings.TryGetValue(action, out var hatDir))
        {
            if (!IsHatActive(_currentJoystickState, hatDir)
                && IsHatActive(_previousJoystickState, hatDir))
            {
                return true;
            }
        }

        if (_joystickButtonBindings.TryGetValue(action, out var buttonIndices))
        {
            for (var i = 0; i < buttonIndices.Length; i++)
            {
                var idx = buttonIndices[i];
                if (idx < _currentJoystickState.Buttons.Length
                    && !_currentJoystickState.Buttons[idx]
                    && idx < _previousJoystickState.Buttons.Length
                    && _previousJoystickState.Buttons[idx])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsHatActive(JoystickSnapshot snapshot, JoystickHatDirection direction)
    {
        return direction switch
        {
            JoystickHatDirection.Up => snapshot.HatUp,
            JoystickHatDirection.Down => snapshot.HatDown,
            JoystickHatDirection.Left => snapshot.HatLeft,
            JoystickHatDirection.Right => snapshot.HatRight,
            _ => false
        };
    }

    /// <summary>
    /// Releases native resources owned by the SDL2 mouse listener.
    /// </summary>
    public void Dispose()
    {
        _sdl2Mouse.Dispose();
    }
}
