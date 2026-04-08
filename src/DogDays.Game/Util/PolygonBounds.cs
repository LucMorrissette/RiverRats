using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DogDays.Game.Util;

/// <summary>
/// Defines a convex or concave polygon boundary for containment tests,
/// random point generation, and horizontal slicing. Used for irregular
/// swim areas in the fishing mini-game.
/// </summary>
public sealed class PolygonBounds
{
    private readonly Vector2[] _vertices;

    /// <summary>Axis-aligned bounding box enclosing the polygon.</summary>
    public Rectangle BoundingBox { get; }

    /// <summary>Geometric centroid of the polygon.</summary>
    public Vector2 Centroid { get; }

    /// <summary>
    /// Creates a polygon from an array of vertices in winding order.
    /// </summary>
    /// <param name="vertices">Polygon vertices (at least 3). Copied internally.</param>
    public PolygonBounds(Vector2[] vertices)
    {
        if (vertices.Length < 3)
            throw new ArgumentException("Polygon requires at least 3 vertices.", nameof(vertices));

        _vertices = new Vector2[vertices.Length];
        Array.Copy(vertices, _vertices, vertices.Length);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        float cx = 0, cy = 0;

        for (var i = 0; i < _vertices.Length; i++)
        {
            var v = _vertices[i];
            if (v.X < minX) minX = v.X;
            if (v.Y < minY) minY = v.Y;
            if (v.X > maxX) maxX = v.X;
            if (v.Y > maxY) maxY = v.Y;
            cx += v.X;
            cy += v.Y;
        }

        BoundingBox = new Rectangle(
            (int)MathF.Floor(minX),
            (int)MathF.Floor(minY),
            (int)MathF.Ceiling(maxX - minX),
            (int)MathF.Ceiling(maxY - minY));

        Centroid = new Vector2(cx / _vertices.Length, cy / _vertices.Length);
    }

    /// <summary>
    /// Creates a rectangular polygon from a <see cref="Rectangle"/>.
    /// </summary>
    public static PolygonBounds FromRectangle(Rectangle rect)
    {
        return new PolygonBounds(new[]
        {
            new Vector2(rect.Left, rect.Top),
            new Vector2(rect.Right, rect.Top),
            new Vector2(rect.Right, rect.Bottom),
            new Vector2(rect.Left, rect.Bottom),
        });
    }

    /// <summary>
    /// Tests whether a point lies inside the polygon using the ray-casting algorithm.
    /// </summary>
    public bool Contains(Vector2 point)
    {
        var inside = false;

        for (int i = 0, j = _vertices.Length - 1; i < _vertices.Length; j = i++)
        {
            var vi = _vertices[i];
            var vj = _vertices[j];

            if ((vi.Y > point.Y) != (vj.Y > point.Y) &&
                point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Returns a new polygon with all vertices moved inward toward the centroid
    /// by <paramref name="margin"/> pixels. Useful for keeping entities away from
    /// sharp corners and edges.
    /// </summary>
    public PolygonBounds Inset(float margin)
    {
        var inset = new Vector2[_vertices.Length];
        for (var i = 0; i < _vertices.Length; i++)
        {
            var dir = Centroid - _vertices[i];
            var len = dir.Length();
            if (len < 0.001f)
            {
                inset[i] = _vertices[i];
            }
            else
            {
                var move = MathF.Min(margin, len * 0.5f);
                inset[i] = _vertices[i] + dir / len * move;
            }
        }
        return new PolygonBounds(inset);
    }

    /// <summary>
    /// Generates a random point inside the polygon using rejection sampling.
    /// </summary>
    public Vector2 RandomPointInside(Random rng)
    {
        var bbox = BoundingBox;

        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var x = bbox.Left + (float)(rng.NextDouble() * bbox.Width);
            var y = bbox.Top + (float)(rng.NextDouble() * bbox.Height);
            var point = new Vector2(x, y);

            if (Contains(point))
                return point;
        }

        // Fallback to centroid if rejection sampling fails (very thin polygon).
        return Centroid;
    }

    /// <summary>
    /// Creates a new polygon that is the bottom portion of this polygon,
    /// clipped by a horizontal line at <paramref name="topFraction"/> of
    /// the vertical extent. For example, 0.35 keeps the bottom 65%.
    /// </summary>
    /// <param name="topFraction">Fraction from the top (0..1) where the cut is made.</param>
    public PolygonBounds SliceHorizontal(float topFraction)
    {
        var minY = (float)BoundingBox.Top;
        var maxY = (float)BoundingBox.Bottom;
        var threshold = minY + topFraction * (maxY - minY);

        // Sutherland–Hodgman clip against half-plane y >= threshold.
        var result = new List<Vector2>();

        for (var i = 0; i < _vertices.Length; i++)
        {
            var current = _vertices[i];
            var next = _vertices[(i + 1) % _vertices.Length];
            var currentInside = current.Y >= threshold;
            var nextInside = next.Y >= threshold;

            if (currentInside)
                result.Add(current);

            if (currentInside != nextInside)
            {
                // Edge crosses the threshold — compute intersection.
                var t = (threshold - current.Y) / (next.Y - current.Y);
                result.Add(new Vector2(
                    current.X + t * (next.X - current.X),
                    threshold));
            }
        }

        // If clipping produced fewer than 3 vertices, return the original.
        if (result.Count < 3)
            return this;

        return new PolygonBounds(result.ToArray());
    }
}
