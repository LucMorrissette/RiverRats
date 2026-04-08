using Microsoft.Xna.Framework;

namespace DogDays.Game.Input;

/// <summary>
/// Abstraction for querying frame-to-frame input state by logical action.
/// </summary>
public interface IInputManager
{
    /// <summary>Advances input state to the current frame.</summary>
    void Update();

    /// <summary>Returns true while at least one key bound to the action is held.</summary>
    bool IsHeld(InputAction action);

    /// <summary>Returns true only on the frame a bound key transitions up -> down.</summary>
    bool IsPressed(InputAction action);

    /// <summary>Returns true only on the frame a bound key transitions down -> up.</summary>
    bool IsReleased(InputAction action);

    /// <summary>
    /// Returns true only on the frame the left mouse button is pressed down.
    /// Uses SDL2 event buffering to reliably detect fast clicks on macOS.
    /// </summary>
    bool IsMouseLeftPressed();

    /// <summary>
    /// Returns true only on the frame the left mouse button is released.
    /// Uses SDL2 event buffering to reliably detect fast clicks on macOS.
    /// </summary>
    bool IsMouseLeftReleased();

    /// <summary>Gets the current mouse cursor position in physical window client coordinates.</summary>
    Point GetMousePosition();

    /// <summary>
    /// Signals the end of the current frame's input processing.
    /// Clears any buffered one-shot events so they don't carry over to the next frame.
    /// </summary>
    void EndFrame();
}
