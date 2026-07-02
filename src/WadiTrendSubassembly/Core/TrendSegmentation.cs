// Global piecewise-linear trend segmentation. Pure C# - no Civil 3D dependency.
//
// Unlike the W1 sliding-window detector, this fits the WHOLE terrain run with a few long
// straight trend lines (what an engineer would draw by hand), then reads the breaks off
// the intersections of consecutive trends:
//
//   1. Seed segments from a vertical-tolerance simplification of the sampled ground.
//   2. Pass A - greedily merge the adjacent pair whose combined least-squares fit has the
//      lowest RMSE, while that RMSE stays within MaxTrendResidual.
//   3. Pass B - merge adjacent segments whose slopes differ by less than
//      SlopeChangeThreshold (small slope changes never split a trend).
//   4. Pass C - segments shorter than MinTrendLength are absorbed at the run edges or
//      dropped in the interior (rounded transitions between two long trends become a gap;
//      the neighbours are extended to their intersection).
//   Passes repeat until stable.
//
// Junction positions = intersection of the two fitted lines - NOT points on the surface.
// Classification: Concave = steep dropping trend turns milder (protect).
//                 Convex  = milder trend turns into steep drop (debug).

namespace WadiTrend.Core;

public sealed class TrendSegmentationSettings
{
    public double MaxTrendResidual { get; init; } = 0.15;
    public double MinTrendLength { get; init; } = 5.0;

    // 0.05 = 5% grade difference. Wadi morphology is gentle: at the test site the dropping
    // bank trends run at only ~10% against ~3% benches, so a 10% threshold merges them away.
    public double SlopeChangeThreshold { get; init; } = 0.05;
}

/// <summary>Full segmentation result: fitted trends, the junction polyline, classified breaks.</summary>
public sealed record TrendChain(
    IReadOnlyList<TrendFit> Trends,
    IReadOnlyList<TerrainPoint> Joints,
    IReadOnlyList<TerrainBreak> Breaks)
{
    public static readonly TrendChain Empty = new(
        Array.Empty<TrendFit>(), Array.Empty<TerrainPoint>(), Array.Empty<TerrainBreak>());
}

public sealed class TrendSegmenter
{
    // Index range [Start..End] (inclusive) into the cleaned point list. Consecutive segments
    // may leave gaps between them after Pass C (dropped rounded transitions).
    private readonly record struct IndexRange(int Start, int End);

    public TrendChain Segment(IReadOnlyList<TerrainPoint> ground, TrendSegmentationSettings settings)
    {
        var pts = SurfaceMath.CleanSort(ground);
        if (pts.Count < 4)
        {
            return TrendChain.Empty;
        }

        var segments = SeedSegments(pts, Math.Max(0.02, settings.MaxTrendResidual));
        for (var iteration = 0; iteration < 20; iteration++)
        {
            var changed = MergeByResidual(pts, segments, settings.MaxTrendResidual);
            changed |= MergeBySlope(pts, segments, settings.SlopeChangeThreshold);
            changed |= AbsorbShortSegments(pts, segments, settings.MinTrendLength, settings.SlopeChangeThreshold);
            if (!changed)
            {
                break;
            }
        }

        return BuildChain(pts, segments, settings);
    }

    // ----- seeding -----

    private static List<IndexRange> SeedSegments(IReadOnlyList<TerrainPoint> pts, double tolerance)
    {
        var keep = new bool[pts.Count];
        keep[0] = true;
        keep[^1] = true;
        SimplifyByVerticalDeviation(pts, 0, pts.Count - 1, tolerance, keep);

        var anchors = new List<int>();
        for (var i = 0; i < pts.Count; i++)
        {
            if (keep[i])
            {
                anchors.Add(i);
            }
        }

        var segments = new List<IndexRange>();
        for (var i = 1; i < anchors.Count; i++)
        {
            segments.Add(new IndexRange(anchors[i - 1], anchors[i]));
        }

        return segments;
    }

    private static void SimplifyByVerticalDeviation(IReadOnlyList<TerrainPoint> pts, int start, int end, double tolerance, bool[] keep)
    {
        if (end <= start + 1)
        {
            return;
        }

        var a = pts[start];
        var b = pts[end];
        var maxDeviation = 0.0;
        var split = -1;
        for (var i = start + 1; i < end; i++)
        {
            var chordY = TerrainPoint.Lerp(a, b, pts[i].X).Y;
            var deviation = Math.Abs(pts[i].Y - chordY);
            if (deviation > maxDeviation)
            {
                maxDeviation = deviation;
                split = i;
            }
        }

        if (split < 0 || maxDeviation <= tolerance)
        {
            return;
        }

        keep[split] = true;
        SimplifyByVerticalDeviation(pts, start, split, tolerance, keep);
        SimplifyByVerticalDeviation(pts, split, end, tolerance, keep);
    }

    // ----- merge passes -----

