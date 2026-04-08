using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Provides keyboard state snapshots. Abstracted for deterministic testing.
/// </summary>
public interface IKeyboardStateSource
{
    /// <summary>Gets the current keyboard state snapshot.</summary>
    KeyboardState GetState();
}
