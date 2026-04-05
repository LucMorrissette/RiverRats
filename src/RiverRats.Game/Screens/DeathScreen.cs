using System;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Audio;
using RiverRats.Game.Core;
using RiverRats.Game.Input;

namespace RiverRats.Game.Screens;

/// <summary>
/// Full-screen death screen with CRT static noise and "MISSION FAILED" text.
/// Shown when the player dies in the forest, then transitions back to the overworld.
/// </summary>
public sealed class DeathScreen : IGameScreen
{
    /// <summary>Seconds before the "Press any key" prompt appears.</summary>
    private const float PromptDelay = 3.0f;

    /// <summary>Seconds before auto-transitioning to the overworld.</summary>
    private const float AutoTransitionDelay = 18.0f;

    /// <summary>Duration of the fade-in from black at the start.</summary>
    private const float FadeInDuration = 1.5f;

    /// <summary>Duration of the fade-out to black before transitioning.</summary>
    private const float FadeOutDuration = 2.0f;

    /// <summary>Width of the low-res noise texture (stretched to fill screen for chunky CRT look).</summary>
    private const int NoiseWidth = 120;

    /// <summary>Height of the low-res noise texture.</summary>
    private const int NoiseHeight = 68;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly GameSessionServices _gameSessionServices;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly ScreenManager _screenManager;
    private readonly Action _requestExit;
    private readonly float _dayNightProgress;
    private readonly IMusicManager _musicManager;

    private Texture2D _noiseTexture;
    private Color[] _noisePixels;
    private Texture2D _pixelTexture;
    private FontSystem _fontSystem;
    private Random _rng;
    private float _elapsed;
    private bool _transitioning;
    private bool _fadingOut;
    private float _fadeOutTimer;

    /// <inheritdoc />
    public bool IsTransparent => false;

