using Microsoft.Xna.Framework.Input;

namespace DogDays.Game.Input;

/// <summary>
/// Lightweight snapshot of the joystick inputs we care about.
/// Unlike <see cref="JoystickState"/>, this struct has a public constructor
/// so tests can create instances without hardware.
/// </summary>
public readonly struct JoystickSnapshot
{
    /// <summary>Whether a joystick device is connected.</summary>
    public readonly bool IsConnected;

    /// <summary>Hat 0 direction states (D-pad).</summary>
    public readonly bool HatUp;
    public readonly bool HatDown;
    public readonly bool HatLeft;
    public readonly bool HatRight;

    /// <summary>Raw button states indexed by button number.</summary>
    public readonly bool[] Buttons;

    /// <summary>Creates a snapshot with the specified state.</summary>
    public JoystickSnapshot(
        bool isConnected,
        bool hatUp = false,
        bool hatDown = false,
        bool hatLeft = false,
        bool hatRight = false,
        bool[] buttons = null)
    {
        IsConnected = isConnected;
        HatUp = hatUp;
        HatDown = hatDown;
        HatLeft = hatLeft;
        HatRight = hatRight;
        Buttons = buttons ?? System.Array.Empty<bool>();
    }

    /// <summary>An empty disconnected snapshot.</summary>
    public static JoystickSnapshot Disconnected => new(false);
}

/// <summary>
/// Abstracts raw joystick state polling for deterministic testing.
/// Used as fallback when the controller has no SDL2 game controller mapping
/// (e.g., Hyperkin Cadet NES USB controller).
/// </summary>
public interface IJoystickStateSource
{
    /// <summary>Samples the current raw joystick state as a snapshot.</summary>
    JoystickSnapshot GetState();
}
