using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;
using WadiTraining.Analysis;
using WadiTraining.Geometry;
using WadiTraining.Models;
using WadiTraining.Runtime;
using WadiTraining.Terrain;

namespace Subassembly;

public sealed class WadiTrainingLevee : CivilSubassemblyBase
{
    protected override void RegisterLogicalNames(CorridorState state)
    {
        ParameterDefinitions.RegisterLogicalNames(state);
    }

    protected override void RegisterInputParameters(CorridorState state)
    {
        ParameterDefinitions.RegisterInputParameters(state);
    }

    protected override void RegisterOutputParameters(CorridorState state)
    {
        ParameterDefinitions.RegisterOutputParameters(state);
    }

    protected override void Draw(CorridorState state)
    {
        var runtime = new CivilRuntime(state);
        var parameters = WadiParameters.From(runtime);

        if (runtime.IsLayoutMode || !runtime.TryGetSurfaceTarget(TargetNames.ExistingGround, out var surfaceId))
        {
            DrawLayout(runtime, parameters);
            return;
        }

        var sampler = new TerrainSampler(runtime, surfaceId);
        if (parameters.BankMode == BankMode.Both)
        {
            DrawBothBanks(runtime, sampler, parameters);
            return;
        }

        var scanDirection = parameters.BankMode == BankMode.Left ? -1 : 1;
        DrawOneBank(runtime, sampler, parameters, crownLocalOffset: 0.0, scanDirection, sampler.GetThalwegLocalOffset());
    }

    private static void DrawBothBanks(CivilRuntime runtime, TerrainSampler sampler, WadiParameters parameters)
    {
        var thalweg = sampler.GetThalwegLocalOffset() ?? 0.0;

        if (TryGetTargetLocalOffset(runtime, TargetNames.LeftBankCrownOffset, out var leftCrown))
        {
            DrawOneBank(runtime, sampler, parameters, leftCrown, scanDirection: 1, thalweg);
        }

        if (TryGetTargetLocalOffset(runtime, TargetNames.RightBankCrownOffset, out var rightCrown))
        {
            DrawOneBank(runtime, sampler, parameters, rightCrown, scanDirection: -1, thalweg);
        }
    }

    private static void DrawOneBank(
        CivilRuntime runtime,
        TerrainSampler sampler,
        WadiParameters parameters,
        double crownLocalOffset,
        int scanDirection,
        double? thalwegLocalOffset)
    {
        var writer = new GeometryWriter(runtime, sampler);
        var detector = new TrendDetector();
        var planner = new ProtectionPlanner();

        var landCrownOffset = crownLocalOffset - scanDirection * parameters.CrownWidth;
        sampler.TryFindDaylight(crownLocalOffset, scanDirection, parameters.LeveeSideSlope,
            parameters.MaxScanDistance, parameters.AnalysisSampleInterval, out var wadiToe);
        sampler.TryFindDaylight(landCrownOffset, -scanDirection, parameters.LeveeSideSlope,
            parameters.MaxScanDistance, parameters.AnalysisSampleInterval, out var landToe);

        writer.DrawBankLevee(crownLocalOffset, scanDirection, parameters, wadiToe, landToe);

        var run = sampler.BuildRun(wadiToe.X, scanDirection, parameters.MaxScanDistance,
            parameters.AnalysisSampleInterval, thalwegLocalOffset);
        var breaks = detector.Detect(run, parameters);
        var intervals = planner.BuildIntervals(run, breaks, parameters);

        writer.DrawTerrainRun(run, intervals, parameters);
        writer.DrawBreakMarkers(run, breaks, parameters);

        TrySetOutputCounts(runtime, breaks);
    }

    private static void DrawLayout(CivilRuntime runtime, WadiParameters parameters)
    {
        var writer = new GeometryWriter(runtime, null);
        writer.DrawLayoutPreview(parameters);
    }

    private static bool TryGetTargetLocalOffset(CivilRuntime runtime, string targetName, out double localOffset)
    {
        localOffset = 0.0;
        if (!runtime.TryGetOffsetTarget(targetName, out var target))
        {
            return false;
        }

        try
        {
            double x = 0.0;
            double y = 0.0;
            var absoluteOffset = target.GetDistanceToAlignment(runtime.AlignmentId, runtime.CurrentStation, ref x, ref y);
            localOffset = absoluteOffset - runtime.OriginOffset;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TrySetOutputCounts(CivilRuntime runtime, IReadOnlyList<BreakCandidate> breaks)
    {
        try
        {
            runtime.State.ParamsLong.Add("ConcaveBreakCount", breaks.Count(b => b.Kind == BreakKind.Concave));
            runtime.State.ParamsLong.Add("ConvexBreakCount", breaks.Count(b => b.Kind == BreakKind.Convex));
        }
        catch
        {
        }
    }
}
