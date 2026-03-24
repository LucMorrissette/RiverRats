using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RiverRats.Game.Input;

/// <summary>
/// Abstracts gamepad state polling for deterministic testing.
/// </summary>
public interface IGamePadStateSource
{
    /// <summary>Samples the current gamepad state.</summary>
    GamePadState GetState();
}
