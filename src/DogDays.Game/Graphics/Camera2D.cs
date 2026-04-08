using System;
using Microsoft.Xna.Framework;

namespace DogDays.Game.Graphics;

/// <summary>
/// 2D orthographic camera that produces a view matrix for world-space SpriteBatch drawing.
/// The camera position is the world-space centre of the virtual viewport and is clamped
/// to the map pixel bounds so the viewport never shows outside the map edges.
/// </summary>
public sealed class Camera2D
{
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;
    private readonly float _minX;
    private readonly float _maxX;
    private readonly float _minY;
    private readonly float _maxY;
    private readonly float _halfWidth;
    private readonly float _halfHeight;

    private const float MaxShakeOffset = 4f;
    private const float TraumaDecayRate = 4f;

    private Vector2 _position;
    private Matrix _viewMatrix;
    private bool _viewMatrixDirty = true;
    private float _trauma;
    private Vector2 _shakeOffset;
    private readonly Random _shakeRng = new();

    /// <summary>
    /// Initializes a camera with fixed virtual viewport dimensions and map pixel bounds for clamping.
    /// When the map is smaller than the viewport on an axis, the camera is locked to the map centre
    /// on that axis so the map stays centred rather than showing out-of-bounds space.
    /// </summary>
    /// <param name="viewportWidth">Virtual viewport width in pixels (e.g. 960).</param>
    /// <param name="viewportHeight">Virtual viewport height in pixels (e.g. 540).</param>
    /// <param name="mapPixelWidth">Total map width in pixels.</param>
    /// <param name="mapPixelHeight">Total map height in pixels.</param>
    public Camera2D(int viewportWidth, int viewportHeight, int mapPixelWidth, int mapPixelHeight)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        var halfViewW = viewportWidth / 2f;
        var halfViewH = viewportHeight / 2f;

        // When the map is narrower than the viewport, lock horizontally to the map centre.
        if (mapPixelWidth <= viewportWidth)
        {
            _minX = _maxX = mapPixelWidth / 2f;
        }
        else
        {
            _minX = halfViewW;
            _maxX = mapPixelWidth - halfViewW;
        }

        // When the map is shorter than the viewport, lock vertically to the map centre.
        if (mapPixelHeight <= viewportHeight)
        {
            _minY = _maxY = mapPixelHeight / 2f;
        }
        else
        {
            _minY = halfViewH;
            _maxY = mapPixelHeight - halfViewH;
        }

        _halfWidth = halfViewW;
        _halfHeight = halfViewH;

        // Start centred on the top-left of the clamped range (first visible tile area).
        _position = new Vector2(_minX, _minY);
    }

    /// <summary>World-space position the camera is currently centred on.</summary>
    public Vector2 Position => _position;

    /// <summary>Current trauma level (0–1). Squared to produce shake intensity.</summary>
    public float Trauma => _trauma;

    /// <summary>
    /// Adds trauma to the camera. Values are clamped to [0, 1].
    /// Trauma is squared to produce shake intensity, so small hits feel subtle
    /// and multi-kills stack into a satisfying rumble.
    /// </summary>
    /// <param name="amount">Trauma to add (typically 0.02–0.15 per event).</param>
    public void AddTrauma(float amount)
    {
        _trauma = MathHelper.Clamp(_trauma + amount, 0f, 1f);
    }

    /// <summary>
    /// Decays trauma and computes the shake offset for this frame.
    /// Call once per frame before <see cref="GetViewMatrix"/>.
    /// </summary>
    /// <param name="dt">Delta time in seconds.</param>
    public void UpdateShake(float dt)
    {
        if (_trauma <= 0f)
        {
            if (_shakeOffset != Vector2.Zero)
            {
                _shakeOffset = Vector2.Zero;
                _viewMatrixDirty = true;
            }
            return;
        }

        _trauma = MathF.Max(_trauma - TraumaDecayRate * dt, 0f);

        // Shake intensity = trauma² for a nice ease-out curve.
        var intensity = _trauma * _trauma;
        _shakeOffset = new Vector2(
            ((float)_shakeRng.NextDouble() * 2f - 1f) * MaxShakeOffset * intensity,
            ((float)_shakeRng.NextDouble() * 2f - 1f) * MaxShakeOffset * intensity);
        _viewMatrixDirty = true;
    }

    /// <summary>
    /// Returns the world-space rectangle currently visible through the viewport.
    /// </summary>
    public Rectangle WorldBounds => new(
        (int)(_position.X - _halfWidth),
        (int)(_position.Y - _halfHeight),
        _viewportWidth,
        _viewportHeight);

    /// <summary>
    /// Moves the camera to look at <paramref name="target"/>, clamped to map bounds.
    /// </summary>
    /// <param name="target">World-space position to centre the viewport on.</param>
    public void LookAt(Vector2 target)
    {
        var clamped = new Vector2(
            MathHelper.Clamp(target.X, _minX, _maxX),
            MathHelper.Clamp(target.Y, _minY, _maxY));

        if (_position != clamped)
        {
            _position = clamped;
            _viewMatrixDirty = true;
        }
    }

    /// <summary>
    /// Returns the view matrix to pass to <c>SpriteBatch.Begin(transformMatrix:)</c>
    /// for all world-space drawing passes.
    /// </summary>
    public Matrix GetViewMatrix()
    {
        if (_viewMatrixDirty)
        {
            // Translate so the camera position maps to the centre of the virtual viewport.
            // Shake offset is applied on top of the base position.
            _viewMatrix = Matrix.CreateTranslation(
                _halfWidth - _position.X + _shakeOffset.X,
                _halfHeight - _position.Y + _shakeOffset.Y,
                0f);
            _viewMatrixDirty = false;
        }

        return _viewMatrix;
    }

    /// <summary>
    /// Converts a position in virtual screen coordinates to world coordinates.
    /// </summary>
    /// <param name="screenPosition">Position in virtual-resolution screen space (0,0 = top-left).</param>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return new Vector2(
            screenPosition.X + _position.X - _halfWidth,
            screenPosition.Y + _position.Y - _halfHeight);
    }
}
