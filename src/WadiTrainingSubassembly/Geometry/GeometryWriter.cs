using Autodesk.Civil.DatabaseServices;
using WadiTraining.Models;
using WadiTraining.Runtime;
using WadiTraining.Terrain;
using ModelSectionPoint = WadiTraining.Models.SectionPoint;

namespace WadiTraining.Geometry;

internal sealed class GeometryWriter
{
    private readonly CivilRuntime _runtime;
    private readonly TerrainSampler? _sampler;

    public GeometryWriter(CivilRuntime runtime, TerrainSampler? sampler)
    {
        _runtime = runtime;
        _sampler = sampler;
    }

    public void DrawBankLevee(double crownLocalOffset, int scanDirection, WadiParameters parameters, ModelSectionPoint wadiToe, ModelSectionPoint landToe)
    {
        var crown = AddPoint(crownLocalOffset, 0.0, "WT_CrownPoint");
        var landCrownOffset = crownLocalOffset - scanDirection * parameters.CrownWidth;
        var landCrown = AddPoint(landCrownOffset, 0.0, "WT_CrownPoint");
        var wadiToePoint = AddPoint(wadiToe.X, wadiToe.Y, "WT_WadiToe");
        var landToePoint = AddPoint(landToe.X, landToe.Y, "WT_LandToe");

        _runtime.State.Links.Add(crown, landCrown, GeometryCodes.Crown);
        _runtime.State.Links.Add(crown, wadiToePoint, GeometryCodes.WadiFace);
        _runtime.State.Links.Add(landCrown, landToePoint, GeometryCodes.LandFace);
    }

    public void DrawTerrainRun(TerrainRun run, IReadOnlyList<ProtectionInterval> intervals, WadiParameters parameters)
    {
        var current = 0.0;
        foreach (var interval in intervals.OrderBy(i => i.StartX))
        {
            if (interval.StartX > current + 1e-6)
            {
                DrawSurfacePolyline(run, current, interval.StartX, GeometryCodes.Surface, parameters.AnalysisSampleInterval);
            }

            DrawSurfacePolyline(run, interval.StartX, interval.EndX, MapProtectionCode(interval.Code), parameters.AnalysisSampleInterval);
            current = Math.Max(current, interval.EndX);
        }

        if (current < run.Length - 1e-6)
        {
            DrawSurfacePolyline(run, current, run.Length, GeometryCodes.Surface, parameters.AnalysisSampleInterval);
        }
    }

    public void DrawBreakMarkers(TerrainRun run, IReadOnlyList<BreakCandidate> breaks, WadiParameters parameters)
    {
        foreach (var candidate in breaks)
        {
            if (candidate.Kind == BreakKind.Convex && !parameters.ShowConvexMarkers)
            {
                continue;
            }

            var local = run.ToLocalPoint(candidate.Point);
            DrawDiamond(local.X, local.Y, parameters.BreakMarkerSize,
                candidate.Kind == BreakKind.Concave ? GeometryCodes.ConcaveMarker : GeometryCodes.ConvexMarker);
        }
    }

    public void DrawLayoutPreview(WadiParameters parameters)
    {
        var direction = parameters.BankMode == BankMode.Left ? -1 : 1;
        var crown = AddPoint(0.0, 0.0, "WT_CrownPoint");
        var landCrown = AddPoint(-direction * parameters.CrownWidth, 0.0, "WT_CrownPoint");
        var wadiToe = AddPoint(direction * 10.0, -10.0 * parameters.LeveeSideSlope, "WT_WadiToe");
        var landToe = AddPoint(-direction * (parameters.CrownWidth + 10.0), -10.0 * parameters.LeveeSideSlope, "WT_LandToe");
        _runtime.State.Links.Add(crown, landCrown, GeometryCodes.Crown);
        _runtime.State.Links.Add(crown, wadiToe, GeometryCodes.WadiFace);
        _runtime.State.Links.Add(landCrown, landToe, GeometryCodes.LandFace);
    }

    private void DrawSurfacePolyline(TerrainRun run, double startX, double endX, string code, double fallbackStep)
    {
        if (_sampler == null)
        {
            return;
        }

        var surfacePoints = _sampler.SampleCivilSurface(run, startX, endX, fallbackStep);
        if (surfacePoints.Count < 2)
        {
            return;
        }

        var civilPoints = surfacePoints
            .Select(run.ToLocalPoint)
            .Select(p => AddPoint(p.X, p.Y, ""))
            .Cast<IPoint>()
            .ToArray();

        if (civilPoints.Length >= 2)
        {
            _runtime.State.Links.Add(civilPoints, code);
        }
    }

    private void DrawDiamond(double x, double y, double size, string code)
    {
        var half = Math.Max(0.05, size) * 0.5;
        var top = AddPoint(x, y + half, code);
        var right = AddPoint(x + half, y, code);
        var bottom = AddPoint(x, y - half, code);
        var left = AddPoint(x - half, y, code);
        _runtime.State.Links.Add(new IPoint[] { top, right, bottom, left, top }, code);
    }

    private Point AddPoint(double offset, double elevation, string code)
    {
        return _runtime.State.Points.Add(offset, elevation, code);
    }

    private static string MapProtectionCode(string code)
    {
        return code switch
        {
            "WT_ToeScour" => GeometryCodes.ToeScour,
            "WT_ToeApron" => GeometryCodes.ToeApron,
            _ => GeometryCodes.Protection
        };
    }
}
