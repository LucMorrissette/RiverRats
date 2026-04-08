using Microsoft.Xna.Framework;
using DogDays.Game.Graphics;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class CloudShadowRendererTests
{
    // ── Resolution / dimensions ─────────────────────────────────

    [Fact]
    public void ResolutionDivisor__IsTwo()
    {
        Assert.Equal(2, CloudShadowRenderer.ResolutionDivisor);
    }

    // Note: CloudShadowRenderer requires a GraphicsDevice for LoadContent / Draw,
    // so these tests focus on the logic that can be verified without a GPU context.
    // The noise generation and rendering correctness are covered by PerlinNoiseTests
    // and visual inspection respectively.
}
