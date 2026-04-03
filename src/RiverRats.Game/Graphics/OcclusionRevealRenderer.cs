using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RiverRats.Game.Entities;

#nullable enable

namespace RiverRats.Game.Graphics;

/// <summary>
/// Describes a world prop that can occlude characters, together with its Y-sort anchor offset.
/// </summary>
/// <param name="Prop">The world prop to test for occlusion.</param>
/// <param name="AnchorOffset">Pixels subtracted from <c>Bounds.Bottom</c> before dividing by map height.
/// Matches the offset used when Y-sorting the prop during the draw pass.</param>
public readonly record struct OcclusionEntry(IWorldProp Prop, float AnchorOffset = 0f);

/// <summary>
/// Manages the render target and shader for revealing the player (and optionally the
/// follower) behind occluding entities via circular alpha-fade lenses. Occluding
/// entities are drawn to a separate render target, then composited back over the scene
/// through the <c>OcclusionReveal</c> shader which reduces alpha in a circle around
/// each character that is occluded.
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
        // Initialise follower lens to the sentinel "inactive" position.
        _effect.Parameters["FollowerCenter"].SetValue(new Vector2(-1f, -1f));

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
    /// Composites the occluder render target back over the scene with circular alpha-fade
    /// reveal lenses centred on the player and, optionally, the follower.
    /// Call this after the occluder SpriteBatch has ended.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch for drawing the composite quad.</param>
    /// <param name="playerWorldCenter">Player centre in world-space pixels.</param>
    /// <param name="followerWorldCenter">
    /// Follower centre in world-space pixels, or <c>null</c> when the follower reveal
    /// lens should be disabled.
    /// </param>
    /// <param name="cameraViewMatrix">Current camera view matrix.</param>
    /// <param name="sceneRenderTarget">
    /// The render target that the scene is being drawn to (the one to restore after capture).
    /// Pass <c>null</c> to restore to the back buffer.
    /// </param>
    public void Composite(
        SpriteBatch spriteBatch,
        Vector2 playerWorldCenter,
        Vector2? followerWorldCenter,
        Matrix cameraViewMatrix,
        RenderTarget2D? sceneRenderTarget)
    {
        // Transform player world position to screen space, then to UV (0–1).
        var screenPos = Vector2.Transform(playerWorldCenter, cameraViewMatrix);
        var playerUv = new Vector2(screenPos.X / _virtualWidth, screenPos.Y / _virtualHeight);
        _effect.Parameters["PlayerCenter"].SetValue(playerUv);

        // Transform follower world position, or use sentinel (-1, -1) to disable the lens.
        if (followerWorldCenter.HasValue)
        {
            var followerScreenPos = Vector2.Transform(followerWorldCenter.Value, cameraViewMatrix);
            var followerUv = new Vector2(
                followerScreenPos.X / _virtualWidth,
                followerScreenPos.Y / _virtualHeight);
            _effect.Parameters["FollowerCenter"].SetValue(followerUv);
        }
        else
        {
            _effect.Parameters["FollowerCenter"].SetValue(new Vector2(-1f, -1f));
        }

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
    /// Checks whether any prop in <paramref name="occluders"/> fully contains
    /// <paramref name="characterBounds"/> and sorts in front of the character.
    /// This method belongs here because the occlusion reveal renderer owns the
    /// concept of "which entities can occlude a character" — it is the renderer
    /// that responds when occlusion is detected.
    /// </summary>
    /// <param name="characterBounds">The character's world-space bounding rectangle.</param>
    /// <param name="characterDepth">The character's Y-sort depth (bottom / mapHeight).</param>
    /// <param name="occluders">Flat list of all world props that can occlude characters, with their anchor offsets.</param>
    /// <param name="mapPixelHeight">Map height in pixels — used to normalise sort depth.</param>
    /// <param name="mapPixelWidth">Map width in pixels — used for X tiebreaker in sort depth.</param>
    /// <returns><c>true</c> if the character is fully contained by any in-front occluder.</returns>
    public static bool CheckOcclusion(
        Rectangle characterBounds,
        float characterDepth,
        IReadOnlyList<OcclusionEntry> occluders,
        float mapPixelHeight,
        float mapPixelWidth)
    {
        for (var i = 0; i < occluders.Count; i++)
        {
            var entry = occluders[i];
            if (entry.Prop.SuppressOcclusion)
                continue;

            var depth = SortDepth(entry.Prop.Bounds, mapPixelHeight, mapPixelWidth, entry.AnchorOffset);
            if (depth > characterDepth && entry.Prop.Bounds.Contains(characterBounds))
                return true;
        }

        return false;
    }

    // ── Shared depth helper (mirrors GameplayScreen.SortDepth exactly) ──────

    private static float SortDepth(Rectangle bounds, float mapHeight, float mapWidth, float anchorOffset = 0f)
    {
        var yDepth = (bounds.Bottom - anchorOffset) / mapHeight;
        var tieBreakerRange = 1f / mapHeight;
        var yScaled = yDepth * (1f - tieBreakerRange);
        var xTie = bounds.Left / (mapWidth * mapHeight);
        return MathHelper.Clamp(yScaled + xTie, 0f, 0.9999f);
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
