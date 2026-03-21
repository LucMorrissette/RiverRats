using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Util;

#nullable enable

namespace RiverRats.Game.Graphics;

/// <summary>
/// Renders drifting cloud shadows over the scene using procedurally generated tileable
/// noise textures. Two independent layers move at different speeds and directions, creating
/// a natural, constantly-shifting cloud canopy effect.
///
/// The renderer draws to its own low-resolution render target (half virtual resolution) and
/// composites with multiply blending, matching the same pattern used by <see cref="LightingRenderer"/>.
/// The low resolution + bilinear upscaling produces inherently soft, diffuse shadow edges.
/// </summary>
public sealed class CloudShadowRenderer : IDisposable
{
    /// <summary>
    /// Resolution divisor applied to the virtual resolution for the shadow map.
    /// 2 means 480×270 virtual → 240×135 shadow map, giving naturally soft edges.
    /// </summary>
    public const int ResolutionDivisor = 2;

    /// <summary>Size of each generated noise texture in pixels.</summary>
    private const int NoiseTextureSize = 256;

    /// <summary>
    /// Custom blend state that multiplies destination by source.
    /// White source pixels leave the scene unchanged; darker pixels darken the scene.
    /// </summary>
    private static readonly BlendState MultiplyBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.DestinationAlpha,
        AlphaDestinationBlend = Blend.Zero
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private readonly int _shadowMapWidth;
    private readonly int _shadowMapHeight;
    private readonly CloudLayer[] _layers;

    private RenderTarget2D _shadowMap = null!;
    private Texture2D _pixelTexture = null!;
    private float _totalElapsedSeconds;
    private bool _disposed;

    /// <summary>Width of the shadow map render target in pixels.</summary>
    public int ShadowMapWidth => _shadowMapWidth;

    /// <summary>Height of the shadow map render target in pixels.</summary>
    public int ShadowMapHeight => _shadowMapHeight;

    /// <summary>Total elapsed seconds driving the wind animation.</summary>
    public float TotalElapsedSeconds => _totalElapsedSeconds;

    /// <summary>
    /// Creates a cloud shadow renderer.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="virtualWidth">Virtual resolution width in pixels.</param>
    /// <param name="virtualHeight">Virtual resolution height in pixels.</param>
    public CloudShadowRenderer(GraphicsDevice graphicsDevice, int virtualWidth, int virtualHeight)
    {
        _graphicsDevice = graphicsDevice;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _shadowMapWidth = virtualWidth / ResolutionDivisor;
        _shadowMapHeight = virtualHeight / ResolutionDivisor;

        _layers = new CloudLayer[]
        {
            new()
            {
                // Base layer: large, slow-moving clouds drifting to the upper-right.
                WindDirection = Vector2.Normalize(new Vector2(1f, -0.3f)),
                WindSpeed = 4f,
                WorldScale = 3.0f,
                BaseFrequency = 3,
                Octaves = 5,
                Persistence = 0.5f,
                CoverageThreshold = 0.67f,
                EdgeSoftness = 0.13f,
                ShadowColor = new Color(160, 160, 180),
            },
            new()
            {
                // Detail layer: smaller, faster wisps drifting on a different heading.
                WindDirection = Vector2.Normalize(new Vector2(0.7f, -0.7f)),
                WindSpeed = 7f,
                WorldScale = 1.6f,
                BaseFrequency = 4,
                Octaves = 4,
                Persistence = 0.45f,
                CoverageThreshold = 0.70f,
                EdgeSoftness = 0.11f,
                ShadowColor = new Color(180, 180, 195),
            }
        };
    }