    /// <summary>Pass A: repeatedly merge the adjacent pair with the lowest combined RMSE while it fits.</summary>
    private static bool MergeByResidual(IReadOnlyList<TerrainPoint> pts, List<IndexRange> segments, double maxResidual)
    {
        var changed = false;
        while (segments.Count > 1)
        {
            var bestIndex = -1;
            var bestRmse = double.MaxValue;
            for (var i = 0; i < segments.Count - 1; i++)
            {
                if (TryFitRange(pts, segments[i].Start, segments[i + 1].End, out var fit) && fit.Rmse < bestRmse)
                {
                    bestRmse = fit.Rmse;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0 || bestRmse > maxResidual)
            {
                break;
            }

            segments[bestIndex] = new IndexRange(segments[bestIndex].Start, segments[bestIndex + 1].End);
            segments.RemoveAt(bestIndex + 1);
            changed = true;
        }

        return changed;
    }

    /// <summary>Pass B: small slope changes never split a trend - merge slope-similar neighbours.</summary>
    private static bool MergeBySlope(IReadOnlyList<TerrainPoint> pts, List<IndexRange> segments, double threshold)
    {
        var changed = false;
        var i = 0;
        while (i < segments.Count - 1)
        {
            if (TryFitRange(pts, segments[i].Start, segments[i].End, out var left) &&
                TryFitRange(pts, segments[i + 1].Start, segments[i + 1].End, out var right) &&
                Math.Abs(right.Slope - left.Slope) < threshold)
            {
                segments[i] = new IndexRange(segments[i].Start, segments[i + 1].End);
                segments.RemoveAt(i + 1);
                changed = true;
            }
            else
            {
                i++;
            }
        }

        return changed;
    }

    /// <summary>
    /// Pass C: interior segments shorter than MinTrendLength whose slope lies BETWEEN their
    /// neighbours' slopes are rounded transitions - dropped, so the neighbours meet at their
    /// line intersection. Short segments steeper than both neighbours (cliff steps) are KEPT:
    /// they carry a real convex+concave pair. Short edge segments merge into their neighbour.
    /// </summary>
    private static bool AbsorbShortSegments(IReadOnlyList<TerrainPoint> pts, List<IndexRange> segments, double minLength, double slopeTolerance)
    {
        var changed = false;
        var i = 0;
        while (i < segments.Count && segments.Count > 1)
        {
            var length = pts[segments[i].End].X - pts[segments[i].Start].X;
            if (length >= minLength)
            {
                i++;
                continue;
            }

            if (i == 0)
            {
                segments[1] = new IndexRange(segments[0].Start, segments[1].End);
                segments.RemoveAt(0);
                changed = true;
            }
            else if (i == segments.Count - 1)
            {
                segments[i - 1] = new IndexRange(segments[i - 1].Start, segments[i].End);
                segments.RemoveAt(i);
                changed = true;
            }
            else if (IsTransitional(pts, segments[i - 1], segments[i], segments[i + 1], slopeTolerance))
            {
                segments.RemoveAt(i); // rounded transition: becomes a gap between the neighbours
                changed = true;
            }
            else
            {
                i++; // short but real (e.g. cliff step) - keep it
            }
        }

        return changed;
    }

    /// <summary>A short middle segment is a rounded transition when its slope sits between its neighbours'.</summary>
    private static bool IsTransitional(IReadOnlyList<TerrainPoint> pts, IndexRange left, IndexRange middle, IndexRange right, double tolerance)
    {
        if (!TryFitRange(pts, left.Start, left.End, out var l) ||
            !TryFitRange(pts, middle.Start, middle.End, out var m) ||
            !TryFitRange(pts, right.Start, right.End, out var r))
        {
            return true;
        }

        var low = Math.Min(l.Slope, r.Slope) - tolerance;
        var high = Math.Max(l.Slope, r.Slope) + tolerance;
        return m.Slope >= low && m.Slope <= high;
    }

    // ----- output -----

    private TrendChain BuildChain(IReadOnlyList<TerrainPoint> pts, List<IndexRange> segments, TrendSegmentationSettings settings)
    {
        var trends = new List<TrendFit>();
        foreach (var segment in segments)
        {
            if (TryFitRange(pts, segment.Start, segment.End, out var fit))
            {
                trends.Add(fit);
            }
        }

        if (trends.Count == 0)
        {
            return TrendChain.Empty;
        }

        var joints = new List<TerrainPoint> { new(trends[0].StartX, trends[0].YAt(trends[0].StartX)) };
        var breaks = new List<TerrainBreak>();
        var threshold = Math.Max(1e-6, settings.SlopeChangeThreshold);

        for (var i = 0; i < trends.Count - 1; i++)
        {
            var before = trends[i];
            var after = trends[i + 1];
            var junction = Intersect(before, after);
            // Keep the polyline monotonic even when near-parallel lines intersect far away.
            if (junction.X <= joints[^1].X + 1e-6 || junction.X < before.StartX || junction.X > after.EndX)
            {
                var midX = 0.5 * (before.EndX + after.StartX);
                junction = new TerrainPoint(midX, 0.5 * (before.YAt(midX) + after.YAt(midX)));
            }

            joints.Add(junction);

            var delta = after.Slope - before.Slope;
            if (delta >= threshold && before.Slope <= -threshold)
            {
                breaks.Add(new TerrainBreak(BreakKind.Concave, junction.X, junction.Y, before, after, Math.Abs(delta)));
            }
            else if (delta <= -threshold && after.Slope <= -threshold)
            {
                breaks.Add(new TerrainBreak(BreakKind.Convex, junction.X, junction.Y, before, after, Math.Abs(delta)));
            }
        }

        joints.Add(new TerrainPoint(trends[^1].EndX, trends[^1].YAt(trends[^1].EndX)));
        return new TrendChain(trends, joints, breaks);
    }

    private static TerrainPoint Intersect(TrendFit a, TrendFit b)
    {
        var denominator = a.Slope - b.Slope;
        if (Math.Abs(denominator) < 1e-9)
        {
            var midX = 0.5 * (a.EndX + b.StartX);
            return new TerrainPoint(midX, a.YAt(midX));
        }

        var x = (b.Intercept - a.Intercept) / denominator;
        return new TerrainPoint(x, a.YAt(x));
    }

    private static bool TryFitRange(IReadOnlyList<TerrainPoint> pts, int start, int end, out TrendFit fit)
    {
        return SurfaceMath.TryFit(pts, pts[start].X, pts[end].X, out fit);
    }
}
