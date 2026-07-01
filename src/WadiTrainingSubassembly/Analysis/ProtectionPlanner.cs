using WadiTraining.Models;

namespace WadiTraining.Analysis;

internal sealed class ProtectionPlanner
{
    public IReadOnlyList<ProtectionInterval> BuildIntervals(
        TerrainRun run,
        IReadOnlyList<BreakCandidate> breaks,
        WadiParameters parameters)
    {
        var intervals = new List<ProtectionInterval>();

        if (parameters.ToeScourLength > 0.0)
        {
            intervals.Add(new ProtectionInterval(0.0, XAtSurfaceDistance(run.Points, 0.0, parameters.ToeScourLength, 1), "WT_ToeScour"));
        }

        if (parameters.ToeApronLength > 0.0)
        {
            var apronStart = intervals.Count > 0 ? intervals[^1].EndX : 0.0;
            intervals.Add(new ProtectionInterval(apronStart, XAtSurfaceDistance(run.Points, apronStart, parameters.ToeApronLength, 1), "WT_ToeApron"));
        }

        foreach (var candidate in breaks.Where(b => b.Kind == BreakKind.Concave))
        {
            var steepLength = Math.Min(parameters.MaxSteepProtectionLength, candidate.Before.Length);
            var start = XAtSurfaceDistance(run.Points, candidate.Point.X, steepLength, -1);
            var end = XAtSurfaceDistance(run.Points, candidate.Point.X, parameters.MildProtectionLength, 1);
            intervals.Add(new ProtectionInterval(start, end, "WT_Protection"));
        }

        return Merge(intervals.Select(i => i.Normalize()), parameters.MergeDistance, run.Length);
    }

    private static IReadOnlyList<ProtectionInterval> Merge(IEnumerable<ProtectionInterval> intervals, double mergeDistance, double runLength)
    {
        var ordered = intervals
            .Select(i => new ProtectionInterval(Math.Clamp(i.StartX, 0.0, runLength), Math.Clamp(i.EndX, 0.0, runLength), i.Code).Normalize())
            .Where(i => i.EndX - i.StartX > 1e-6)
            .OrderBy(i => i.StartX)
            .ToArray();
        if (ordered.Length == 0)
        {
            return Array.Empty<ProtectionInterval>();
        }

        var merged = new List<ProtectionInterval> { ordered[0] };
        foreach (var interval in ordered.Skip(1))
        {
            var previous = merged[^1];
            if (interval.StartX - previous.EndX <= mergeDistance)
            {
                var code = previous.Code == interval.Code ? previous.Code : "WT_Protection";
                merged[^1] = new ProtectionInterval(previous.StartX, Math.Max(previous.EndX, interval.EndX), code);
            }
            else
            {
                merged.Add(interval);
            }
        }

        return merged;
    }

    private static double XAtSurfaceDistance(IReadOnlyList<SectionPoint> points, double originX, double distance, int direction)
    {
        if (points.Count < 2 || distance <= 0.0)
        {
            return originX;
        }

        var ordered = points.OrderBy(p => p.X).ToArray();
        var current = ClampX(ordered, originX);
        var remaining = distance;

        while (remaining > 1e-8)
        {
            var segment = FindSegment(ordered, current, direction);
            if (segment == null)
            {
                return current;
            }

            var (a, b) = segment.Value;
            var next = direction > 0 ? b : a;
            var currentPoint = PointAtX(a, b, current);
            var segmentLength = Distance(currentPoint, next);
            if (segmentLength >= remaining)
            {
                var ratio = remaining / segmentLength;
                return current + direction * Math.Abs(next.X - current) * ratio;
            }

            remaining -= segmentLength;
            current = next.X;
        }

        return current;
    }

    private static (SectionPoint A, SectionPoint B)? FindSegment(IReadOnlyList<SectionPoint> points, double x, int direction)
    {
        if (direction > 0)
        {
            for (var i = 1; i < points.Count; i++)
            {
                if (x >= points[i - 1].X - 1e-8 && x < points[i].X - 1e-8)
                {
                    return (points[i - 1], points[i]);
                }
            }
        }
        else
        {
            for (var i = points.Count - 1; i > 0; i--)
            {
                if (x <= points[i].X + 1e-8 && x > points[i - 1].X + 1e-8)
                {
                    return (points[i - 1], points[i]);
                }
            }
        }

        return null;
    }

    private static SectionPoint PointAtX(SectionPoint a, SectionPoint b, double x)
    {
        return SectionPoint.Lerp(a, b, x);
    }

    private static double Distance(SectionPoint a, SectionPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double ClampX(IReadOnlyList<SectionPoint> points, double x)
    {
        return Math.Clamp(x, points[0].X, points[^1].X);
    }
}
