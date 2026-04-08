namespace DogDays.Game.Screens;

/// <summary>
/// Pure timing state machine extracted from <see cref="DeathScreen"/> so that
/// the elapsed/fade logic can be unit-tested without a GraphicsDevice.
/// </summary>
public sealed class DeathScreenTimingLogic
{
    /// <summary>Seconds before the "Press any key" prompt appears.</summary>
    public const float PromptDelay = 3.0f;

    /// <summary>Seconds before auto-transitioning to the overworld.</summary>
    public const float AutoTransitionDelay = 18.0f;

    /// <summary>Duration of the fade-in from black at the start.</summary>
    public const float FadeInDuration = 1.5f;

    /// <summary>Duration of the fade-out to black before transitioning.</summary>
    public const float FadeOutDuration = 2.0f;

    private float _elapsed;
    private bool _fadingOut;
    private float _fadeOutTimer;
    private bool _transitioning;

    /// <summary>Total elapsed time since the screen became active.</summary>
    public float Elapsed => _elapsed;

    /// <summary>Whether the fade-out phase has begun.</summary>
    public bool IsFadingOut => _fadingOut;

    /// <summary>How long the fade-out has been running (0 → FadeOutDuration).</summary>
    public float FadeOutTimer => _fadeOutTimer;

    /// <summary>Whether the transition to the next screen has been triggered.</summary>
    public bool IsTransitioning => _transitioning;

    /// <summary>
    /// True when the "Press any key" prompt should be visible
    /// (elapsed >= PromptDelay and not yet fading out).
    /// </summary>
    public bool ShowPrompt => _elapsed >= PromptDelay && !_fadingOut;

    /// <summary>
    /// Alpha of the fade-in overlay (1 = fully black, 0 = fully transparent).
    /// Only meaningful while elapsed &lt; FadeInDuration.
    /// </summary>
    public float FadeInAlpha =>
        _elapsed < FadeInDuration
            ? 1f - System.Math.Clamp(_elapsed / FadeInDuration, 0f, 1f)
            : 0f;

    /// <summary>
    /// Alpha of the fade-out overlay (0 = transparent, 1 = fully black).
    /// Only meaningful while IsFadingOut is true.
    /// </summary>
    public float FadeOutAlpha =>
        _fadingOut
            ? System.Math.Clamp(_fadeOutTimer / FadeOutDuration, 0f, 1f)
            : 0f;

    /// <summary>
    /// Advances the timing state by <paramref name="dt"/> seconds.
    /// Returns <c>true</c> if the fade-out has completed and
    /// <see cref="IsTransitioning"/> just became true this tick.
    /// </summary>
    /// <param name="dt">Elapsed seconds this frame.</param>
    /// <param name="inputPressed">Whether the player pressed a confirm/move key.</param>
    /// <returns><c>true</c> when the screen should hand off to the next screen.</returns>
    public bool Tick(float dt, bool inputPressed)
    {
        _elapsed += dt;

        if (_fadingOut)
        {
            _fadeOutTimer += dt;
            if (_fadeOutTimer >= FadeOutDuration && !_transitioning)
            {
                _transitioning = true;
                return true;
            }

            return false;
        }

        // After prompt delay, player input triggers fade-out.
        if (_elapsed >= PromptDelay && inputPressed)
        {
            BeginFadeOut();
            return false;
        }

        // Auto-transition after timeout.
        if (_elapsed >= AutoTransitionDelay)
        {
            BeginFadeOut();
        }

        return false;
    }

    /// <summary>Begins the fade-out phase (idempotent).</summary>
    public void BeginFadeOut()
    {
        if (_fadingOut || _transitioning) return;
        _fadingOut = true;
        _fadeOutTimer = 0f;
    }
}
