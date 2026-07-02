// Reads existing ground from the Civil 3D target surface and converts it into the run
// coordinates the Core algorithm uses.
//
// Two coordinate frames appear here — do not mix them up when debugging:
//   LOCAL:  subassembly frame. Offset relative to the attachment origin (levee crown),
//           elevation relative to origin elevation. LocalPoint(Offset, Dy).
//   RUN:    analysis frame. X = distance from the wadi-side toe toward the wadi (always
//           positive), Y = elevation relative to origin. TerrainPoint(X, Y).
// TerrainRun converts between the two.

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using WadiTrend.Core;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;

namespace WadiTrend.Civil;

/// <summary>A point in the subassembly's local frame (offset from origin, elevation above origin).</summary>
public readonly record struct LocalPoint(double Offset, double Dy);

/// <summary>The analysis run from the wadi toe toward the wadi, with frame conversion.</summary>
public sealed record TerrainRun(int Direction, double ToeLocalOffset, double StopLocalOffset, IReadOnlyList<TerrainPoint> Points)
{
    public double Length => Math.Abs(StopLocalOffset - ToeLocalOffset);

    public double ToLocalOffset(double runX) => ToeLocalOffset + Direction * runX;

    public double ToRunX(double localOffset) => Direction * (localOffset - ToeLocalOffset);

    public LocalPoint ToLocal(TerrainPoint p) => new(ToLocalOffset(p.X), p.Y);
}

public sealed class TerrainSampler
{
    private readonly CorridorContext _context;
    private readonly ObjectId _surfaceId;
    private readonly CivilSurface _surface;
    private readonly Alignment _alignment;

    public TerrainSampler(CorridorContext context, ObjectId surfaceId)
    {
        _context = context;
        _surfaceId = surfaceId;
        var transactionManager = HostApplicationServices.WorkingDatabase.TransactionManager;
        _surface = (CivilSurface)transactionManager.GetObject(surfaceId, OpenMode.ForRead, false, false);
        _alignment = (Alignment)transactionManager.GetObject(context.AlignmentId, OpenMode.ForRead, false, false);
    }

    /// <summary>Ground elevation under a local offset. False where the surface has no data.</summary>
    public bool TryGetGround(double localOffset, out LocalPoint point)
    {
        try
        {
            var absoluteOffset = _context.OriginOffset + localOffset;
            double east = 0.0, north = 0.0;
            _alignment.PointLocation(_context.CurrentStation, absoluteOffset, ref east, ref north);
            var elevation = _surface.FindElevationAtXY(east, north);
            point = new LocalPoint(localOffset, elevation - _context.OriginElevation);
            return true;
        }
        catch
        {
            point = default;
            return false;
        }
    }

    /// <summary>
    /// Walks the levee face from the crown at <paramref name="crownLocalOffset"/> in
    /// <paramref name="direction"/> at grade <paramref name="sideSlope"/> until it daylights
    /// into the ground, then refines by bisection. Falls back to ground at max distance.
    /// </summary>
    public bool TryFindDaylight(
        double crownLocalOffset,
        int direction,
        double sideSlope,
        double maxDistance,
        double step,
        out LocalPoint toe)
    {
        toe = default;
        var safeStep = Math.Max(0.1, step);
        if (!TryFaceGroundGap(crownLocalOffset, crownLocalOffset, sideSlope, out var previousGap))
        {
            previousGap = double.PositiveInfinity;
        }

        var previousOffset = crownLocalOffset;
        for (var distance = safeStep; distance <= maxDistance; distance += safeStep)
        {
            var currentOffset = crownLocalOffset + direction * distance;
            if (!TryFaceGroundGap(crownLocalOffset, currentOffset, sideSlope, out var currentGap))
            {
                previousOffset = currentOffset;
                continue;
            }

            if (currentGap <= 0.0 || Math.Sign(currentGap) != Math.Sign(previousGap))
            {
                toe = RefineDaylight(crownLocalOffset, previousOffset, currentOffset, sideSlope);
                return true;
            }

            previousOffset = currentOffset;
            previousGap = currentGap;
        }

        var fallbackOffset = crownLocalOffset + direction * maxDistance;
        return TryGetGround(fallbackOffset, out toe);
    }

    /// <summary>
    /// Builds the analysis run from the wadi toe toward the wadi, sampled at
    /// <paramref name="interval"/>. Stops at the scan-limit offset when provided,
    /// otherwise at <paramref name="maxScanDistance"/>.
    /// </summary>
    public TerrainRun BuildRun(double toeLocalOffset, int direction, double maxScanDistance, double interval, double? stopLocalOffset)
    {
        var stop = toeLocalOffset + direction * maxScanDistance;
        if (stopLocalOffset.HasValue)
        {
            var towardStop = direction * (stopLocalOffset.Value - toeLocalOffset);
            if (towardStop > 0.0)
            {
                stop = toeLocalOffset + direction * Math.Min(towardStop, maxScanDistance);
            }
        }

        var length = Math.Abs(stop - toeLocalOffset);
        var safeInterval = Math.Max(0.05, interval);
        var points = new List<TerrainPoint>();
        for (var x = 0.0; x <= length + 1e-9; x += safeInterval)
        {
            var clamped = Math.Min(x, length);
            if (TryGetGround(toeLocalOffset + direction * clamped, out var ground))
            {
                points.Add(new TerrainPoint(clamped, ground.Dy));
            }
        }

        if (points.Count == 0 || Math.Abs(points[^1].X - length) > 1e-6)
        {
            if (TryGetGround(stop, out var ground))
            {
                points.Add(new TerrainPoint(length, ground.Dy));
            }
        }

        return new TerrainRun(direction, toeLocalOffset, stop, points);
    }

