using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;
using WadiTraining.Analysis;
using WadiTraining.Geometry;
using WadiTraining.Models;
using WadiTraining.Runtime;
using WadiTraining.Terrain;

namespace Subassembly;

public class WadiTrainingLeveeOneSide : CivilSubassemblyBase
{
    protected override void RegisterLogicalNames(CorridorState state)
    {
        ParameterDefinitions.RegisterOneSideLogicalNames(state);
    }

    protected override void RegisterInputParameters(CorridorState state)
    {
        ParameterDefinitions.RegisterOneSideInputParameters(state);
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
            WadiTrainingLeveeCore.DrawLayout(runtime, parameters);
            return;
        }

        var sampler = new TerrainSampler(runtime, surfaceId);
        var scanDirection = parameters.BankMode == BankMode.Left ? -1 : 1;
        WadiTrainingLeveeCore.DrawOneBank(
            runtime,
            sampler,
            parameters,
            crownLocalOffset: 0.0,
            scanDirection,
            sampler.GetScanLimitLocalOffset());
    }
}

public sealed class WadiTrainingLeveeBothSides : CivilSubassemblyBase
{
    protected override void RegisterLogicalNames(CorridorState state)
    {
        ParameterDefinitions.RegisterBothSidesLogicalNames(state);
    }

    protected override void RegisterInputParameters(CorridorState state)
    {
        ParameterDefinitions.RegisterBothSidesInputParameters(state);
    }

    protected override void RegisterOutputParameters(CorridorState state)
    {
        ParameterDefinitions.RegisterOutputParameters(state);
    }

    protected override void Draw(CorridorState state)
    {
        var runtime = new CivilRuntime(state);
        var parameters = WadiParameters.From(runtime, BankMode.Both);

        if (runtime.IsLayoutMode || !runtime.TryGetSurfaceTarget(TargetNames.ExistingGround, out var surfaceId))
        {
            WadiTrainingLeveeCore.DrawLayout(runtime, parameters);
            return;
        }

        var sampler = new TerrainSampler(runtime, surfaceId);
        var allBreaks = new List<BreakCandidate>();

        if (WadiTrainingLeveeCore.TryGetTargetLocalOffset(runtime, TargetNames.LeftBankCrownOffset, out var leftCrown))
        {
            allBreaks.AddRange(WadiTrainingLeveeCore.DrawOneBank(
                runtime,
                sampler,
                parameters,
                leftCrown,
                scanDirection: 1,
                stopLocalOffset: 0.0));
        }

        if (WadiTrainingLeveeCore.TryGetTargetLocalOffset(runtime, TargetNames.RightBankCrownOffset, out var rightCrown))
        {
            allBreaks.AddRange(WadiTrainingLeveeCore.DrawOneBank(
                runtime,
                sampler,
                parameters,
                rightCrown,
                scanDirection: -1,
                stopLocalOffset: 0.0));
        }

        WadiTrainingLeveeCore.TrySetOutputCounts(runtime, allBreaks);
    }
}

// Backward-compatible class name for older imported tools. New packets use the two explicit classes above.
public sealed class WadiTrainingLevee : WadiTrainingLeveeOneSide
{
}

internal static class WadiTrainingLeveeCore
{
    public static IReadOnlyList<BreakCandidate> DrawOneBank(
        CivilRuntime runtime,
        TerrainSampler sampler,
        WadiParameters parameters,
        double crownLocalOffset,
        int scanDirection,
        double? stopLocalOffset)
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
            parameters.AnalysisSampleInterval, stopLocalOffset);
        var breaks = detector.Detect(run, parameters);
        var intervals = planner.BuildIntervals(run, breaks, parameters);

        writer.DrawTerrainRun(run, intervals, parameters);
        writer.DrawBreakMarkers(run, breaks, parameters);
        TrySetOutputCounts(runtime, breaks);

        return breaks;
    }

    public static void DrawLayout(CivilRuntime runtime, WadiParameters parameters)
    {
        var writer = new GeometryWriter(runtime, null);
        writer.DrawLayoutPreview(parameters);
        runtime.State.LayoutModeDisplayType = (CorridorLayoutModeDisplay)4;
    }

    public static bool TryGetTargetLocalOffset(CivilRuntime runtime, string targetName, out double localOffset)
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

    public static void TrySetOutputCounts(CivilRuntime runtime, IReadOnlyList<BreakCandidate> breaks)
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