    /// <summary>
    /// Generates the noise textures and allocates the shadow map render target.
    /// Call during the screen's LoadContent phase.
    /// </summary>
    public void LoadContent()
    {
        _shadowMap?.Dispose();
        _shadowMap = new RenderTarget2D(
            _graphicsDevice,
            _shadowMapWidth,
            _shadowMapHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);

        for (var i = 0; i < _layers.Length; i++)
        {
            _layers[i].Texture?.Dispose();
            _layers[i].Texture = GenerateCloudTexture(_layers[i]);
        }

        _pixelTexture?.Dispose();
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Advances the wind animation. Call once per frame during Update.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _totalElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    /// <summary>
    /// Renders the cloud shadow overlay and composites it onto the scene.
    /// Should be called after entities are drawn but before the lighting pass.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use.</param>
    /// <param name="cameraPosition">Current camera centre in world pixels.</param>
    /// <param name="nightStrength">
    /// Night strength from <see cref="DayNightCycle.NightStrength"/> (0 = full day, 1 = full night).
    /// Cloud shadows fade out at night since the scene is already dark.
    /// </param>
    /// <param name="currentRenderTarget">
    /// Render target to restore after the shadow map pass (null = backbuffer).
    /// </param>
    public void Draw(
        SpriteBatch spriteBatch,
        Vector2 cameraPosition,
        float nightStrength,
        RenderTarget2D? currentRenderTarget)
    {
        // Fade shadows away during night — they're invisible against the dark anyway.
        if (nightStrength >= 0.8f)
            return;

        RenderShadowMap(spriteBatch, cameraPosition, nightStrength);
        CompositeShadowMap(spriteBatch, currentRenderTarget);
    }

    /// <summary>
    /// Releases GPU resources owned by this renderer.
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

        _shadowMap?.Dispose();
        _shadowMap = null!;

        _pixelTexture?.Dispose();
        _pixelTexture = null!;

        for (var i = 0; i < _layers.Length; i++)
        {
            _layers[i].Texture?.Dispose();
            _layers[i].Texture = null!;
        }

        _disposed = true;
    }

