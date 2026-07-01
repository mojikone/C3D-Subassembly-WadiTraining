using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using WadiTraining.Models;
using WadiTraining.Runtime;
using CivilSurface = Autodesk.Civil.DatabaseServices.Surface;
using ModelSectionPoint = WadiTraining.Models.SectionPoint;

namespace WadiTraining.Terrain;

internal sealed class TerrainSampler
{
    private readonly CivilRuntime _runtime;
    private readonly ObjectId _surfaceId;
    private readonly CivilSurface _surface;
    private readonly Alignment _alignment;

    public TerrainSampler(CivilRuntime runtime, ObjectId surfaceId)
    {
        _runtime = runtime;
        _surfaceId = surfaceId;
        var transactionManager = HostApplicationServices.WorkingDatabase.TransactionManager;
        _surface = (CivilSurface)transactionManager.GetObject(surfaceId, OpenMode.ForRead, false, false);
        _alignment = (Alignment)transactionManager.GetObject(runtime.AlignmentId, OpenMode.ForRead, false, false);
    }

    public bool TryGetGroundPoint(double localOffset, out ModelSectionPoint point)
    {
        try
        {
            var absoluteOffset = _runtime.OriginOffset + localOffset;
            double east = 0.0;
            double north = 0.0;
            _alignment.PointLocation(_runtime.CurrentStation, absoluteOffset, ref east, ref north);
            var elevation = _surface.FindElevationAtXY(east, north);
            point = new ModelSectionPoint(localOffset, elevation - _runtime.OriginElevation);
            return true;
        }
        catch
        {
            point = default;
            return false;
        }
    }

    public bool TryFindDaylight(
        double crownLocalOffset,
        int direction,
        double sideSlope,
        double maxDistance,
        double step,
        out ModelSectionPoint toe)
    {
        toe = default;
        var safeStep = Math.Max(0.1, step);
        if (!TryDifference(crownLocalOffset, crownLocalOffset, sideSlope, out var previousDifference))
        {
            previousDifference = double.PositiveInfinity;
        }

        var previousOffset = crownLocalOffset;
        for (var distance = safeStep; distance <= maxDistance; distance += safeStep)
        {
            var currentOffset = crownLocalOffset + direction * distance;
            if (!TryDifference(crownLocalOffset, currentOffset, sideSlope, out var currentDifference))
            {
                previousOffset = currentOffset;
                continue;
            }

            if (currentDifference <= 0.0 || Math.Sign(currentDifference) != Math.Sign(previousDifference))
            {
                toe = RefineDaylight(crownLocalOffset, previousOffset, currentOffset, sideSlope);
                return true;
            }

            previousOffset = currentOffset;
            previousDifference = currentDifference;
        }

        var fallbackOffset = crownLocalOffset + direction * maxDistance;
        return TryGetGroundPoint(fallbackOffset, out toe);
    }

    public TerrainRun BuildRun(double toeLocalOffset, int direction, double maxScanDistance, double interval, double? thalwegLocalOffset)
    {
        var stopLocalOffset = toeLocalOffset + direction * maxScanDistance;
        if (thalwegLocalOffset.HasValue)
        {
            var targetDistance = direction * (thalwegLocalOffset.Value - toeLocalOffset);
            if (targetDistance > 0.0)
            {
                stopLocalOffset = toeLocalOffset + direction * Math.Min(targetDistance, maxScanDistance);
            }
        }

        var length = Math.Abs(stopLocalOffset - toeLocalOffset);
        var points = new List<ModelSectionPoint>();
        var safeInterval = Math.Max(0.05, interval);
        for (var x = 0.0; x <= length + 1e-8; x += safeInterval)
        {
            var clampedX = Math.Min(x, length);
            if (TryGetGroundPoint(toeLocalOffset + direction * clampedX, out var localPoint))
            {
                points.Add(new ModelSectionPoint(clampedX, localPoint.Y));
            }
        }

        if (points.Count == 0 || Math.Abs(points[^1].X - length) > 1e-6)
        {
            if (TryGetGroundPoint(stopLocalOffset, out var localPoint))
            {
                points.Add(new ModelSectionPoint(length, localPoint.Y));
            }
        }

        return new TerrainRun(direction, toeLocalOffset, stopLocalOffset, points);
    }

