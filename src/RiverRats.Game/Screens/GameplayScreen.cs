using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using RiverRats.Components;
using RiverRats.Data;
using RiverRats.Game.Components;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Graphics;
using RiverRats.Game.Audio;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Game.UI;
using RiverRats.Game.World;

// EntityDepthFilter is now in RiverRats.Game.Systems (shared).
// OcclusionEntry and OcclusionRevealRenderer.CheckOcclusion() replace the old CheckOcclusion method.

namespace RiverRats.Game.Screens;

/// <summary>
/// Primary gameplay screen. Owns the player, camera, world renderer, and gameplay loop.
/// </summary>
public sealed class GameplayScreen : IGameScreen
{
    private const float FrontDoorInvitationTileScale = 0.0125f;
    private const int PlayerFramePixels = 32;
    private const float PlayerMoveSpeedPixelsPerSecond = 96f;
    private const float PlayerAccelerationRate = 10f;
    private const int WalkFramesPerDirection = 4;
    private const float WalkFrameDuration = 0.15f;
    private const float HudFontSize = 14f;
    private const float DayNightCycleDurationSeconds = 600f;
    private const float DayNightCycleStartProgress = 0.30f;
    private const int GradientStripCount = 32;
    private const int MaxFireflyLights = 32;
    private const int MaxParticleCount = 512;
    private const float CabinSortAnchorOffsetPixels = 52f;
    private const float PineTreeSortAnchorOffsetPixels = 10f;
    private const float FishingPromptUiScale = 1.15f;
    private const float FishingPromptHeadGapPixels = 6f;
    private const int FishingInteractionReachPixels = 16;
    private const float BirchTreeSortAnchorOffsetPixels = 10f;
    private const float DeadTreeSortAnchorOffsetPixels = 10f;
    private const float DeciduousTreeSortAnchorOffsetPixels = 10f;
    private const float BushSortAnchorOffsetPixels = 4f;
    private const float ZoneTransitionFadeDurationSeconds = 0.4f;
    private const float ZoneTransitionBlackHoldSeconds = 0.15f;
    private const float GameplayMusicVolume = 1f;
    private static readonly WaterShaderConfig WaterShader = WaterShaderConfig.Default;
    private static readonly EmptyInputManager EmptyInput = new();

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

    private static readonly ParticleProfile ArrowTrailSparkProfile = new()
    {
        SpawnRate = 1f,
        MinLife = 0.45f,
        MaxLife = 0.7f,
        MinSpeed = 28f,
        MaxSpeed = 60f,
        MinScale = 0.12f,
        MaxScale = 0.22f,
        StartColor = new Color(255, 222, 120, 230),
        EndColor = new Color(255, 90, 28, 0),
        SpreadRadians = MathHelper.ToRadians(28f),
        Gravity = 260f,
        MinGroundOffset = 6f,
        MaxGroundOffset = 12f,
        MaxGroundBounces = 3,
        BounceDamping = 0.45f,
        BounceFriction = 0.72f,
    };

    private static readonly ParticleProfile LevelUpBurstProfile = new()
    {
        SpawnRate = 1f,
        MinLife = 0.4f,
        MaxLife = 0.7f,
        MinSpeed = 60f,
        MaxSpeed = 100f,
        MinScale = 0.3f,
        MaxScale = 0.6f,
        StartColor = new Color(255, 215, 0, 255),
        EndColor = new Color(255, 180, 0, 0),
        SpreadRadians = MathHelper.ToRadians(30f),
        Gravity = 80f,
    };

    // Shared combat constants (kept here — still referenced inline below).
    private const float GnomeDeathTrauma = 0.06f;
    private const float PlayerHitTrauma = 0.15f;
    private const float PlayerHitFlashDuration = 0.3f;
    private const int GnomeDeathSfxCount = 12;
    private const float GnomeDeathSfxVolume = 0.5f;
    private const int PlayerHurtSfxCount = 4;
    private const float PlayerHurtSfxVolume = 0.7f;
    private const int OrbCollectVariationSfxCount = 12;
    private const int RedOrbCollectVariationSfxCount = 3;
    private const float OrbCollectSfxVolume = 0.75f;
    private const float RedOrbCollectSfxVolume = 0.82f;
    private const float BomberBlastRadiusSq = 48f * 48f;
    private const float BomberDeathTrauma = 0.12f;
    private const float DoorOpenSfxVolume = 0.48f;
    private const float DoorCloseSfxVolume = 0.42f;
    private const float EnergyOrbDropChance = 0.75f;
    private const int MaxEnergyOrbs = 128;
    private const int MaxExplosions = 16;
    private const int MaxHealthPickups = 2;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly Action _requestExit;
    private readonly string _mapAssetName;
    private readonly string _spawnPointId;
    private readonly Vector2? _spawnPosition;
    private readonly bool _fadeInFromBlack;
    private readonly MapConfig _mapConfig;

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
    private Texture2D _frontDoorClosedTexture;
    private Texture2D _frontDoorOpenTexture;
    private Texture2D _dockLegLeftTexture;
    private Texture2D _sunkenLogTexture;
    private Texture2D _sunkenChestTexture;
    private Texture2D _firepitTexture;
    private Firepit[] _firepits;
    private Texture2D _smallFireSpriteSheet;
    private Texture2D _cozyLakeCabinTexture;
    private Cabin[] _cozyLakeCabins;
    private Boulder[] _boulders;
    private GardenGnome[] _gardenGnomes;
    private Boulder[] _welcomeMats;
    private Boulder[] _areaRugs;
    private Boulder[] _couches;
    private Boulder[] _oldTvs;
    private Boulder[] _gameConsoles;
    private Boulder[] _pottedPlants1;
    private Boulder[] _entertainmentShelves;
    private Boulder[] _sunkenLogs;
    private Boulder[] _underwaterSunkenLogs;
    private SunkenChest[] _sunkenChests;
    private SunkenChest[] _underwaterSunkenChests;
    private FlatShoreDepthSimulator[] _flatShoreDepthSimulators;
    private Boulder[] _seaweeds;
    private Tree[] _pineTrees;
    private Tree[] _birchTrees;
    private Tree[] _deadTrees;
    private Tree[] _deciduousTrees;
    private Tree[] _bushes;
    private Dock[] _docks;
    private FrontDoor[] _frontDoors;
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
    private int _debugOverlayMode;
    private RippleSystem _rippleSystem;
    private GnomeSpawner _gnomeSpawner;
    private WaveManager _waveManager;
    private XpLevelSystem _xpSystem;
    private FlowField _flowField;
    private Texture2D _gnomeEnemyTexture;
    private Texture2D _projectileArrowTexture;
    private ProjectileSystem _projectileSystem;
    private SlashSystem _slashSystem;
    private Texture2D _hatchetTexture;
    private Texture2D _explosionTexture;
    private Texture2D _energyOrbTexture;
    private Texture2D _energyOrbRedTexture;
    private EnergyOrbSystem _energyOrbSystem;
    private ExplosionSystem _explosionSystem;
    private HealthPickupSystem _healthPickupSystem;
    private FollowerSystem _followerSystem;
    private CrtTransitionRenderer _crtTransitionRenderer;
    private readonly List<OcclusionEntry> _occluders = new();
    private float _playerHitFlashTimer;
    private Health _playerHealth;
    private PlayerCombatStats _combatStats;
    private const float LevelUpFlashDuration = 0.5f;
    private float _levelUpFlashTimer;
    private bool _playerDead;
    private float _deathDelayTimer;
    private readonly SoundEffect[] _gnomeDeathSfx = new SoundEffect[GnomeDeathSfxCount];
    private readonly SoundEffect[] _playerHurtSfx = new SoundEffect[PlayerHurtSfxCount];
    private readonly SoundEffect[] _orbCollectVariationSfx = new SoundEffect[OrbCollectVariationSfxCount];
    private readonly SoundEffect[] _redOrbCollectVariationSfx = new SoundEffect[RedOrbCollectVariationSfxCount];
    private SoundEffect _doorOpenSfx;
    private SoundEffect _doorCloseSfx;
    private readonly Random _sfxRng = new Random();
    private readonly IMusicManager _musicManager = new MusicManager();
    private Texture2D _hookIconTexture;
    private bool _playerInFishingZone;
    private HudRenderer _hudRenderer;
    private ForestHudRenderer _forestHudRenderer;
    private FontSystem _fontSystem;
    private readonly ScreenManager _screenManager;
    private readonly float _dayNightStartProgress;
    private FadeState _fadeState;
    private float _fadeAlpha;
    private float _fadeHoldTimer;
    private ZoneTransitionRequest? _pendingZoneTransition;
    private bool _pendingFishingTransition;

