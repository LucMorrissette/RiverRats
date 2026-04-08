using Microsoft.Xna.Framework;
using DogDays.Game.Util;
using System;
using Xunit;

namespace DogDays.Tests.Unit;

/// <summary>
/// Tests for <see cref="PolygonBounds"/> containment, spawning, and slicing.
/// </summary>
public sealed class PolygonBoundsTests
{
    private static PolygonBounds CreateSquare(float x, float y, float size)
    {
        return new PolygonBounds(new[]
        {
            new Vector2(x, y),
            new Vector2(x + size, y),
            new Vector2(x + size, y + size),
            new Vector2(x, y + size),
        });
    }

    [Fact]
    public void Constructor__RequiresAtLeast3Vertices()
    {
        Assert.Throws<ArgumentException>(() => new PolygonBounds(new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
        }));
    }

    [Fact]
    public void Contains__PointInsideSquare__ReturnsTrue()
    {
        var poly = CreateSquare(10, 10, 100);
        Assert.True(poly.Contains(new Vector2(50, 50)));
    }

    [Fact]
    public void Contains__PointOutsideSquare__ReturnsFalse()
    {
        var poly = CreateSquare(10, 10, 100);
        Assert.False(poly.Contains(new Vector2(5, 50)));
        Assert.False(poly.Contains(new Vector2(200, 50)));
        Assert.False(poly.Contains(new Vector2(50, 5)));
        Assert.False(poly.Contains(new Vector2(50, 200)));
    }

    [Fact]
    public void Contains__PointInsideTriangle__ReturnsTrue()
    {
        var triangle = new PolygonBounds(new[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(50, 80),
        });

        Assert.True(triangle.Contains(new Vector2(50, 30)));
    }

    [Fact]
    public void Contains__PointOutsideTriangle__ReturnsFalse()
    {
        var triangle = new PolygonBounds(new[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(50, 80),
        });

        // Below-right of the triangle edge.
        Assert.False(triangle.Contains(new Vector2(90, 70)));
    }

    [Fact]
    public void BoundingBox__EncloseAllVertices()
    {
        var poly = CreateSquare(20, 30, 60);
        var bbox = poly.BoundingBox;

        Assert.Equal(20, bbox.Left);
        Assert.Equal(30, bbox.Top);
        Assert.Equal(60, bbox.Width);
        Assert.Equal(60, bbox.Height);
    }

    [Fact]
    public void Centroid__IsAverageOfVertices()
    {
        var poly = CreateSquare(0, 0, 100);
        Assert.Equal(50f, poly.Centroid.X, 0.01f);
        Assert.Equal(50f, poly.Centroid.Y, 0.01f);
    }

    [Fact]
    public void FromRectangle__ProducesMatchingPolygon()
    {
        var rect = new Rectangle(10, 20, 100, 50);
        var poly = PolygonBounds.FromRectangle(rect);

        Assert.True(poly.Contains(new Vector2(50, 40)));
        Assert.False(poly.Contains(new Vector2(5, 40)));
        Assert.False(poly.Contains(new Vector2(50, 15)));
    }

    [Fact]
    public void RandomPointInside__AlwaysInsidePolygon()
    {
        var poly = CreateSquare(10, 10, 100);
        var rng = new Random(42);

        for (var i = 0; i < 100; i++)
        {
            var point = poly.RandomPointInside(rng);
            Assert.True(poly.Contains(point),
                $"Random point ({point.X}, {point.Y}) was outside the polygon");
        }
    }

    [Fact]
    public void SliceHorizontal__KeepsBottomPortion()
    {
        var poly = CreateSquare(0, 0, 100);

        // Slice at 50% — keep bottom half.
        var sliced = poly.SliceHorizontal(0.5f);

        // A point in the bottom half should be inside.
        Assert.True(sliced.Contains(new Vector2(50, 75)));

        // A point in the top half should be outside.
        Assert.False(sliced.Contains(new Vector2(50, 25)));
    }

    [Fact]
    public void SliceHorizontal__PreservesIrregularShape()
    {
        // Trapezoid: wide at top, narrow at bottom.
        var trapezoid = new PolygonBounds(new[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(80, 100),
            new Vector2(20, 100),
        });

        // Slice at 50% (y=50). The polygon at y=50 goes from x=10 to x=90.
        var sliced = trapezoid.SliceHorizontal(0.5f);

        // Point at center of bottom half should be inside.
        Assert.True(sliced.Contains(new Vector2(50, 75)));

        // Point at the wide top area should now be outside.
        Assert.False(sliced.Contains(new Vector2(50, 25)));

        // Point near the right edge just below the slice should be inside
        // (narrower than original top).
        Assert.True(sliced.Contains(new Vector2(85, 55)));
    }

    [Fact]
    public void SliceHorizontal__ZeroFraction__ReturnsFullPolygon()
    {
        var poly = CreateSquare(0, 0, 100);
        var sliced = poly.SliceHorizontal(0f);

        Assert.True(sliced.Contains(new Vector2(50, 10)));
        Assert.True(sliced.Contains(new Vector2(50, 90)));
    }
}