    public double? GetThalwegLocalOffset()
    {
        if (!_runtime.TryGetOffsetTarget(TargetNames.ThalwegOffset, out var target))
        {
            return null;
        }

        try
        {
            double x = 0.0;
            double y = 0.0;
            var absoluteOffset = target.GetDistanceToAlignment(_runtime.AlignmentId, _runtime.CurrentStation, ref x, ref y);
            return absoluteOffset - _runtime.OriginOffset;
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<ModelSectionPoint> SampleCivilSurface(TerrainRun run, double startX, double endX, double fallbackStep)
    {
        var start = Math.Clamp(Math.Min(startX, endX), 0.0, run.Length);
        var end = Math.Clamp(Math.Max(startX, endX), 0.0, run.Length);
        if (end - start < 1e-6)
        {
            return Array.Empty<ModelSectionPoint>();
        }

        try
        {
            var startLocalOffset = run.ToLocalOffset(start);
            var endLocalOffset = run.ToLocalOffset(end);
            if (!TryGetGroundPoint(startLocalOffset, out var startLocal) ||
                !TryGetGroundPoint(endLocalOffset, out var endLocal))
            {
                return SampleFromAnalysis(run, start, end, fallbackStep);
            }

            var startPoint = _runtime.State.Points.Add(startLocalOffset, startLocal.Y, "");
            var endPoint = _runtime.State.Points.Add(endLocalOffset, endLocal.Y, "");
            startPoint.IsHidden = true;
            endPoint.IsHidden = true;

            var sampledLinks = _runtime.State.SampleSection(_surfaceId, _runtime.AlignmentId, startPoint, endPoint);
            var points = new List<ModelSectionPoint>();
            foreach (var link in sampledLinks)
            {
                AddSamplePoint(points, run, link.StartPointOffset, link.StartPointElevation);
                AddSamplePoint(points, run, link.EndPointOffset, link.EndPointElevation);
            }

            return points
                .Where(p => p.X >= start - 1e-6 && p.X <= end + 1e-6)
                .OrderBy(p => p.X)
                .DistinctBy(p => Math.Round(p.X, 6))
                .ToArray();
        }
        catch
        {
            return SampleFromAnalysis(run, start, end, fallbackStep);
        }
    }

    private IReadOnlyList<ModelSectionPoint> SampleFromAnalysis(TerrainRun run, double start, double end, double step)
    {
        var points = new List<ModelSectionPoint>();
        var safeStep = Math.Max(0.05, step);
        for (var x = start; x <= end + 1e-8; x += safeStep)
        {
            var clamped = Math.Min(x, end);
            if (TryGetGroundPoint(run.ToLocalOffset(clamped), out var localPoint))
            {
                points.Add(new ModelSectionPoint(clamped, localPoint.Y));
            }
        }

        if (points.Count == 0 || Math.Abs(points[^1].X - end) > 1e-6)
        {
            if (TryGetGroundPoint(run.ToLocalOffset(end), out var localPoint))
            {
                points.Add(new ModelSectionPoint(end, localPoint.Y));
            }
        }

        return points;
    }

    private void AddSamplePoint(List<ModelSectionPoint> points, TerrainRun run, double reportedOffset, double reportedElevation)
    {
        var localOffset = Math.Abs(reportedOffset - run.ToLocalOffset(0.0)) <= run.Length + 10.0
            ? reportedOffset
            : reportedOffset - _runtime.OriginOffset;
        var x = run.Direction * (localOffset - run.ToeLocalOffset);
        if (TryGetGroundPoint(localOffset, out var ground))
        {
            points.Add(new ModelSectionPoint(x, ground.Y));
            return;
        }

        points.Add(new ModelSectionPoint(x, reportedElevation - _runtime.OriginElevation));
    }

    private bool TryDifference(double crownOffset, double localOffset, double sideSlope, out double difference)
    {
        if (!TryGetGroundPoint(localOffset, out var ground))
        {
            difference = double.NaN;
            return false;
        }

        var designY = -sideSlope * Math.Abs(localOffset - crownOffset);
        difference = designY - ground.Y;
        return true;
    }

    private ModelSectionPoint RefineDaylight(double crownOffset, double lowOffset, double highOffset, double sideSlope)
    {
        for (var i = 0; i < 40; i++)
        {
            var mid = 0.5 * (lowOffset + highOffset);
            if (!TryDifference(crownOffset, mid, sideSlope, out var midDifference))
            {
                break;
            }

            if (midDifference > 0.0)
            {
                lowOffset = mid;
            }
            else
            {
                highOffset = mid;
            }
        }

        var resultOffset = 0.5 * (lowOffset + highOffset);
        if (TryGetGroundPoint(resultOffset, out var point))
        {
            return point;
        }

        return new ModelSectionPoint(resultOffset, -sideSlope * Math.Abs(resultOffset - crownOffset));
    }
}
