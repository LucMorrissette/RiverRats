using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
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
using RiverRats.Game.UI;
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
    private const float HudFontSize = 14f;
    private const float DayNightCycleDurationSeconds = 120f;
    private const float DayNightCycleStartProgress = 0.30f;
    private const int GradientStripCount = 32;
    private const int MaxFireflyLights = 32;
    private const int MaxParticleCount = 512;
    private const float CabinSortAnchorOffsetPixels = 20f;
    private const float PineTreeSortAnchorOffsetPixels = 10f;
    private static readonly WaterShaderConfig WaterShader = WaterShaderConfig.Default;
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
    private OcclusionRevealRenderer _occlusionRevealRenderer;
    private DebugRenderer _debugRenderer;
    private bool _isPlayerOccluded;
    private bool _isFollowerOccluded;
    private RenderTarget2D _previousRenderTarget;
    private bool _showCollisionBounds;
    private RippleSystem _rippleSystem;
    private readonly MusicManager _musicManager = new();
    private HudRenderer _hudRenderer;
    private FontSystem _fontSystem;

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

        _boulders = PropFactory.CreateBoulders(_boulderTexture, _worldRenderer.PropPlacements);
        _docks = PropFactory.CreateDocks(_dockTexture, _worldRenderer.PropPlacements);
        _dockLegsLeft = PropFactory.CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: false);
        _surfaceReachDockLegsLeft = PropFactory.CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: true);
        _sunkenLogs = PropFactory.CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: false);
        _underwaterSunkenLogs = PropFactory.CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: true);
        _sunkenChests = PropFactory.CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: false);
        _underwaterSunkenChests = PropFactory.CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: true);
        _flatShoreDepthSimulators = PropFactory.CreateFlatShoreDepthSimulators(flatShoreDepthSimulatorTexture, _worldRenderer.PropPlacements);
        _seaweeds = PropFactory.CreateSeaweeds(seaweedTextures, _worldRenderer.PropPlacements);
        _firepits = PropFactory.CreateFirepits(_firepitTexture, _smallFireSpriteSheet, _worldRenderer.PropPlacements);
        _cozyLakeCabins = PropFactory.CreatePropsByType(_cozyLakeCabinTexture, _worldRenderer.PropPlacements, "cozy-lake-cabin", isUnderwater: false);
        _pineTrees = PropFactory.CreatePropsByType(pineTreeTexture, _worldRenderer.PropPlacements, "pine-tree", isUnderwater: false);
        _smokeTexture = _content.Load<Texture2D>("Sprites/smoke-puff");
        _particleManager = new ParticleManager(MaxParticleCount);
        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].AttachSmokeEmitter(new ParticleEmitter(_particleManager, FireSmokeProfile));
            _firepits[i].AttachSparkEmitter(new ParticleEmitter(_particleManager, FireSparkProfile));
        }
        var propObstacleBounds = PropFactory.MergeRectangleArrays(PropFactory.GetBoulderBounds(_boulders), PropFactory.GetFirepitBounds(_firepits));
        _collisionMap = new WorldCollisionMap(_worldRenderer, PropFactory.MergeObstacleBounds(propObstacleBounds, _worldRenderer.ColliderBounds), PropFactory.GetDockBounds(_docks));
        _camera.LookAt(_player.Center);

        _dayNightCycle = new DayNightCycle(
            DayNightCycleDurationSeconds,
            DayNightCycleStartProgress);

        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
        _debugRenderer = new DebugRenderer(_pixelTexture);

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
        _rippleSystem = new RippleSystem();

        _waterDistortionEffect.Parameters["Amplitude"].SetValue(WaterShader.Amplitude);
        _waterDistortionEffect.Parameters["Frequency"].SetValue(WaterShader.Frequency);
        _waterDistortionEffect.Parameters["Speed"].SetValue(WaterShader.Speed);
        _waterDistortionEffect.Parameters["RippleAmplitude"].SetValue(WaterShader.RippleAmplitude);
        _waterDistortionEffect.Parameters["RippleFrequency"].SetValue(WaterShader.RippleFrequency);
        _waterDistortionEffect.Parameters["RippleSpeed"].SetValue(WaterShader.RippleSpeed);
        _waterDistortionEffect.Parameters["AspectRatio"].SetValue((float)_virtualWidth / _virtualHeight);
        _waterDistortionEffect.Parameters["WaterTintColor"].SetValue(WaterShader.WaterTintColor);

        _lightingRenderer = new LightingRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        var radialGradient = _content.Load<Texture2D>("Sprites/RadialGradient");
        _lightingRenderer.LoadContent(radialGradient);

        _cloudShadowRenderer = new CloudShadowRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        _cloudShadowRenderer.LoadContent();
        _fireLightData = new LightData[_firepits.Length];
        _fireflyManager = new FireflyManager();
        _combinedLightData = new LightData[_firepits.Length + MaxFireflyLights];

        _occlusionRevealRenderer = new OcclusionRevealRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        _occlusionRevealRenderer.LoadContent(_content);

        _musicManager.LoadContent(_content);
        _musicManager.PlaySong("GameplayTheme", loopDelaySeconds: 5f);

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            // Higher resolution factor rasterizes glyphs at 2× then downscales,
            // producing sharper text at the small 480×270 virtual resolution.
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(_content.RootDirectory, "Fonts", "Nunito.ttf")));
        _hudRenderer = new HudRenderer();
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
        _isPlayerOccluded = CheckOcclusion(_player.Bounds, _player.Bounds.Bottom / (float)_worldRenderer.MapPixelHeight);
        _isFollowerOccluded = CheckOcclusion(_follower.Bounds, _follower.Bounds.Bottom / (float)_worldRenderer.MapPixelHeight);
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
        _rippleSystem.Update(gameTime, input, _camera, _graphicsDevice, _virtualWidth, _virtualHeight);
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var worldMatrix = _camera.GetViewMatrix();

        // --- Pass 1: Render water tiles to a separate render target ---
        // Cache the render target that was active when Draw() was called (set by Game1).
        // GetRenderTargets() allocates a new array every call, so we only do this once
        // and store the result for the duration of the frame.
        var bindings = _graphicsDevice.GetRenderTargets();
        _previousRenderTarget = bindings.Length > 0
            ? bindings[0].RenderTarget as RenderTarget2D
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
        _graphicsDevice.SetRenderTarget(_previousRenderTarget);

        // --- Pass 2: Composite water with distortion shader ---
        // LinearClamp smooths the UV displacement for natural-looking waves
        // instead of the pixel-snapping that PointClamp would produce.
        _waterDistortionEffect.Parameters["Time"].SetValue(_worldRenderer.WaterElapsedSeconds);
        // Anchor waves to world space so they don't slide when the camera pans.
        _waterDistortionEffect.Parameters["CameraOffset"].SetValue(
            new Vector2(_camera.Position.X / _virtualWidth, _camera.Position.Y / _virtualHeight));
        _rippleSystem.SetShaderParameters(_waterDistortionEffect, _camera, _virtualWidth, _virtualHeight);
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
        var playerDepth = _player.Bounds.Bottom / mapHeight;

        var followerDepth = _follower.Bounds.Bottom / mapHeight;
        var anyOccluded = _isPlayerOccluded || _isFollowerOccluded;

        if (anyOccluded)
        {
            // The cutoff depth for the "behind" pass is the shallowest (smallest) of
            // whichever characters are occluded, so all occluded characters are drawn
            // in the first pass and the reveal lens uncovers them through the occluders.
            var behindCutoff = _isPlayerOccluded && _isFollowerOccluded
                ? Math.Min(playerDepth, followerDepth)
                : _isPlayerOccluded ? playerDepth : followerDepth;

            // --- Pass 4a: Entities behind/at shallowest occluded character + characters ---
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            DrawWorldEntities(mapHeight, behindCutoff, EntityDepthFilter.BehindOrAtPlayer);
            _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet, followerDepth);
            _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet, playerDepth);
            if (_showCollisionBounds)
            {
                DrawCollisionBounds();
            }
            _worldSpriteBatch.End();

            // --- Pass 4b: Entities in front of shallowest occluded character → occluder render target ---
            _occlusionRevealRenderer.BeginCapture();
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            DrawWorldEntities(mapHeight, behindCutoff, EntityDepthFilter.InFrontOfPlayer);
            _worldSpriteBatch.End();

            // --- Pass 4c: Composite occluders with circular reveal lens(es) ---
            _occlusionRevealRenderer.Composite(
                _worldSpriteBatch,
                _player.Center,
                _isFollowerOccluded ? _follower.Center : null,
                worldMatrix,
                _previousRenderTarget);
        }
        else
        {
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            DrawWorldEntities(mapHeight, playerDepth, EntityDepthFilter.All);
            _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet, followerDepth);
            _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet, playerDepth);
            if (_showCollisionBounds)
            {
                DrawCollisionBounds();
            }
            _worldSpriteBatch.End();
        }

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
            _previousRenderTarget);

        // Lighting pass: fills a low-res lightmap with ambient darkness, draws fire glows
        // additively, then composites it over the scene with multiply blend.
        // The LightingRenderer also handles the day/night darkening, replacing the old
        // single-quad multiply pass. When NightStrength = 0 the pass is skipped entirely.
        _lightingRenderer.Draw(
            _worldSpriteBatch,
            _dayNightCycle.NightStrength,
            worldMatrix,
            _previousRenderTarget);

    }

    /// <inheritdoc />
    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        // Get the font at a size scaled for the actual window resolution.
        // sceneScale converts virtual pixels to window pixels.
        var scaledFont = _fontSystem.GetFont(HudFontSize * sceneScale);

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);
        _hudRenderer.Draw(spriteBatch, scaledFont, _pixelTexture, _dayNightCycle.GameHour, sceneScale);
        spriteBatch.End();
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _occlusionRevealRenderer?.UnloadContent();
        _cloudShadowRenderer?.UnloadContent();
        _lightingRenderer?.UnloadContent();
        _worldSpriteBatch?.Dispose();
        _pixelTexture?.Dispose();
        _waterRenderTarget?.Dispose();
        _surfaceReachRenderTarget?.Dispose();
        _surfaceReachGradientTarget?.Dispose();
        _fontSystem?.Dispose();
    }

    private void DrawCollisionBounds()
    {
        _debugRenderer.DrawTileGrid(
            _worldSpriteBatch,
            _worldRenderer.MapPixelWidth,
            _worldRenderer.MapPixelHeight,
            _worldRenderer.TileWidthPixels,
            _worldRenderer.TileHeightPixels);
        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _player.FootBounds, Color.Yellow);
        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _player.Bounds, Color.OrangeRed);
        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _follower.Bounds, Color.Cyan);

        for (var i = 0; i < _boulders.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _boulders[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _firepits[i].Bounds, Color.Red);
        }

        var colliderBounds = _worldRenderer.ColliderBounds;
        for (var i = 0; i < colliderBounds.Count; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, colliderBounds[i], Color.Magenta);
        }
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

    /// <summary>Depth filter mode for <see cref="DrawWorldEntities"/>.</summary>
    private enum EntityDepthFilter { All, BehindOrAtPlayer, InFrontOfPlayer }

    /// <summary>
    /// Draws all world prop entities (not the player or follower) that pass
    /// the depth filter relative to the player. Each entity's sort depth is
    /// computed identically to the original single-pass drawing code.
    /// </summary>
    private void DrawWorldEntities(float mapHeight, float playerDepth, EntityDepthFilter filter)
    {
        for (var i = 0; i < _boulders.Length; i++)
        {
            var depth = _boulders[i].Bounds.Bottom / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _boulders[i].Draw(_worldSpriteBatch, depth);
        }

        // Docks always draw at depth 0 — always behind every sorted entity.
        if (filter != EntityDepthFilter.InFrontOfPlayer)
        {
            for (var i = 0; i < _docks.Length; i++)
                _docks[i].Draw(_worldSpriteBatch);
        }

        for (var i = 0; i < _sunkenLogs.Length; i++)
        {
            var depth = _sunkenLogs[i].Bounds.Bottom / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _sunkenLogs[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _sunkenChests.Length; i++)
        {
            var depth = _sunkenChests[i].Bounds.Bottom / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _sunkenChests[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            var depth = _firepits[i].Bounds.Bottom / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _firepits[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _cozyLakeCabins.Length; i++)
        {
            var cabinBounds = _cozyLakeCabins[i].Bounds;
            var depth = (cabinBounds.Bottom - CabinSortAnchorOffsetPixels) / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _cozyLakeCabins[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _pineTrees.Length; i++)
        {
            var treeBounds = _pineTrees[i].Bounds;
            var depth = (treeBounds.Bottom - PineTreeSortAnchorOffsetPixels) / mapHeight;
            if (PassesDepthFilter(depth, playerDepth, filter))
                _pineTrees[i].Draw(_worldSpriteBatch, depth);
        }
    }

    private static bool PassesDepthFilter(float depth, float playerDepth, EntityDepthFilter filter)
    {
        return filter switch
        {
            EntityDepthFilter.BehindOrAtPlayer => depth <= playerDepth,
            EntityDepthFilter.InFrontOfPlayer => depth > playerDepth,
            _ => true
        };
    }

    /// <summary>
    /// Checks whether any world entity that sorts in front of <paramref name="characterBounds"/>
    /// fully conceals those bounds. Used to activate the reveal lens for the player or follower.
    /// </summary>
    /// <param name="characterBounds">The character's world-space bounding rectangle.</param>
    /// <param name="characterDepth">The character's sort depth (bottom / mapHeight).</param>
    private bool CheckOcclusion(Rectangle characterBounds, float characterDepth)
    {
        var mapHeight = (float)_worldRenderer.MapPixelHeight;

        for (var i = 0; i < _cozyLakeCabins.Length; i++)
        {
            var depth = (_cozyLakeCabins[i].Bounds.Bottom - CabinSortAnchorOffsetPixels) / mapHeight;
            if (depth > characterDepth && _cozyLakeCabins[i].Bounds.Contains(characterBounds))
                return true;
        }

        for (var i = 0; i < _pineTrees.Length; i++)
        {
            var depth = (_pineTrees[i].Bounds.Bottom - PineTreeSortAnchorOffsetPixels) / mapHeight;
            if (depth > characterDepth && _pineTrees[i].Bounds.Contains(characterBounds))
                return true;
        }

        for (var i = 0; i < _boulders.Length; i++)
        {
            var depth = _boulders[i].Bounds.Bottom / mapHeight;
            if (depth > characterDepth && _boulders[i].Bounds.Contains(characterBounds))
                return true;
        }

        for (var i = 0; i < _sunkenLogs.Length; i++)
        {
            var depth = _sunkenLogs[i].Bounds.Bottom / mapHeight;
            if (depth > characterDepth && _sunkenLogs[i].Bounds.Contains(characterBounds))
                return true;
        }

        return false;
    }
}
