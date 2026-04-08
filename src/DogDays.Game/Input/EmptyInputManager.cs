using Microsoft.Xna.Framework;

namespace DogDays.Game.Input;

/// <summary>
/// Null-object input manager that reports no input.
/// Useful for screens that should not process player controls.
/// </summary>
public sealed class EmptyInputManager : IInputManager
{
    /// <inheritdoc />
    public void Update()
    {
    }

    /// <inheritdoc />
    public bool IsHeld(InputAction action)
    {
        _ = action;
        return false;
    }

    /// <inheritdoc />
    public bool IsPressed(InputAction action)
    {
        _ = action;
        return false;
    }

    /// <inheritdoc />
    public bool IsReleased(InputAction action)
    {
        _ = action;
        return false;
    }

    /// <inheritdoc />
    public bool IsMouseLeftPressed() => false;

    /// <inheritdoc />
    public bool IsMouseLeftReleased() => false;

    /// <inheritdoc />
    public Point GetMousePosition() => Point.Zero;

    /// <inheritdoc />
    public void EndFrame()
    {
    }
}
