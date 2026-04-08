using Microsoft.Xna.Framework;
using DogDays.Game.UI;
using DogDays.Tests.Helpers;

namespace DogDays.Tests.Unit;

public sealed class QuestDiscoveryGlintPassTests
{
    [Fact]
    public void Trigger__CreatesImmediateParticlesAndFollowups__HasActiveGlints()
    {
        var pass = new QuestDiscoveryGlintPass();

        pass.Trigger(new Rectangle(120, 24, 340, 72), sceneScale: 2);

        Assert.True(pass.ActiveParticleCount > 0);
        Assert.True(pass.PendingBurstCount > 0);
        Assert.True(pass.HasActiveParticles);
    }

    [Fact]
    public void Update__AfterLongDelay__ClearsParticlesAndScheduledBursts()
    {
        var pass = new QuestDiscoveryGlintPass();
        pass.Trigger(new Rectangle(120, 24, 340, 72), sceneScale: 2);

        pass.Update(FakeGameTime.FromSeconds(2.0f));

        Assert.Equal(0, pass.ActiveParticleCount);
        Assert.Equal(0, pass.PendingBurstCount);
        Assert.False(pass.HasActiveParticles);
    }
}