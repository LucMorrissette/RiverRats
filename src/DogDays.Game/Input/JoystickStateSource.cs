using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Production joystick source that polls the first joystick device and
/// converts the <see cref="JoystickState"/> into a <see cref="JoystickSnapshot"/>.
/// </summary>
public sealed class JoystickStateSource : IJoystickStateSource
{
    /// <inheritdoc />
    public JoystickSnapshot GetState()
    {
        var state = Joystick.GetState(0);
        if (!state.IsConnected)
        {
            return JoystickSnapshot.Disconnected;
        }

        var hatUp = state.Hats.Length > 0 && state.Hats[0].Up == ButtonState.Pressed;
        var hatDown = state.Hats.Length > 0 && state.Hats[0].Down == ButtonState.Pressed;
        var hatLeft = state.Hats.Length > 0 && state.Hats[0].Left == ButtonState.Pressed;
        var hatRight = state.Hats.Length > 0 && state.Hats[0].Right == ButtonState.Pressed;

        var buttons = new bool[state.Buttons.Length];
        for (var i = 0; i < state.Buttons.Length; i++)
        {
            buttons[i] = state.Buttons[i] == ButtonState.Pressed;
        }

        return new JoystickSnapshot(true, hatUp, hatDown, hatLeft, hatRight, buttons);
    }
}
