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
    private const float DayNightCycleDurationSeconds = 300f;
    private const float DayNightCycleStartProgress = 0.30f;
    private const int MaxRipples = 8;
    private const float RippleMaxAge = 2f;
    private static readonly FollowerMovementConfig FollowerConfig = new();
    private static readonly Vector2 FollowerStartOffset = new(0f, FollowerConfig.FollowDistancePixels);

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
    private RenderTarget2D _waterRenderTarget;
    private Effect _waterDistortionEffect;
    private bool _showCollisionBounds;
    private readonly Vector2[] _rippleWorldPositions = new Vector2[MaxRipples];
    private readonly float[] _rippleAges = new float[MaxRipples];
    private readonly Vector3[] _rippleShaderData = new Vector3[MaxRipples];
    private int _rippleCount;

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

        var followerStartPosition = initialPosition + FollowerStartOffset;
        _follower = new FollowerBlock(
            followerStartPosition,
            new Point(PlayerFramePixels, PlayerFramePixels),
            new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight),
            FollowerConfig);

        _boulders = CreateBoulders(_boulderTexture);
        _collisionMap = new WorldCollisionMap(_worldRenderer, GetBoulderBounds(_boulders));
        _camera.LookAt(_player.Center);

        _dayNightCycle = new DayNightCycle(
            DayNightCycleDurationSeconds,
            DayNightCycleStartProgress);

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });

        _waterRenderTarget = new RenderTarget2D(
            _graphicsDevice,
            _virtualWidth,
            _virtualHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
        _waterDistortionEffect = _content.Load<Effect>("Effects/WaterDistortion");

        // Adjust these parameters to change the overall wave effect on water tiles.
        _waterDistortionEffect.Parameters["Amplitude"].SetValue(0.002f); // How far pixels get displaced. Higher = more dramatic waves.
        _waterDistortionEffect.Parameters["Frequency"].SetValue(25f); 	// Wave tightness. Higher = more ripples packed in. Lower = broad rolling waves.
        _waterDistortionEffect.Parameters["Speed"].SetValue(1f); //How fast waves animate.

        // water riples are additive on top of the base wave distortion, so they can be stronger without looking unnatural. Adjust these to change the click ripple effect.
        _waterDistortionEffect.Parameters["RippleAmplitude"].SetValue(0.005f); // Additional displacement for click ripples. Higher = bigger splashes.
        _waterDistortionEffect.Parameters["RippleFrequency"].SetValue(40f); // Ripple tightness. Higher = tighter, more circular ripples. Lower = looser, more wave-like ripples.
        _waterDistortionEffect.Parameters["RippleSpeed"].SetValue(15f); // How fast ripples expand and fade.
        
        _waterDistortionEffect.Parameters["AspectRatio"].SetValue((float)_virtualWidth / _virtualHeight);
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
        _follower.Update(gameTime, _player.Position, _player.Facing, GetFollowerRestPosition());

        _playerAnimator.Direction = _player.Facing;
        _playerAnimator.Update(gameTime, _player.IsMoving);
        _followerAnimator.Direction = _follower.Facing;
        _followerAnimator.Update(gameTime, _follower.IsMoving);
        _camera.LookAt(_player.Center);
        _worldRenderer.Update(gameTime);
        _dayNightCycle.Update(gameTime);
        UpdateRipples(gameTime, input);
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var worldMatrix = _camera.GetViewMatrix();

        // --- Pass 1: Render water tiles to a separate render target ---
        var previousRenderTarget = _graphicsDevice.GetRenderTargets().Length > 0
            ? _graphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D
            : null;

        _graphicsDevice.SetRenderTarget(_waterRenderTarget);
        _graphicsDevice.Clear(Color.Transparent);
        _worldRenderer.DrawWater(worldMatrix);

        // --- Switch back to the scene render target ---
        _graphicsDevice.SetRenderTarget(previousRenderTarget);

        // --- Pass 2: Draw non-water terrain ---
        _worldRenderer.DrawTerrain(worldMatrix);

        // --- Pass 3: Composite water with distortion shader ---
        // LinearClamp smooths the UV displacement for natural-looking waves
        // instead of the pixel-snapping that PointClamp would produce.
        _waterDistortionEffect.Parameters["Time"].SetValue(_worldRenderer.WaterElapsedSeconds);
        // Anchor waves to world space so they don't slide when the camera pans.
        _waterDistortionEffect.Parameters["CameraOffset"].SetValue(
            new Vector2(_camera.Position.X / _virtualWidth, _camera.Position.Y / _virtualHeight));
        SetRippleShaderParameters();
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            effect: _waterDistortionEffect);
        _worldSpriteBatch.Draw(
            _waterRenderTarget,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);
        _worldSpriteBatch.End();

        // --- Pass 4: Entities ---
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
        _waterRenderTarget?.Dispose();
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

    private Vector2? GetFollowerRestPosition()
    {
        if (_player.IsMoving || _follower is null || _collisionMap is null)
        {
            return null;
        }

        var (firstOffset, secondOffset) = GetRestOffsetsForFacing(_player.Facing);
        var firstPosition = _player.Position + firstOffset;
        var secondPosition = _player.Position + secondOffset;
        var firstOpen = IsFollowerRestPositionOpen(firstPosition);
        var secondOpen = IsFollowerRestPositionOpen(secondPosition);

        if (!firstOpen && !secondOpen)
        {
            return null;
        }

        if (firstOpen && secondOpen)
        {
            var firstDistance = Vector2.DistanceSquared(_follower.Position, firstPosition);
            var secondDistance = Vector2.DistanceSquared(_follower.Position, secondPosition);
            return firstDistance <= secondDistance ? firstPosition : secondPosition;
        }

        return firstOpen ? firstPosition : secondPosition;
    }

    private static (Vector2 FirstOffset, Vector2 SecondOffset) GetRestOffsetsForFacing(FacingDirection facing)
    {
        var sideOffset = FollowerConfig.SideRestOffsetPixels;

        return facing switch
        {
            FacingDirection.Left or FacingDirection.Right =>
                (new Vector2(0f, -sideOffset), new Vector2(0f, sideOffset)),
            _ =>
                (new Vector2(-sideOffset, 0f), new Vector2(sideOffset, 0f))
        };
    }

    private bool IsFollowerRestPositionOpen(Vector2 candidatePosition)
    {
        var candidateBounds = new Rectangle(
            (int)MathF.Round(candidatePosition.X),
            (int)MathF.Round(candidatePosition.Y),
            PlayerFramePixels,
            PlayerFramePixels);
        var worldBounds = new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight);

        if (!worldBounds.Contains(candidateBounds))
        {
            return false;
        }

        if (candidateBounds.Intersects(_player.Bounds))
        {
            return false;
        }

        return !_collisionMap.IsWorldRectangleBlocked(candidateBounds);
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

    private void UpdateRipples(GameTime gameTime, IInputManager input)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Age existing ripples; remove expired ones by swapping with the last.
        for (var i = _rippleCount - 1; i >= 0; i--)
        {
            _rippleAges[i] += dt;
            if (_rippleAges[i] >= RippleMaxAge)
            {
                _rippleCount--;
                _rippleWorldPositions[i] = _rippleWorldPositions[_rippleCount];
                _rippleAges[i] = _rippleAges[_rippleCount];
            }
        }

        if (input.IsMouseLeftPressed() && _rippleCount < MaxRipples)
        {
            var virtualPos = PhysicalToVirtualMousePosition(input.GetMousePosition());
            var worldPos = _camera.ScreenToWorld(virtualPos);
            _rippleWorldPositions[_rippleCount] = worldPos;
            _rippleAges[_rippleCount] = 0f;
            _rippleCount++;
        }
    }

    private void SetRippleShaderParameters()
    {
        // Convert world positions to screen UV space for the shader.
        for (var i = 0; i < _rippleCount; i++)
        {
            var screenX = (_rippleWorldPositions[i].X - _camera.Position.X) / _virtualWidth + 0.5f;
            var screenY = (_rippleWorldPositions[i].Y - _camera.Position.Y) / _virtualHeight + 0.5f;
            _rippleShaderData[i] = new Vector3(screenX, screenY, _rippleAges[i]);
        }

        _waterDistortionEffect.Parameters["Ripples"].SetValue(_rippleShaderData);
        _waterDistortionEffect.Parameters["RippleCount"].SetValue(_rippleCount);
    }

    private Vector2 PhysicalToVirtualMousePosition(Point physicalPosition)
    {
        var viewport = _graphicsDevice.Viewport;
        var scaleX = viewport.Width / _virtualWidth;
        var scaleY = viewport.Height / _virtualHeight;
        var scale = Math.Max(1, Math.Min(scaleX, scaleY));
        var scaledW = _virtualWidth * scale;
        var scaledH = _virtualHeight * scale;
        var offsetX = (viewport.Width - scaledW) / 2;
        var offsetY = (viewport.Height - scaledH) / 2;

        return new Vector2(
            (physicalPosition.X - offsetX) / (float)scale,
            (physicalPosition.Y - offsetY) / (float)scale);
    }
}
