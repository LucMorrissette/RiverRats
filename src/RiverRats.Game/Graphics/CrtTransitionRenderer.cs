using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiverRats.Game.Graphics;

/// <summary>
/// Draws the CRT power-off/on screen transition. During power-off the scene image
/// squeezes vertically into a bright horizontal line, then the line shrinks to a dot.
/// Power-on reverses the sequence. The effect is driven by an external alpha value
/// (<c>0 = scene fully visible, 1 = screen fully dark</c>).
/// Extracted from <c>GameplayScreen.DrawCrtPowerTransition</c>.
/// </summary>
public sealed class CrtTransitionRenderer
{
    /// <summary>Alpha (0–0.6): vertical squeeze phase. Alpha (0.6–1.0): horizontal shrink phase.</summary>
    private const float PhaseOneBoundary = 0.6f;
    private const int LineThickness = 3;

    private static readonly Color CrtDark = new(30, 30, 40);

    private readonly Texture2D _pixelTexture;

    /// <summary>
    /// Creates the renderer.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white pixel texture used for all solid fills.</param>
    public CrtTransitionRenderer(Texture2D pixelTexture)
    {
        _pixelTexture = pixelTexture;
    }

    /// <summary>
    /// Draws the CRT transition effect. The sprite batch must be ended before this call;
    /// this method manages its own Begin/End pairs.
    /// </summary>
    /// <param name="spriteBatch">Sprite batch to draw with.</param>
    /// <param name="viewport">Current viewport (used for screen dimensions).</param>
    /// <param name="fadeAlpha">
    /// Transition progress: 0 = fully transparent (no effect), 1 = fully dark.
    /// Values below 0 are clamped — the method is a no-op at alpha 0.
    /// </param>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport, float fadeAlpha)
    {
        if (fadeAlpha <= 0f)
            return;

        var screenW = viewport.Width;
        var screenH = viewport.Height;
        var centerY = screenH / 2;
        var centerX = screenW / 2;

        if (fadeAlpha < PhaseOneBoundary)
        {
            // Phase 1: Black bars close from top and bottom.
            var phaseProgress = fadeAlpha / PhaseOneBoundary; // 0→1
            var halfGap = (int)((1f - phaseProgress) * centerY);
            if (halfGap < LineThickness / 2)
                halfGap = LineThickness / 2;

            var topBarHeight = centerY - halfGap;
            var bottomBarY = centerY + halfGap;

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp);

            if (topBarHeight > 0)
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenW, topBarHeight), CrtDark);

            if (bottomBarY < screenH)
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, bottomBarY, screenW, screenH - bottomBarY), CrtDark);

            // Phosphor glow on the remaining strip — brighter as it gets thinner.
            var glowAlpha = phaseProgress * 0.4f;
            spriteBatch.Draw(
                _pixelTexture,
                new Rectangle(0, topBarHeight, screenW, bottomBarY - topBarHeight),
                Color.White * glowAlpha);

            spriteBatch.End();
        }
        else
        {
            // Phase 2: Full vertical squeeze done — shrink the line horizontally.
            var phaseProgress = (fadeAlpha - PhaseOneBoundary) / (1f - PhaseOneBoundary); // 0→1
            var halfWidth = (int)((1f - phaseProgress) * centerX);
            var lineTop = centerY - LineThickness / 2;

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp);

            // Full dark background.
            spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, screenW, screenH), CrtDark);

            // Bright shrinking line/dot.
            if (halfWidth > 0)
            {
                var dotAlpha = 1f - phaseProgress * 0.5f;
                var lineX = centerX - halfWidth;
                var lineW = halfWidth * 2;
                spriteBatch.Draw(
                    _pixelTexture,
                    new Rectangle(lineX, lineTop, lineW, LineThickness),
                    Color.White * dotAlpha);
            }

            spriteBatch.End();
        }
    }
}
