using System;
using System.Collections.Generic;
using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Components;
using RiverRats.Game.Core;
using RiverRats.Game.Data;
using RiverRats.Game.Entities;
using RiverRats.Game.Graphics;
using RiverRats.Game.Input;
using RiverRats.Game.Systems;
using RiverRats.Game.Util;
using RiverRats.Game.World;

namespace RiverRats.Game.Screens;

/// <summary>
/// Side-view fishing mini-game screen. Renders the scene from a TMX map
/// designed in Tiled, with animated fish silhouettes swimming in the
/// underwater area. Cancel returns to the overworld.
/// <para>
/// All cast / fight state-machine logic lives in <see cref="FishingCastLogic"/>
/// (pure, GPU-free) and is reflected in <see cref="FishingCastState"/>.
/// The screen owns GPU resources and translates <see cref="FishingCastEvents"/>
/// into SFX calls, particle spawns, and ripple triggers each frame.
/// </para>
/// </summary>
public sealed class FishingScreen : IGameScreen
{
    private const float ZoneTransitionFadeDurationSeconds = 0.4f;
    private const float ZoneTransitionBlackHoldSeconds = 0.15f;
    private const float HintFontSize = 10f;
    private const string HintText = "Press Cancel to return";
    private const string DefaultFishingMapAsset = "Maps/FishingSpot";

    /// <summary>Height of the sky cloud region in tile rows.</summary>
    private const int SkyCloudTileRows = 2;

    /// <summary>Tile height in pixels (matches the TMX tileheight).</summary>
    private const int TileHeightPx = FishingCastLogic.TileHeightPx;

    /// <summary>Top grass shelf row in the fishing map.</summary>
    private const int GrassShelfRow = 6;

    /// <summary>Left margin for the fishing characters on the grass shelf.</summary>
    private const float GrassShelfLeftMarginPx = 8f;

    /// <summary>Horizontal spacing between the follower and player (can be 0 for side-by-side).</summary>
    private const float CharacterSpacingPx = 0.5f;

    /// <summary>Name of the TMX object layer that defines swim bounds for fish.</summary>
    private const string SwimBoundsLayerName = "SwimBounds";

    private static readonly Color HintColor = new(200, 200, 200, 180);

    // Set to true to enable fish attraction / strike / hooking behavior.
    private static readonly bool FishAttractionEnabled = true;

    /// <summary>Screen shake intensity in pixels.</summary>
    private const float ShakeIntensity = 2.5f;

    /// <summary>Duration (seconds) of screen shake.</summary>
    private const float ShakeDuration = 0.25f;

    /// <summary>How fast the fish wiggles while hooked (rad/s).</summary>
    private const float WiggleSpeed = FishingCastLogic.WiggleSpeed;

    /// <summary>Maximum wiggle rotation in radians.</summary>
    private const float WiggleAmplitude = FishingCastLogic.WiggleAmplitude;

    /// <summary>Number of splash particles emitted on breach.</summary>
    private const int SplashParticleCount = 12;

    // --- Water shader tuning ---
    private const float WaterAmplitude = 0.003f;
    private const float WaterFrequency = 20f;
    private const float WaterSpeed = 1.2f;
    private const float WaterRippleAmplitude = 0.012f;
    private const float WaterRippleFrequency = 35f;
    private const float WaterRippleSpeed = 20f;
    private const float WaterSplashBrightness = 0.45f;
    private const float WaterSplashRingSpeed = 25f;
    private const float WaterCausticIntensity = 0.08f;
    private const float WaterCausticScale = 8f;
    private const float WaterSpookBrightness = 0.55f;
    private const float WaterSpookRingSpeed = 12f;

    /// <summary>Duration (seconds) the catch toast is displayed.</summary>
    private const float ToastDurationSeconds = 2.5f;

    /// <summary>Font size for the catch toast text.</summary>
    private const float ToastFontSize = 10f;

    private static readonly Color ToastColor = new(255, 255, 200, 230);
    private static readonly Color ToastShadowColor = new(0, 0, 0, 160);

    // SFX variant counts.
    private const int CastSfxCount = 4;
    private const int PlopSfxCount = 4;
    private const int TwitchSfxCount = 3;
    private const int ReelSfxCount = 3;
    private const int StrikeSfxCount = 4;
    private const int CatchSfxCount = 3;

    // SFX playback volumes.
    private const float CastSfxVolume = 0.7f;
    private const float PlopSfxVolume = 0.65f;
    private const float TwitchSfxVolume = 0.5f;
    private const float ReelSfxVolume = 0.3f;
    private const float StrikeSfxVolume = 0.85f;
    private const float CatchSfxVolume = 0.8f;

    /// <summary>Minimum interval (seconds) between reel tick SFX to avoid overlapping clicks.</summary>
    private const float ReelTickIntervalSeconds = 0.08f;

    // Fish population counts per species.
    private const int MinnowCount = 4;
    private const int BassCount = 3;
    private const int CatfishCount = 2;

    /// <summary>Character sprite frame size in pixels.</summary>
    private const int CharacterFramePixels = 32;

    /// <summary>Walk animation columns per direction row on the sprite sheet.</summary>
    private const int WalkFramesPerDirection = 4;

    /// <summary>Seconds each walk animation frame is displayed.</summary>
    private const float WalkFrameDuration = 0.15f;

    /// <summary>
    /// Offset from the player position to place the idle fishing rod overlay.
    /// Aligns the rod handle with the character's right hand (facing right).
    /// </summary>
    private static readonly Vector2 FishingRodOffset = new(17f, -8f);

    /// <summary>
    /// Offset from the player position to place the wind-up fishing rod overlay.
    /// Rod is swept behind/above the character's head.
    /// </summary>
    private static readonly Vector2 FishingRodWindupOffset = new(-8f, -12f);

    /// <summary>
    /// Offset from the player position to place the cast-complete rod overlay.
    /// Same handle position as idle, rod has a slight droop.
    /// </summary>
    private static readonly Vector2 FishingRodCastOffset = FishingCastLogic.FishingRodCastOffset;

    /// <summary>Delay before the power gauge appears after holding Confirm.</summary>
    private const float WindupDelaySeconds = FishingCastLogic.WindupDelaySeconds;

    /// <summary>Speed at which the power gauge needle oscillates (cycles per second).</summary>
    private const float GaugeSpeedCyclesPerSecond = FishingCastLogic.GaugeSpeedCyclesPerSecond;

    /// <summary>Green zone half-width at minimum cast distance (fraction of gauge, 0–1).</summary>
    private const float GaugeGreenHalfClose = FishingCastLogic.GaugeGreenHalfClose;

    /// <summary>Green zone half-width at maximum cast distance (fraction of gauge, 0–1).</summary>
    private const float GaugeGreenHalfFar = FishingCastLogic.GaugeGreenHalfFar;

