using WadiTraining.Models;

namespace WadiTraining.Analysis;

internal sealed class TrendDetector
{
    public IReadOnlyList<BreakCandidate> Detect(TerrainRun run, WadiParameters parameters)
    {
        var points = Clean(run.Points);
        if (points.Count < 3)
        {
            return Array.Empty<BreakCandidate>();
        }

        var anchors = Simplify(points, parameters.MaxTrendResidual);
        if (anchors.Count < 3)
        {
            return Array.Empty<BreakCandidate>();
        }

        var raw = new List<BreakCandidate>();
        for (var i = 1; i < anchors.Count - 1; i++)
        {
            var breakX = anchors[i].X;
            var beforeStart = Math.Max(anchors[i - 1].X, breakX - parameters.TrendWindowLength);
            var afterEnd = Math.Min(anchors[i + 1].X, breakX + parameters.TrendWindowLength);
            if (!TryFit(points, beforeStart, breakX, out var before) ||
                !TryFit(points, breakX, afterEnd, out var after))
            {
                continue;
            }

            TryAddCandidate(raw, before, after, points, parameters);
        }

        return SuppressDuplicates(raw, parameters.MinBreakSpacing);
    }

    private static bool TryFit(IReadOnlyList<SectionPoint> points, double startX, double endX, out TrendLine trend)
    {
        var start = Math.Min(startX, endX);
        var end = Math.Max(startX, endX);
        var window = Window(points, start, end);
        if (window.Count < 2)
        {
            trend = default;
            return false;
        }

        var n = window.Count;
        var sumX = window.Sum(p => p.X);
        var sumY = window.Sum(p => p.Y);
        var sumXX = window.Sum(p => p.X * p.X);
        var sumXY = window.Sum(p => p.X * p.Y);
        var denominator = n * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 1e-12)
        {
            trend = default;
            return false;
        }

        var slope = (n * sumXY - sumX * sumY) / denominator;
        var intercept = (sumY - slope * sumX) / n;
        var rmse = Math.Sqrt(window.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2.0)) / n);
        trend = new TrendLine(slope, intercept, start, end, rmse);
        return true;
    }

    private static void TryAddCandidate(
        List<BreakCandidate> candidates,
        TrendLine before,
        TrendLine after,
        IReadOnlyList<SectionPoint> surface,
        WadiParameters parameters)
    {
        if (before.Rmse > parameters.MaxTrendResidual || after.Rmse > parameters.MaxTrendResidual)
        {
            return;
        }

        var beforeAbs = Math.Abs(before.Slope);
        var afterAbs = Math.Abs(after.Slope);
        var slopeDelta = Math.Abs(before.Slope - after.Slope);
        if (slopeDelta < parameters.SlopeChangeThreshold)
        {
            return;
        }

        BreakKind kind;
        if (before.Slope < after.Slope &&
            before.Slope < -1e-6 &&
            beforeAbs > afterAbs &&
            before.Length >= parameters.MinSteepTrendLength &&
            after.Length >= parameters.MinMildTrendLength)
        {
            kind = BreakKind.Concave;
        }
        else if (after.Slope < before.Slope &&
                 after.Slope < -1e-6 &&
                 afterAbs > beforeAbs &&
                 before.Length >= parameters.MinMildTrendLength &&
                 after.Length >= parameters.MinSteepTrendLength)
        {
            kind = BreakKind.Convex;
        }
        else
        {
            return;
        }

        var denominator = before.Slope - after.Slope;
        if (Math.Abs(denominator) < 1e-9)
        {
            return;
        }

        var x = (after.Intercept - before.Intercept) / denominator;
        if (x < before.StartX - 1e-6 || x > after.EndX + 1e-6)
        {
            return;
        }

        var y = InterpolateY(surface, x);
        var support = Math.Sqrt(Math.Max(0.001, before.Length * after.Length));
        var score = slopeDelta * support / Math.Max(0.001, before.Rmse + after.Rmse + 0.001);
        candidates.Add(new BreakCandidate(kind, new SectionPoint(x, y), before, after, score));
    }

    private static IReadOnlyList<BreakCandidate> SuppressDuplicates(IReadOnlyList<BreakCandidate> candidates, double spacing)
    {
        var result = new List<BreakCandidate>();
        foreach (var group in candidates.GroupBy(c => c.Kind))
        {
            foreach (var candidate in group.OrderByDescending(c => c.Score))
            {
                if (result.Where(existing => existing.Kind == candidate.Kind)
                    .All(existing => Math.Abs(existing.Point.X - candidate.Point.X) > spacing))
                {
                    result.Add(candidate);
                }
            }
        }

        return result.OrderBy(c => c.Point.X).ToArray();
    }

    private static IReadOnlyList<SectionPoint> Clean(IReadOnlyList<SectionPoint> points)
    {
        return points
            .Where(p => double.IsFinite(p.X) && double.IsFinite(p.Y))
            .OrderBy(p => p.X)
            .GroupBy(p => Math.Round(p.X, 6))
            .Select(g => g.First())
            .ToArray();
    }

    private static IReadOnlyList<SectionPoint> Simplify(IReadOnlyList<SectionPoint> points, double tolerance)
    {
        var safeTolerance = Math.Max(0.005, tolerance);
        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifyRange(points, 0, points.Count - 1, safeTolerance, keep);
        return points.Where((_, index) => keep[index]).ToArray();
    }

    private static void SimplifyRange(IReadOnlyList<SectionPoint> points, int start, int end, double tolerance, bool[] keep)
    {
        if (end <= start + 1)
        {
            return;
        }

        var maxDistance = 0.0;
        var split = -1;
        for (var i = start + 1; i < end; i++)
        {
            var distance = DistanceToSegment(points[i], points[start], points[end]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                split = i;
            }
        }

        if (split < 0 || maxDistance <= tolerance)
        {
            return;
        }

        keep[split] = true;
        SimplifyRange(points, start, split, tolerance, keep);
        SimplifyRange(points, split, end, tolerance, keep);
    }

    private static double DistanceToSegment(SectionPoint point, SectionPoint a, SectionPoint b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (Math.Abs(dx) < 1e-12 && Math.Abs(dy) < 1e-12)
        {
            return Distance(point, a);
        }

        var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0.0, 1.0);
        var projected = new SectionPoint(a.X + t * dx, a.Y + t * dy);
        return Distance(point, projected);
    }

    private static double Distance(SectionPoint a, SectionPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static IReadOnlyList<SectionPoint> Window(IReadOnlyList<SectionPoint> points, double start, double end)
    {
        var window = new List<SectionPoint> { PointAtX(points, start) };
        window.AddRange(points.Where(p => p.X > start + 1e-8 && p.X < end - 1e-8));
        window.Add(PointAtX(points, end));
        return window
            .OrderBy(p => p.X)
            .GroupBy(p => Math.Round(p.X, 6))
            .Select(g => g.First())
            .ToArray();
    }

    private static SectionPoint PointAtX(IReadOnlyList<SectionPoint> points, double x)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1];
            var current = points[i];
            if (x >= previous.X - 1e-8 && x <= current.X + 1e-8)
            {
                return SectionPoint.Lerp(previous, current, x);
            }
        }

        return points.OrderBy(p => Math.Abs(p.X - x)).First();
    }

    private static double InterpolateY(IReadOnlyList<SectionPoint> points, double x)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1];
            var current = points[i];
            if (x >= previous.X - 1e-8 && x <= current.X + 1e-8)
            {
                return SectionPoint.Lerp(previous, current, x).Y;
            }
        }

        return points.OrderBy(p => Math.Abs(p.X - x)).First().Y;
    }
}
