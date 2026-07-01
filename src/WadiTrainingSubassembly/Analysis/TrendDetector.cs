using WadiTraining.Models;

namespace WadiTraining.Analysis;

internal sealed class TrendDetector
{
    public IReadOnlyList<BreakCandidate> Detect(TerrainRun run, WadiParameters parameters)
    {
        if (run.Points.Count < 6)
        {
            return Array.Empty<BreakCandidate>();
        }

        var raw = new List<BreakCandidate>();
        foreach (var point in run.Points)
        {
            var x = point.X;
            if (x < parameters.TrendWindowLength || x > run.Length - parameters.TrendWindowLength)
            {
                continue;
            }

            if (!TryFit(run.Points, x - parameters.TrendWindowLength, x, out var before) ||
                !TryFit(run.Points, x, x + parameters.TrendWindowLength, out var after))
            {
                continue;
            }

            if (before.Rmse > parameters.MaxTrendResidual || after.Rmse > parameters.MaxTrendResidual)
            {
                continue;
            }

            var beforeAbs = Math.Abs(before.Slope);
            var afterAbs = Math.Abs(after.Slope);
            var minimumRatio = 1.0 + parameters.SlopeChangeThreshold;

            if (before.Slope < after.Slope && beforeAbs >= afterAbs * minimumRatio)
            {
                if (before.Length >= parameters.MinSteepTrendLength && after.Length >= parameters.MinMildTrendLength)
                {
                    TryAddCandidate(raw, BreakKind.Concave, before, after, run.Points);
                }
            }
            else if (after.Slope < before.Slope && afterAbs >= beforeAbs * minimumRatio)
            {
                if (after.Length >= parameters.MinSteepTrendLength && before.Length >= parameters.MinMildTrendLength)
                {
                    TryAddCandidate(raw, BreakKind.Convex, before, after, run.Points);
                }
            }
        }

        return SuppressDuplicates(raw, parameters.MinBreakSpacing);
    }

    private static bool TryFit(IReadOnlyList<SectionPoint> points, double startX, double endX, out TrendLine trend)
    {
        var window = points.Where(p => p.X >= startX - 1e-8 && p.X <= endX + 1e-8).ToArray();
        if (window.Length < 3)
        {
            trend = default;
            return false;
        }

        var n = window.Length;
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
        trend = new TrendLine(slope, intercept, window[0].X, window[^1].X, rmse);
        return true;
    }

    private static void TryAddCandidate(
        List<BreakCandidate> candidates,
        BreakKind kind,
        TrendLine before,
        TrendLine after,
        IReadOnlyList<SectionPoint> surface)
    {
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
        var slopeDelta = Math.Abs(before.Slope - after.Slope);
        var score = slopeDelta / Math.Max(0.001, before.Rmse + after.Rmse + 0.001);
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
