// Converts W2 results into Civil 3D points and links. Points and links only, no shapes.
// Visible points: crown pair, both toes, break markers. Everything else hidden.

using WadiTrend.Core;
using Autodesk.Civil.DatabaseServices;
using CivilPoint = Autodesk.Civil.DatabaseServices.Point;

namespace WadiTrend.Civil;

public sealed class SectionDrawer
{
    private readonly CorridorContext _context;
    private readonly TerrainSampler? _sampler;

    public SectionDrawer(CorridorContext context, TerrainSampler? sampler)
    {
        _context = context;
        _sampler = sampler;
    }

    public void DrawLevee(double crownLocalOffset, int scanDirection, WadiTrendParameters parameters, LocalPoint wadiToe, LocalPoint landToe)
    {
        var crown = AddVisiblePoint(crownLocalOffset, 0.0, WadiCodes.CrownPoint);
        var landCrown = AddVisiblePoint(crownLocalOffset - scanDirection * parameters.CrownWidth, 0.0, WadiCodes.CrownPoint);
        var wadiToePoint = AddVisiblePoint(wadiToe.Offset, wadiToe.Dy, WadiCodes.WadiToePoint);
        var landToePoint = AddVisiblePoint(landToe.Offset, landToe.Dy, WadiCodes.LandToePoint);

        _context.State.Links.Add(crown, landCrown, WadiCodes.Crown);
        _context.State.Links.Add(crown, wadiToePoint, WadiCodes.WadiFace);
        _context.State.Links.Add(landCrown, landToePoint, WadiCodes.LandFace);
    }

    /// <summary>One ground-following link across the whole run (actual surface, dense vertices).</summary>
    public void DrawSurfaceRun(TerrainRun run, WadiTrendParameters parameters)
    {
        if (_sampler is null)
        {
            return;
        }

        var groundPoints = _sampler.SampleSurfaceRun(run, 0.0, run.Length, parameters.AnalysisSampleInterval);
        if (groundPoints.Count < 2)
        {
            return;
        }

        var vertices = groundPoints
            .Select(run.ToLocal)
            .Select(p => AddHiddenPoint(p.Offset, p.Dy))
            .Cast<IPoint>()
            .ToArray();
        _context.State.Links.Add(vertices, WadiCodes.Surface);
    }

    /// <summary>The fitted trend lines, drawn straight from junction to junction.</summary>
    public void DrawTrendChain(TerrainRun run, TrendChain chain)
    {
        for (var i = 1; i < chain.Joints.Count; i++)
        {
            var a = run.ToLocal(chain.Joints[i - 1]);
            var b = run.ToLocal(chain.Joints[i]);
            _context.State.Links.Add(AddHiddenPoint(a.Offset, a.Dy), AddHiddenPoint(b.Offset, b.Dy), WadiCodes.Trend);
        }
    }

    /// <summary>Diamond marker + coded point per classified break, at the trend-line intersection.</summary>
    public void DrawBreakMarkers(TerrainRun run, IReadOnlyList<TerrainBreak> breaks, WadiTrendParameters parameters)
    {
        foreach (var breakPoint in breaks)
        {
            if (breakPoint.Kind == BreakKind.Convex && !parameters.ShowConvexMarkers)
            {
                continue;
            }

            var local = run.ToLocal(new TerrainPoint(breakPoint.X, breakPoint.Y));
            var code = breakPoint.Kind == BreakKind.Concave ? WadiCodes.ConcaveMarker : WadiCodes.ConvexMarker;
            AddVisiblePoint(local.Offset, local.Dy, code);
            DrawDiamond(local.Offset, local.Dy, parameters.BreakMarkerSize, code);
        }
    }

    public void DrawLayoutPreview(WadiTrendParameters parameters)
    {
        var direction = parameters.ScanDirection;
        var drop = 10.0 * parameters.LeveeSideSlope;
        var crown = AddVisiblePoint(0.0, 0.0, WadiCodes.CrownPoint);
        var landCrown = AddVisiblePoint(-direction * parameters.CrownWidth, 0.0, WadiCodes.CrownPoint);
        var wadiToe = AddVisiblePoint(direction * 10.0, -drop, WadiCodes.WadiToePoint);
        var landToe = AddVisiblePoint(-direction * (parameters.CrownWidth + 10.0), -drop, WadiCodes.LandToePoint);
        _context.State.Links.Add(crown, wadiToe, WadiCodes.WadiFace);
        _context.State.Links.Add(crown, landCrown, WadiCodes.Crown);
        _context.State.Links.Add(landCrown, landToe, WadiCodes.LandFace);
    }

    private void DrawDiamond(double offset, double dy, double size, string code)
    {
        var half = Math.Max(0.05, size) * 0.5;
        var top = AddHiddenPoint(offset, dy + half);
        var right = AddHiddenPoint(offset + half, dy);
        var bottom = AddHiddenPoint(offset, dy - half);
        var left = AddHiddenPoint(offset - half, dy);
        _context.State.Links.Add(new IPoint[] { top, right, bottom, left, top }, code);
    }

    private CivilPoint AddVisiblePoint(double offset, double dy, string code)
    {
        return _context.State.Points.Add(offset, dy, code);
    }

    private CivilPoint AddHiddenPoint(double offset, double dy)
    {
        var point = _context.State.Points.Add(offset, dy, "");
        point.IsHidden = true;
        return point;
    }
}
