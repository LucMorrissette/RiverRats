namespace DogDays.Game.Data;

/// <summary>
/// Cardinal facing direction for entities.
/// Values match sprite sheet row indices (Down=0, Left=1, Right=2, Up=3).
/// </summary>
public enum FacingDirection
{
    /// <summary>Facing the camera (south).</summary>
    Down = 0,

    /// <summary>Facing left (west).</summary>
    Left = 1,

    /// <summary>Facing right (east).</summary>
    Right = 2,

    /// <summary>Facing away from the camera (north).</summary>
    Up = 3,
}
