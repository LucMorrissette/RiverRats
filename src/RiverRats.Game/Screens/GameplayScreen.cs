using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using RiverRats.Game.Components;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Graphics;
using RiverRats.Game.Audio;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Game.World;

namespace RiverRats.Game.Screens;

/// <summary>
/// Primary gameplay screen. Owns the player, camera, world renderer, and gameplay loop.
/// </summary>
public sealed class GameplayScreen : IGameScreen
{
    private const int PlayerFramePixels = 32;
    private const float PlayerMoveSpeedPixelsPerSecond = 96f;
    private const float PlayerAccelerationRate = 10f;
    private const int WalkFramesPerDirection = 4;
    private const float WalkFrameDuration = 0.15f;
    private const float DayNightCycleDurationSeconds = 120f;
    private const float DayNightCycleStartProgress = 0.30f;
    private const int MaxRipples = 8;
    private const float RippleMaxAge = 2f;
    private const int GradientStripCount = 32;
    private const int SmallFireFramePixels = 16;
    private const int SmallFireFrameCount = 8;
    private const float SmallFireFrameDuration = 0.1f;
    private const float FirepitAttachDistancePixels = 24f;
    private const float FirepitAttachDistanceSquared = FirepitAttachDistancePixels * FirepitAttachDistancePixels;
    private const float CabinSortAnchorOffsetPixels = 20f;
    private const float PineTreeSortAnchorOffsetPixels = 10f;
    private static readonly Color DebugTileGridColor = new(255, 255, 255, 40);
    private static readonly FollowerMovementConfig FollowerConfig = new();
    private static readonly Vector2 FollowerStartOffset = new(0f, FollowerConfig.FollowDistancePixels);

    private static readonly ParticleProfile FireSmokeProfile = new()
    {
        SpawnRate = 3f,
        MinLife = 2.0f,
        MaxLife = 3.5f,
        MinSpeed = 8f,
        MaxSpeed = 18f,
        MinScale = 0.8f,
        MaxScale = 2.2f,
        StartColor = new Color(160, 160, 160, 60),
        EndColor = new Color(100, 100, 100, 0),
        SpreadRadians = MathHelper.ToRadians(20),
        Gravity = -12f
    };

