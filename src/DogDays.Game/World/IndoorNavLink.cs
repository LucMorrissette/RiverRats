namespace DogDays.Game.World;

/// <summary>
/// An explicit bidirectional connection between two <see cref="IndoorNavNode"/> instances.
/// </summary>
/// <param name="NodeIdA">First endpoint node id.</param>
/// <param name="NodeIdB">Second endpoint node id.</param>
public readonly record struct IndoorNavLink(int NodeIdA, int NodeIdB);