    /// <summary>Center of the green zone on the gauge (fraction, 0–1).</summary>
    private const float GaugeGreenCenter = FishingCastLogic.GaugeGreenCenter;

    // Power gauge visual dimensions (in virtual-resolution pixels).
    private const int GaugeWidth = 4;
    private const int GaugeHeight = 40;
    private const int GaugeMarginRight = 10;

    private static readonly Color GaugeBackColor = new(30, 30, 30, 200);
    private static readonly Color GaugeRedColor = new(200, 50, 40, 220);
    private static readonly Color GaugeGreenColor = new(50, 180, 60, 220);
    private static readonly Color GaugeNeedleColor = new(255, 255, 255, 255);

    /// <summary>Speed of the aim cursor in pixels per second.</summary>
    private const float AimSpeedPxPerSecond = FishingCastLogic.AimSpeedPxPerSecond;

    /// <summary>Row where the water surface sits (matches the TMX map).</summary>
    private const int WaterSurfaceRow = FishingCastLogic.WaterSurfaceRow;

    /// <summary>Left boundary (px) of the aimable water surface. Past the shore slope.</summary>
    private const float AimMinX = FishingCastLogic.AimMinX;

    /// <summary>Y position of the aim arrow on the water surface.</summary>
    private static readonly float AimY = FishingCastLogic.AimY;

    /// <summary>Size of the aim arrow indicator in pixels.</summary>
    private const int AimArrowSize = 5;

    private static readonly Color AimArrowColor = new(255, 255, 80, 220);

    /// <summary>Duration of the lure flight arc in seconds.</summary>
    private const float LureFlightDurationSeconds = FishingCastLogic.LureFlightDurationSeconds;

    /// <summary>Peak height of the lure arc above the straight-line path (pixels).</summary>
    private const float LureArcHeight = FishingCastLogic.LureArcHeight;

    /// <summary>How far off-target a bad (red-zone) cast lands (pixels).</summary>
    private const float BadCastMinOffset = FishingCastLogic.BadCastMinOffset;
    private const float BadCastMaxOffset = FishingCastLogic.BadCastMaxOffset;

    /// <summary>Color of the fishing line.</summary>
    private static readonly Color LineColor = new(180, 180, 180, 160);

    /// <summary>Number of segments used to approximate the line curve.</summary>
    private const int LineSegments = 24;

    /// <summary>Duration (seconds) for the line to settle from taut to fully slack after landing.</summary>
    private const float LineSettleDurationSeconds = FishingCastLogic.LineSettleDurationSeconds;

    /// <summary>Maximum sag (pixels) of the slack catenary at full settle.</summary>
    private const float LineMaxSag = FishingCastLogic.LineMaxSag;

    /// <summary>Small sag (pixels) that appears when the player stops reeling, so the line doesn't look perfectly taut.</summary>
    private const float LineRelaxSag = FishingCastLogic.LineRelaxSag;

    /// <summary>Pixels the fish breaches above the water surface during a strike.</summary>
    private const float StrikeBreachHeight = FishingCastLogic.StrikeBreachHeight;

    /// <summary>Total duration of the strike animation (lunge + breach + dive) in seconds.</summary>
    private const float StrikeDuration = FishingCastLogic.StrikeDuration;

    /// <summary>Time within the strike when the breach reaches its peak.</summary>
    private const float StrikeBreachPeakTime = FishingCastLogic.StrikeBreachPeakTime;

    /// <summary>Time within the strike when the dive back down begins.</summary>
    private const float StrikeDiveStartTime = FishingCastLogic.StrikeDiveStartTime;

    /// <summary>Speed (px/sec) at which a hooked fish is reeled toward the rod tip.</summary>
    private const float HookedReelSpeed = FishingCastLogic.HookedReelSpeed;

    /// <summary>Speed (px/sec) at which a hooked fish drifts back when the player is not reeling.</summary>
    private const float HookedDriftBackSpeed = FishingCastLogic.HookedDriftBackSpeed;

    // --- Fish fight & line tension ---

    /// <summary>Minimum seconds between fight bursts.</summary>
    private const float FightCooldownMin = FishingCastLogic.FightCooldownMin;

    /// <summary>Maximum seconds between fight bursts.</summary>
    private const float FightCooldownMax = FishingCastLogic.FightCooldownMax;

    /// <summary>Duration (seconds) of a single fight burst.</summary>
    private const float FightBurstDuration = FishingCastLogic.FightBurstDuration;

    /// <summary>Speed (px/sec) the fish pulls away during a fight burst.</summary>
    private const float FightPullSpeed = FishingCastLogic.FightPullSpeed;

    /// <summary>Tension gained per second when reeling during a fight burst.</summary>
    private const float TensionReelDuringFight = FishingCastLogic.TensionReelDuringFight;

    /// <summary>Tension gained per second when reeling normally (no fight).</summary>
    private const float TensionReelNormal = FishingCastLogic.TensionReelNormal;

    /// <summary>Tension lost per second when NOT reeling.</summary>
    private const float TensionDecay = FishingCastLogic.TensionDecay;

    /// <summary>Tension value at which the line snaps (0–1 scale).</summary>
    private const float TensionSnapThreshold = FishingCastLogic.TensionSnapThreshold;

    /// <summary>Line color when tension is at maximum (danger).</summary>
    private static readonly Color LineDangerColor = new(255, 60, 40, 220);

    /// <summary>Stamina drained from the fish per fight burst (0–1 scale).</summary>
    private const float StaminaDrainPerBurst = FishingCastLogic.StaminaDrainPerBurst;

    /// <summary>Minimum stamina multiplier — even an exhausted fish puts up a feeble fight.</summary>
    private const float StaminaFloor = FishingCastLogic.StaminaFloor;

    /// <summary>Rod offset for the hooked-rod sprite (same handle position as the cast rod).</summary>
    private static readonly Vector2 FishingRodHookedOffset = FishingCastLogic.FishingRodHookedOffset;

    /// <summary>Offset from the hooked-rod sprite origin to the bent rod tip.</summary>
    private static readonly Vector2 HookedRodTipLocalOffset = FishingCastLogic.HookedRodTipLocalOffset;

    /// <summary>Offset from the cast-rod sprite origin to the rod tip.</summary>
    private static readonly Vector2 RodTipLocalOffset = FishingCastLogic.RodTipLocalOffset;

    /// <summary>Speed at which line slack is reeled in (sag pixels per second).</summary>
    private const float ReelSlackSpeed = FishingCastLogic.ReelSlackSpeed;

    /// <summary>Speed at which the lure is retrieved toward the rod tip (pixels per second).</summary>
    private const float ReelLureSpeed = FishingCastLogic.ReelLureSpeed;

    /// <summary>Distance (px) the lure moves toward the player per twitch/pop.</summary>
    private const float TwitchDistancePx = FishingCastLogic.TwitchDistancePx;

