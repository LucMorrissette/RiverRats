using System;
using Microsoft.Xna.Framework;
using DogDays.Game.Data;
using DogDays.Game.Entities;
using DogDays.Game.World;
using Xunit;

namespace DogDays.Tests.Unit;

public class GrandpaNpcTests
{
    private static readonly Point FrameSize = new(32, 32);

    private static IndoorNavGraph SquareGraph()
    {
        var nodes = new IndoorNavNode[]
        {
            new(1, new Vector2(100f, 100f), "a", null),
            new(2, new Vector2(200f, 100f), "b", null),
            new(3, new Vector2(200f, 200f), "c", null),
            new(4, new Vector2(100f, 200f), "d", null),
        };
        var links = new IndoorNavLink[]
        {
            new(1, 2),
            new(2, 3),
            new(3, 4),
            new(4, 1),
        };

        return new IndoorNavGraph(nodes, links);
    }

    [Fact]
    public void FaceToward__FacesPlayerOnDominantAxis()
    {
        var grandpa = new GrandpaNpc(new Vector2(100f, 100f), FrameSize, SquareGraph(), random: new Random(42));

        grandpa.FaceToward(new Vector2(116f, 60f));

        Assert.Equal(FacingDirection.Up, grandpa.Facing);
    }
}