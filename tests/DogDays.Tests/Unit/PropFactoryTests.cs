using DogDays.Game.Entities;

namespace DogDays.Tests.Unit;

public class PropFactoryTests
{
    [Fact]
    public void CalculateUniformScale__WhenTargetWidthIsZero__ReturnsOne()
    {
        var scale = PropFactory.CalculateUniformScale(87, 0);

        Assert.Equal(1f, scale);
    }

    [Fact]
    public void CalculateUniformScale__WhenTargetWidthIsSpecified__ReturnsAspectLockedScale()
    {
        var scale = PropFactory.CalculateUniformScale(87, 64);

        Assert.Equal(64f / 87f, scale, 6);
    }
}