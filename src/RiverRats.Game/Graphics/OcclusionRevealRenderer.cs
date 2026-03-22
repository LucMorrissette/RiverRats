using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

#nullable enable

namespace RiverRats.Game.Graphics;

/// <summary>
/// Manages the render target and shader for revealing the player behind occluding
/// entities via a circular alpha-fade lens. Occluding entities are drawn to a
/// separate render target, then composited back over the scene through the
/// <c>OcclusionReveal</c> shader which reduces alpha in a circle around the player.
/// </summary>
public sealed class OcclusionRevealRenderer : IDisposable
{
    /// <summary>Radius of the reveal circle in UV space (fraction of screen height).</summary>
    private const float DefaultRevealRadius = 0.12f;

    /// <summary>
    /// Fraction of <see cref="DefaultRevealRadius"/> used as a soft edge.
    /// 0 = hard circle, 1 = entirely gradient.
    /// </summary>
    private const float DefaultEdgeSoftness = 0.15f;

    /// <summary>Minimum alpha at the centre of the reveal lens (0 = fully see-through).</summary>
    private const float DefaultMinAlpha = 0.05f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;

    private RenderTarget2D _occluderTarget = null!;
    private Effect _effect = null!;

    /// <summary>
    /// Creates a new occlusion reveal renderer.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device for render target creation.</param>
    /// <param name="virtualWidth">Virtual resolution width.</param>
    /// <param name="virtualHeight">Virtual resolution height.</param>
    public OcclusionRevealRenderer(GraphicsDevice graphicsDevice, int virtualWidth, int virtualHeight)
    {
        _graphicsDevice = graphicsDevice;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
    }

    /// <summary>The render target that occluding entities should be drawn into.</summary>
    public RenderTarget2D OccluderTarget => _occluderTarget;

    /// <summary>
    /// Loads the shader and creates the render target.
    /// </summary>
    /// <param name="content">Content manager for loading the compiled effect.</param>
    public void LoadContent(ContentManager content)
    {
        _effect = content.Load<Effect>("Effects/OcclusionReveal");
        _effect.Parameters["RevealRadius"].SetValue(DefaultRevealRadius);
        _effect.Parameters["EdgeSoftness"].SetValue(DefaultEdgeSoftness);
        _effect.Parameters["MinAlpha"].SetValue(DefaultMinAlpha);
        _effect.Parameters["AspectRatio"].SetValue((float)_virtualWidth / _virtualHeight);

        _occluderTarget = new RenderTarget2D(
            _graphicsDevice,
            _virtualWidth,
            _virtualHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
    }

    /// <summary>
    /// Prepares the render target for drawing occluding entities.
    /// Call this before the occluder entity draw pass.
    /// </summary>
    public void BeginCapture()
    {
        _graphicsDevice.SetRenderTarget(_occluderTarget);
        _graphicsDevice.Clear(Color.Transparent);
    }

    /// <summary>
    /// Composites the occluder render target back over the scene with the
    /// circular alpha-fade reveal centred on the player.
    /// Call this after the occluder SpriteBatch has ended.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for drawing the composite quad.</param>
    /// <param name="playerWorldCenter">Player centre in world-space pixels.</param>
    /// <param name="cameraViewMatrix">Current camera view matrix.</param>
    /// <param name="sceneRenderTarget">
    /// The render target that the scene is being drawn to (the one to restore after capture).
    /// Pass <c>null</c> to restore to the back buffer.
    /// </param>
    public void Composite(
        SpriteBatch spriteBatch,
        Vector2 playerWorldCenter,
        Matrix cameraViewMatrix,
        RenderTarget2D? sceneRenderTarget)
    {
        // Transform player world position to screen space, then to UV (0–1).
        var screenPos = Vector2.Transform(playerWorldCenter, cameraViewMatrix);
        var playerUv = new Vector2(screenPos.X / _virtualWidth, screenPos.Y / _virtualHeight);
        _effect.Parameters["PlayerCenter"].SetValue(playerUv);

        _graphicsDevice.SetRenderTarget(sceneRenderTarget);

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Immediate,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.PointClamp,
            effect: _effect);
        spriteBatch.Draw(
            _occluderTarget,
            new Rectangle(0, 0, _virtualWidth, _virtualHeight),
            Color.White);
        spriteBatch.End();
    }

    /// <summary>
    /// Releases the render target and shader resources.
    /// </summary>
    public void UnloadContent()
    {
        _occluderTarget?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        UnloadContent();
    }
}
