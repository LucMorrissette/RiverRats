using System;
using Microsoft.Xna.Framework;

namespace DogDays.Tests.Helpers;

/// <summary>
/// Deterministic factories for creating GameTime instances in tests.
/// </summary>
public static class FakeGameTime
{
    /// <summary>
    /// Creates a GameTime representing a single frame at 60 FPS.
    /// </summary>
    public static GameTime OneFrame()
    {
        return FromSeconds(1f / 60f);
    }

    /// <summary>
    /// Creates a GameTime with the provided elapsed time.
    /// </summary>
    /// <param name="seconds">Elapsed frame duration in seconds.</param>
    public static GameTime FromSeconds(float seconds)
    {
        return new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(seconds));
    }
}