    /// <inheritdoc />
    public bool IsTransparent => false;

    /// <summary>Music manager for this screen's audio, exposed for overlay screens (e.g., pause).</summary>
    public IMusicManager MusicManager => _musicManager;

    /// <summary>
    /// Creates a gameplay screen.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device for rendering.</param>
    /// <param name="content">Content manager for loading assets.</param>
    /// <param name="virtualWidth">Virtual resolution width.</param>
    /// <param name="virtualHeight">Virtual resolution height.</param>
    /// <param name="screenManager">Screen manager used to push overlay screens.</param>
    /// <param name="requestExit">Callback to request the game exit.</param>
    /// <param name="mapAssetName">Content asset name for the TMX map to load.</param>
    /// <param name="spawnPointId">Name of the spawn point to place the player at, or null for map center.</param>
    /// <param name="fadeInFromBlack">When true, the screen starts fully black and fades in.</param>
    /// <param name="dayNightStartProgress">Starting cycle progress (0–1). Pass the previous zone's progress to preserve time across transitions.</param>
    /// <param name="spawnPosition">Exact world-space position to place the player at. Overrides spawnPointId when set.</param>
    public GameplayScreen(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight,
        ScreenManager screenManager,
        Action requestExit,
        string mapAssetName = "Maps/StarterMap",
        string spawnPointId = null,
        bool fadeInFromBlack = false,
        float dayNightStartProgress = DayNightCycleStartProgress,
        Vector2? spawnPosition = null)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _screenManager = screenManager;
        _requestExit = requestExit;
        _mapAssetName = mapAssetName;
        _spawnPointId = spawnPointId;
        _spawnPosition = spawnPosition;
        _fadeInFromBlack = fadeInFromBlack;
        _mapConfig = MapConfig.ForMap(mapAssetName);
        _dayNightStartProgress = dayNightStartProgress;
    }

    /// <inheritdoc />
    public void LoadContent()
    {
        _fadeState = _fadeInFromBlack ? FadeState.FadingIn : FadeState.None;
        _fadeAlpha = _fadeInFromBlack ? 1f : 0f;
        _fadeHoldTimer = 0f;
        _pendingZoneTransition = null;

        _worldSpriteBatch = new SpriteBatch(_graphicsDevice);
        _worldRenderer = new TiledWorldRenderer(_graphicsDevice, _content, _mapAssetName);
        _camera = new Camera2D(
            _virtualWidth,
            _virtualHeight,
            _worldRenderer.MapPixelWidth,
            _worldRenderer.MapPixelHeight);

        _playerSpriteSheet = _content.Load<Texture2D>("Sprites/generic_character_sheet");
        _followerSpriteSheet = _content.Load<Texture2D>("Sprites/companion_character_sheet");
        _boulderTexture = _content.Load<Texture2D>("Sprites/boulder");
        _dockTexture = _content.Load<Texture2D>("Sprites/wooden-dock");
        _frontDoorClosedTexture = _content.Load<Texture2D>("Sprites/front-door-closed");
        _frontDoorOpenTexture = _content.Load<Texture2D>("Sprites/front-door-open");
        _doorOpenSfx = _content.Load<SoundEffect>("Audio/SFX/door_open_creak");
        _doorCloseSfx = _content.Load<SoundEffect>("Audio/SFX/door_close_clunk");
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
        var birchTreeTexture = _content.Load<Texture2D>("Sprites/birch-tree");
        var deadTreeTextures = new[]
        {
            _content.Load<Texture2D>("Sprites/dead-tree1"),
            _content.Load<Texture2D>("Sprites/dead-tree2"),
            _content.Load<Texture2D>("Sprites/dead-tree3"),
            _content.Load<Texture2D>("Sprites/dead-tree4"),
        };
        var deadTreeCollisionBoxes = new[]
        {
            PropFactory.DeadTree1CollisionBoxes,
            PropFactory.DeadTree2CollisionBoxes,
            PropFactory.DeadTree3CollisionBoxes,
            PropFactory.DeadTree4CollisionBoxes,
        };
        var deciduousTreeTextures = new[]
        {
            _content.Load<Texture2D>("Sprites/deciduous-tree1"),
            _content.Load<Texture2D>("Sprites/deciduous-tree2"),
            _content.Load<Texture2D>("Sprites/deciduous-tree3"),
            _content.Load<Texture2D>("Sprites/deciduous-tree4"),
        };
        var deciduousTreeCollisionBoxes = new[]
        {
            PropFactory.DeciduousTree1CollisionBoxes,
            PropFactory.DeciduousTree2CollisionBoxes,
            PropFactory.DeciduousTree3CollisionBoxes,
            PropFactory.DeciduousTree4CollisionBoxes,
        };
        _playerAnimator = new SpriteAnimator(
            PlayerFramePixels, PlayerFramePixels,
            WalkFramesPerDirection, WalkFrameDuration);
        _followerAnimator = new SpriteAnimator(
            PlayerFramePixels, PlayerFramePixels,
            WalkFramesPerDirection, WalkFrameDuration);

        var initialPosition = _spawnPosition
            ?? FindSpawnPosition(_worldRenderer.SpawnPoints, _spawnPointId)
            ?? new Vector2(
                (_worldRenderer.MapPixelWidth / 2f) - (PlayerFramePixels / 2f),
                (_worldRenderer.MapPixelHeight / 2f) - (PlayerFramePixels / 2f));

        _player = new PlayerBlock(
            initialPosition,
            new Point(PlayerFramePixels, PlayerFramePixels),
            PlayerMoveSpeedPixelsPerSecond,
            new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight),
            PlayerAccelerationRate);

        _followerSystem = new FollowerSystem(
            _mapConfig.FollowerConfig,
            PlayerFramePixels,
            useClusterAttraction: _mapAssetName == "Maps/WoodsBehindCabin");

        var followerStartPosition = initialPosition + new Vector2(0f, _mapConfig.FollowerConfig.FollowDistancePixels);
        _follower = new FollowerBlock(
            followerStartPosition,
            new Point(PlayerFramePixels, PlayerFramePixels),
            new Rectangle(0, 0, _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight),
            _mapConfig.FollowerConfig);

        _boulders = PropFactory.CreateBoulders(_boulderTexture, _worldRenderer.PropPlacements);
        _docks = PropFactory.CreateDocks(_dockTexture, _worldRenderer.PropPlacements);
        _frontDoors = PropFactory.CreateFrontDoors(_frontDoorClosedTexture, _frontDoorOpenTexture, _worldRenderer.PropPlacements);
        _dockLegsLeft = PropFactory.CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: false);
        _surfaceReachDockLegsLeft = PropFactory.CreatePropsByType(_dockLegLeftTexture, _worldRenderer.PropPlacements, "dock-leg-left", isUnderwater: true, reachesSurface: true);
        var sunkenLogs = PropFactory.CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: false);
        var logTexture = _content.Load<Texture2D>("Sprites/log");
        var landLogs = PropFactory.CreatePropsByType(logTexture, _worldRenderer.PropPlacements, "log", isUnderwater: false);
        _sunkenLogs = [.. sunkenLogs, .. landLogs];
        _underwaterSunkenLogs = PropFactory.CreatePropsByType(_sunkenLogTexture, _worldRenderer.PropPlacements, "sunken-log", isUnderwater: true);
        _sunkenChests = PropFactory.CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: false);
        _underwaterSunkenChests = PropFactory.CreateSunkenChests(_sunkenChestTexture, _worldRenderer.PropPlacements, isUnderwater: true);
        _flatShoreDepthSimulators = PropFactory.CreateFlatShoreDepthSimulators(flatShoreDepthSimulatorTexture, _worldRenderer.PropPlacements);
        _seaweeds = PropFactory.CreateSeaweeds(seaweedTextures, _worldRenderer.PropPlacements);
        _firepits = PropFactory.CreateFirepits(_firepitTexture, _smallFireSpriteSheet, _worldRenderer.PropPlacements);
        _cozyLakeCabins = PropFactory.CreateCabins(_cozyLakeCabinTexture, PropFactory.CozyCabinCollisionBoxes, _worldRenderer.PropPlacements, "cozy-lake-cabin");
        _pineTrees = PropFactory.CreateTrees(pineTreeTexture, PropFactory.PineTreeCollisionBoxes, _worldRenderer.PropPlacements, "pine-tree");
        _birchTrees = PropFactory.CreateTrees(birchTreeTexture, PropFactory.BirchTreeCollisionBoxes, _worldRenderer.PropPlacements, "birch-tree");
        _deadTrees = PropFactory.CreateVariantTrees(deadTreeTextures, deadTreeCollisionBoxes, _worldRenderer.PropPlacements, "dead-tree");
        _deciduousTrees = PropFactory.CreateVariantTrees(deciduousTreeTextures, deciduousTreeCollisionBoxes, _worldRenderer.PropPlacements, "deciduous-tree");
        var bushTextures = new[]
        {
            _content.Load<Texture2D>("Sprites/bush1"),
            _content.Load<Texture2D>("Sprites/bush2"),
            _content.Load<Texture2D>("Sprites/bush3"),
        };
        var bushCollisionBoxes = new[]
        {
            PropFactory.Bush1CollisionBoxes,
            PropFactory.Bush2CollisionBoxes,
            PropFactory.Bush3CollisionBoxes,
        };
        _bushes = PropFactory.CreateVariantTrees(bushTextures, bushCollisionBoxes, _worldRenderer.PropPlacements, "bush");

        // Garden gnomes must be created after trees so we can find the nearest tree for hide behavior.
        var allTrees = new List<Tree>(_pineTrees.Length + _birchTrees.Length + _deadTrees.Length + _deciduousTrees.Length);
        allTrees.AddRange(_pineTrees);
        allTrees.AddRange(_birchTrees);
        allTrees.AddRange(_deadTrees);
        allTrees.AddRange(_deciduousTrees);
        _gardenGnomes = PropFactory.CreateGardenGnomes(_content.Load<Texture2D>("Sprites/garden-gnome"), _worldRenderer.PropPlacements, allTrees);
        _welcomeMats = PropFactory.CreateWelcomeMats(_content.Load<Texture2D>("Sprites/welcome-mat"), _worldRenderer.PropPlacements);
        _areaRugs = PropFactory.CreateAreaRugs(_content.Load<Texture2D>("Sprites/area-rug"), _worldRenderer.PropPlacements);
        _couches = PropFactory.CreateCouches(_content.Load<Texture2D>("Sprites/old-couch"), _worldRenderer.PropPlacements);
        _oldTvs = PropFactory.CreatePropsByType(_content.Load<Texture2D>("Sprites/old-tv"), _worldRenderer.PropPlacements, "old-tv", isUnderwater: false);
        _gameConsoles = PropFactory.CreatePropsByType(_content.Load<Texture2D>("Sprites/game-console"), _worldRenderer.PropPlacements, "game-console", isUnderwater: false);
        _pottedPlants1 = PropFactory.CreatePropsByType(_content.Load<Texture2D>("Sprites/potted-plant-1"), _worldRenderer.PropPlacements, "potted-plant-1", isUnderwater: false);
        _entertainmentShelves = PropFactory.CreatePropsByType(_content.Load<Texture2D>("Sprites/entertainment-shelf"), _worldRenderer.PropPlacements, "entertainment-shelf", isUnderwater: false, collisionHeightPixels: 32);

        _smokeTexture = _content.Load<Texture2D>("Sprites/smoke-puff");
        _hookIconTexture = _content.Load<Texture2D>("Sprites/hook-icon");
        _particleManager = new ParticleManager(MaxParticleCount);

        if (_mapAssetName == "Maps/WoodsBehindCabin")
        {
            _gnomeEnemyTexture = _content.Load<Texture2D>("Sprites/garden-gnome");
            _projectileArrowTexture = _content.Load<Texture2D>("Sprites/projectile-arrow");
            _explosionTexture = _content.Load<Texture2D>("Sprites/explosion");
            _energyOrbTexture = _content.Load<Texture2D>("Sprites/energy-orb");
            _energyOrbRedTexture = _content.Load<Texture2D>("Sprites/energy-orb-red");
            for (var i = 0; i < GnomeDeathSfxCount; i++)
                _gnomeDeathSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/gnome_death_{i:D2}");
            for (var i = 0; i < PlayerHurtSfxCount; i++)
                _playerHurtSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/player_hurt_{i:D2}");
            for (var i = 0; i < OrbCollectVariationSfxCount; i++)
                _orbCollectVariationSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/orb_collect_v03p03_family_{i:D2}");
            for (var i = 0; i < RedOrbCollectVariationSfxCount; i++)
                _redOrbCollectVariationSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/orb_collect_red_var_{i:D2}");
            _combatStats = new PlayerCombatStats();
            _playerHealth = new Health(_combatStats.MaxHp);
            _playerHealth.OnDied += OnPlayerDied;
            _xpSystem = new XpLevelSystem(_combatStats, _playerHealth);
            _xpSystem.OnLevelUp += level =>
            {
                _levelUpFlashTimer = LevelUpFlashDuration;
                if (_redOrbCollectVariationSfx[0] != null)
                    _redOrbCollectVariationSfx[0].Play(0.9f, 0.3f, 0f);

                // Spawn gold particles around player on level-up.
                for (var i = 0; i < 12; i++)
                {
                    var angle = i * (MathF.PI * 2f / 12f);
                    var speed = 60f + (float)_sfxRng.NextDouble() * 40f;
                    var vel = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
                    _particleManager.Emit(LevelUpBurstProfile, _player.Center, 1, angle);
                }
            };
            _gnomeSpawner = new GnomeSpawner(initialCount: 10, spawnIntervalSeconds: 0.15f, maxActive: 200);
            _projectileSystem = new ProjectileSystem(
                maxProjectiles: 32,
                fireIntervalSeconds: 1.8f,
                trailParticleManager: _particleManager,
                trailParticleProfile: ArrowTrailSparkProfile);
            _waveManager = new WaveManager(_gnomeSpawner);
            _waveManager.OnWaveCleared += waveNum => { /* future: banner display */ };
            _waveManager.OnAllWavesComplete += OnAllWavesComplete;
            _waveManager.StartFirstWave();

            _forestHudRenderer = new ForestHudRenderer();
            _energyOrbSystem = new EnergyOrbSystem(MaxEnergyOrbs);
            _explosionSystem = new ExplosionSystem(MaxExplosions);
            _healthPickupSystem = new HealthPickupSystem(MaxHealthPickups, _sfxRng);
        }

        if (_mapAssetName == "Maps/WoodsBehindCabin")
            _slashSystem = new SlashSystem();

        if (_mapAssetName == "Maps/WoodsBehindCabin")
            _hatchetTexture = _content.Load<Texture2D>("Sprites/hatchet");

        if (_gnomeSpawner != null)
        {
            _gnomeSpawner.OnGnomeDied = (pos, enemyType, dropsLoot) =>
            {
                _explosionSystem?.Spawn(pos);
                if (dropsLoot && _sfxRng.NextDouble() < EnergyOrbDropChance)
                    _energyOrbSystem?.Spawn(pos, _sfxRng);
                _camera.AddTrauma(GnomeDeathTrauma);
                _gnomeDeathSfx[_sfxRng.Next(GnomeDeathSfxCount)].Play(GnomeDeathSfxVolume, 0f, 0f);

                // Bomber gnomes explode on death, dealing damage to the player if within blast radius.
                // Only trigger blast damage for loot-dropping deaths (player-killed, not wave-end cleanup).
                if (dropsLoot && enemyType == EnemyType.Bomber)
                {
                    _camera.AddTrauma(BomberDeathTrauma);
                    var distSq = Vector2.DistanceSquared(pos, _player.Center);
                    if (distSq <= BomberBlastRadiusSq && _playerHealth != null && _playerHealth.IsAlive && !_playerHealth.IsInvincible)
                    {
                        _playerHealth.TakeDamage(1);
                        _playerHealth.SetInvincibleForDuration(1.0f);
                        _playerHitFlashTimer = PlayerHitFlashDuration;
                        _playerHurtSfx[_sfxRng.Next(PlayerHurtSfxCount)].Play(PlayerHurtSfxVolume, 0f, 0f);
                    }
                }
            };
            _gnomeSpawner.OnPlayerHit = () =>
            {
                if (_playerHealth != null)
                {
                    if (!_playerHealth.IsAlive) return;
                    if (_playerHealth.IsInvincible) return;
                    _playerHealth.TakeDamage(1);
                    _playerHealth.SetInvincibleForDuration(1.0f);
                }

                _playerHitFlashTimer = PlayerHitFlashDuration;
                _camera.AddTrauma(PlayerHitTrauma);
                _playerHurtSfx[_sfxRng.Next(PlayerHurtSfxCount)].Play(PlayerHurtSfxVolume, 0f, 0f);
            };
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].AttachSmokeEmitter(new ParticleEmitter(_particleManager, FireSmokeProfile));
            _firepits[i].AttachSparkEmitter(new ParticleEmitter(_particleManager, FireSparkProfile));
        }
        var propObstacleBounds = PropFactory.MergeRectangleArrays(PropFactory.GetBoulderBounds(_boulders), PropFactory.GetFirepitBounds(_firepits));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetBoulderBounds(_couches));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetBoulderBounds(_oldTvs));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetBoulderBounds(_gameConsoles));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetBoulderBounds(_pottedPlants1));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetBoulderBounds(_entertainmentShelves));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetTreeCollisionBounds(_pineTrees));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetTreeCollisionBounds(_birchTrees));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetTreeCollisionBounds(_deadTrees));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetTreeCollisionBounds(_deciduousTrees));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetTreeCollisionBounds(_bushes));
        propObstacleBounds = PropFactory.MergeRectangleArrays(propObstacleBounds, PropFactory.GetCabinCollisionBounds(_cozyLakeCabins));
        _collisionMap = new WorldCollisionMap(_worldRenderer, PropFactory.MergeObstacleBounds(propObstacleBounds, _worldRenderer.ColliderBounds), PropFactory.GetDockBounds(_docks));

        if (_mapAssetName == "Maps/WoodsBehindCabin")
            _flowField = new FlowField(_worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight, _collisionMap, agentRadius: 8);

        _camera.LookAt(_player.Center);

        _dayNightCycle = new DayNightCycle(
            DayNightCycleDurationSeconds,
            _dayNightStartProgress);

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

        if (_mapConfig.HasCloudShadows)
        {
            _cloudShadowRenderer = new CloudShadowRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
            _cloudShadowRenderer.LoadContent();
        }
        _fireLightData = new LightData[_firepits.Length];
        _fireflyManager = new FireflyManager();
        _combinedLightData = new LightData[_firepits.Length + MaxFireflyLights];

        _occlusionRevealRenderer = new OcclusionRevealRenderer(_graphicsDevice, _virtualWidth, _virtualHeight);
        _occlusionRevealRenderer.LoadContent(_content);

        _musicManager.LoadContent(_content);
        _musicManager.SetVolume(_fadeInFromBlack ? 0f : GameplayMusicVolume);
        _musicManager.PlaySong(_mapConfig.SongName, loopDelaySeconds: 5f);

        _fontSystem = new FontSystem(new FontSystemSettings
        {
            // Higher resolution factor rasterizes glyphs at 2× then downscales,
            // producing sharper text at the small 480×270 virtual resolution.
            FontResolutionFactor = 2f,
            KernelWidth = 0,
            KernelHeight = 0,
        });
        _fontSystem.AddFont(File.ReadAllBytes(
            Path.Combine(global::System.AppContext.BaseDirectory, _content.RootDirectory, "Fonts", "Nunito.ttf")));
        _hudRenderer = new HudRenderer();

        // Build flat occluder list for OcclusionRevealRenderer.CheckOcclusion().
        _occluders.Clear();
        foreach (var c in _cozyLakeCabins)  _occluders.Add(new OcclusionEntry(c, CabinSortAnchorOffsetPixels));
        foreach (var d in _frontDoors)      _occluders.Add(new OcclusionEntry(d));
        foreach (var t in _pineTrees)       _occluders.Add(new OcclusionEntry(t, PineTreeSortAnchorOffsetPixels));
        foreach (var t in _birchTrees)      _occluders.Add(new OcclusionEntry(t, BirchTreeSortAnchorOffsetPixels));
        foreach (var t in _deadTrees)       _occluders.Add(new OcclusionEntry(t, DeadTreeSortAnchorOffsetPixels));
        foreach (var t in _deciduousTrees)  _occluders.Add(new OcclusionEntry(t, DeciduousTreeSortAnchorOffsetPixels));
        foreach (var b in _bushes)          _occluders.Add(new OcclusionEntry(b, BushSortAnchorOffsetPixels));
        foreach (var b in _boulders)        _occluders.Add(new OcclusionEntry(b));
        foreach (var c in _couches)         _occluders.Add(new OcclusionEntry(c));
        foreach (var t in _oldTvs)          _occluders.Add(new OcclusionEntry(t));
        foreach (var g in _gameConsoles)    _occluders.Add(new OcclusionEntry(g));
        foreach (var p in _pottedPlants1)   _occluders.Add(new OcclusionEntry(p));
        foreach (var e in _entertainmentShelves) _occluders.Add(new OcclusionEntry(e));
        foreach (var l in _sunkenLogs)      _occluders.Add(new OcclusionEntry(l));

        _crtTransitionRenderer = new CrtTransitionRenderer(_pixelTexture);
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime, IInputManager input)
    {
        if (input.IsPressed(InputAction.Exit))
        {
            _requestExit();
            return;
        }

        if (_fadeState != FadeState.None)
        {
            UpdateFade(gameTime);
            UpdateWorldPresentation(gameTime, EmptyInput, animateCharacters: false);
            return;
        }

        _playerHealth?.Update(gameTime);

        if (_playerDead)
        {
            _deathDelayTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_deathDelayTimer <= 0f)
            {
                _screenManager.Replace(new DeathScreen(
                    _graphicsDevice,
                    _content,
                    _virtualWidth,
                    _virtualHeight,
                    _screenManager,
                    _requestExit,
                    _musicManager,
                    _dayNightCycle?.CycleProgress ?? 0f));
                _playerDead = false;
            }

            UpdateWorldPresentation(gameTime, EmptyInput, animateCharacters: false);
            return;
        }

        if (input.IsPressed(InputAction.Pause))
        {
            _screenManager.Push(new PauseScreen(
                _screenManager,
                _musicManager,
                _graphicsDevice,
                _content,
                _virtualWidth,
                _virtualHeight));
            return;
        }

        if (input.IsPressed(InputAction.ToggleCollisionDebug))
        {
            _debugOverlayMode = (_debugOverlayMode + 1) % 3;
        }

        _player.Update(gameTime, input, _collisionMap);

        if (_combatStats != null)
            _player.SpeedMultiplier = _combatStats.SpeedMultiplier;

        // Check zone transition triggers after player movement.
        for (var i = 0; i < _worldRenderer.ZoneTriggers.Count; i++)
        {
            var trigger = _worldRenderer.ZoneTriggers[i];
            if (_player.FootBounds.Intersects(trigger.Bounds))
            {
                BeginZoneTransition(trigger);
                return;
            }
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            if (TryStartFishing())
            {
                return;
            }

            TryToggleNearbyFirepit();
        }

        _follower.Update(gameTime,
            _followerSystem.GetLeaderTargetPosition(_player, _follower, _gnomeSpawner),
            _player.Facing,
            _followerSystem.GetRestPosition(_player, _follower, _collisionMap,
                _worldRenderer.MapPixelWidth, _worldRenderer.MapPixelHeight));
        UpdateFrontDoors();

        _flowField?.Update(_player.Center);
        _waveManager?.Update(gameTime, _camera.WorldBounds);
        _gnomeSpawner?.Update(gameTime, _player.Center, _player.Bounds, _camera.WorldBounds, _flowField, _collisionMap);
        if (_projectileSystem != null && _gnomeSpawner != null)
        {
            if (_combatStats != null)
            {
                _projectileSystem.SpeedMultiplier = _combatStats.ProjectileSpeedMultiplier;
                _projectileSystem.CooldownMultiplier = _combatStats.CooldownMultiplier;
                _projectileSystem.RangeMultiplier = _combatStats.ProjectileRangeMultiplier;
            }
            _projectileSystem.Update(gameTime, _player.Center, _follower.Center, _gnomeSpawner, _collisionMap);
        }
        if (_slashSystem != null && _gnomeSpawner != null)
            _slashSystem.Update(gameTime, _player.Center, _follower.Center,
                _player.Facing, _follower.Facing, _gnomeSpawner);

        _healthPickupSystem?.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            _player.Center,
            _playerHealth,
            _collisionMap,
            _worldRenderer.MapPixelWidth,
            _worldRenderer.MapPixelHeight,
            _sfxRng);

        UpdateWorldPresentation(gameTime, input, animateCharacters: true);
    }

    private void UpdateWorldPresentation(GameTime gameTime, IInputManager input, bool animateCharacters)
    {
        _playerAnimator.Direction = _player.Facing;
        _playerAnimator.Update(gameTime, animateCharacters && _player.IsMoving);
        _followerAnimator.Direction = _follower.Facing;
        _followerAnimator.Update(gameTime, animateCharacters && _follower.IsMoving);
        _camera.LookAt(_player.Center);
        _camera.UpdateShake((float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_playerHitFlashTimer > 0f)
            _playerHitFlashTimer = Math.Max(0f, _playerHitFlashTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        if (_levelUpFlashTimer > 0f)
            _levelUpFlashTimer = Math.Max(0f, _levelUpFlashTimer - (float)gameTime.ElapsedGameTime.TotalSeconds);
        _isPlayerOccluded = OcclusionRevealRenderer.CheckOcclusion(
            _player.Bounds,
            SortDepth(_player.Bounds, _worldRenderer.MapPixelHeight, _worldRenderer.MapPixelWidth),
            _occluders,
            _worldRenderer.MapPixelHeight,
            _worldRenderer.MapPixelWidth);
        _isFollowerOccluded = OcclusionRevealRenderer.CheckOcclusion(
            _follower.Bounds,
            SortDepth(_follower.Bounds, _worldRenderer.MapPixelHeight, _worldRenderer.MapPixelWidth),
            _occluders,
            _worldRenderer.MapPixelHeight,
            _worldRenderer.MapPixelWidth);
        _worldRenderer.Update(gameTime);
        for (var i = 0; i < _gardenGnomes.Length; i++)
        {
            _gardenGnomes[i].Update(gameTime, _player.Position);
        }

        _playerInFishingZone = IsPlayerInFishingZone();

        if (_mapConfig.HasDayNightCycle)
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
        if (_mapConfig.HasAmbientFireflies)
        {
            _fireflyManager.Update(gameTime, _dayNightCycle.NightStrength, cameraWorldBounds);
        }

        // Combine fire + firefly lights into one array for the lighting renderer.
        Array.Copy(_fireLightData, 0, _combinedLightData, 0, activeFireLightCount);
        var fireflyLightCount = _mapConfig.HasAmbientFireflies
            ? _fireflyManager.WriteLightData(_combinedLightData, activeFireLightCount)
            : 0;
        _lightingRenderer.SetLights(_combinedLightData, activeFireLightCount + fireflyLightCount);
        _particleManager.Update(gameTime);
        _explosionSystem?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        _energyOrbSystem?.Update(
            (float)gameTime.ElapsedGameTime.TotalSeconds,
            _player.Center,
            _xpSystem,
            _orbCollectVariationSfx,
            _redOrbCollectVariationSfx,
            OrbCollectSfxVolume,
            RedOrbCollectSfxVolume,
            _sfxRng);
        if (_mapConfig.HasCloudShadows)
        {
            _cloudShadowRenderer.Update(gameTime);
        }
        _musicManager.Update(gameTime);
        _rippleSystem.Update(gameTime, input, _camera, _graphicsDevice, _virtualWidth, _virtualHeight);
    }

    private void UpdateFrontDoors()
    {
        var tileSizePixels = Math.Max(_worldRenderer.TileWidthPixels, _worldRenderer.TileHeightPixels);
        var invitationDistancePixels = (int)MathF.Round(tileSizePixels * FrontDoorInvitationTileScale);
        var anyOpened = false;
        var anyClosed = false;

        for (var i = 0; i < _frontDoors.Length; i++)
        {
            var wasOpen = _frontDoors[i].IsOpen;
            _frontDoors[i].UpdateInvitationState(_player.Bounds, invitationDistancePixels);
            if (!wasOpen && _frontDoors[i].IsOpen)
            {
                anyOpened = true;
            }
            else if (wasOpen && !_frontDoors[i].IsOpen)
            {
                anyClosed = true;
            }
        }

        if (anyOpened)
        {
            _doorOpenSfx.Play(DoorOpenSfxVolume, 0f, 0f);
        }

        if (anyClosed)
        {
            _doorCloseSfx.Play(DoorCloseSfxVolume, 0f, 0f);
        }
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
        var mapWidth = (float)_worldRenderer.MapPixelWidth;
        var playerDepth = SortDepth(_player.Bounds, mapHeight, mapWidth);
        var playerTint = GetPlayerHitTint();

        var followerDepth = SortDepth(_follower.Bounds, mapHeight, mapWidth);
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
            DrawWorldEntities(mapHeight, mapWidth, behindCutoff, EntityDepthFilter.BehindOrAtPlayer);
            _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet, followerDepth);
            _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet, playerDepth, playerTint);
            _worldSpriteBatch.End();

            // --- Pass 4b: Entities in front of shallowest occluded character → occluder render target ---
            _occlusionRevealRenderer.BeginCapture();
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.FrontToBack,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            DrawWorldEntities(mapHeight, mapWidth, behindCutoff, EntityDepthFilter.InFrontOfPlayer);
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
            DrawWorldEntities(mapHeight, mapWidth, playerDepth, EntityDepthFilter.All);
            _follower.Draw(_worldSpriteBatch, _followerAnimator, _followerSpriteSheet, followerDepth);
            _player.Draw(_worldSpriteBatch, _playerAnimator, _playerSpriteSheet, playerDepth, playerTint);
            _worldSpriteBatch.End();
        }

        // --- Debug overlay: collision bounds (Deferred so they render on top of all entities) ---
        // Mode 0 = off, Mode 1 = grid + bounds, Mode 2 = bounds only
        if (_debugOverlayMode > 0)
        {
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            DrawCollisionBounds();
            _worldSpriteBatch.End();
        }

        // --- Pass 4-fire: Fire flame overlay (deferred, always on top of Y-sorted entities) ---
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            transformMatrix: worldMatrix);
        for (var i = 0; i < _firepits.Length; i++)
        {
            _firepits[i].DrawFire(_worldSpriteBatch);
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

        // --- Pass 4b2: Hatchet sprites for slash system ---
        if (_slashSystem != null && _hatchetTexture != null)
        {
            _worldSpriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                transformMatrix: worldMatrix);
            var hatchetOrigin = new Vector2(_hatchetTexture.Width * 0.5f, _hatchetTexture.Height * 0.5f);
            if (_slashSystem.PlayerIsSweeping)
            {
                var pAngle = _slashSystem.PlayerAngle;
                var pPos = _player.Center + SlashSystem.Radius * new Vector2(MathF.Cos(pAngle), MathF.Sin(pAngle));
                _worldSpriteBatch.Draw(_hatchetTexture, pPos, null, Color.White, pAngle, hatchetOrigin, 1.5f, SpriteEffects.None, 0f);
            }
            if (_slashSystem.FollowerIsSweeping)
            {
                var fAngle = _slashSystem.FollowerAngle;
                var fPos = _follower.Center + SlashSystem.Radius * new Vector2(MathF.Cos(fAngle), MathF.Sin(fAngle));
                _worldSpriteBatch.Draw(_hatchetTexture, fPos, null, Color.White, fAngle, hatchetOrigin, 1.5f, SpriteEffects.None, 0f);
            }
            _worldSpriteBatch.End();
        }

        // --- Pass 4c: Fireflies (additive blend for a soft glow look) ---
        _worldSpriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.Additive,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: worldMatrix);
        if (_mapConfig.HasAmbientFireflies)
        {
            _fireflyManager.Draw(_worldSpriteBatch, _smokeTexture);
        }
        _worldSpriteBatch.End();

        // --- Pass 5: Cloud shadows (multiply blend, half-res for soft edges) ---
        if (_mapConfig.HasCloudShadows)
        {
            _cloudShadowRenderer.Draw(
                _worldSpriteBatch,
                _camera.Position,
                _dayNightCycle.NightStrength,
                _previousRenderTarget);
        }

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
        if (_playerInFishingZone)
        {
            var playerScreenTop = Vector2.Transform(_player.Position, _camera.GetViewMatrix());
            var gameViewport = _graphicsDevice.Viewport;
            var scaledSceneWidth = _virtualWidth * sceneScale;
            var scaledSceneHeight = _virtualHeight * sceneScale;
            var sceneOffsetX = (gameViewport.Width - scaledSceneWidth) / 2;
            var sceneOffsetY = (gameViewport.Height - scaledSceneHeight) / 2;
            var playerWindowTop = new Vector2(
                sceneOffsetX + playerScreenTop.X * sceneScale,
                sceneOffsetY + playerScreenTop.Y * sceneScale);
            var scaledIconWidth = _hookIconTexture.Width * FishingPromptUiScale;
            var scaledIconHeight = _hookIconTexture.Height * FishingPromptUiScale;
            var playerScreenWidth = PlayerFramePixels * sceneScale;
            var iconX = playerWindowTop.X + playerScreenWidth * 0.5f - scaledIconWidth * 0.5f;
            var iconY = playerWindowTop.Y - scaledIconHeight + FishingPromptHeadGapPixels;

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(
                _hookIconTexture,
                new Vector2(iconX, iconY),
                null,
                Color.White,
                0f,
                Vector2.Zero,
                FishingPromptUiScale,
                SpriteEffects.None,
                0f);
            spriteBatch.End();
        }

        // Level-up gold screen flash (drawn before HUD, after scene).
        if (_levelUpFlashTimer > 0f)
        {
            var flashAlpha = (_levelUpFlashTimer / LevelUpFlashDuration) * 0.35f;
            var flashColor = new Color(255, 215, 0) * flashAlpha;
            var vp = _graphicsDevice.Viewport;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, vp.Width, vp.Height), flashColor);
            spriteBatch.End();
        }

        // Get the font at a size scaled for the actual window resolution.
        // sceneScale converts virtual pixels to window pixels.
        var scaledFont = _fontSystem.GetFont(HudFontSize * sceneScale);

        if (_mapConfig.HasDayNightCycle)
        {
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp);
            _hudRenderer.Draw(spriteBatch, scaledFont, _pixelTexture, _dayNightCycle.GameHour, sceneScale);
            spriteBatch.End();
        }

        if (_forestHudRenderer != null)
        {
            var vp = _graphicsDevice.Viewport;
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp);
            _forestHudRenderer.Draw(spriteBatch, scaledFont, _pixelTexture, _playerHealth, _combatStats,
                _waveManager.CurrentWaveNumber, _waveManager.State, _waveManager.CountdownSeconds,
                _waveManager.WaveTimeRemaining, _waveManager.WaveDuration,
                sceneScale, vp.Width, vp.Height);
            spriteBatch.End();
        }

        if (_fadeAlpha <= 0f)
        {
            return;
        }

        var viewport = _graphicsDevice.Viewport;
        _crtTransitionRenderer.Draw(spriteBatch, viewport, _fadeAlpha);
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
        // Mode 1 = grid + bounds, Mode 2 = bounds only
        if (_debugOverlayMode == 1)
        {
            _debugRenderer.DrawTileGrid(
                _worldSpriteBatch,
                _worldRenderer.MapPixelWidth,
                _worldRenderer.MapPixelHeight,
                _worldRenderer.TileWidthPixels,
                _worldRenderer.TileHeightPixels);
        }

        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _player.FootBounds, Color.Yellow);
        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _player.Bounds, Color.OrangeRed);
        _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _follower.Bounds, Color.Cyan);

        for (var i = 0; i < _boulders.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _boulders[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _couches.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _couches[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _oldTvs.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _oldTvs[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _gameConsoles.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _gameConsoles[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _pottedPlants1.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _pottedPlants1[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _entertainmentShelves.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _entertainmentShelves[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _firepits[i].Bounds, Color.Red);
        }

        for (var i = 0; i < _pineTrees.Length; i++)
        {
            for (var j = 0; j < _pineTrees[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _pineTrees[i].GetCollisionBounds(j), Color.LimeGreen);
            }
        }

        for (var i = 0; i < _birchTrees.Length; i++)
        {
            for (var j = 0; j < _birchTrees[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _birchTrees[i].GetCollisionBounds(j), Color.LimeGreen);
            }
        }

        for (var i = 0; i < _deadTrees.Length; i++)
        {
            for (var j = 0; j < _deadTrees[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _deadTrees[i].GetCollisionBounds(j), Color.LimeGreen);
            }
        }

        for (var i = 0; i < _deciduousTrees.Length; i++)
        {
            for (var j = 0; j < _deciduousTrees[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _deciduousTrees[i].GetCollisionBounds(j), Color.LimeGreen);
            }
        }

        for (var i = 0; i < _bushes.Length; i++)
        {
            for (var j = 0; j < _bushes[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _bushes[i].GetCollisionBounds(j), Color.LimeGreen);
            }
        }

        for (var i = 0; i < _cozyLakeCabins.Length; i++)
        {
            for (var j = 0; j < _cozyLakeCabins[i].CollisionBoxCount; j++)
            {
                _debugRenderer.DrawRectangleOutline(_worldSpriteBatch, _cozyLakeCabins[i].GetCollisionBounds(j), Color.Orange);
            }
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

    private void OnPlayerDied()
    {
        _playerDead = true;
        _deathDelayTimer = 1.0f;
    }

    private void OnAllWavesComplete()
    {
        var victoryTrigger = new ZoneTriggerData(
            Rectangle.Empty,
            "Maps/StarterMap",
            "from-woods");
        BeginZoneTransition(victoryTrigger);
    }

    private void BeginZoneTransition(ZoneTriggerData trigger)
    {
        _pendingZoneTransition = new ZoneTransitionRequest(trigger.TargetMap, trigger.TargetSpawnId);
        _fadeState = FadeState.FadingOut;
        _fadeAlpha = 0f;
    }

    private void UpdateFade(GameTime gameTime)
    {
        var fadeStep = (float)(gameTime.ElapsedGameTime.TotalSeconds / ZoneTransitionFadeDurationSeconds);

        if (_fadeState == FadeState.FadingOut)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha + fadeStep, 0f, 1f);
            _musicManager.SetVolume(GameplayMusicVolume * (1f - _fadeAlpha));
            if (_fadeAlpha >= 1f)
            {
                _fadeState = FadeState.HoldingBlack;
                _fadeHoldTimer = ZoneTransitionBlackHoldSeconds;
            }

            return;
        }

        if (_fadeState == FadeState.HoldingBlack)
        {
            _fadeAlpha = 1f;
            _musicManager.SetVolume(0f);
            _fadeHoldTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_fadeHoldTimer <= 0f && _pendingFishingTransition)
            {
                _screenManager.Replace(new FishingScreen(
                    _graphicsDevice,
                    _content,
                    _virtualWidth,
                    _virtualHeight,
                    _screenManager,
                    _requestExit,
                    _mapAssetName,
                    _player.Position,
                    _dayNightCycle?.CycleProgress ?? 0f));
            }
            else if (_fadeHoldTimer <= 0f && _pendingZoneTransition is { } transition)
            {
                _screenManager.Replace(new GameplayScreen(
                    _graphicsDevice,
                    _content,
                    _virtualWidth,
                    _virtualHeight,
                    _screenManager,
                    _requestExit,
                    transition.TargetMap,
                    transition.TargetSpawnId,
                    fadeInFromBlack: true,
                    dayNightStartProgress: _dayNightCycle.CycleProgress));
            }

            return;
        }

        if (_fadeState == FadeState.FadingIn)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha - fadeStep, 0f, 1f);
            _musicManager.SetVolume(GameplayMusicVolume * (1f - _fadeAlpha));
            if (_fadeAlpha <= 0f)
            {
                _fadeState = FadeState.None;
                _musicManager.SetVolume(GameplayMusicVolume);
            }
        }
    }

    private static Vector2? FindSpawnPosition(IReadOnlyList<SpawnPointData> spawnPoints, string spawnPointId)
    {
        if (spawnPointId is null || spawnPoints.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < spawnPoints.Count; i++)
        {
            if (string.Equals(spawnPoints[i].Name, spawnPointId, StringComparison.Ordinal))
            {
                return spawnPoints[i].Position;
            }
        }

        return null;
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

    private bool TryStartFishing()
    {
        var fishingBounds = GetFishingInteractionBounds();
        var playerFacing = _player.Facing;

        for (var i = 0; i < _worldRenderer.FishingZones.Count; i++)
        {
            var zone = _worldRenderer.FishingZones[i];
            if (fishingBounds.Intersects(zone.Bounds) && playerFacing == zone.FacingDirection)
            {
                _screenManager.Push(new ConfirmationScreen(
                    _screenManager,
                    _graphicsDevice,
                    _content,
                    "Fish here?",
                    new[] { "Yes", "No" },
                    onSelect: selectedIndex =>
                    {
                        if (selectedIndex == 0)
                        {
                            BeginFishingTransition();
                        }
                    },
                    defaultSelection: 1));
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerInFishingZone()
    {
        var fishingBounds = GetFishingInteractionBounds();
        for (var i = 0; i < _worldRenderer.FishingZones.Count; i++)
        {
            if (fishingBounds.Intersects(_worldRenderer.FishingZones[i].Bounds))
            {
                return true;
            }
        }

        return false;
    }

    private Rectangle GetFishingInteractionBounds()
    {
        var footBounds = _player.FootBounds;

        return _player.Facing switch
        {
            FacingDirection.Down => new Rectangle(
                footBounds.X,
                footBounds.Y,
                footBounds.Width,
                footBounds.Height + FishingInteractionReachPixels),
            FacingDirection.Up => new Rectangle(
                footBounds.X,
                footBounds.Y - FishingInteractionReachPixels,
                footBounds.Width,
                footBounds.Height + FishingInteractionReachPixels),
            FacingDirection.Left => new Rectangle(
                footBounds.X - FishingInteractionReachPixels,
                footBounds.Y,
                footBounds.Width + FishingInteractionReachPixels,
                footBounds.Height),
            FacingDirection.Right => new Rectangle(
                footBounds.X,
                footBounds.Y,
                footBounds.Width + FishingInteractionReachPixels,
                footBounds.Height),
            _ => footBounds,
        };
    }

    private void BeginFishingTransition()
    {
        _pendingFishingTransition = true;
        _fadeState = FadeState.FadingOut;
        _fadeAlpha = 0f;
    }

    private enum FadeState
    {
        None,
        FadingOut,
        HoldingBlack,
        FadingIn,
    }

    private readonly record struct ZoneTransitionRequest(string TargetMap, string TargetSpawnId);

    /// <summary>
    /// Draws all world prop entities (not the player or follower) that pass
    /// the depth filter relative to the player. Each entity's sort depth is
    /// computed identically to the original single-pass drawing code.
    /// </summary>
    private void DrawWorldEntities(float mapHeight, float mapWidth, float playerDepth, EntityDepthFilter filter)
    {
        // Welcome mats and area rugs always draw at depth 0 — always on the ground, behind every sorted entity.
        if (filter != EntityDepthFilter.InFrontOfPlayer)
        {
            for (var i = 0; i < _welcomeMats.Length; i++)
                _welcomeMats[i].Draw(_worldSpriteBatch, 0f);
            for (var i = 0; i < _areaRugs.Length; i++)
                _areaRugs[i].Draw(_worldSpriteBatch, 0f);
        }

        for (var i = 0; i < _boulders.Length; i++)
        {
            var depth = SortDepth(_boulders[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _boulders[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _couches.Length; i++)
        {
            var depth = SortDepth(_couches[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _couches[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _oldTvs.Length; i++)
        {
            var depth = SortDepth(_oldTvs[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _oldTvs[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _gameConsoles.Length; i++)
        {
            var depth = SortDepth(_gameConsoles[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _gameConsoles[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _pottedPlants1.Length; i++)
        {
            var depth = SortDepth(_pottedPlants1[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _pottedPlants1[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _entertainmentShelves.Length; i++)
        {
            var depth = SortDepth(_entertainmentShelves[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _entertainmentShelves[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _gardenGnomes.Length; i++)
        {
            var depth = SortDepth(_gardenGnomes[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _gardenGnomes[i].Draw(_worldSpriteBatch, depth);
        }

        // Docks always draw at depth 0 — always behind every sorted entity.
        if (filter != EntityDepthFilter.InFrontOfPlayer)
        {
            for (var i = 0; i < _docks.Length; i++)
                _docks[i].Draw(_worldSpriteBatch);
        }

        for (var i = 0; i < _sunkenLogs.Length; i++)
        {
            var depth = SortDepth(_sunkenLogs[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _sunkenLogs[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _sunkenChests.Length; i++)
        {
            var depth = SortDepth(_sunkenChests[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _sunkenChests[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _firepits.Length; i++)
        {
            var depth = SortDepth(_firepits[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _firepits[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _cozyLakeCabins.Length; i++)
        {
            var depth = SortDepth(_cozyLakeCabins[i].Bounds, mapHeight, mapWidth, CabinSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _cozyLakeCabins[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _frontDoors.Length; i++)
        {
            var depth = SortDepth(_frontDoors[i].Bounds, mapHeight, mapWidth);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _frontDoors[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _pineTrees.Length; i++)
        {
            var depth = SortDepth(_pineTrees[i].Bounds, mapHeight, mapWidth, PineTreeSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _pineTrees[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _birchTrees.Length; i++)
        {
            var depth = SortDepth(_birchTrees[i].Bounds, mapHeight, mapWidth, BirchTreeSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _birchTrees[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _deadTrees.Length; i++)
        {
            var depth = SortDepth(_deadTrees[i].Bounds, mapHeight, mapWidth, DeadTreeSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _deadTrees[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _deciduousTrees.Length; i++)
        {
            var depth = SortDepth(_deciduousTrees[i].Bounds, mapHeight, mapWidth, DeciduousTreeSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _deciduousTrees[i].Draw(_worldSpriteBatch, depth);
        }

        for (var i = 0; i < _bushes.Length; i++)
        {
            var depth = SortDepth(_bushes[i].Bounds, mapHeight, mapWidth, BushSortAnchorOffsetPixels);
            if (PassesDepthFilter(depth, playerDepth, filter))
                _bushes[i].Draw(_worldSpriteBatch, depth);
        }

        if (_gnomeSpawner != null)
        {
            for (var i = 0; i < _gnomeSpawner.Gnomes.Count; i++)
            {
                var gnome = _gnomeSpawner.Gnomes[i];
                var depth = SortDepth(gnome.Bounds, mapHeight, mapWidth);
                if (PassesDepthFilter(depth, playerDepth, filter))
                    gnome.Draw(_worldSpriteBatch, _gnomeEnemyTexture, depth);
            }
        }

        if (_projectileSystem != null)
        {
            var projectiles = _projectileSystem.Projectiles;
            for (var i = 0; i < projectiles.Length; i++)
            {
                if (!projectiles[i].IsAlive) continue;
                var depth = SortDepth(projectiles[i].Bounds, mapHeight, mapWidth);
                if (PassesDepthFilter(depth, playerDepth, filter))
                    projectiles[i].Draw(_worldSpriteBatch, _projectileArrowTexture, depth);
            }
        }

                _energyOrbSystem?.Draw(_worldSpriteBatch, _energyOrbTexture, _energyOrbRedTexture, mapHeight, mapWidth, playerDepth, filter);

        _healthPickupSystem?.Draw(_worldSpriteBatch, _pixelTexture, mapHeight, mapWidth, playerDepth, filter);

        if (filter == EntityDepthFilter.All || filter == EntityDepthFilter.InFrontOfPlayer)
            _explosionSystem?.Draw(_worldSpriteBatch, _explosionTexture, mapHeight, mapWidth);
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
    /// Computes a stable sort depth for Y-sorting. Uses <c>Bounds.Bottom</c> as the
    /// primary key and <c>Bounds.Left</c> as a tiebreaker so that props at the same Y
    /// never flicker their draw order. The X contribution is always less than one pixel
    /// of Y depth, so it cannot override the vertical ordering.
    /// </summary>
    private static float SortDepth(Rectangle bounds, float mapHeight, float mapWidth, float anchorOffset = 0f)
    {
        var yDepth = (bounds.Bottom - anchorOffset) / mapHeight;

        // Keep deterministic X tie-breaking while guaranteeing the final layer depth
        // stays inside [0, 1). Without this headroom, entities at the map bottom can
        // exceed 1.0 and disappear in SpriteBatch front-to-back sorting.
        var tieBreakerRange = 1f / mapHeight;
        var yScaled = yDepth * (1f - tieBreakerRange);
        var xTie = bounds.Left / (mapWidth * mapHeight);

        return MathHelper.Clamp(yScaled + xTie, 0f, 0.9999f);
    }

    /// <summary>
    /// Computes the player sprite tint during a hit flash.
    /// Flashes red then fades back to white over <see cref="PlayerHitFlashDuration"/>.
    /// </summary>
    private Color? GetPlayerHitTint()
    {
        if (_playerHitFlashTimer <= 0f)
            return null;

        var t = _playerHitFlashTimer / PlayerHitFlashDuration;
        return Color.Lerp(Color.White, new Color(255, 60, 60), t);
    }
}