    private static readonly ParticleProfile FireSparkProfile = new()
    {
        SpawnRate = 5f,
        MinLife = 0.4f,
        MaxLife = 0.8f,
        MinSpeed = 50f,
        MaxSpeed = 110f,
        MinScale = 0.15f,
        MaxScale = 0.35f,
        StartColor = new Color(255, 240, 80, 255),
        EndColor = new Color(255, 120, 20, 0),
        SpreadRadians = MathHelper.PiOver4,
        Gravity = -80f
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly Action _requestExit;

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
    private Texture2D _dockTexture;
    private Texture2D _dockLegLeftTexture;
    private Texture2D _sunkenLogTexture;
    private Texture2D _sunkenChestTexture;
    private Texture2D _firepitTexture;
    private Firepit[] _firepits;
    private Texture2D _smallFireSpriteSheet;
    private Texture2D _cozyLakeCabinTexture;
    private Boulder[] _cozyLakeCabins;
    private Boulder[] _boulders;
    private Boulder[] _sunkenLogs;
    private Boulder[] _underwaterSunkenLogs;
    private SunkenChest[] _sunkenChests;
    private SunkenChest[] _underwaterSunkenChests;
    private FlatShoreDepthSimulator[] _flatShoreDepthSimulators;
    private Boulder[] _seaweeds;
    private Boulder[] _pineTrees;
    private Dock[] _docks;
    private Boulder[] _dockLegsLeft;
    private WorldCollisionMap _collisionMap;
    private DayNightCycle _dayNightCycle;
    private Texture2D _pixelTexture;
    private RenderTarget2D _waterRenderTarget;
    private RenderTarget2D _surfaceReachRenderTarget;
    private RenderTarget2D _surfaceReachGradientTarget;
    private Effect _waterDistortionEffect;
    private ParticleManager _particleManager;
    private Texture2D _smokeTexture;
    private Boulder[] _surfaceReachDockLegsLeft;
    private LightingRenderer _lightingRenderer;
    private CloudShadowRenderer _cloudShadowRenderer;
    private LightData[] _fireLightData = Array.Empty<LightData>();
    private LightData[] _combinedLightData = Array.Empty<LightData>();
    private FireflyManager _fireflyManager;
    private bool _showCollisionBounds;
    private readonly Vector2[] _rippleWorldPositions = new Vector2[MaxRipples];
    private readonly float[] _rippleAges = new float[MaxRipples];
    private readonly Vector3[] _rippleShaderData = new Vector3[MaxRipples];
    private int _rippleCount;
    private readonly MusicManager _musicManager = new();

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
        _dockTexture = _content.Load<Texture2D>("Sprites/wooden-dock");
        _dockLegLeftTexture = _content.Load<Texture2D>("Tilesets/wooden-dock-leg-left");
        _sunkenLogTexture = _content.Load<Texture2D>("Sprites/sunken-log");
        _sunkenChestTexture = _content.Load<Texture2D>("Sprites/sunken-chest");
        var flatShoreDepthSimulatorTexture = _content.Load<Texture2D>("Sprites/flat-shore-depth-simulator");
        var seaweedTextures = new[]
        {
            _content.Load<Texture2D>("Sprites/seaweed1"),
            _content.Load<Texture2D>("Sprites/seaweed2"),
            _content.Load<Texture2D>("Sprites/seaweed3"),
            _content.Load<Texture2D>("Sprites/seaweed4"),
        };
        _firepitTexture = _content.Load<Texture2D>("Sprites/basic-firepit");
        _smallFireSpriteSheet = _content.Load<Texture2D>("Sprites/small-fire");
        _cozyLakeCabinTexture = _content.Load<Texture2D>("Sprites/cozy_lake_cabin");
        var pineTreeTexture = _content.Load<Texture2D>("Sprites/pine-tree");
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

        _boulders = CreateBoulders(_boulderTexture, _worldRenderer.PropPlacements);
        _docks = CreateDocks(_dockTexture, _worldRenderer.PropPlacements);
        _dockLegsLeft = CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: false);
        _surfaceReachDockLegsLeft = CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: true);
        _sunkenLogs = CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: false);
        _underwaterSunkenLogs = CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: true);
        _sunkenChests = CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: false);
        _underwaterSunkenChests = CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: true);
        _flatShoreDepthSimulators = CreateFlatShoreDepthSimulators(flatShoreDepthSimulatorTexture, _worldRenderer.PropPlacements);
        _seaweeds = CreateSeaweeds(seaweedTextures, _worldRenderer.PropPlacements);
        _firepits = CreateFirepits(_firepitTexture, _smallFireSpriteSheet, _worldRenderer.PropPlacements);
        _cozyLakeCabins = CreatePropsByType(_cozyLakeCabinTexture, _worldRenderer.PropPlacements, "cozy-lake-cabin", isUnderwater: false);
        _pineTrees = CreatePropsByType(pineTreeTexture, _worldRenderer.PropPlacements, "pine-tree", isUnderwater: false);
        _smokeTexture = _content.Load<Texture2D>("Sprites/smoke-puff");
        _particleManager = new ParticleManager(512);
        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].AttachSmokeEmitter(new ParticleEmitter(_particleManager, FireSmokeProfile));
            _firepits[i].AttachSparkEmitter(new ParticleEmitter(_particleManager, FireSparkProfile));
        }
        var propObstacleBounds = MergeRectangleArrays(GetBoulderBounds(_boulders), GetFirepitBounds(_firepits));
        _collisionMap = new WorldCollisionMap(_worldRenderer, MergeObstacleBounds(propObstacleBounds, _worldRenderer.ColliderBounds), GetDockBounds(_docks));
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
        _surfaceReachRenderTarget = new RenderTarget2D(
            _graphicsDevice,
            _virtualWidth,
            _virtualHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
        _surfaceReachGradientTarget = new RenderTarget2D(
            _graphicsDevice,
            _virtualWidth,
            _virtualHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
        _waterDistortionEffect = _content.Load<Effect>("Effects/WaterDistortion");

        // Adjust these parameters to change the overall wave effect on composited water layers.
        _waterDistortionEffect.Parameters["Amplitude"].SetValue(0.004f); // How far pixels get displaced. Higher = more dramatic waves.
        _waterDistortionEffect.Parameters["Frequency"].SetValue(25f); 	// Wave tightness. Higher = more ripples packed in. Lower = broad rolling waves.
        _waterDistortionEffect.Parameters["Speed"].SetValue(1f); //How fast waves animate.

        // water riples are additive on top of the base wave distortion, so they can be stronger without looking unnatural. Adjust these to change the click ripple effect.
        _waterDistortionEffect.Parameters["RippleAmplitude"].SetValue(0.020f); // Additional displacement for click ripples. Higher = bigger splashes.
        _waterDistortionEffect.Parameters["RippleFrequency"].SetValue(40f); // Ripple tightness. Higher = tighter, more circular ripples. Lower = looser, more wave-like ripples.
        _waterDistortionEffect.Parameters["RippleSpeed"].SetValue(18f); // How fast ripples expand and fade.
        
        _waterDistortionEffect.Parameters["AspectRatio"].SetValue((float)_virtualWidth / _virtualHeight);

        // Tint applied to surface-reach props (dock legs) — blends from white at surface to this colour at full depth.
        _waterDistortionEffect.Parameters["WaterTintColor"].SetValue(new Vector3(0.55f, 0.65f, 0.85f));

        _lightingRenderer = new LightingRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        var radialGradient = _content.Load<Texture2D>("Sprites/RadialGradient");
        _lightingRenderer.LoadContent(radialGradient);

        _cloudShadowRenderer = new CloudShadowRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        _cloudShadowRenderer.LoadContent();
        _fireLightData = new LightData[_firepits.Length];
        _fireflyManager = new FireflyManager();
        _combinedLightData = new LightData[_firepits.Length + 32];

        _musicManager.LoadContent(_content);
        _musicManager.PlaySong("GameplayTheme", loopDelaySeconds: 5f);
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

        if (input.IsPressed(InputAction.Confirm))
        {
            TryToggleNearbyFirepit();
        }

        _follower.Update(gameTime, _player.Position, _player.Facing, GetFollowerRestPosition());

        _playerAnimator.Direction = _player.Facing;
        _playerAnimator.Update(gameTime, _player.IsMoving);
        _followerAnimator.Direction = _follower.Facing;
        _followerAnimator.Update(gameTime, _follower.IsMoving);
        _camera.LookAt(_player.Center);
        _worldRenderer.Update(gameTime);
        _dayNightCycle.Update(gameTime);
        var activeFireLightCount = 0;
        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].Update(gameTime);
            if (_firepits[i].TryGetLightData(out var lightData))
            {
                _fireLightData[activeFireLightCount] = lightData;
                activeFireLightCount++;
            }
        }
        var cameraWorldBounds = new Rectangle(
            (int)(_camera.Position.X - _virtualWidth / 2f),
            (int)(_camera.Position.Y - _virtualHeight / 2f),
            _virtualWidth,
            _virtualHeight);
        _fireflyManager.Update(gameTime, _dayNightCycle.NightStrength, cameraWorldBounds);

        // Combine fire + firefly lights into one array for the lighting renderer.
        Array.Copy(_fireLightData, 0, _combinedLightData, 0, activeFireLightCount);
        var fireflyLightCount = _fireflyManager.WriteLightData(_combinedLightData, activeFireLightCount);
        _lightingRenderer.SetLights(_combinedLightData, activeFireLightCount + fireflyLightCount);
        _particleManager.Update(gameTime);
        _cloudShadowRenderer.Update(gameTime);
        _musicManager.Update(gameTime);
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
        _worldRenderer.DrawWaterBottom(worldMatrix);

        // Draw underwater props between bottom and surface so the surface waves render over them.
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _underwaterSunkenLogs.Length; i++)
        {
            _underwaterSunkenLogs[i].Draw(_worldSpriteBatch);
        }
        for (var i = 0; i < _dockLegsLeft.Length; i++)
        {
            _dockLegsLeft[i].Draw(_worldSpriteBatch);
        }
        for (var i = 0; i < _underwaterSunkenChests.Length; i++)
        {
            _underwaterSunkenChests[i].Draw(_worldSpriteBatch);
        }
        for (var i = 0; i < _flatShoreDepthSimulators.Length; i++)
        {
            _flatShoreDepthSimulators[i].Draw(_worldSpriteBatch);
        }
        for (var i = 0; i < _seaweeds.Length; i++)
        {
            _seaweeds[i].Draw(_worldSpriteBatch);
        }
        _worldSpriteBatch.End();

        _worldRenderer.DrawWaterSurface(worldMatrix);

        // --- Pass 1b: Render surface-reaching underwater props to separate render target ---
        // Draw props normally (full color, full alpha) — no gradient encoding here.
        _graphicsDevice.SetRenderTarget(_surfaceReachRenderTarget);
        _graphicsDevice.Clear(Color.Transparent);
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _surfaceReachDockLegsLeft.Length; i++)
        {
            _surfaceReachDockLegsLeft[i].Draw(_worldSpriteBatch);
        }
        _worldSpriteBatch.End();

        // --- Pass 1c: Render gradient masks for surface-reaching props ---
        // Solid white rectangles with vertical alpha gradient (0 at top, 1 at bottom).
        // The SurfaceReachDistortion shader reads this mask to scale distortion per-pixel.
        _graphicsDevice.SetRenderTarget(_surfaceReachGradientTarget);
        _graphicsDevice.Clear(Color.Transparent);
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _surfaceReachDockLegsLeft.Length; i++)
        {
            DrawGradientMask(_surfaceReachDockLegsLeft[i]);
        }
        _worldSpriteBatch.End();

        // --- Switch back to the scene render target ---
        _graphicsDevice.SetRenderTarget(previousRenderTarget);

        // --- Pass 2: Composite water with distortion shader ---
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

        // --- Pass 2b: Composite surface-reach props with per-prop vertical gradient distortion ---
        // SurfaceReachDistortion technique reads the gradient mask to scale amplitude per-pixel.
        _waterDistortionEffect.CurrentTechnique = _waterDistortionEffect.Techniques["SurfaceReachDistortion"];
        _waterDistortionEffect.Parameters["GradientMaskTexture"].SetValue(_surfaceReachGradientTarget);
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            effect: _waterDistortionEffect);
        _worldSpriteBatch.Draw(
            _surfaceReachRenderTarget,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);
        _worldSpriteBatch.End();
        // Restore the default technique for the next frame's water pass.
        _waterDistortionEffect.CurrentTechnique = _waterDistortionEffect.Techniques["WaterDistortion"];

        // --- Pass 3: Draw non-water terrain above the composited water ---
        _worldRenderer.DrawTerrain(worldMatrix);

        // --- Pass 4: Entities (Y-sorted by bottom edge) ---
        // FrontToBack: layerDepth 0 = drawn first (behind), 1 = drawn last (in front).
        // Each entity's depth is its bottom Y / map height so lower-on-screen = in front.
        var mapHeight = (float)_worldRenderer.MapPixelHeight;
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.FrontToBack,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _boulders.Length; i++)
        {
            _boulders[i].Draw(_worldSpriteBatch, _boulders[i].Bounds.Bottom / mapHeight);
        }

        for (var i = 0; i < _docks.Length; i++)
        {
            _docks[i].Draw(_worldSpriteBatch);
        }

        for (var i = 0; i < _sunkenLogs.Length; i++)
        {
            _sunkenLogs[i].Draw(_worldSpriteBatch, _sunkenLogs[i].Bounds.Bottom / mapHeight);
        }

        for (var i = 0; i < _sunkenChests.Length; i++)
        {
            _sunkenChests[i].Draw(_worldSpriteBatch, _sunkenChests[i].Bounds.Bottom / mapHeight);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].Draw(_worldSpriteBatch, _firepits[i].Bounds.Bottom / mapHeight);
        }

        for (var i = 0; i < _cozyLakeCabins.Length; i++)
        {
            var cabinBounds = _cozyLakeCabins[i].Bounds;
            _cozyLakeCabins[i].Draw(_worldSpriteBatch, (cabinBounds.Bottom - CabinSortAnchorOffsetPixels) / mapHeight);
        }

        for (var i = 0; i < _pineTrees.Length; i++)
        {
            var treeBounds = _pineTrees[i].Bounds;
            _pineTrees[i].Draw(_worldSpriteBatch, (treeBounds.Bottom - PineTreeSortAnchorOffsetPixels) / mapHeight);
        }

        _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet, _follower.Bounds.Bottom / mapHeight);
        _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet, _player.Bounds.Bottom / mapHeight);
        if (_showCollisionBounds)
        {
            DrawCollisionBounds();
        }
        _worldSpriteBatch.End();

        // --- Pass 4b: Smoke particles ---
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        _particleManager.Draw(_worldSpriteBatch, _smokeTexture);
        _worldSpriteBatch.End();

        // --- Pass 4c: Fireflies (additive blend for a soft glow look) ---
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.Additive,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: worldMatrix);
        _fireflyManager.Draw(_worldSpriteBatch, _smokeTexture);
        _worldSpriteBatch.End();

        // --- Pass 5: Cloud shadows (multiply blend, half-res for soft edges) ---
        _cloudShadowRenderer.Draw(
            _worldSpriteBatch,
            _camera.Position,
            _dayNightCycle.NightStrength,
            previousRenderTarget);

        // Lighting pass: fills a low-res lightmap with ambient darkness, draws fire glows
        // additively, then composites it over the scene with multiply blend.
        // The LightingRenderer also handles the day/night darkening, replacing the old
        // single-quad multiply pass. When NightStrength = 0 the pass is skipped entirely.
        _lightingRenderer.Draw(
            _worldSpriteBatch,
            _dayNightCycle.NightStrength,
            worldMatrix,
            previousRenderTarget);
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _cloudShadowRenderer?.UnloadContent();
        _lightingRenderer?.UnloadContent();
        _worldSpriteBatch?.Dispose();
        _pixelTexture?.Dispose();
        _waterRenderTarget?.Dispose();
        _surfaceReachRenderTarget?.Dispose();
        _surfaceReachGradientTarget?.Dispose();
    }

    private void DrawCollisionBounds()
    {
        DrawTileGrid();
        DrawRectangleOutline(_player.FootBounds, Color.Yellow);
        DrawRectangleOutline(_player.Bounds, Color.OrangeRed);

        DrawRectangleOutline(_follower.Bounds, Color.Cyan);

        for (var i = 0; i < _boulders.Length; i++)
        {
            DrawRectangleOutline(_boulders[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            DrawRectangleOutline(_firepits[i].Bounds, Color.Red);
        }

        var colliderBounds = _worldRenderer.ColliderBounds;
        for (var i = 0; i < colliderBounds.Count; i++)
        {
            DrawRectangleOutline(colliderBounds[i], Color.Magenta);
        }
    }

    private void DrawTileGrid()
    {
        if (_worldSpriteBatch is null || _worldRenderer is null || _pixelTexture is null)
        {
            return;
        }

        var mapWidth = _worldRenderer.MapPixelWidth;
        var mapHeight = _worldRenderer.MapPixelHeight;
        var tileWidth = _worldRenderer.TileWidthPixels;
        var tileHeight = _worldRenderer.TileHeightPixels;

        for (var x = 0; x <= mapWidth; x += tileWidth)
        {
            _worldSpriteBatch.Draw(
                _pixelTexture,
                new Rectangle(x, 0, 1, mapHeight),
                DebugTileGridColor);
        }

        for (var y = 0; y <= mapHeight; y += tileHeight)
        {
            _worldSpriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, y, mapWidth, 1),
                DebugTileGridColor);
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

    /// <summary>
    /// Draws a gradient mask rectangle at the prop's position using the 1×1 white pixel texture.
    /// Alpha interpolates from 0 (top strip) to 255 (bottom strip), encoding the distortion
    /// scale factor for the SurfaceReachDistortion shader technique.
    /// </summary>
    private void DrawGradientMask(Boulder prop)
    {
        var bounds = prop.Bounds;
        var stripHeight = Math.Max(1, bounds.Height / GradientStripCount);
        var actualStripCount = (bounds.Height + stripHeight - 1) / stripHeight;

        for (var strip = 0; strip < actualStripCount; strip++)
        {
            var y = bounds.Y + (strip * stripHeight);
            var height = Math.Min(stripHeight, bounds.Bottom - y);

            // Alpha ramps from 0 at top strip to 255 at bottom strip.
            var alpha = (byte)(strip * 255 / Math.Max(1, actualStripCount - 1));

            var destRect = new Rectangle(bounds.X, y, bounds.Width, height);
            _worldSpriteBatch.Draw(_pixelTexture, destRect, new Color(alpha, alpha, alpha, alpha));
        }
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

    private static Rectangle[] GetDockBounds(Dock[] docks)
    {
        var bounds = new Rectangle[docks.Length];
        for (var i = 0; i < docks.Length; i++)
        {
            bounds[i] = docks[i].Bounds;
        }

        return bounds;
    }

    private static Rectangle[] GetFirepitBounds(Firepit[] firepits)
    {
        var bounds = new Rectangle[firepits.Length];
        for (var i = 0; i < firepits.Length; i++)
        {
            bounds[i] = firepits[i].Bounds;
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

    private void TryToggleNearbyFirepit()
    {
        var nearestIndex = -1;
        var nearestDistanceSquared = float.MaxValue;
        var playerFootBounds = _player.FootBounds;
        var playerCenter = new Vector2(playerFootBounds.Center.X, playerFootBounds.Center.Y);

        for (var i = 0; i < _firepits.Length; i++)
        {
            if (!_firepits[i].CanInteract(playerFootBounds))
            {
                continue;
            }

            var distanceSquared = Vector2.DistanceSquared(playerCenter, _firepits[i].Center);
            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestIndex = i;
        }

        if (nearestIndex >= 0)
        {
            _firepits[nearestIndex].ToggleLit();
        }
    }

    private static Boulder[] CreateBoulders(Texture2D boulderTexture, IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var boulders = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "boulder", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            boulders.Add(new Boulder(placement.Position, boulderTexture));
        }

        return boulders.ToArray();
    }

    private static Dock[] CreateDocks(Texture2D dockTexture, IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var docks = new List<Dock>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "dock", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            docks.Add(new Dock(placement.Position, dockTexture));
        }

        return docks.ToArray();
    }

    private static Firepit[] CreateFirepits(
        Texture2D firepitTexture,
        Texture2D smallFireSpriteSheet,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var firepitPlacements = new List<TiledWorldRenderer.MapPropPlacement>(placements.Count);
        var smallFirePlacements = new List<TiledWorldRenderer.MapPropPlacement>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (string.Equals(placement.PropType, "firepit", StringComparison.OrdinalIgnoreCase))
            {
                firepitPlacements.Add(placement);
                continue;
            }

            if (string.Equals(placement.PropType, "small-fire", StringComparison.OrdinalIgnoreCase))
            {
                smallFirePlacements.Add(placement);
            }
        }

        var firepits = new List<Firepit>(firepitPlacements.Count);
        var assignedSmallFires = new bool[smallFirePlacements.Count];
        for (var i = 0; i < firepitPlacements.Count; i++)
        {
            var firepitPlacement = firepitPlacements[i];
            SmallFire attachedFire = null!;
            var nearestSmallFireIndex = FindNearestSmallFireIndex(firepitPlacement.Position, smallFirePlacements, assignedSmallFires);
            if (nearestSmallFireIndex >= 0)
            {
                assignedSmallFires[nearestSmallFireIndex] = true;
                attachedFire = CreateSmallFire(smallFireSpriteSheet, smallFirePlacements[nearestSmallFireIndex].Position);
            }

            firepits.Add(new Firepit(firepitPlacement.Position, firepitTexture, attachedFire));
        }

        return firepits.ToArray();
    }

    private static int FindNearestSmallFireIndex(
        Vector2 firepitPosition,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> smallFirePlacements,
        bool[] assignedSmallFires)
    {
        var nearestIndex = -1;
        var nearestDistanceSquared = FirepitAttachDistanceSquared;
        for (var i = 0; i < smallFirePlacements.Count; i++)
        {
            if (assignedSmallFires[i])
            {
                continue;
            }

            var distanceSquared = Vector2.DistanceSquared(firepitPosition, smallFirePlacements[i].Position);
            if (distanceSquared > nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearestIndex = i;
        }

        return nearestIndex;
    }

    private static SmallFire CreateSmallFire(Texture2D spriteSheet, Vector2 position)
    {
        var animator = new LoopAnimator(SmallFireFramePixels, SmallFireFramePixels, SmallFireFrameCount, SmallFireFrameDuration);
        return new SmallFire(position, spriteSheet, animator);
    }

    private static Boulder[] CreatePropsByType(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        string propType,
        bool isUnderwater,
        bool reachesSurface = false)
    {
        var props = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, propType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater != isUnderwater)
            {
                continue;
            }

            if (placement.ReachesSurface != reachesSurface)
            {
                continue;
            }

            props.Add(new Boulder(placement.Position, texture));
        }

        return props.ToArray();
    }

    private static SunkenChest[] CreateSunkenChests(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements,
        bool isUnderwater)
    {
        var chests = new List<SunkenChest>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "sunken-chest", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (placement.IsUnderwater != isUnderwater)
            {
                continue;
            }

            chests.Add(new SunkenChest(placement.Position, texture));
        }

        return chests.ToArray();
    }

    private static FlatShoreDepthSimulator[] CreateFlatShoreDepthSimulators(
        Texture2D texture,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var props = new List<FlatShoreDepthSimulator>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!string.Equals(placement.PropType, "flat-shore-depth-simulator", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            props.Add(new FlatShoreDepthSimulator(placement.Position, texture));
        }

        return props.ToArray();
    }

    private static Boulder[] CreateSeaweeds(
        Texture2D[] variantTextures,
        IReadOnlyList<TiledWorldRenderer.MapPropPlacement> placements)
    {
        var seaweeds = new List<Boulder>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (!placement.PropType.StartsWith("seaweed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse variant index from prop type (seaweed1 → 0, seaweed2 → 1, etc.)
            var lastChar = placement.PropType[^1];
            var variantIndex = lastChar - '1';
            if (variantIndex < 0 || variantIndex >= variantTextures.Length)
            {
                continue;
            }

            seaweeds.Add(new Boulder(placement.Position, variantTextures[variantIndex]));
        }

        return seaweeds.ToArray();
    }

    private static Rectangle[] MergeObstacleBounds(Rectangle[] boulderBounds, IReadOnlyList<Rectangle> colliderBounds)
    {
        if (colliderBounds.Count == 0)
        {
            return boulderBounds;
        }

        var merged = new Rectangle[boulderBounds.Length + colliderBounds.Count];
        boulderBounds.CopyTo(merged, 0);
        for (var i = 0; i < colliderBounds.Count; i++)
        {
            merged[boulderBounds.Length + i] = colliderBounds[i];
        }

        return merged;
    }

    private static Rectangle[] MergeRectangleArrays(Rectangle[] first, Rectangle[] second)
    {
        if (second.Length == 0)
        {
            return first;
        }

        if (first.Length == 0)
        {
            return second;
        }

        var merged = new Rectangle[first.Length + second.Length];
        first.CopyTo(merged, 0);
        second.CopyTo(merged, first.Length);
        return merged;
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
        // Build per-slot shader data: active ripples get their real age, inactive
        // slots get age = -1 so the shader's step(0, age) masks them out.
        for (var i = 0; i < MaxRipples; i++)
        {
            if (i < _rippleCount)
            {
                var screenX = (_rippleWorldPositions[i].X - _camera.Position.X) / _virtualWidth + 0.5f;
                var screenY = (_rippleWorldPositions[i].Y - _camera.Position.Y) / _virtualHeight + 0.5f;
                _rippleShaderData[i] = new Vector3(screenX, screenY, _rippleAges[i]);
            }
            else
            {
                _rippleShaderData[i] = new Vector3(0f, 0f, -1f); // inactive
            }
        }

        // MojoShader (DesktopGL) does not support float3 arrays; use individual params.
        _waterDistortionEffect.Parameters["Ripple0"].SetValue(_rippleShaderData[0]);
        _waterDistortionEffect.Parameters["Ripple1"].SetValue(_rippleShaderData[1]);
        _waterDistortionEffect.Parameters["Ripple2"].SetValue(_rippleShaderData[2]);
        _waterDistortionEffect.Parameters["Ripple3"].SetValue(_rippleShaderData[3]);
        _waterDistortionEffect.Parameters["Ripple4"].SetValue(_rippleShaderData[4]);
        _waterDistortionEffect.Parameters["Ripple5"].SetValue(_rippleShaderData[5]);
        _waterDistortionEffect.Parameters["Ripple6"].SetValue(_rippleShaderData[6]);
        _waterDistortionEffect.Parameters["Ripple7"].SetValue(_rippleShaderData[7]);
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
