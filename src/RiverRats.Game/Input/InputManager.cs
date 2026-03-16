using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace RiverRats.Game.Input;

/// <summary>
/// Keyboard-backed input manager that tracks previous/current frame states
/// and exposes action-based hold/press/release queries.
/// </summary>
public sealed class InputManager : IInputManager
{
    private readonly Dictionary<InputAction, Keys[]> _bindings;
    private readonly IKeyboardStateSource _keyboardStateSource;

    private KeyboardState _previousState;
    private KeyboardState _currentState;

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
    }

    /// <inheritdoc />
    public void Update()
    {
        _previousState = _currentState;
        _currentState = _keyboardStateSource.GetState();
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
            [InputAction.Cancel] = new[] { Keys.Escape, Keys.Back },
            [InputAction.Exit] = new[] { Keys.Escape },
            [InputAction.ToggleCollisionDebug] = new[] { Keys.U }
        };
    }
}
