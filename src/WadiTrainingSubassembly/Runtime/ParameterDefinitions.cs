using Autodesk.Civil.Runtime;

namespace WadiTraining.Runtime;

internal static class ParameterDefinitions
{
    private const int LogicalOffsetTarget = 4;
    private const int LogicalSurfaceTarget = 1;
    private const int LogicalElevationTarget = 5;

    public static void RegisterLogicalNames(CorridorState state)
    {
        AddLogical(state, TargetNames.ExistingGround, LogicalSurfaceTarget, "Existing Ground Surface");
        AddLogical(state, TargetNames.ThalwegOffset, LogicalOffsetTarget, "Thalweg Offset");
        AddLogical(state, TargetNames.LeftBankCrownOffset, LogicalOffsetTarget, "Left Bank Crown Offset");
        AddLogical(state, TargetNames.RightBankCrownOffset, LogicalOffsetTarget, "Right Bank Crown Offset");
        AddLogical(state, TargetNames.LeftBankCrownElevation, LogicalElevationTarget, "Left Bank Crown Elevation");
        AddLogical(state, TargetNames.RightBankCrownElevation, LogicalElevationTarget, "Right Bank Crown Elevation");
    }

    public static void RegisterInputParameters(CorridorState state)
    {
        state.ParamsString.Add(ParameterNames.Version, "W2.0");
        state.ParamsLong.Add(ParameterNames.BankMode, 0);
        state.ParamsDouble.Add(ParameterNames.CrownWidth, 4.0);
        state.ParamsDouble.Add(ParameterNames.LeveeSideSlope, 0.5);
        state.ParamsDouble.Add(ParameterNames.MaxScanDistance, 250.0);
        state.ParamsDouble.Add(ParameterNames.AnalysisSampleInterval, 0.5);
        state.ParamsDouble.Add(ParameterNames.TrendWindowLength, 5.0);
        state.ParamsDouble.Add(ParameterNames.MinMildTrendLength, 5.0);
        state.ParamsDouble.Add(ParameterNames.MinSteepTrendLength, 0.6);
        state.ParamsDouble.Add(ParameterNames.SlopeChangeThreshold, 0.20);
        state.ParamsDouble.Add(ParameterNames.MaxTrendResidual, 0.25);
        state.ParamsDouble.Add(ParameterNames.MinBreakSpacing, 5.0);
        state.ParamsDouble.Add(ParameterNames.MildProtectionLength, 2.0);
        state.ParamsDouble.Add(ParameterNames.MaxSteepProtectionLength, 3.0);
        state.ParamsDouble.Add(ParameterNames.MergeDistance, 5.0);
        state.ParamsDouble.Add(ParameterNames.ToeScourLength, 2.0);
        state.ParamsDouble.Add(ParameterNames.ToeApronLength, 2.0);
        state.ParamsDouble.Add(ParameterNames.BreakMarkerSize, 0.5);
        state.ParamsLong.Add(ParameterNames.ShowConvexMarkers, 1);
    }

    public static void RegisterOutputParameters(CorridorState state)
    {
        state.ParamsLong.Add("ConcaveBreakCount", 0);
        state.ParamsLong.Add("ConvexBreakCount", 0);
    }

    private static void AddLogical(CorridorState state, string name, int typeCode, string displayName)
    {
        var parameter = state.ParamsLong.Add(name, typeCode);
        parameter.DisplayName = displayName;
    }
}
