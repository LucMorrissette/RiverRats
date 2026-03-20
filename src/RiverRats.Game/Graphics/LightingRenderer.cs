using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#nullable enable

namespace RiverRats.Game.Graphics;

/// <summary>
/// Renders a lightmap-based 2D lighting pass.
/// Fills a low-resolution render target with an ambient darkness color derived from the
/// day/night cycle, draws each registered light additively onto it using a radial gradient
/// texture, then composites the lightmap over the scene with multiply blending.
/// The low resolution + LinearClamp upscaling produces a naturally soft glow.
/// </summary>
public sealed class LightingRenderer
{
    /// <summary>
    /// Divisor applied to the virtual resolution to produce the lightmap size.
    /// 4 means 480×270 virtual → 120×68 lightmap, giving a naturally blurry glow.
    /// </summary>
    public const int ResolutionDivisor = 4;

    /// <summary>
    /// Ambient darkness color used to fill the lightmap at full night (NightStrength = 1).
    /// Matches the NightTint in DayNightCycle so the fire punches through consistently.
    /// </summary>
    private static readonly Color NightAmbient = new(60, 60, 120);

    /// <summary>
    /// Custom blend state that multiplies the destination (scene) color by the
    /// source (lightmap) color. Lit areas (white) pass through unchanged;
    /// dark areas tint the scene toward black.
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
    private readonly List<LightData> _lights = new();

    private RenderTarget2D _lightmap = null!;
    private Texture2D _gradientTexture = null!;

    /// <summary>Width of the lightmap render target in pixels.</summary>
    public int LightmapWidth => _virtualWidth / ResolutionDivisor;

    /// <summary>Height of the lightmap render target in pixels.</summary>
    public int LightmapHeight => _virtualHeight / ResolutionDivisor;

    /// <summary>
    /// Creates a LightingRenderer.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="virtualWidth">Virtual resolution width in pixels.</param>
    /// <param name="virtualHeight">Virtual resolution height in pixels.</param>
    public LightingRenderer(GraphicsDevice graphicsDevice, int virtualWidth, int virtualHeight)
    {
        _graphicsDevice = graphicsDevice;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
    }

    /// <summary>
    /// Loads the lightmap render target and radial gradient texture.
    /// Call this during the screen's LoadContent phase.
    /// </summary>
    /// <param name="gradientTexture">
    /// A radial gradient texture: white at the center, transparent at the edges.
    /// Used to draw each light's glow blob onto the lightmap.
    /// </param>
    public void LoadContent(Texture2D gradientTexture)
    {
        _gradientTexture = gradientTexture;
        _lightmap?.Dispose();
        _lightmap = new RenderTarget2D(_graphicsDevice, LightmapWidth, LightmapHeight);
    }

    /// <summary>
    /// Releases GPU resources owned by the lighting renderer.
    /// Call this when the owning screen unloads.
    /// </summary>
    public void UnloadContent()
    {
        _lightmap?.Dispose();
        _lightmap = null!;
    }

    /// <summary>
    /// Replaces the light list for the current frame.
    /// Call once per frame before <see cref="Draw"/>, after updating all fires.
    /// </summary>
    public void SetLights(LightData[] lights)
    {
        SetLights(lights, lights.Length);
    }

    /// <summary>
    /// Replaces the light list for the current frame using only the first <paramref name="count"/> entries.
    /// </summary>
    public void SetLights(LightData[] lights, int count)
    {
        if (count < 0 || count > lights.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _lights.Clear();
        for (var i = 0; i < count; i++)
        {
            _lights.Add(lights[i]);
        }
    }

    /// <summary>
    /// Returns true when the lighting pass can be skipped (full day, no lights active).
    /// </summary>
    public static bool ShouldSkip(float nightStrength) => nightStrength <= 0f;

    /// <summary>
    /// Computes the ambient color to fill the lightmap with for a given night strength.
    /// At nightStrength = 0 returns Color.White (no darkening).
    /// At nightStrength = 1 returns the full night ambient color.
    /// </summary>
    public static Color GetAmbientColor(float nightStrength)
    {
        if (nightStrength <= 0f) return Color.White;
        if (nightStrength >= 1f) return NightAmbient;
        return Color.Lerp(Color.White, NightAmbient, nightStrength);
    }

    /// <summary>
    /// Renders the lightmap and composites it over the current scene.
    /// Handles its own render target switching and SpriteBatch calls.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use.</param>
    /// <param name="nightStrength">Current night strength from <see cref="DayNightCycle.NightStrength"/>.</param>
    /// <param name="cameraTransform">The camera's world-to-screen transformation matrix.</param>
    /// <param name="currentRenderTarget">Render target to restore after the lightmap pass (null = backbuffer).</param>
    public void Draw(
        SpriteBatch spriteBatch,
        float nightStrength,
        Matrix cameraTransform,
        RenderTarget2D? currentRenderTarget)
    {
        if (ShouldSkip(nightStrength))
            return;

        RenderLightmap(spriteBatch, nightStrength, cameraTransform);
        CompositeLightmap(spriteBatch, currentRenderTarget);
    }

    private void RenderLightmap(SpriteBatch spriteBatch, float nightStrength, Matrix cameraTransform)
    {
        _graphicsDevice.SetRenderTarget(_lightmap);
        _graphicsDevice.Clear(GetAmbientColor(nightStrength));

        // Scale the camera transform down to lightmap resolution so world-space
        // light positions map correctly onto the low-res lightmap.
        var scale = 1f / ResolutionDivisor;
        var lightmapTransform = cameraTransform * Matrix.CreateScale(scale, scale, 1f);

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.Additive,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: lightmapTransform);

        foreach (var light in _lights)
        {
            var diameter = light.Radius * 2f;
            var destRect = new Rectangle(
                (int)(light.Position.X - light.Radius),
                (int)(light.Position.Y - light.Radius),
                (int)diameter,
                (int)diameter);

            var tint = new Color(light.Color, light.Intensity);
            spriteBatch.Draw(_gradientTexture, destRect, tint);
        }

        spriteBatch.End();
    }

    private void CompositeLightmap(SpriteBatch spriteBatch, RenderTarget2D? currentRenderTarget)
    {
        _graphicsDevice.SetRenderTarget(currentRenderTarget);

        // LinearClamp gives free bilinear upscaling: the low-res lightmap
        // blurs naturally when stretched to full virtual resolution.
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: MultiplyBlend,
            samplerState: SamplerState.LinearClamp);

        spriteBatch.Draw(
            _lightmap,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);

        spriteBatch.End();
    }
}