    /// <summary>
    /// Creates a new death screen.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="content">Content manager for loading fonts.</param>
    /// <param name="virtualWidth">Virtual resolution width.</param>
    /// <param name="virtualHeight">Virtual resolution height.</param>
    /// <param name="screenManager">Screen manager for transitions.</param>
    /// <param name="requestExit">Callback to request game exit.</param>
    /// <param name="musicManager">Music manager to stop background music.</param>
    /// <param name="dayNightProgress">Current day/night cycle progress to pass to the overworld.</param>
    internal DeathScreen(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight,
        ScreenManager screenManager,
        GameSessionServices gameSessionServices,
        Action requestExit,
        IMusicManager musicManager,
        float dayNightProgress)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _screenManager = screenManager;
        _gameSessionServices = gameSessionServices;
        _requestExit = requestExit;
        _musicManager = musicManager;
        _dayNightProgress = dayNightProgress;
    }

    /// <inheritdoc />
    public void LoadContent()
    {
        _musicManager.PlaySong("ForestFailSong", loopDelaySeconds: -1f);
        _rng = new Random();

        _noiseTexture = new Texture2D(_graphicsDevice, NoiseWidth, NoiseHeight);
        _noisePixels = new Color[NoiseWidth * NoiseHeight];

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(global::System.AppContext.BaseDirectory, _content.RootDirectory, "Fonts", "Nunito.ttf")));
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime, IInputManager input)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsed += dt;

        // While fading out, count down and transition when done
        if (_fadingOut)
        {
            _fadeOutTimer += dt;
            if (_fadeOutTimer >= FadeOutDuration)
            {
                FinishTransition();
            }

            return;
        }

        RegenerateNoise();

        // After prompt appears, check for input to continue
        if (_elapsed >= PromptDelay)
        {
            if (input.IsPressed(InputAction.Confirm) ||
                input.IsPressed(InputAction.Pause) ||
                input.IsPressed(InputAction.MoveUp) ||
                input.IsPressed(InputAction.MoveDown) ||
                input.IsPressed(InputAction.MoveLeft) ||
                input.IsPressed(InputAction.MoveRight))
            {
                BeginFadeOut();
                return;
            }
        }

        // Auto-transition after timeout
        if (_elapsed >= AutoTransitionDelay)
        {
            BeginFadeOut();
        }
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Draw chunky CRT static noise stretched to fill the virtual resolution
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        spriteBatch.Draw(_noiseTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), Color.White);
        spriteBatch.End();

        // Semi-transparent dark overlay to dim the static
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), new Color(0, 0, 0, 120));

        // Fade-in: black overlay fading from opaque to transparent
        if (_elapsed < FadeInDuration)
        {
            var fadeInAlpha = 1f - MathHelper.Clamp(_elapsed / FadeInDuration, 0f, 1f);
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), Color.Black * fadeInAlpha);
        }

        // Fade-out: black overlay fading from transparent to opaque
        if (_fadingOut)
        {
            var fadeOutAlpha = MathHelper.Clamp(_fadeOutTimer / FadeOutDuration, 0f, 1f);
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), Color.Black * fadeOutAlpha);
        }

        spriteBatch.End();
    }

    /// <inheritdoc />
    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        // Compute overall opacity for fade-in and fade-out
        var overlayAlpha = 1f;
        if (_elapsed < FadeInDuration)
        {
            overlayAlpha = MathHelper.Clamp(_elapsed / FadeInDuration, 0f, 1f);
        }

        if (_fadingOut)
        {
            overlayAlpha *= 1f - MathHelper.Clamp(_fadeOutTimer / FadeOutDuration, 0f, 1f);
        }

        var screenWidth = _graphicsDevice.Viewport.Width;
        var screenHeight = _graphicsDevice.Viewport.Height;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // "MISSION FAILED" text with flicker
        var fontSize = 32f * sceneScale;
        var font = _fontSystem.GetFont(fontSize);
        var text = "MISSION FAILED";
        var textSize = font.MeasureString(text);
        var pos = new Vector2(
            (screenWidth - textSize.X) / 2f,
            (screenHeight - textSize.Y) / 2f - 20f * sceneScale);

        var flicker = 0.85f + 0.15f * MathF.Sin(_elapsed * 12f);
        var textColor = new Color(220, 30, 30) * flicker * overlayAlpha;

        // Shadow for readability against static
        var shadowOffset = MathF.Max(1f, sceneScale);
        spriteBatch.DrawString(font, text, pos + new Vector2(shadowOffset, shadowOffset), new Color(0, 0, 0, 180) * overlayAlpha);
        spriteBatch.DrawString(font, text, pos, textColor);

        // "Press any key" prompt after delay
        if (_elapsed >= PromptDelay && !_fadingOut)
        {
            var promptFont = _fontSystem.GetFont(14f * sceneScale);
            var promptText = "Press any key to continue";
            var promptSize = promptFont.MeasureString(promptText);
            var promptPos = new Vector2(
                (screenWidth - promptSize.X) / 2f,
                pos.Y + textSize.Y + 16f * sceneScale);

            var promptAlpha = (0.6f + 0.4f * MathF.Sin(_elapsed * 3f)) * overlayAlpha;
            spriteBatch.DrawString(promptFont, promptText, promptPos, Color.White * promptAlpha);
        }

        spriteBatch.End();
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _noiseTexture?.Dispose();
        _pixelTexture?.Dispose();
        _fontSystem?.Dispose();
    }

    /// <summary>
    /// Fills the noise texture with random grayscale pixels for the CRT static effect.
    /// Allocates via SetData each frame, but acceptable for this short-lived screen.
    /// </summary>
    private void RegenerateNoise()
    {
        for (var i = 0; i < _noisePixels.Length; i++)
        {
            var val = (byte)_rng.Next(256);
            _noisePixels[i] = new Color(val, val, val);
        }

        _noiseTexture.SetData(_noisePixels);
    }

    private void BeginFadeOut()
    {
        if (_fadingOut || _transitioning) return;
        _fadingOut = true;
        _fadeOutTimer = 0f;
    }

    private void FinishTransition()
    {
        if (_transitioning) return;
        _transitioning = true;

        _musicManager.StopSong();
        _screenManager.Replace(new GameplayScreen(
            _graphicsDevice,
            _content,
            _virtualWidth,
            _virtualHeight,
            _screenManager,
            _gameSessionServices,
            _requestExit,
            "Maps/StarterMap",
            "from-woods",
            fadeInFromBlack: true,
            dayNightStartProgress: _dayNightProgress));
    }
}
