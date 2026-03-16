using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using RiverRats.Game.Components;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Graphics;
using RiverRats.Game.Input;
using RiverRats.Game.World;

namespace RiverRats.Game.Screens;

/// <summary>
/// Primary gameplay screen. Owns the player, camera, world renderer, and gameplay loop.
/// </summary>
public sealed class GameplayScreen : IGameScreen
{
    private const int PlayerFramePixels = 32;
    private const int WorldTilePixels = 32;
    private const float PlayerMoveSpeedPixelsPerSecond = 96f;
    private const float PlayerAccelerationRate = 10f;
    private const int WalkFramesPerDirection = 4;
    private const float WalkFrameDuration = 0.15f;
    private const float DayNightCycleDurationSeconds = 60f;
    private const float DayNightCycleStartProgress = 0.30f;
    private static readonly Vector2 FollowerPositionOffset = new(0f, 32f);

    private static readonly Point[] BoulderTilePositions =
    {
        new(12, 10),
        new(12, 9),
        new(12, 11),
        new(11, 10),
        new(13, 10),
        new(14, 9),
        new(14, 10),
        new(14, 11)
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly Action _requestExit;

    /// <summary>Multiply blend: finalColor = sourceColor × destColor. White = no change, dark = darken.</summary>
    private static readonly BlendState MultiplyBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.DestinationAlpha,
        AlphaDestinationBlend = Blend.Zero
    };

    private SpriteBatch _worldSpriteBatch;
    private TiledWorldRenderer _worldRenderer;
    private Camera2D _camera;
    private PlayerBlock _player;
    private SpriteAnimator _playerAnimator;
    private Texture2D _playerSpriteSheet;
    private FollowerBlock _follower;
    private SpriteAnimator _followerAnimator;
    private Texture2D _followerSpriteSheet;
    private Texture2D _boulderTexture;
    private Boulder[] _boulders;
    private WorldCollisionMap _collisionMap;
    private DayNightCycle _dayNightCycle;
    private Texture2D _pixelTexture;
    private bool _showCollisionBounds;

    /// <inheritdoc />
    public bool IsTransparent => false;

    /// <summary>
    /// Creates a gameplay screen.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device for rendering.</param>
    /// <param name="content">Content manager for loading assets.</param>
    /// <param name="virtualWidth">Virtual resolution width.</param>
    /// <param name="virtualHeight">Virtual resolution height.</param>
    /// <param name="requestExit">Callback to request the game exit.</param>
    public GameplayScreen(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight,
        Action requestExit)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _requestExit = requestExit;
    }

    /// <inheritdoc />
    public void LoadContent()
    {
        _worldSpriteBatch = new SpriteBatch(_graphicsDevice);
        _worldRenderer = new TiledWorldRenderer(_graphicsDevice, _content, "Maps/StarterMap");
        _camera = new Camera2D(
            _virtualWidth,
            _virtualHeight,
            _worldRenderer.MapPixelWidth,
            _worldRenderer.MapPixelHeight);

        _playerSpriteSheet = _content.Load<Texture2D>("Sprites/generic_character_sheet");
        _followerSpriteSheet = _content.Load<Texture2D>("Sprites/companion_character_sheet");
        _boulderTexture = _content.Load<Texture2D>("Sprites/boulder");
        _playerAnimator = new SpriteAnimator(
            PlayerFramePixels, PlayerFramePixels,
            WalkFramesPerDirection, WalkFrameDuration);
        _followerAnimator = new SpriteAnimator(
            PlayerFramePixels, PlayerFramePixels,
            WalkFramesPerDirection, WalkFrameDuration);

        var initialPosition = new Vector2(
            (_worldRenderer.MapPixelWidth / 2f) - (PlayerFramePixels / 2f),
            (_worldRenderer.MapPixelHeight / 2f) - (PlayerFramePixels / 2f));

        _player = new PlayerBlock(
            initialPosition,
            new Point(PlayerFramePixels, PlayerFramePixels),
            PlayerMoveSpeedPixelsPerSecond,
            new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight),
            PlayerAccelerationRate);

        var followerStartPosition = initialPosition + FollowerPositionOffset;
        _follower = new FollowerBlock(
            followerStartPosition,
            new Point(PlayerFramePixels, PlayerFramePixels),
            new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight),
            FollowerPositionOffset);

        _boulders = CreateBoulders(_boulderTexture);
        _collisionMap = new WorldCollisionMap(_worldRenderer, GetBoulderBounds(_boulders));
        _camera.LookAt(_player.Center);

        _dayNightCycle = new DayNightCycle(
            DayNightCycleDurationSeconds,
            DayNightCycleStartProgress);

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime, IInputManager input)
    {
        if (input.IsPressed(InputAction.Exit))
        {
            _requestExit();
            return;
        }

        if (input.IsPressed(InputAction.ToggleCollisionDebug))
        {
            _showCollisionBounds = !_showCollisionBounds;
        }

        _player.Update(gameTime, input, _collisionMap);

        _playerAnimator.Direction = _player.Facing;
        _playerAnimator.Update(gameTime, _player.IsMoving);
        _followerAnimator.Direction = _follower.Facing;
        _followerAnimator.Update(gameTime, false);
        _camera.LookAt(_player.Center);
        _worldRenderer.Update(gameTime);
        _dayNightCycle.Update(gameTime);
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var worldMatrix = _camera.GetViewMatrix();
        _worldRenderer.Draw(worldMatrix);

        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _boulders.Length; i++)
        {
            _boulders[i].Draw(_worldSpriteBatch);
        }

        _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet);
        _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet);
        if (_showCollisionBounds)
        {
            DrawCollisionBounds();
        }
        _worldSpriteBatch.End();

        // Day/night overlay — multiply blend darkens + tints the scene.
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: MultiplyBlend,
            samplerState: SamplerState.PointClamp);
        _worldSpriteBatch.Draw(
            _pixelTexture,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            _dayNightCycle.CurrentTint);
        _worldSpriteBatch.End();
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _worldSpriteBatch?.Dispose();
        _pixelTexture?.Dispose();
    }

    private void DrawCollisionBounds()
    {
        DrawRectangleOutline(_player.FootBounds, Color.Yellow);
        DrawRectangleOutline(_player.Bounds, Color.OrangeRed);

        DrawRectangleOutline(_follower.Bounds, Color.Cyan);

        for (var i = 0; i < _boulders.Length; i++)
        {
            DrawRectangleOutline(_boulders[i].Bounds, Color.Red);
        }
    }

    private void DrawRectangleOutline(Rectangle rectangle, Color color, int thickness = 1)
    {
        if (_worldSpriteBatch is null || rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            return;
        }

        _worldSpriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
        _worldSpriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
        _worldSpriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
        _worldSpriteBatch.Draw(_pixelTexture, new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
    }

    private static Rectangle[] GetBoulderBounds(Boulder[] boulders)
    {
        var bounds = new Rectangle[boulders.Length];
        for (var i = 0; i < boulders.Length; i++)
        {
            bounds[i] = boulders[i].Bounds;
        }

        return bounds;
    }

    private static Boulder[] CreateBoulders(Texture2D boulderTexture)
    {
        var boulders = new Boulder[BoulderTilePositions.Length];
        for (var i = 0; i < BoulderTilePositions.Length; i++)
        {
            var tilePosition = BoulderTilePositions[i];
            var worldPosition = new Vector2(
                tilePosition.X * WorldTilePixels,
                tilePosition.Y * WorldTilePixels);
            boulders[i] = new Boulder(worldPosition, boulderTexture);
        }

        return boulders;
    }
}
