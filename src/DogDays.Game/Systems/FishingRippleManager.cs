using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Systems;

/// <summary>
/// Manages event-driven water ripples and splash highlights for the fishing scene.
/// Ripples are spawned by gameplay events (lure splash, fish strike, catch) rather
/// than mouse clicks. Also tracks expanding splash highlight rings and red spook rings.
/// </summary>
internal sealed class FishingRippleManager
{
    private const int MaxRipples = 8;
    private const float RippleMaxAge = 2.5f;

    private const int MaxSplashes = 4;
    private const float SplashMaxAge = 0.8f;

    private const int MaxSpookRings = 4;
    private const float SpookRingMaxAge = 1.6f;

    private readonly Vector2[] _ripplePositions = new Vector2[MaxRipples];
    private readonly float[] _rippleAges = new float[MaxRipples];
    private int _rippleCount;

    private readonly Vector2[] _splashPositions = new Vector2[MaxSplashes];
    private readonly float[] _splashAges = new float[MaxSplashes];
    private int _splashCount;

    private readonly Vector2[] _spookPositions = new Vector2[MaxSpookRings];
    private readonly float[] _spookAges = new float[MaxSpookRings];
    private int _spookCount;

    // Reusable arrays to avoid per-frame allocation.
    private readonly Vector3[] _rippleData = new Vector3[MaxRipples];
    private readonly Vector3[] _splashData = new Vector3[MaxSplashes];
    private readonly Vector3[] _spookData = new Vector3[MaxSpookRings];

    /// <summary>
    /// Spawns a distortion ripple at the given world position.
    /// </summary>
    /// <param name="worldPosition">Position in world (map) pixel coordinates.</param>
    public void SpawnRipple(Vector2 worldPosition)
    {
        if (_rippleCount < MaxRipples)
        {
            _ripplePositions[_rippleCount] = worldPosition;
            _rippleAges[_rippleCount] = 0f;
            _rippleCount++;
        }
    }

    /// <summary>
    /// Spawns a bright splash highlight ring at the given world position.
    /// </summary>
    /// <param name="worldPosition">Position in world (map) pixel coordinates.</param>
    public void SpawnSplash(Vector2 worldPosition)
    {
        if (_splashCount < MaxSplashes)
        {
            _splashPositions[_splashCount] = worldPosition;
            _splashAges[_splashCount] = 0f;
            _splashCount++;
        }
    }

    /// <summary>
    /// Spawns a red spook ring at the given world position (bad cast warning wave).
    /// </summary>
    /// <param name="worldPosition">Position in world (map) pixel coordinates.</param>
    public void SpawnSpookRing(Vector2 worldPosition)
    {
        if (_spookCount < MaxSpookRings)
        {
            _spookPositions[_spookCount] = worldPosition;
            _spookAges[_spookCount] = 0f;
            _spookCount++;
        }
    }

    /// <summary>
    /// Ages all active ripples and splashes, removing expired ones.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Age ripples; swap-remove expired.
        for (var i = _rippleCount - 1; i >= 0; i--)
        {
            _rippleAges[i] += dt;
            if (_rippleAges[i] >= RippleMaxAge)
            {
                _rippleCount--;
                _ripplePositions[i] = _ripplePositions[_rippleCount];
                _rippleAges[i] = _rippleAges[_rippleCount];
            }
        }

        // Age splashes; swap-remove expired.
        for (var i = _splashCount - 1; i >= 0; i--)
        {
            _splashAges[i] += dt;
            if (_splashAges[i] >= SplashMaxAge)
            {
                _splashCount--;
                _splashPositions[i] = _splashPositions[_splashCount];
                _splashAges[i] = _splashAges[_splashCount];
            }
        }

        // Age spook rings; swap-remove expired.
        for (var i = _spookCount - 1; i >= 0; i--)
        {
            _spookAges[i] += dt;
            if (_spookAges[i] >= SpookRingMaxAge)
            {
                _spookCount--;
                _spookPositions[i] = _spookPositions[_spookCount];
                _spookAges[i] = _spookAges[_spookCount];
            }
        }
    }

    /// <summary>
    /// Writes ripple and splash data to the fishing water shader effect.
    /// </summary>
    /// <param name="effect">The <c>FishingWater</c> effect to configure.</param>
    /// <param name="virtualWidth">Virtual resolution width in pixels.</param>
    /// <param name="virtualHeight">Virtual resolution height in pixels.</param>
    public void SetShaderParameters(Effect effect, int virtualWidth, int virtualHeight)
    {
        var invW = 1f / virtualWidth;
        var invH = 1f / virtualHeight;

        // Pack ripple world positions into UV space (the RT covers the full virtual res).
        for (var i = 0; i < MaxRipples; i++)
        {
            if (i < _rippleCount)
            {
                _rippleData[i] = new Vector3(
                    _ripplePositions[i].X * invW,
                    _ripplePositions[i].Y * invH,
                    _rippleAges[i]);
            }
            else
            {
                _rippleData[i] = new Vector3(0f, 0f, -1f);
            }
        }

        effect.Parameters["Ripple0"].SetValue(_rippleData[0]);
        effect.Parameters["Ripple1"].SetValue(_rippleData[1]);
        effect.Parameters["Ripple2"].SetValue(_rippleData[2]);
        effect.Parameters["Ripple3"].SetValue(_rippleData[3]);
        effect.Parameters["Ripple4"].SetValue(_rippleData[4]);
        effect.Parameters["Ripple5"].SetValue(_rippleData[5]);
        effect.Parameters["Ripple6"].SetValue(_rippleData[6]);
        effect.Parameters["Ripple7"].SetValue(_rippleData[7]);

        // Pack splash positions into UV space.
        for (var i = 0; i < MaxSplashes; i++)
        {
            if (i < _splashCount)
            {
                _splashData[i] = new Vector3(
                    _splashPositions[i].X * invW,
                    _splashPositions[i].Y * invH,
                    _splashAges[i]);
            }
            else
            {
                _splashData[i] = new Vector3(0f, 0f, -1f);
            }
        }

        effect.Parameters["Splash0"].SetValue(_splashData[0]);
        effect.Parameters["Splash1"].SetValue(_splashData[1]);
        effect.Parameters["Splash2"].SetValue(_splashData[2]);
        effect.Parameters["Splash3"].SetValue(_splashData[3]);

        // Pack spook ring positions into UV space.
        for (var i = 0; i < MaxSpookRings; i++)
        {
            if (i < _spookCount)
            {
                _spookData[i] = new Vector3(
                    _spookPositions[i].X * invW,
                    _spookPositions[i].Y * invH,
                    _spookAges[i]);
            }
            else
            {
                _spookData[i] = new Vector3(0f, 0f, -1f);
            }
        }

        effect.Parameters["Spook0"].SetValue(_spookData[0]);
        effect.Parameters["Spook1"].SetValue(_spookData[1]);
        effect.Parameters["Spook2"].SetValue(_spookData[2]);
        effect.Parameters["Spook3"].SetValue(_spookData[3]);
    }
}
