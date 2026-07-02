// Civil 3D entry point for the W2 trend-finder. The class name + namespace are referenced
// from the .atc inside the .pkt (DotNetClass="Subassembly.WadiTrendOneSide") — keep in sync
// with scripts/Build-Pkt.ps1.

using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.Runtime;
using WadiTrend.Civil;
using WadiTrend.Core;

namespace Subassembly;

public class WadiTrendOneSide
{
    public void GetLogicalNames()
    {
        Run(WadiTrendParameters.RegisterLogicalNames);
    }

    public void GetInputParameters()
    {
        Run(WadiTrendParameters.RegisterInputParameters);
    }

    public void GetOutputParameters()
    {
        Run(WadiTrendParameters.RegisterOutputParameters);
    }

    public void Draw()
    {
        Run(DrawSection);
    }

    private static void DrawSection(CorridorState state)
    {
        var context = new CorridorContext(state);
        var parameters = WadiTrendParameters.From(context);

        if (context.IsLayoutMode || !context.TryGetSurfaceTarget(WadiTargets.ExistingGround, out var surfaceId))
        {
            new SectionDrawer(context, null).DrawLayoutPreview(parameters);
            state.LayoutModeDisplayType = (CorridorLayoutModeDisplay)4;
            return;
        }

        var sampler = new TerrainSampler(context, surfaceId);
        var drawer = new SectionDrawer(context, sampler);
        var direction = parameters.ScanDirection;

        var landCrownOffset = -direction * parameters.CrownWidth;
        sampler.TryFindDaylight(0.0, direction, parameters.LeveeSideSlope,
            parameters.MaxScanDistance, parameters.AnalysisSampleInterval, out var wadiToe);
        sampler.TryFindDaylight(landCrownOffset, -direction, parameters.LeveeSideSlope,
            parameters.MaxScanDistance, parameters.AnalysisSampleInterval, out var landToe);
        drawer.DrawLevee(0.0, direction, parameters, wadiToe, landToe);

        var run = sampler.BuildRun(wadiToe.Offset, direction, parameters.MaxScanDistance,
            parameters.AnalysisSampleInterval, sampler.GetScanLimitLocalOffset());
        var chain = new TrendSegmenter().Segment(run.Points, parameters.Segmentation);

        drawer.DrawSurfaceRun(run, parameters);
        if (parameters.ShowTrendLines)
        {
            drawer.DrawTrendChain(run, chain);
        }

        drawer.DrawBreakMarkers(run, chain.Breaks, parameters);

        context.TrySetLongOutput(WadiTrendParameters.Names.ConcaveBreakCount, chain.Breaks.Count(b => b.Kind == BreakKind.Concave));
        context.TrySetLongOutput(WadiTrendParameters.Names.ConvexBreakCount, chain.Breaks.Count(b => b.Kind == BreakKind.Convex));
        context.TrySetLongOutput(WadiTrendParameters.Names.TrendCount, chain.Trends.Count);
    }

    private static void Run(Action<CorridorState> action)
    {
        CorridorState? state = null;
        try
        {
            state = CivilApplication.ActiveDocument.CorridorState;
            action(state);
        }
        catch (Exception ex)
        {
            if (state == null)
            {
                throw;
            }

            state.RecordError((CorridorError)(-2147221503), (CorridorErrorLevel)3, ex.Message, ex.Source ?? "WadiTrend", true);
        }
    }
}
