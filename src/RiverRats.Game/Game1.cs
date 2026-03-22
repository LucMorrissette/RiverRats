using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Input;
using RiverRats.Game.Screens;
#if WINDOWS
using System.Threading;
using Clipboard = System.Windows.Forms.Clipboard;
using DrawingBitmap = System.Drawing.Bitmap;
#endif

namespace RiverRats.Game;

/// <summary>
/// Main game class. Owns the virtual-resolution pipeline and delegates all
/// gameplay to the <see cref="ScreenManager"/>.
/// </summary>
public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 480;
    private const int VirtualHeight = 270;
    private const int StartupScale = 3;

    private readonly GraphicsDeviceManager _graphics;
    private readonly IInputManager _inputManager;
    private readonly ScreenManager _screenManager;

    private SpriteBatch _spriteBatch;
    private RenderTarget2D _sceneRenderTarget;
    private Rectangle _sceneDestination;
    private bool _copyScreenshotRequested;
#if WINDOWS
    private Color[] _screenshotBuffer;
#endif

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "River Rats";
        Window.ClientSizeChanged += OnClientSizeChanged;

        // Reach profile is compatible with DesktopGL / macOS / Linux.
        _graphics.GraphicsProfile = GraphicsProfile.Reach;

        _inputManager = new InputManager();
        _screenManager = new ScreenManager();

        _graphics.PreferredBackBufferWidth = VirtualWidth * StartupScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * StartupScale;
    }

    protected override void Initialize()
    {
        RecalculateSceneDestination();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _sceneRenderTarget = new RenderTarget2D(
            GraphicsDevice,
            VirtualWidth,
            VirtualHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            // PreserveContents required because the RT is switched away from and
            // back to during multi-pass per-screen rendering.
            RenderTargetUsage.PreserveContents);

        var gameplayScreen = new GameplayScreen(
            GraphicsDevice,
            Content,
            VirtualWidth,
            VirtualHeight,
            Exit);
        _screenManager.Push(gameplayScreen);
    }

    protected override void Update(GameTime gameTime)
    {
        _inputManager.Update();
        if (_inputManager.IsPressed(InputAction.CopyScreenshotToClipboard))
        {
            _copyScreenshotRequested = true;
        }

        _screenManager.Update(gameTime, _inputManager);

        _inputManager.EndFrame();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
        GraphicsDevice.Clear(Color.Black);

        _screenManager.Draw(gameTime, _spriteBatch);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_sceneRenderTarget, _sceneDestination, Color.White);
        _spriteBatch.End();

        // --- Overlay pass: HUD at native window resolution for crisp font rendering ---
        var viewport = GraphicsDevice.Viewport;
        var scaleX = viewport.Width / VirtualWidth;
        var scaleY = viewport.Height / VirtualHeight;
        var sceneScale = Math.Max(1, Math.Min(scaleX, scaleY));
        _screenManager.DrawOverlay(gameTime, _spriteBatch, sceneScale);

        if (_copyScreenshotRequested)
        {
            CopySceneRenderTargetToClipboard();
            _copyScreenshotRequested = false;
        }

        base.Draw(gameTime);
    }

    private void CopySceneRenderTargetToClipboard()
    {
#if WINDOWS
        if (_sceneRenderTarget is null)
        {
            return;
        }

        var pixelCount = _sceneRenderTarget.Width * _sceneRenderTarget.Height;
        if (_screenshotBuffer is null || _screenshotBuffer.Length != pixelCount)
        {
            _screenshotBuffer = new Color[pixelCount];
        }

        _sceneRenderTarget.GetData(_screenshotBuffer);

        using var bitmap = new DrawingBitmap(_sceneRenderTarget.Width, _sceneRenderTarget.Height);
        for (var y = 0; y < _sceneRenderTarget.Height; y++)
        {
            var rowOffset = y * _sceneRenderTarget.Width;
            for (var x = 0; x < _sceneRenderTarget.Width; x++)
            {
                var color = _screenshotBuffer[rowOffset + x];
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
            }
        }

        using var clipboardImage = new DrawingBitmap(bitmap);
        var clipboardThread = new Thread(() => Clipboard.SetImage(clipboardImage));
        clipboardThread.SetApartmentState(ApartmentState.STA);
        clipboardThread.Start();
        clipboardThread.Join();
#endif
    }

    private void RecalculateSceneDestination()
    {
        var viewport = GraphicsDevice.Viewport;
        var scaleX = viewport.Width / VirtualWidth;
        var scaleY = viewport.Height / VirtualHeight;
        var scale = Math.Max(1, Math.Min(scaleX, scaleY));

        var scaledWidth = VirtualWidth * scale;
        var scaledHeight = VirtualHeight * scale;
        var offsetX = (viewport.Width - scaledWidth) / 2;
        var offsetY = (viewport.Height - scaledHeight) / 2;

        _sceneDestination = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        if (GraphicsDevice is not null)
        {
            RecalculateSceneDestination();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            (_inputManager as IDisposable)?.Dispose();
        }

        base.Dispose(disposing);
    }
}
