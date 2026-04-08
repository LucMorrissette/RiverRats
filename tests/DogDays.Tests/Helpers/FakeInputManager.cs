using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DogDays.Game.Input;

namespace DogDays.Tests.Helpers;

/// <summary>
/// Scriptable input fake for integration tests.
/// </summary>
public sealed class FakeInputManager : IInputManager
{
    private readonly HashSet<InputAction> _held = new();
    private readonly HashSet<InputAction> _pressed = new();
    private readonly HashSet<InputAction> _released = new();
    private bool _mouseLeftPressed;
    private bool _mouseLeftReleased;
    private Point _mousePosition;

    /// <summary>Clears one-frame pressed/released flags and keeps held states.</summary>
    public void Update()
    {
        _pressed.Clear();
        _released.Clear();
        _mouseLeftPressed = false;
        _mouseLeftReleased = false;
    }

    /// <summary>Sets an action as pressed for this frame and held until released.</summary>
    public void Press(InputAction action)
    {
        _pressed.Add(action);
        _held.Add(action);
        _released.Remove(action);
    }

    /// <summary>Sets an action as released for this frame and not held afterward.</summary>
    public void Release(InputAction action)
    {
        _released.Add(action);
        _held.Remove(action);
        _pressed.Remove(action);
    }

    /// <summary>Sets a left mouse click at the given position for this frame.</summary>
    public void ClickMouse(Point position)
    {
        _mouseLeftPressed = true;
        _mousePosition = position;
    }

    /// <summary>Sets a left mouse release at the given position for this frame.</summary>
    public void ReleaseMouse(Point position)
    {
        _mouseLeftReleased = true;
        _mousePosition = position;
    }

    /// <inheritdoc />
    public bool IsMouseLeftPressed() => _mouseLeftPressed;

    /// <inheritdoc />
    public bool IsMouseLeftReleased() => _mouseLeftReleased;

    /// <inheritdoc />
    public Point GetMousePosition() => _mousePosition;

    /// <inheritdoc />
    public bool IsHeld(InputAction action)
    {
        return _held.Contains(action);
    }

    /// <inheritdoc />
    public bool IsPressed(InputAction action)
    {
        return _pressed.Contains(action);
    }

    /// <inheritdoc />
    public bool IsReleased(InputAction action)
    {
        return _released.Contains(action);
    }

    /// <inheritdoc />
    public void EndFrame()
    {
    }
}
