using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Production keyboard state provider that reads directly from MonoGame input.
/// </summary>
public sealed class KeyboardStateSource : IKeyboardStateSource
{
    /// <inheritdoc />
    public KeyboardState GetState()
    {
        return Keyboard.GetState();
    }
}