    /// <summary>Local offset of the optional scan-limit (thalweg) target, when assigned.</summary>
    public double? GetScanLimitLocalOffset()
    {
        if (!_context.TryGetOffsetTarget(WadiTargets.ScanLimitOffset, out var target))
        {
            return null;
        }

        try
        {
            double x = 0.0, y = 0.0;
            var absoluteOffset = target.GetDistanceToAlignment(_context.AlignmentId, _context.CurrentStation, ref x, ref y);
            return absoluteOffset - _context.OriginOffset;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Points for drawing ground-following links between run stations startX..endX.
    /// Union of (a) TIN crossings from CorridorState.SampleSection — exact surface bends —
    /// and (b) interval samples, which guarantee a maximum vertex spacing even where
    /// SampleSection reports sparse or incomplete crossings. Result: drawn links follow
    /// the actual surface within the sample interval everywhere, usually exactly.
    /// </summary>
    public IReadOnlyList<TerrainPoint> SampleSurfaceRun(TerrainRun run, double startX, double endX, double fallbackStep)
    {
        var start = Math.Clamp(Math.Min(startX, endX), 0.0, run.Length);
        var end = Math.Clamp(Math.Max(startX, endX), 0.0, run.Length);
        if (end - start < 1e-6)
        {
            return Array.Empty<TerrainPoint>();
        }

        var points = new List<TerrainPoint>();
        try
        {
            points.AddRange(SampleTinCrossings(run, start, end));
        }
        catch
        {
            // SampleSection unavailable; interval samples below still cover the span.
        }

        points.AddRange(SampleByInterval(run, start, end, fallbackStep));
        return points
            .Where(p => p.X >= start - 1e-6 && p.X <= end + 1e-6)
            .OrderBy(p => p.X)
            .GroupBy(p => Math.Round(p.X, 3))
            .Select(g => g.First())
            .ToArray();
    }

    private IEnumerable<TerrainPoint> SampleTinCrossings(TerrainRun run, double start, double end)
    {
        var startOffset = run.ToLocalOffset(start);
        var endOffset = run.ToLocalOffset(end);
        if (!TryGetGround(startOffset, out var startGround) || !TryGetGround(endOffset, out var endGround))
        {
            return Array.Empty<TerrainPoint>();
        }

        var startPoint = _context.State.Points.Add(startOffset, startGround.Dy, "");
        var endPoint = _context.State.Points.Add(endOffset, endGround.Dy, "");
        startPoint.IsHidden = true;
        endPoint.IsHidden = true;

        var sampledLinks = _context.State.SampleSection(_surfaceId, _context.AlignmentId, startPoint, endPoint);
        var points = new List<TerrainPoint>();
        foreach (var link in sampledLinks)
        {
            AddSampledPoint(points, run, link.StartPointOffset, link.StartPointElevation);
            AddSampledPoint(points, run, link.EndPointOffset, link.EndPointElevation);
        }

        return points;
    }

    private IReadOnlyList<TerrainPoint> SampleByInterval(TerrainRun run, double start, double end, double step)
    {
        var points = new List<TerrainPoint>();
        var safeStep = Math.Max(0.05, step);
        for (var x = start; x <= end + 1e-9; x += safeStep)
        {
            var clamped = Math.Min(x, end);
            if (TryGetGround(run.ToLocalOffset(clamped), out var ground))
            {
                points.Add(new TerrainPoint(clamped, ground.Dy));
            }
        }

        if (points.Count == 0 || Math.Abs(points[^1].X - end) > 1e-6)
        {
            if (TryGetGround(run.ToLocalOffset(end), out var ground))
            {
                points.Add(new TerrainPoint(end, ground.Dy));
            }
        }

        return points;
    }

    // SampleSection has reported offsets either local or absolute depending on context;
    // resolve by re-reading the ground at the interpreted offset (same heuristic Codex shipped).
    private void AddSampledPoint(List<TerrainPoint> points, TerrainRun run, double reportedOffset, double reportedElevation)
    {
        var localOffset = Math.Abs(reportedOffset - run.ToLocalOffset(0.0)) <= run.Length + 10.0
            ? reportedOffset
            : reportedOffset - _context.OriginOffset;
        var x = run.ToRunX(localOffset);
        if (TryGetGround(localOffset, out var ground))
        {
            points.Add(new TerrainPoint(x, ground.Dy));
            return;
        }

        points.Add(new TerrainPoint(x, reportedElevation - _context.OriginElevation));
    }

    /// <summary>Vertical gap between the levee face design line and the ground at an offset (+ = face above ground).</summary>
    private bool TryFaceGroundGap(double crownOffset, double localOffset, double sideSlope, out double gap)
    {
        if (!TryGetGround(localOffset, out var ground))
        {
            gap = double.NaN;
            return false;
        }

        var faceDy = -sideSlope * Math.Abs(localOffset - crownOffset);
        gap = faceDy - ground.Dy;
        return true;
    }

    private LocalPoint RefineDaylight(double crownOffset, double lowOffset, double highOffset, double sideSlope)
    {
        for (var i = 0; i < 40; i++)
        {
            var mid = 0.5 * (lowOffset + highOffset);
            if (!TryFaceGroundGap(crownOffset, mid, sideSlope, out var midGap))
            {
                break;
            }

            if (midGap > 0.0)
            {
                lowOffset = mid;
            }
            else
            {
                highOffset = mid;
            }
        }

        var offset = 0.5 * (lowOffset + highOffset);
        if (TryGetGround(offset, out var point))
        {
            return point;
        }

        return new LocalPoint(offset, -sideSlope * Math.Abs(offset - crownOffset));
    }
}
