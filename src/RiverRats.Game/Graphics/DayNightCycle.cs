using Microsoft.Xna.Framework;

namespace RiverRats.Game.Graphics;

/// <summary>
/// Tracks a looping day/night cycle and produces a multiply-blend tint color.
/// The cycle progresses linearly through Night → Dawn → Day → Dusk → Night.
/// </summary>
public sealed class DayNightCycle
{
    /// <summary>Cycle progress where night ends and dawn begins.</summary>
    private const float NightEnd = 0.20f;

    /// <summary>Cycle progress where dawn ends and full day begins.</summary>
    private const float DawnEnd = 0.30f;

    /// <summary>Cycle progress where day ends and dusk begins.</summary>
    private const float DayEnd = 0.70f;

    /// <summary>Cycle progress where dusk ends and night begins.</summary>
    private const float DuskEnd = 0.80f;

    /// <summary>Multiply tint during full night — dark blue cast.</summary>
    private static readonly Color NightTint = new(60, 60, 120);

    /// <summary>Multiply tint during full day — no change (identity).</summary>
    private static readonly Color DayTint = Color.White;

    private readonly float _cycleDurationSeconds;
    private float _elapsedSeconds;

    /// <summary>
    /// Creates a day/night cycle.
    /// </summary>
    /// <param name="cycleDurationSeconds">Total duration of one full cycle in seconds.</param>
    /// <param name="startProgress">Initial position in the cycle, 0–1 (0 = start of night, 0.3 = start of day).</param>
    public DayNightCycle(float cycleDurationSeconds, float startProgress = 0f)
    {
        _cycleDurationSeconds = cycleDurationSeconds;
        _elapsedSeconds = MathHelper.Clamp(startProgress, 0f, 1f) * cycleDurationSeconds;
        CurrentTint = ComputeTint(CycleProgress);
    }

    /// <summary>Current position in the cycle, normalized 0–1.</summary>
    public float CycleProgress => (_elapsedSeconds % _cycleDurationSeconds) / _cycleDurationSeconds;

    /// <summary>Current tint color to apply to the scene via multiply blend.</summary>
    public Color CurrentTint { get; private set; }

    /// <summary>
    /// Advances the cycle clock by the elapsed frame time.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
        CurrentTint = ComputeTint(CycleProgress);
    }

    private static Color ComputeTint(float progress)
    {
        if (progress < NightEnd)
            return NightTint;

        if (progress < DawnEnd)
        {
            var t = (progress - NightEnd) / (DawnEnd - NightEnd);
            return Color.Lerp(NightTint, DayTint, t);
        }

        if (progress < DayEnd)
            return DayTint;

        if (progress < DuskEnd)
        {
            var t = (progress - DayEnd) / (DuskEnd - DayEnd);
            return Color.Lerp(DayTint, NightTint, t);
        }

        return NightTint;
    }
}
