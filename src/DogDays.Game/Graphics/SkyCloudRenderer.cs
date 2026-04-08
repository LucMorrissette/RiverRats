using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Graphics;

/// <summary>
/// Renders procedural drifting clouds over a sky region using an HLSL shader.
/// Designed for the fishing mini-game's side-view scene where the top portion
/// of the map is open sky. Clouds drift smoothly from left to right.
/// </summary>
public sealed class SkyCloudRenderer : IDisposable
{
    /// <summary>Default horizontal drift speed in UV units per second.</summary>
    private const float DefaultWindSpeed = 0.012f;

    /// <summary>Default base noise frequency multiplier.</summary>
    private const float DefaultCloudScale = 4.0f;

    /// <summary>Default cloud coverage threshold (higher = fewer clouds).</summary>
    private const float DefaultCoverage = 0.42f;

    /// <summary>Default edge softness band width.</summary>
    private const float DefaultSoftness = 0.25f;

    /// <summary>Default overall cloud opacity.</summary>
    private const float DefaultOpacity = 0.65f;

    private static readonly Vector3 DefaultCloudColorA = new(0.95f, 0.96f, 0.98f); // Bright sunlit white
    private static readonly Vector3 DefaultCloudColorB = new(0.75f, 0.78f, 0.85f); // Soft shadow blue-grey

    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _skyRegionWidth;
    private readonly int _skyRegionHeight;

    private Effect _effect = null!;
    private Texture2D _pixelTexture = null!;
    private float _totalElapsedSeconds;
    private bool _disposed;

    /// <summary>Horizontal drift speed in UV units per second.</summary>
    public float WindSpeed { get; set; } = DefaultWindSpeed;

    /// <summary>Base noise frequency multiplier. Higher values produce smaller clouds.</summary>
    public float CloudScale { get; set; } = DefaultCloudScale;

    /// <summary>Cloud coverage threshold (0–1). Higher values produce fewer clouds.</summary>
    public float Coverage { get; set; } = DefaultCoverage;

    /// <summary>Edge softness band width for smooth cloud edges.</summary>
    public float Softness { get; set; } = DefaultSoftness;

    /// <summary>Overall cloud opacity (0–1).</summary>
    public float Opacity { get; set; } = DefaultOpacity;

    /// <summary>Bright (sunlit) cloud color for highlights.</summary>
    public Vector3 CloudColorA { get; set; } = DefaultCloudColorA;

    /// <summary>Shadow cloud color for darker undersides.</summary>
    public Vector3 CloudColorB { get; set; } = DefaultCloudColorB;

    /// <summary>
    /// Creates a sky cloud renderer for a specified region.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="skyRegionWidth">Width of the sky region in pixels.</param>
    /// <param name="skyRegionHeight">Height of the sky region in pixels.</param>
    public SkyCloudRenderer(GraphicsDevice graphicsDevice, int skyRegionWidth, int skyRegionHeight)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _skyRegionWidth = skyRegionWidth;
        _skyRegionHeight = skyRegionHeight;
    }

    /// <summary>
    /// Loads the cloud shader effect. Call during the screen's LoadContent phase.
    /// </summary>
    /// <param name="content">Content manager for loading the compiled shader.</param>
    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Effects/Clouds");

        _pixelTexture?.Dispose();
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Advances the wind animation timer. Call once per frame during Update.
    /// </summary>
    /// <param name="gameTime">Current game timing.</param>
    public void Update(GameTime gameTime)
    {
        _totalElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    /// <summary>
    /// Draws the clouds over the sky region of the scene.
    /// Should be called after the map tiles are drawn but before entities.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to draw with.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (_effect is null || _disposed)
            return;

        var aspectRatio = (float)_skyRegionWidth / _skyRegionHeight;

        _effect.Parameters["Time"]?.SetValue(_totalElapsedSeconds);
        _effect.Parameters["WindSpeed"]?.SetValue(WindSpeed);
        _effect.Parameters["AspectRatio"]?.SetValue(aspectRatio);
        _effect.Parameters["CloudScale"]?.SetValue(CloudScale);
        _effect.Parameters["Coverage"]?.SetValue(Coverage);
        _effect.Parameters["Softness"]?.SetValue(Softness);
        _effect.Parameters["Opacity"]?.SetValue(Opacity);
        _effect.Parameters["CloudColorA"]?.SetValue(CloudColorA);
        _effect.Parameters["CloudColorB"]?.SetValue(CloudColorB);

        // Draw a quad covering only the sky region with the cloud shader.
        // AlphaBlend: cloud alpha composites naturally over the sky tiles.
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            effect: _effect);

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(0, 0, _skyRegionWidth, _skyRegionHeight),
            Color.White);

        spriteBatch.End();
    }

    /// <summary>
    /// Releases resources owned by this renderer.
    /// </summary>
    public void UnloadContent()
    {
        Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _pixelTexture?.Dispose();
        _pixelTexture = null!;
        // Effect is managed by ContentManager — don't dispose it here.

        _disposed = true;
    }
}