    /// <summary>Duration (seconds) of the rod flip-up animation on a twitch.</summary>
    private const float TwitchDurationSeconds = FishingCastLogic.TwitchDurationSeconds;

    /// <summary>Time window (seconds) in which rapid twitches are counted.</summary>
    private const float RapidTwitchWindowSeconds = FishingCastLogic.RapidTwitchWindowSeconds;

    /// <summary>Max twitches within the window before they start spooking fish.</summary>
    private const int RapidTwitchSafeCount = FishingCastLogic.RapidTwitchSafeCount;

    /// <summary>Rotation (radians) the rod tilts upward during a twitch. Negative = counter-clockwise.</summary>
    private const float TwitchRotation = FishingCastLogic.TwitchRotation;

    /// <summary>Offset from the idle-rod sprite origin to the tip (where line attaches).</summary>
    private static readonly Vector2 IdleRodTipLocalOffset = new(40f, 6f);

    /// <summary>Length (pixels) of the hanging line in the idle pose.</summary>
    private const float IdleLineLengthPx = 13f;

    /// <summary>Horizontal amplitude (pixels) of the idle lure sway.</summary>
    private const float IdleSwayAmplitudePx = FishingCastLogic.IdleSwayAmplitudePx;

    /// <summary>Speed of the idle lure sway (cycles per second).</summary>
    private const float IdleSwayCyclesPerSecond = FishingCastLogic.IdleSwayCyclesPerSecond;

    /// <summary>Player top-left position on the shore (world pixels).</summary>
    private Vector2 _playerPosition;

    /// <summary>Follower top-left position on the shore (world pixels).</summary>
    private Vector2 _followerPosition;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly GameSessionServices _gameSessionServices;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly ScreenManager _screenManager;
    private readonly Action _requestExit;
    private readonly string _returnMapName;
    private readonly Vector2 _returnPosition;
    private readonly float _dayNightProgress;
    private readonly string _fishingMapAsset;

    private Texture2D _pixelTexture;
    private Texture2D _fishTexture;
    private SimpleTiledRenderer _mapRenderer;
    private SkyCloudRenderer _skyCloudRenderer;
    private SpriteAnimator _playerAnimator;
    private SpriteAnimator _followerAnimator;
    private Texture2D _playerSpriteSheet;
    private Texture2D _followerSpriteSheet;
    private Texture2D _fishingRodTexture;
    private Texture2D _fishingRodWindupTexture;
    private Texture2D _fishingRodCastTexture;
    private Texture2D _fishingRodHookedTexture;
    private Texture2D _frogLureDangle;
    private Texture2D _frogLureRest;
    private Texture2D _frogLureActive;
    private FontSystem _fontSystem;

    // ── Pure-logic cast state machine ─────────────────────────────────────────

    /// <summary>
    /// Pure-data state for the cast / fight state machine.
    /// All logic lives in <see cref="FishingCastLogic"/>.
    /// </summary>
    private readonly FishingCastState _castState = new();

    // ── Convenience accessors delegating to _castState ────────────────────────

    private CastState CastPhase => _castState.State;

    /// <summary>Line color computed each frame from tension.</summary>
    private Color _currentLineColor = LineColor;

    private readonly List<FishSilhouette> _fish = new();

    /// <summary>The fish that bit the lure (null when no fish is hooked).</summary>
    private FishSilhouette _hookedFish;

    private FadeState _fadeState;
    private float _fadeAlpha;
    private float _fadeHoldTimer;
    private string _toastText;
    private float _toastTimer;
    private static readonly Random _catchRng = new();

    // SFX arrays.
    private readonly SoundEffect[] _castSfx = new SoundEffect[CastSfxCount];
    private readonly SoundEffect[] _plopSfx = new SoundEffect[PlopSfxCount];
    private readonly SoundEffect[] _twitchSfx = new SoundEffect[TwitchSfxCount];
    private readonly SoundEffect[] _reelSfx = new SoundEffect[ReelSfxCount];
    private readonly SoundEffect[] _strikeSfx = new SoundEffect[StrikeSfxCount];
    private readonly SoundEffect[] _catchSfx = new SoundEffect[CatchSfxCount];
    private static readonly Random _sfxRng = new();
    private float _reelTickCooldown;

    // Screen shake state.
    private float _shakeTimer;

    // Splash particles.
    private readonly List<SplashParticle> _splashParticles = new();

    // Water shader state.
    private Effect _fishingWaterEffect;
    private RenderTarget2D _waterRenderTarget;
    private FishingRippleManager _rippleManager;
    private float _waterElapsedSeconds;

    /// <summary>
    /// Cached render target that was active when <see cref="Draw"/> was called (set by Game1).
    /// <c>GetRenderTargets()</c> allocates a new array every call; caching it here means
    /// we call the GPU API exactly once per frame instead of twice.
    /// </summary>
    private RenderTarget2D _previousRenderTarget;

    /// <inheritdoc />
    public bool IsTransparent => false;

