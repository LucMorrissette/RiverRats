using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Production gamepad source that polls the first controller.
/// </summary>
public sealed class GamePadStateSource : IGamePadStateSource
{
    /// <inheritdoc />
    public GamePadState GetState() => GamePad.GetState(PlayerIndex.One);
}
