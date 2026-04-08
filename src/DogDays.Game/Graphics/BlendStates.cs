using Microsoft.Xna.Framework.Graphics;

namespace DogDays.Game.Graphics;

/// <summary>
/// Shared custom blend states used by multiple renderers.
/// </summary>
internal static class BlendStates
{
    /// <summary>
    /// Multiplies the destination (scene) color by the source (overlay) color.
    /// White source pixels pass through unchanged; darker pixels darken the scene.
    /// </summary>
    internal static readonly BlendState Multiply = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.DestinationAlpha,
        AlphaDestinationBlend = Blend.Zero
    };
}