    /// <summary>
    /// Creates a fishing mini-game screen.
    /// </summary>
    internal FishingScreen(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        int virtualWidth,
        int virtualHeight,
        ScreenManager screenManager,
        GameSessionServices gameSessionServices,
        Action requestExit,
        string returnMapName,
        Vector2 returnPosition,
        float dayNightProgress,
        string fishingMapAsset = DefaultFishingMapAsset)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _screenManager = screenManager ?? throw new ArgumentNullException(nameof(screenManager));
        _gameSessionServices = gameSessionServices ?? throw new ArgumentNullException(nameof(gameSessionServices));
        _requestExit = requestExit ?? throw new ArgumentNullException(nameof(requestExit));
        _returnMapName = returnMapName ?? throw new ArgumentNullException(nameof(returnMapName));
        _returnPosition = returnPosition;
        _dayNightProgress = dayNightProgress;
        _fishingMapAsset = fishingMapAsset ?? DefaultFishingMapAsset;
    }

    /// <inheritdoc />
    public void LoadContent()
    {
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

        _mapRenderer = new SimpleTiledRenderer(_graphicsDevice, _content, _fishingMapAsset);
        _gameSessionServices.EventBus.Publish(GameEventType.ZoneEntered, _fishingMapAsset, 1);

        // Initialise aim bounds from the loaded map.
        _castState.AimMaxX = _mapRenderer.MapPixelWidth - AimArrowSize;
        _castState.AimX    = MathHelper.Lerp(AimMinX, _castState.AimMaxX, 0.5f);

        FishingCastLogic.CalculateCharacterPositions(
            GrassShelfRow, CharacterFramePixels,
            GrassShelfLeftMarginPx, CharacterSpacingPx,
            out _followerPosition, out _playerPosition);

        var skyWidth  = _mapRenderer.MapPixelWidth;
        var skyHeight = SkyCloudTileRows * TileHeightPx;
        _skyCloudRenderer = new SkyCloudRenderer(_graphicsDevice, skyWidth, skyHeight);
        _skyCloudRenderer.LoadContent(_content);

        _fishTexture          = _content.Load<Texture2D>("Sprites/fish-silhouettes");
        _playerSpriteSheet    = _content.Load<Texture2D>("Sprites/generic_character_sheet");
        _followerSpriteSheet  = _content.Load<Texture2D>("Sprites/companion_character_sheet");
        _fishingRodTexture    = _content.Load<Texture2D>("Sprites/fishing_rod");
        _fishingRodWindupTexture = _content.Load<Texture2D>("Sprites/fishing_rod_windup");
        _fishingRodCastTexture   = _content.Load<Texture2D>("Sprites/fishing_rod_cast");
        _fishingRodHookedTexture = _content.Load<Texture2D>("Sprites/fishing_rod_hooked");
        _frogLureDangle = _content.Load<Texture2D>("Sprites/frog_lure1");
        _frogLureRest   = _content.Load<Texture2D>("Sprites/frog_lure2");
        _frogLureActive = _content.Load<Texture2D>("Sprites/frog_lure3");

        // Load fishing SFX.
        for (var i = 0; i < CastSfxCount; i++)
            _castSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_cast_{i:D2}");
        for (var i = 0; i < PlopSfxCount; i++)
            _plopSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_plop_{i:D2}");
        for (var i = 0; i < TwitchSfxCount; i++)
            _twitchSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_twitch_{i:D2}");
        for (var i = 0; i < ReelSfxCount; i++)
            _reelSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_reel_{i:D2}");
        for (var i = 0; i < StrikeSfxCount; i++)
            _strikeSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_strike_{i:D2}");
        for (var i = 0; i < CatchSfxCount; i++)
            _catchSfx[i] = _content.Load<SoundEffect>($"Audio/SFX/fishing_catch_{i:D2}");

        _playerAnimator = new SpriteAnimator(
            CharacterFramePixels, CharacterFramePixels,
            WalkFramesPerDirection, WalkFrameDuration)
        { Direction = FacingDirection.Right };
        _followerAnimator = new SpriteAnimator(
            CharacterFramePixels, CharacterFramePixels,
            WalkFramesPerDirection, WalkFrameDuration)
        { Direction = FacingDirection.Right };

        SpawnFish();

        // Water distortion / ripple / caustic shader.
        _fishingWaterEffect = _content.Load<Effect>("Effects/FishingWater");
        _fishingWaterEffect.Parameters["Amplitude"].SetValue(WaterAmplitude);
        _fishingWaterEffect.Parameters["Frequency"].SetValue(WaterFrequency);
        _fishingWaterEffect.Parameters["Speed"].SetValue(WaterSpeed);
        _fishingWaterEffect.Parameters["RippleAmplitude"].SetValue(WaterRippleAmplitude);
        _fishingWaterEffect.Parameters["RippleFrequency"].SetValue(WaterRippleFrequency);
        _fishingWaterEffect.Parameters["RippleSpeed"].SetValue(WaterRippleSpeed);
        _fishingWaterEffect.Parameters["SplashBrightness"].SetValue(WaterSplashBrightness);
        _fishingWaterEffect.Parameters["SplashRingSpeed"].SetValue(WaterSplashRingSpeed);
        _fishingWaterEffect.Parameters["CausticIntensity"].SetValue(WaterCausticIntensity);
        _fishingWaterEffect.Parameters["CausticScale"].SetValue(WaterCausticScale);
        _fishingWaterEffect.Parameters["SpookBrightness"].SetValue(WaterSpookBrightness);
        _fishingWaterEffect.Parameters["SpookRingSpeed"].SetValue(WaterSpookRingSpeed);
        _fishingWaterEffect.Parameters["AspectRatio"].SetValue((float)_virtualWidth / _virtualHeight);
        _fishingWaterEffect.Parameters["WaterSurfaceV"].SetValue((float)(WaterSurfaceRow * TileHeightPx) / _virtualHeight);
        _waterRenderTarget = new RenderTarget2D(
            _graphicsDevice, _virtualWidth, _virtualHeight,
            false, SurfaceFormat.Color, DepthFormat.None,
            0, RenderTargetUsage.DiscardContents);
        _rippleManager = new FishingRippleManager();

        // Start with a fade-in from black.
        _fadeState = FadeState.FadingIn;
        _fadeAlpha = 1f;
        _fadeHoldTimer = 0f;
    }

    /// <inheritdoc />
    public void Update(GameTime gameTime, IInputManager input)
    {
        if (_fadeState != FadeState.None)
        {
            UpdateFade(gameTime);
            return;
        }

        if (input.IsPressed(InputAction.Exit))
        {
            _requestExit();
            return;
        }

        if (input.IsPressed(InputAction.Cancel))
        {
            // If a fish is hooked or striking, Cancel releases it — fish flees.
            if (CastPhase is CastState.FishStrike or CastState.FishHooked)
            {
                _hookedFish?.SetRotation(0f);
                _hookedFish?.Flee();
                _hookedFish = null;
                _castState.State       = CastState.Idle;
                _castState.TwitchTimer = 0f;
                _castState.CurrentSag  = 0f;
                _castState.LineTension = 0f;
                _currentLineColor = LineColor;
            }
            // If the lure is out, Cancel reels it in instantly.
            else if (CastPhase is CastState.LureFlying or CastState.CastComplete
                or CastState.ReelingSlack or CastState.ReelingLure)
            {
                _castState.State       = CastState.Idle;
                _castState.TwitchTimer = 0f;
                _castState.CurrentSag  = 0f;
            }
            else
            {
                BeginReturnTransition();
            }
            return;
        }

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Tick the pure-logic cast state machine.
        FishingCastLogic.Tick(
            _castState,
            dt,
            confirmHeld:          input.IsHeld(InputAction.Confirm),
            confirmPressed:       input.IsPressed(InputAction.Confirm),
            moveLeftPressed:      input.IsPressed(InputAction.MoveLeft),
            moveRightHeld:        input.IsHeld(InputAction.MoveRight),
            moveLeftHeld:         input.IsHeld(InputAction.MoveLeft),
            playerPosition:       _playerPosition,
            virtualWidth:         _virtualWidth,
            virtualHeight:        _virtualHeight,
            rng:                  _catchRng,
            hookedFishCenter:     _hookedFish?.Center ?? Vector2.Zero,
            hookedFishReelSpeedMultiplier: _hookedFish?.ReelSpeedMultiplier ?? 1f,
            fishCount:            _fish.Count,
            getFishAttractionState: i => (int)_fish[i].Attraction,
            getFishCenter:        i => _fish[i].Center,
            out var events);

        // Translate events → side effects.
        ApplyCastEvents(in events, dt);

        _skyCloudRenderer.Update(gameTime);
        _rippleManager.Update(gameTime);
        _waterElapsedSeconds += dt;

        // Catch toast timer.
        if (_toastTimer > 0f)
            _toastTimer -= dt;

        // Screen shake.
        if (_shakeTimer > 0f)
            _shakeTimer -= dt;

        // Reel click cooldown.
        if (_reelTickCooldown > 0f)
            _reelTickCooldown -= dt;

        // Update splash particles.
        for (var i = _splashParticles.Count - 1; i >= 0; i--)
        {
            _splashParticles[i].Life -= dt;
            if (_splashParticles[i].Life <= 0f)
            {
                _splashParticles.RemoveAt(i);
                continue;
            }
            _splashParticles[i].Velocity.Y += 120f * dt; // gravity
            _splashParticles[i].Position   += _splashParticles[i].Velocity * dt;
        }

        // Update fish.
        for (var i = _fish.Count - 1; i >= 0; i--)
        {
            _fish[i].Update(gameTime);

            // Remove fleeing fish once they leave the screen.
            if (_fish[i].IsFleeing && _fish[i].Center.X > _virtualWidth + 32)
                _fish.RemoveAt(i);
        }

        // Feed lure events to fish attraction while lure is in water.
        if (FishAttractionEnabled &&
            CastPhase is CastState.CastComplete or CastState.ReelingSlack or CastState.ReelingLure
            && _castState.LurePosition.Y >= AimY)
        {
            var lureEvt = ToEntityLureEvent(events.LureEvent);
            for (var i = 0; i < _fish.Count; i++)
                _fish[i].UpdateAttraction(_castState.LurePosition, dt, lureEvt);
        }

        // Update line color from tension.
        _currentLineColor = Color.Lerp(LineColor, LineDangerColor, _castState.LineTension);
    }

    // ── Event translation ─────────────────────────────────────────────────────

    private void ApplyCastEvents(in FishingCastEvents events, float dt)
    {
        if (events.PlayCastSfx)
            _castSfx[_sfxRng.Next(CastSfxCount)].Play(CastSfxVolume, 0f, 0f);

        if (events.PlayPlopSfx)
            _plopSfx[_sfxRng.Next(PlopSfxCount)].Play(PlopSfxVolume, 0f, 0f);

        if (events.PlayTwitchSfx)
            _twitchSfx[_sfxRng.Next(TwitchSfxCount)].Play(TwitchSfxVolume, 0f, 0f);

        if (events.PlayReelTick)
            TryPlayReelTick();

        if (events.PlayStrikeSfx)
            _strikeSfx[_sfxRng.Next(StrikeSfxCount)].Play(StrikeSfxVolume, 0f, 0f);

        if (events.PlayCatchSfx)
            _catchSfx[_sfxRng.Next(CatchSfxCount)].Play(CatchSfxVolume, 0f, 0f);

        if (events.TriggerShake)
            _shakeTimer = ShakeDuration;

        // Splash / ripple events.
        if (events.SpawnLureLandingSplash)
            SpawnLureLandingSplash(events.SplashPosition.X, events.SplashPosition.Y);
        else if (events.SpawnBadCastSplash)
            SpawnBadCastSplash(events.SplashPosition.X, events.SplashPosition.Y);
        else if (events.SpawnSplash)
            SpawnSplash(events.SplashPosition.X, events.SplashPosition.Y);

        if (events.SpawnTwitchRipple)
        {
            _rippleManager.SpawnRipple(events.SplashPosition);
            _rippleManager.SpawnSplash(events.SplashPosition);
        }

        if (events.SpawnSpookRings)
        {
            _rippleManager.SpawnRipple(events.SplashPosition);
            _rippleManager.SpawnSpookRing(events.SplashPosition);
        }

        if (events.SpawnStrikeSpookRings)
        {
            _rippleManager.SpawnSpookRing(events.StrikeRipplePosition);
            _rippleManager.SpawnSpookRing(new Vector2(events.StrikeRipplePosition.X - 5f, events.StrikeRipplePosition.Y));
        }

        if (events.SpawnFightSpookRing)
            _rippleManager.SpawnSpookRing(events.FightRipplePosition);

        // Fish hook.
        if (events.FishHooked && events.HookedFishIndex < _fish.Count)
        {
            _hookedFish = _fish[events.HookedFishIndex];
            _hookedFish.SetHooked();
        }

        // Fish position update (fight / reel).
        if (events.UpdateHookedFishPosition && _hookedFish != null)
        {
            _hookedFish.SetPosition(events.HookedFishNewPosition - FishSilhouette.SpriteHalfSize);
            _hookedFish.SetRotation(events.HookedFishRotation);
            if (!events.HookedFishFaceLeft)
                _hookedFish.SetFacingLeft(false);
            else
                _hookedFish.SetFacingLeft(true);
        }

        // Spook fish scattered by a strike.
        if (events.StrikeSpooked != null)
        {
            foreach (var idx in events.StrikeSpooked)
                if (idx < _fish.Count) _fish[idx].Spook();
        }
        if (events.StrikeNearbySpook != null)
        {
            foreach (var idx in events.StrikeNearbySpook)
                if (idx < _fish.Count) _fish[idx].Spook();
        }

        // Fish fled (line snap or Cancel).
        if (events.FishFled && _hookedFish != null)
        {
            _hookedFish.SetRotation(0f);
            _hookedFish.Flee();
            _hookedFish = null;
            _currentLineColor = LineColor;
        }

        // Catch!
        if (events.FishReachedRod && _hookedFish != null)
        {
            ShowCatchToast(_hookedFish);
            SpawnSplash(events.CatchRodTipPosition.X, AimY);
            _fish.Remove(_hookedFish);
            _hookedFish = null;
            _currentLineColor = LineColor;
        }
    }

    private void TryPlayReelTick()
    {
        if (_reelTickCooldown <= 0f)
        {
            _reelSfx[_sfxRng.Next(ReelSfxCount)].Play(ReelSfxVolume, 0f, 0f);
            _reelTickCooldown = ReelTickIntervalSeconds;
        }
    }

    private static FishSilhouette.LureEvent ToEntityLureEvent(FishLureEvent e) => e switch
    {
        FishLureEvent.Splash    => FishSilhouette.LureEvent.Splash,
        FishLureEvent.BadSplash => FishSilhouette.LureEvent.BadSplash,
        FishLureEvent.Twitch    => FishSilhouette.LureEvent.Twitch,
        FishLureEvent.ReelTick  => FishSilhouette.LureEvent.ReelTick,
        _                       => FishSilhouette.LureEvent.None,
    };

    /// <inheritdoc />
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Compute shake offset.
        var shakeOffset = Vector2.Zero;
        if (_shakeTimer > 0f)
        {
            var magnitude = ShakeIntensity * (_shakeTimer / ShakeDuration);
            shakeOffset = new Vector2(
                (float)(_catchRng.NextDouble() * 2 - 1) * magnitude,
                (float)(_catchRng.NextDouble() * 2 - 1) * magnitude);
        }
        var shakeMatrix = Matrix.CreateTranslation(shakeOffset.X, shakeOffset.Y, 0f);

        // Cache the active render target once per frame (GetRenderTargets() allocates).
        var bindings = _graphicsDevice.GetRenderTargets();
        _previousRenderTarget = bindings.Length > 0
            ? bindings[0].RenderTarget as RenderTarget2D
            : null;

        // --- Pass 1: Render water layer to render target ---
        _graphicsDevice.SetRenderTarget(_waterRenderTarget);
        _graphicsDevice.Clear(Color.Transparent);
        _mapRenderer.DrawLayer("water", Matrix.Identity);
        _graphicsDevice.SetRenderTarget(_previousRenderTarget);

        // --- Pass 2: Composite water with distortion shader ---
        _fishingWaterEffect.Parameters["Time"].SetValue(_waterElapsedSeconds);
        _rippleManager.SetShaderParameters(_fishingWaterEffect, _virtualWidth, _virtualHeight);
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            effect: _fishingWaterEffect,
            transformMatrix: shakeMatrix);
        spriteBatch.Draw(
            _waterRenderTarget,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);
        spriteBatch.End();

        // --- Pass 3: Non-water tile layers (sky, shore, details) ---
        _mapRenderer.DrawLayer("sky", shakeMatrix);
        _mapRenderer.DrawLayer("Shore", shakeMatrix);
        _mapRenderer.DrawLayer("Details", shakeMatrix);

        // Draw procedural clouds over the sky region (top rows).
        _skyCloudRenderer.Draw(spriteBatch);

        // Draw player and follower standing on the shore.
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp, transformMatrix: shakeMatrix);
        _followerAnimator.Draw(spriteBatch, _followerSpriteSheet, _followerPosition);
        _playerAnimator.Draw(spriteBatch, _playerSpriteSheet, _playerPosition);

        var rodTexture = CastPhase switch
        {
            CastState.WindingUp  => _fishingRodWindupTexture,
            CastState.Charging   => _fishingRodWindupTexture,
            CastState.LureFlying => _fishingRodCastTexture,
            CastState.CastComplete  => _fishingRodCastTexture,
            CastState.ReelingSlack  => _fishingRodCastTexture,
            CastState.ReelingLure   => _fishingRodCastTexture,
            CastState.FishStrike    => _fishingRodHookedTexture,
            CastState.FishHooked    => _fishingRodHookedTexture,
            _ => _fishingRodTexture,
        };
        var rodOffset = CastPhase switch
        {
            CastState.WindingUp  => FishingRodWindupOffset,
            CastState.Charging   => FishingRodWindupOffset,
            CastState.LureFlying => FishingRodCastOffset,
            CastState.CastComplete  => FishingRodCastOffset,
            CastState.ReelingSlack  => FishingRodCastOffset,
            CastState.ReelingLure   => FishingRodCastOffset,
            CastState.FishStrike    => FishingRodHookedOffset,
            CastState.FishHooked    => FishingRodHookedOffset,
            _ => FishingRodOffset,
        };
        if (_castState.TwitchTimer <= 0f)
        {
            spriteBatch.Draw(rodTexture, _playerPosition + rodOffset, Color.White);
        }
        else
        {
            // During a twitch, rotate the rod slightly upward around the handle.
            var twitchT  = _castState.TwitchTimer / TwitchDurationSeconds;
            var rotation = TwitchRotation * twitchT;
            var origin   = new Vector2(5f, 30f); // Handle position in the rod sprite.
            spriteBatch.Draw(
                rodTexture,
                _playerPosition + rodOffset + origin,
                null,
                Color.White,
                rotation,
                origin,
                1f,
                SpriteEffects.None,
                0f);
        }

        // Draw the power gauge when charging.
        if (CastPhase == CastState.Charging)
        {
            DrawPowerGauge(spriteBatch);
        }

        // Draw the fishing line and lure.
        if (CastPhase == CastState.Idle)
        {
            DrawIdleLineAndLure(spriteBatch);
        }
        else if (CastPhase is CastState.FishStrike or CastState.FishHooked)
        {
            DrawFishingLine(spriteBatch);
        }
        else if (CastPhase is CastState.LureFlying or CastState.CastComplete
            or CastState.ReelingSlack or CastState.ReelingLure)
        {
            DrawFishingLine(spriteBatch);
            DrawLure(spriteBatch);
        }

        // Draw the aim arrow when the player can still adjust aim.
        if (CastPhase is CastState.Idle or CastState.WindingUp)
        {
            DrawAimArrow(spriteBatch);
        }

        spriteBatch.End();

        // Draw fish, splash particles, and fade overlay on top.
        spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp, transformMatrix: shakeMatrix);

        for (var i = 0; i < _fish.Count; i++)
            _fish[i].Draw(spriteBatch, _fishTexture, Color.White);

        // Draw splash particles with varied sizes.
        for (var i = 0; i < _splashParticles.Count; i++)
        {
            var p     = _splashParticles[i];
            var alpha = MathHelper.Clamp(p.Life / 0.3f, 0f, 1f);
            var tint  = Color.Lerp(p.Tint, Color.Transparent, 1f - alpha);
            spriteBatch.Draw(_pixelTexture,
                new Rectangle((int)p.Position.X, (int)p.Position.Y, p.Size, p.Size),
                tint);
        }

        // Fade overlay.
        if (_fadeAlpha > 0f)
        {
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(0, 0, _virtualWidth, _virtualHeight),
                new Color(0, 0, 0, _fadeAlpha));
        }

        spriteBatch.End();
    }

    /// <inheritdoc />
    public void DrawOverlay(GameTime gameTime, SpriteBatch spriteBatch, int sceneScale)
    {
        if (_fadeState != FadeState.None)
            return;

        var font     = _fontSystem.GetFont(HintFontSize * sceneScale);
        var textSize = font.MeasureString(HintText);

        var viewport = _graphicsDevice.Viewport;
        var position = new Vector2(
            (viewport.Width - textSize.X) / 2f,
            viewport.Height - textSize.Y - (8f * sceneScale));

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);
        spriteBatch.DrawString(font, HintText, position, HintColor);
        spriteBatch.End();

        // Draw catch toast if active.
        if (_toastTimer > 0f && _toastText != null)
        {
            var toastFont = _fontSystem.GetFont(ToastFontSize * sceneScale);
            var toastSize = toastFont.MeasureString(_toastText);
            var toastPos  = new Vector2(
                (viewport.Width - toastSize.X) / 2f,
                (_playerPosition.Y - 12f) * sceneScale);

            float alpha = _toastTimer < 0.5f ? _toastTimer / 0.5f : 1f;
            var color   = ToastColor * alpha;
            var shadow  = ToastShadowColor * alpha;

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp);
            spriteBatch.DrawString(toastFont, _toastText, toastPos + new Vector2(sceneScale, sceneScale), shadow);
            spriteBatch.DrawString(toastFont, _toastText, toastPos, color);
            spriteBatch.End();
        }
    }

    /// <inheritdoc />
    public void UnloadContent()
    {
        _pixelTexture?.Dispose();
        _fontSystem?.Dispose();
        _skyCloudRenderer?.UnloadContent();
        _waterRenderTarget?.Dispose();
        _mapRenderer?.Dispose();
    }

    // ── Fade / screen transition ──────────────────────────────────────────────

    private void BeginReturnTransition()
    {
        _fadeState = FadeState.FadingOut;
        _fadeAlpha = 0f;
    }

    private void UpdateFade(GameTime gameTime)
    {
        var fadeStep = (float)(gameTime.ElapsedGameTime.TotalSeconds / ZoneTransitionFadeDurationSeconds);

        if (_fadeState == FadeState.FadingOut)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha + fadeStep, 0f, 1f);
            if (_fadeAlpha >= 1f)
            {
                _fadeState     = FadeState.HoldingBlack;
                _fadeHoldTimer = ZoneTransitionBlackHoldSeconds;
            }
            return;
        }

        if (_fadeState == FadeState.HoldingBlack)
        {
            _fadeAlpha      = 1f;
            _fadeHoldTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_fadeHoldTimer <= 0f)
            {
                _screenManager.Replace(new GameplayScreen(
                    _graphicsDevice,
                    _content,
                    _virtualWidth,
                    _virtualHeight,
                    _screenManager,
                    _gameSessionServices,
                    _requestExit,
                    _returnMapName,
                    fadeInFromBlack: true,
                    dayNightStartProgress: _dayNightProgress,
                    spawnPosition: _returnPosition));
            }
            return;
        }

        if (_fadeState == FadeState.FadingIn)
        {
            _fadeAlpha = MathHelper.Clamp(_fadeAlpha - fadeStep, 0f, 1f);
            if (_fadeAlpha <= 0f)
                _fadeState = FadeState.None;
        }
    }

    private enum FadeState
    {
        None,
        FadingOut,
        HoldingBlack,
        FadingIn,
    }

    // ── Fish spawn / population ───────────────────────────────────────────────

    private void SpawnFish()
    {
        var rng = new Random();

        PolygonBounds swimBounds;
        var polyBounds = _mapRenderer.GetObjectPolygons(SwimBoundsLayerName);
        if (polyBounds.Count > 0)
        {
            swimBounds = polyBounds[0];
        }
        else
        {
            var rectBounds = _mapRenderer.GetObjectRectangles(SwimBoundsLayerName);
            swimBounds = rectBounds.Count > 0
                ? PolygonBounds.FromRectangle(rectBounds[0])
                : PolygonBounds.FromRectangle(new Rectangle(0, 0, _mapRenderer.MapPixelWidth, _mapRenderer.MapPixelHeight));
        }

        SpawnSpecies(FishSilhouette.FishType.Minnow, MinnowCount, swimBounds, rng);

        var bassBounds    = swimBounds.SliceHorizontal(0.35f);
        SpawnSpecies(FishSilhouette.FishType.Bass, BassCount, bassBounds, rng);

        var catfishBounds = swimBounds.SliceHorizontal(0.55f);
        SpawnSpecies(FishSilhouette.FishType.Catfish, CatfishCount, catfishBounds, rng);
    }

    private void SpawnSpecies(FishSilhouette.FishType type, int count, PolygonBounds swimBounds, Random rng)
    {
        for (var i = 0; i < count; i++)
        {
            var pos  = swimBounds.RandomPointInside(rng) - FishSilhouette.SpriteHalfSize;
            var fish = new FishSilhouette(type, pos, swimBounds, rng);
            _fish.Add(fish);
        }
    }

    // ── Toast ─────────────────────────────────────────────────────────────────

    private void ShowCatchToast(FishSilhouette fish)
    {
        var name = fish.Species switch
        {
            FishSilhouette.FishType.Minnow  => "Minnow",
            FishSilhouette.FishType.Bass    => "Bass",
            FishSilhouette.FishType.Catfish => "Catfish",
            _                               => "Fish",
        };

        var (minW, maxW) = fish.Species switch
        {
            FishSilhouette.FishType.Minnow  => (0.1f, 0.5f),
            FishSilhouette.FishType.Bass    => (1.0f, 6.0f),
            FishSilhouette.FishType.Catfish => (3.0f, 15.0f),
            _                               => (0.5f, 3.0f),
        };

        var weight = minW + (float)_catchRng.NextDouble() * (maxW - minW);
        _toastText  = $"{name} — {weight:F1} lbs";
        _toastTimer = ToastDurationSeconds;
    }

    // ── Splash particles ──────────────────────────────────────────────────────

    private static readonly Color[] SplashTints =
    {
        new(200, 230, 255, 220),
        new(180, 220, 255, 200),
        new(255, 255, 255, 200),
        new(160, 210, 240, 180),
    };

    private void SpawnSplash(float x, float y)
    {
        for (var i = 0; i < SplashParticleCount; i++)
        {
            var angle = -MathHelper.PiOver2 + ((float)_catchRng.NextDouble() - 0.5f) * MathHelper.Pi * 0.8f;
            var speed = 25f + (float)_catchRng.NextDouble() * 60f;
            var size  = _catchRng.Next(1, 4);
            var tint  = SplashTints[_catchRng.Next(SplashTints.Length)];
            _splashParticles.Add(new SplashParticle
            {
                Position = new Vector2(x + (float)(_catchRng.NextDouble() * 8 - 4), y),
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Life     = 0.35f + (float)_catchRng.NextDouble() * 0.25f,
                Size     = size,
                Tint     = tint,
            });
        }

        var splashPos = new Vector2(x, y);
        _rippleManager.SpawnRipple(splashPos);
        _rippleManager.SpawnSplash(splashPos);
    }

    private void SpawnLureLandingSplash(float x, float y)
    {
        SpawnSplash(x, y);

        for (var i = 0; i < 6; i++)
        {
            var angle = -MathHelper.PiOver2 + ((float)_catchRng.NextDouble() - 0.5f) * MathHelper.Pi * 1.0f;
            var speed = 40f + (float)_catchRng.NextDouble() * 45f;
            var size  = _catchRng.Next(2, 5);
            var tint  = SplashTints[_catchRng.Next(SplashTints.Length)];
            _splashParticles.Add(new SplashParticle
            {
                Position = new Vector2(x + (float)(_catchRng.NextDouble() * 12 - 6), y),
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Life     = 0.4f + (float)_catchRng.NextDouble() * 0.3f,
                Size     = size,
                Tint     = tint,
            });
        }

        _rippleManager.SpawnRipple(new Vector2(x - 6f, y));
        _rippleManager.SpawnRipple(new Vector2(x + 6f, y));
        _rippleManager.SpawnSplash(new Vector2(x, y));
    }

    private static readonly Color[] BadSplashTints =
    {
        new(255, 80, 60, 220),
        new(255, 120, 80, 200),
        new(255, 60, 40, 200),
        new(220, 90, 70, 180),
    };

    private void SpawnBadCastSplash(float x, float y)
    {
        for (var i = 0; i < SplashParticleCount; i++)
        {
            var angle = -MathHelper.PiOver2 + ((float)_catchRng.NextDouble() - 0.5f) * MathHelper.Pi * 0.9f;
            var speed = 30f + (float)_catchRng.NextDouble() * 55f;
            var size  = _catchRng.Next(1, 4);
            var tint  = BadSplashTints[_catchRng.Next(BadSplashTints.Length)];
            _splashParticles.Add(new SplashParticle
            {
                Position = new Vector2(x + (float)(_catchRng.NextDouble() * 8 - 4), y),
                Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                Life     = 0.35f + (float)_catchRng.NextDouble() * 0.25f,
                Size     = size,
                Tint     = tint,
            });
        }

        var pos = new Vector2(x, y);
        _rippleManager.SpawnRipple(pos);
        _rippleManager.SpawnSpookRing(pos);
        _rippleManager.SpawnSpookRing(new Vector2(x - 5f, y));
        _rippleManager.SpawnSpookRing(new Vector2(x + 5f, y));
    }

    // ── Drawing helpers ───────────────────────────────────────────────────────

    private void DrawPowerGauge(SpriteBatch spriteBatch)
    {
        var gaugeX = (int)(_playerPosition.X + CharacterFramePixels + GaugeMarginRight);
        var gaugeY = (int)(_playerPosition.Y + (CharacterFramePixels - GaugeHeight) / 2f);

        spriteBatch.Draw(_pixelTexture,
            new Rectangle(gaugeX - 1, gaugeY - 1, GaugeWidth + 2, GaugeHeight + 2),
            GaugeBackColor);

        spriteBatch.Draw(_pixelTexture,
            new Rectangle(gaugeX, gaugeY, GaugeWidth, GaugeHeight),
            GaugeRedColor);

        var (greenStart, greenEnd) = FishingCastLogic.GaugeGreenZone(_castState);
        var greenY = gaugeY + (int)(GaugeHeight * (1f - greenEnd));
        var greenH = (int)(GaugeHeight * (greenEnd - greenStart));
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(gaugeX, greenY, GaugeWidth, greenH),
            GaugeGreenColor);

        var needleValue = FishingCastLogic.GaugeValue(_castState);
        var needleY     = gaugeY + (int)(GaugeHeight * (1f - needleValue));
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(gaugeX - 1, needleY, GaugeWidth + 2, 2),
            GaugeNeedleColor);
    }

    private void DrawFishingLine(SpriteBatch spriteBatch)
    {
        Vector2 rodTip;
        Vector2 end;
        float   sag;

        if (CastPhase is CastState.FishStrike or CastState.FishHooked)
        {
            rodTip = FishingCastLogic.GetHookedRodTipPosition(_playerPosition);
            end    = _hookedFish.MouthPosition;
            sag    = 0f;
        }
        else
        {
            rodTip = FishingCastLogic.GetCastRodTipPosition(_castState, _playerPosition);
            end    = _castState.LurePosition + new Vector2(_castState.LureSwayOffset, 0f);

            sag = 0f;
            if (CastPhase is CastState.CastComplete or CastState.ReelingSlack)
                sag = _castState.CurrentSag;
        }

        var mid = (rodTip + end) * 0.5f;
        mid.Y += sag;

        var prev = rodTip;
        for (var i = 1; i <= LineSegments; i++)
        {
            var st  = i / (float)LineSegments;
            var inv = 1f - st;
            var point = inv * inv * rodTip + 2f * inv * st * mid + st * st * end;
            DrawLinePixels(spriteBatch, prev, point);
            prev = point;
        }
    }

    private void DrawLinePixels(SpriteBatch spriteBatch, Vector2 a, Vector2 b)
    {
        var x0 = (int)a.X; var y0 = (int)a.Y;
        var x1 = (int)b.X; var y1 = (int)b.Y;
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            spriteBatch.Draw(_pixelTexture, new Rectangle(x0, y0, 1, 1), _currentLineColor);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx)  { err += dx; y0 += sy; }
        }
    }

    private void DrawLure(SpriteBatch spriteBatch)
    {
        var pos = new Vector2(
            _castState.LurePosition.X + _castState.LureSwayOffset,
            _castState.LurePosition.Y);

        if (CastPhase == CastState.LureFlying)
        {
            var origin = new Vector2(_frogLureActive.Width, 0f);
            spriteBatch.Draw(_frogLureActive, pos, null, Color.White,
                0f, origin, 1f, SpriteEffects.FlipHorizontally, 0f);
        }
        else if (CastPhase is CastState.ReelingSlack or CastState.ReelingLure || _castState.TwitchTimer > 0f)
        {
            spriteBatch.Draw(_frogLureActive, pos, null, Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
        else
        {
            spriteBatch.Draw(_frogLureRest, pos, null, Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
    }

    private void DrawIdleLineAndLure(SpriteBatch spriteBatch)
    {
        var tip        = _playerPosition + FishingRodOffset + IdleRodTipLocalOffset;
        var swayOffset = (float)Math.Sin(_castState.SwayTimer * IdleSwayCyclesPerSecond * MathHelper.TwoPi) * IdleSwayAmplitudePx;
        var bottom     = tip + new Vector2(swayOffset, IdleLineLengthPx);

        DrawLinePixels(spriteBatch, tip, bottom);

        var origin = new Vector2(_frogLureDangle.Width / 2f, 0f);
        spriteBatch.Draw(_frogLureDangle, bottom, null, Color.White,
            0f, origin, 1f, SpriteEffects.None, 0f);
    }

    private void DrawAimArrow(SpriteBatch spriteBatch)
    {
        var arrowX = (int)_castState.AimX;
        var arrowY = (int)AimY;

        for (var row = 0; row < AimArrowSize; row++)
        {
            var halfWidth = AimArrowSize - 1 - row;
            var cx = arrowX + AimArrowSize / 2;
            for (var dx = -halfWidth; dx <= halfWidth; dx++)
            {
                spriteBatch.Draw(_pixelTexture,
                    new Rectangle(cx + dx, arrowY + row, 1, 1),
                    AimArrowColor);
            }
        }
    }

    // ── Nested types ──────────────────────────────────────────────────────────

    /// <summary>Simple particle for water splash effects.</summary>
    private sealed class SplashParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public int Size;
        public Color Tint;
    }
}
