using Microsoft.Xna.Framework;

namespace RiverRats.Game.Entities;

/// <summary>
/// A pooled health pickup (pill) that auto-despawns after a fixed lifetime,
/// fading out near the end. Managed externally by a spawning system.
/// </summary>
internal sealed class HealthPickup
{
    private const float DespawnTime = 10f;
    private const float FadeStartTime = 7f;
    private const float PickupRadius = 16f;

    /// <summary>Squared pickup radius for distance checks without sqrt.</summary>
    internal const float PickupRadiusSq = PickupRadius * PickupRadius;

    private Vector2 _position;
    private float _age;
    private bool _active;

    /// <summary>Current world position of the pickup.</summary>
    internal Vector2 Position => _position;

    /// <summary>Whether the pickup is currently active and collectible.</summary>
    internal bool IsActive => _active;

    /// <summary>
    /// Visual opacity (1 = fully visible, 0 = invisible).
    /// Fades linearly from 1 to 0 between <see cref="FadeStartTime"/> and <see cref="DespawnTime"/>.
    /// </summary>
    internal float Opacity => _age >= FadeStartTime
        ? 1f - (_age - FadeStartTime) / (DespawnTime - FadeStartTime)
        : 1f;

    /// <summary>Axis-aligned bounding rectangle (16×16) centred on the pickup position.</summary>
    internal Rectangle Bounds => new(
        (int)_position.X - 8,
        (int)_position.Y - 8,
        16, 16);

    /// <summary>
    /// Activates this pooled pickup at the given world position, resetting its age.
    /// </summary>
    /// <param name="position">World position to place the pickup.</param>
    internal void Spawn(Vector2 position)
    {
        _position = position;
        _age = 0f;
        _active = true;
    }

    /// <summary>Immediately deactivates this pickup (e.g. after collection).</summary>
    internal void Deactivate()
    {
        _active = false;
    }

    /// <summary>
    /// Updates age and auto-despawns when the lifetime expires.
    /// </summary>
    /// <param name="dt">Elapsed time in seconds since the last frame.</param>
    internal void Update(float dt)
    {
        if (!_active) return;
        _age += dt;
        if (_age >= DespawnTime)
            _active = false;
    }
}
