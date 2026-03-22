using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Audio;
using RiverRats.Game.Input;

namespace RiverRats.Game.Screens;

/// <summary>
/// A transparent overlay screen that dims music and draws a dark overlay when the game is paused.
/// </summary>
public sealed class PauseScreen : IGameScreen
{
    private const float PausedMusicVolume = 0.2f;
    private const float NormalMusicVolume = 1.0f;
    private static readonly Color OverlayColor = new(0, 0, 0, 128); // 50% black
    private const string PausedText = "Game Paused";
    private const float PausedFontSize = 24f;

    private readonly ScreenManager _screenManager;
    private readonly IMusicManager _musicManager;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private Texture2D _pixelTexture; // 1×1 white pixel for overlay drawing
    private FontSystem _fontSystem;

    public bool IsTransparent => true;

    public PauseScreen(
        ScreenManager screenManager,
        IMusicManager musicManager,
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight)
    {
        _screenManager = screenManager;
        _musicManager = musicManager;
        _graphicsDevice = graphicsDevice;
        _content = content;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
    }

    public void LoadContent()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _musicManager.SetVolume(PausedMusicVolume);

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(_content.RootDirectory, "Fonts", "Nunito.ttf")));
    }

    public void Update(GameTime gameTime, IInputManager input)
    {
        if (input.IsPressed(InputAction.Pause))
        {
            _screenManager.Pop();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _virtualWidth, _virtualHeight), OverlayColor);
        spriteBatch.End();
    }

    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        var font = _fontSystem.GetFont(PausedFontSize * sceneScale);
        var textSize = font.MeasureString(PausedText);

        // Center the text in the viewport
        var viewport = _graphicsDevice.Viewport;
        var position = new Vector2(
            (viewport.Width - textSize.X) / 2f,
            (viewport.Height - textSize.Y) / 2f);

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);
        spriteBatch.DrawString(font, PausedText, position, Color.White);
        spriteBatch.End();
    }

    public void UnloadContent()
    {
        _musicManager.SetVolume(NormalMusicVolume);
        _pixelTexture?.Dispose();
        _fontSystem?.Dispose();
    }
}
