// Shared surface/geometry math for the algorithm core. Pure C# — no Civil 3D dependency.

namespace WadiTrend.Core;

public static class SurfaceMath
{
    /// <summary>Removes non-finite points, sorts by X, drops duplicate X values.</summary>
    public static IReadOnlyList<TerrainPoint> CleanSort(IEnumerable<TerrainPoint> points)
    {
        return points
            .Where(p => double.IsFinite(p.X) && double.IsFinite(p.Y))
            .OrderBy(p => p.X)
            .GroupBy(p => Math.Round(p.X, 6))
            .Select(g => g.First())
            .ToArray();
    }

    /// <summary>Interpolated ground elevation at <paramref name="x"/>. Clamps outside the data range.</summary>
    public static double ElevationAt(IReadOnlyList<TerrainPoint> points, double x)
    {
        if (points.Count == 0)
        {
            return 0.0;
        }

        if (x <= points[0].X)
        {
            return points[0].Y;
        }

        for (var i = 1; i < points.Count; i++)
        {
            if (x <= points[i].X + 1e-9)
            {
                return TerrainPoint.Lerp(points[i - 1], points[i], x).Y;
            }
        }

        return points[^1].Y;
    }

    /// <summary>
    /// Least-squares linear fit of all points inside [startX, endX], with interpolated points
    /// added at both window edges so short windows still fit reliably.
    /// Returns false when the window holds fewer than 2 distinct points or is degenerate.
    /// </summary>
    public static bool TryFit(IReadOnlyList<TerrainPoint> points, double startX, double endX, out TrendFit fit)
    {
        fit = default;
        var start = Math.Min(startX, endX);
        var end = Math.Max(startX, endX);
        if (points.Count < 2 || end - start < 1e-9)
        {
            return false;
        }

        // Clamp the window to the data range; reject windows entirely outside it.
        start = Math.Max(start, points[0].X);
        end = Math.Min(end, points[^1].X);
        if (end - start < 1e-9)
        {
            return false;
        }

        var window = new List<TerrainPoint> { new(start, ElevationAt(points, start)) };
        window.AddRange(points.Where(p => p.X > start + 1e-9 && p.X < end - 1e-9));
        window.Add(new TerrainPoint(end, ElevationAt(points, end)));

        var n = window.Count;
        double sumX = 0.0, sumY = 0.0, sumXX = 0.0, sumXY = 0.0;
        foreach (var p in window)
        {
            sumX += p.X;
            sumY += p.Y;
            sumXX += p.X * p.X;
            sumXY += p.X * p.Y;
        }

        var denominator = n * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 1e-12)
        {
            return false;
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;
        var sse = window.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2.0));
        fit = new TrendFit(slope, intercept, start, end, Math.Sqrt(sse / n));
        return true;
    }

    /// <summary>
    /// Walks <paramref name="distance"/> measured ALONG the surface (not horizontally) from
    /// <paramref name="originX"/> in <paramref name="direction"/> (+1/-1) and returns the reached X.
    /// Stops at the data boundary when the surface runs out.
    /// </summary>
    public static double XAtSurfaceDistance(IReadOnlyList<TerrainPoint> points, double originX, double distance, int direction)
    {
        if (points.Count < 2 || distance <= 0.0)
        {
            return originX;
        }

        var current = Math.Clamp(originX, points[0].X, points[^1].X);
        var remaining = distance;

        while (remaining > 1e-9)
        {
            var segment = NextSegment(points, current, direction);
            if (segment is null)
            {
                return current;
            }

            var (a, b) = segment.Value;
            var from = TerrainPoint.Lerp(a, b, current);
            var to = direction > 0 ? b : a;
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var segmentLength = Math.Sqrt(dx * dx + dy * dy);
            if (segmentLength >= remaining)
            {
                return current + direction * Math.Abs(dx) * (remaining / segmentLength);
            }

            remaining -= segmentLength;
            current = to.X;
        }

        return current;
    }

    private static (TerrainPoint A, TerrainPoint B)? NextSegment(IReadOnlyList<TerrainPoint> points, double x, int direction)
    {
        if (direction > 0)
        {
            for (var i = 1; i < points.Count; i++)
            {
                if (x >= points[i - 1].X - 1e-9 && x < points[i].X - 1e-9)
                {
                    return (points[i - 1], points[i]);
                }
            }
        }
        else
        {
            for (var i = points.Count - 1; i > 0; i--)
            {
                if (x <= points[i].X + 1e-9 && x > points[i - 1].X + 1e-9)
                {
                    return (points[i - 1], points[i]);
                }
            }
        }

        return null;
    }
}