    private void RenderShadowMap(SpriteBatch spriteBatch, Vector2 cameraPosition, float nightStrength)
    {
        _graphicsDevice.SetRenderTarget(_shadowMap);
        _graphicsDevice.Clear(Color.White);

        for (var i = 0; i < _layers.Length; i++)
        {
            DrawCloudLayer(spriteBatch, _layers[i], cameraPosition);
        }

        // Fade shadows toward white (invisible) as night approaches.
        // An alpha-blended white overlay brightens the shadow map, weakening
        // cloud shadows proportionally as the scene darkens from the day/night cycle.
        var nightFade = MathHelper.Clamp(nightStrength / 0.8f, 0f, 1f);
        if (nightFade > 0f)
        {
            var fadeAlpha = (byte)(nightFade * 255);
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, 0, _shadowMapWidth, _shadowMapHeight),
                new Color(255, 255, 255, (int)fadeAlpha));
            spriteBatch.End();
        }
    }

    private void DrawCloudLayer(SpriteBatch spriteBatch, CloudLayer layer, Vector2 cameraPosition)
    {
        var texture = layer.Texture;
        if (texture is null)
            return;

        // World-space size of one full tile repetition.
        var tileWorldW = NoiseTextureSize * layer.WorldScale;
        var tileWorldH = NoiseTextureSize * layer.WorldScale;

        // Wind offset in world pixels.
        var windOffsetX = layer.WindDirection.X * layer.WindSpeed * _totalElapsedSeconds;
        var windOffsetY = layer.WindDirection.Y * layer.WindSpeed * _totalElapsedSeconds;

        // Camera viewport top-left in world pixels.
        var viewLeft = cameraPosition.X - _virtualWidth * 0.5f;
        var viewTop = cameraPosition.Y - _virtualHeight * 0.5f;

        // UV offset: where the viewport falls within the repeating cloud pattern.
        // Dividing by tile world size converts world pixels to texture UV [0..1] repeats.
        var uTexels = (viewLeft - windOffsetX) / tileWorldW * NoiseTextureSize;
        var vTexels = (viewTop - windOffsetY) / tileWorldH * NoiseTextureSize;

        // How many texture repeats fit across the shadow map viewport.
        var uScale = (float)_virtualWidth / tileWorldW;
        var vScale = (float)_virtualHeight / tileWorldH;
        var srcW = (int)MathF.Ceiling(uScale * NoiseTextureSize);
        var srcH = (int)MathF.Ceiling(vScale * NoiseTextureSize);

        // Split into integer texel offset (for the source rect) and fractional
        // remainder (applied to the destination position). This gives sub-texel
        // smooth scrolling that avoids the choppy snap of integer-only source rects.
        var srcX = (int)MathF.Floor(uTexels);
        var srcY = (int)MathF.Floor(vTexels);
        var fracX = uTexels - srcX;
        var fracY = vTexels - srcY;

        // Source rect uses integer texel coords; fractional part shifts the destination.
        var srcRect = new Rectangle(srcX, srcY, srcW + 1, srcH + 1);

        // Destination is slightly oversized to accommodate the extra source texel,
        // then offset by the fractional amount for smooth sub-pixel scrolling.
        var texelToShadow = (float)_shadowMapWidth / srcW;
        var destX = -fracX * texelToShadow;
        var destY = -fracY * texelToShadow;
        var destW = (srcW + 1) * texelToShadow;
        var destH = (srcH + 1) * texelToShadow;

        // White tint draws the texture as-is — shadow color is already baked into
        // the noise texture. Using anything other than White here would darken the
        // clear-sky (white) areas and create a global tinted overlay.

        // LinearWrap handles seamless tiling — no manual tile loop needed.
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: MultiplyBlend,
            samplerState: SamplerState.LinearWrap);

        spriteBatch.Draw(
            texture,
            new Vector2(destX, destY),
            srcRect,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(destW / srcRect.Width, destH / srcRect.Height),
            SpriteEffects.None,
            0f);

        spriteBatch.End();
    }

    private void CompositeShadowMap(SpriteBatch spriteBatch, RenderTarget2D? currentRenderTarget)
    {
        _graphicsDevice.SetRenderTarget(currentRenderTarget);

        // LinearClamp gives free bilinear upscaling: the low-res shadow map
        // blurs naturally when stretched to full virtual resolution, producing soft edges.
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: MultiplyBlend,
            samplerState: SamplerState.LinearClamp);

        spriteBatch.Draw(
            _shadowMap,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);

        spriteBatch.End();
    }

    /// <summary>
    /// Generates a tileable cloud shadow texture for the given layer.
    /// Cloud regions use the layer's shadow color; clear sky regions are white.
    /// </summary>
    private Texture2D GenerateCloudTexture(CloudLayer layer)
    {
        var noiseMap = PerlinNoise.GenerateTileableNoiseMap(
            NoiseTextureSize,
            NoiseTextureSize,
            layer.BaseFrequency,
            layer.Octaves,
            layer.Persistence);

        var pixels = new Color[NoiseTextureSize * NoiseTextureSize];
        var threshold = layer.CoverageThreshold;
        var softness = layer.EdgeSoftness;
        var shadowColor = layer.ShadowColor;

        for (var i = 0; i < noiseMap.Length; i++)
        {
            // SmoothStep from clear sky to full cloud shadow.
            var density = SmoothStep(threshold - softness, threshold + softness, noiseMap[i]);

            // White (no shadow) where clear; shadow color where cloudy.
            pixels[i] = Color.Lerp(Color.White, shadowColor, density);
        }

        var texture = new Texture2D(_graphicsDevice, NoiseTextureSize, NoiseTextureSize);
        texture.SetData(pixels);
        return texture;
    }

    /// <summary>
    /// Hermite smoothstep: returns 0 when x &lt;= edge0, 1 when x &gt;= edge1,
    /// and a smooth S-curve in between. Produces soft cloud edges.
    /// </summary>
    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// Configuration and runtime state for a single cloud layer.
    /// </summary>
    private sealed class CloudLayer
    {
        /// <summary>Normalized wind direction vector.</summary>
        public Vector2 WindDirection;

        /// <summary>Wind speed in world pixels per second.</summary>
        public float WindSpeed;

        /// <summary>World-space scale factor. One noise texel covers this many world pixels.</summary>
        public float WorldScale;

        /// <summary>Base noise frequency (cells across the texture) — controls cloud size.</summary>
        public int BaseFrequency;

        /// <summary>Number of noise octaves. More = finer edge detail.</summary>
        public int Octaves;

        /// <summary>Amplitude falloff per octave (0–1).</summary>
        public float Persistence;

        /// <summary>Noise value threshold for cloud coverage (0–1). Higher = fewer clouds.</summary>
        public float CoverageThreshold;

        /// <summary>Width of the soft edge band around the threshold.</summary>
        public float EdgeSoftness;

        /// <summary>Color to tint the scene where clouds cast shadow.</summary>
        public Color ShadowColor;

        /// <summary>Generated noise texture (owned by the renderer).</summary>
        public Texture2D? Texture;
    }
}
