using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RiverRats.Game.Input;

/// <summary>
/// Keyboard-backed input manager that tracks previous/current frame states
/// and exposes action-based hold/press/release queries.
/// Uses an SDL2 event listener to reliably detect fast mouse clicks on macOS,
/// where <c>Mouse.GetState()</c> polling misses press+release cycles that
/// complete between two consecutive polls.
/// </summary>
public sealed class InputManager : IInputManager, IDisposable
{
    private readonly Dictionary<InputAction, Keys[]> _bindings;
    private readonly IKeyboardStateSource _keyboardStateSource;
    private readonly Sdl2MouseListener _sdl2Mouse = new();

    private KeyboardState _previousState;
    private KeyboardState _currentState;
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    /// <summary>
    /// Creates an input manager with default key bindings and a production keyboard source.
    /// </summary>
    public InputManager()
        : this(new KeyboardStateSource())
    {
    }

    /// <summary>
    /// Creates an input manager with default key bindings and a custom keyboard source.
    /// </summary>
    /// <param name="keyboardStateSource">Keyboard source used to sample current state each frame.</param>
    public InputManager(IKeyboardStateSource keyboardStateSource)
    {
        _keyboardStateSource = keyboardStateSource ?? throw new ArgumentNullException(nameof(keyboardStateSource));
        _bindings = CreateDefaultBindings();
        _previousState = _keyboardStateSource.GetState();
        _currentState = _previousState;
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

        return false;
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

        return false;
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

        return false;
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
            [InputAction.CopyScreenshotToClipboard] = new[] { Keys.P },
            [InputAction.Pause] = new[] { Keys.Escape }
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
