using System;
using System.Collections.Generic;
using DogDays.Game.Screens;
using Xunit;

namespace DogDays.Tests.Unit;

public sealed class FishingScreenPopulationTests
{
    [Fact]
    public void RollFishPopulation__SameSeed__ReturnsSameProfile()
    {
        var first = FishingScreen.RollFishPopulation(new Random(42));
        var second = FishingScreen.RollFishPopulation(new Random(42));

        Assert.Equal(first, second);
    }

    [Fact]
    public void RollFishPopulation__AlwaysIncludesEachSpecies()
    {
        for (var seed = 0; seed < 128; seed++)
        {
            var profile = FishingScreen.RollFishPopulation(new Random(seed));

            Assert.True(profile.PerchCount >= 1);
            Assert.True(profile.BassCount >= 1);
            Assert.True(profile.CatfishCount >= 1);
        }
    }

    [Fact]
    public void RollFishPopulation__KeepsTotalPopulationWithinExpectedRange()
    {
        for (var seed = 0; seed < 128; seed++)
        {
            var profile = FishingScreen.RollFishPopulation(new Random(seed));
            var totalCount = profile.PerchCount + profile.BassCount + profile.CatfishCount;

            Assert.InRange(totalCount, 7, 10);
        }
    }

    [Fact]
    public void RollFishPopulation__AcrossSeeds__ProducesMultipleProfiles()
    {
        var profiles = new HashSet<(int PerchCount, int BassCount, int CatfishCount)>();

        for (var seed = 0; seed < 32; seed++)
        {
            profiles.Add(FishingScreen.RollFishPopulation(new Random(seed)));
        }

        Assert.True(profiles.Count > 1);
    }
}