// Everything about W2 parameters and targets: names, Civil 3D registration, typed snapshot.
// Registration defaults MUST match the .atc defaults written by scripts/Build-Pkt.ps1.

using Autodesk.Civil.Runtime;
using WadiTrend.Core;

namespace WadiTrend.Civil;

public static class WadiTargets
{
    public const string ExistingGround = "ExistingGround";
    public const string ScanLimitOffset = "ScanLimitOffset";
}

public enum SideOption
{
    Right = 0,
    Left = 1
}

public sealed class WadiTrendParameters
{
    public const string VersionTag = "C2.1";

    public SideOption Side { get; init; } = SideOption.Right;
    public double CrownWidth { get; init; } = 4.0;
    public double LeveeSideSlope { get; init; } = 0.5;
    public double MaxScanDistance { get; init; } = 250.0;
    public double AnalysisSampleInterval { get; init; } = 0.5;
    public double BreakMarkerSize { get; init; } = 0.5;
    public bool ShowConvexMarkers { get; init; } = true;
    public bool ShowTrendLines { get; init; } = true;
    public TrendSegmentationSettings Segmentation { get; init; } = new();

    public int ScanDirection => Side == SideOption.Left ? -1 : 1;

    public static WadiTrendParameters From(CorridorContext context)
    {
        return new WadiTrendParameters
        {
            Side = context.GetLong(Names.Side, 0) == 1 ? SideOption.Left : SideOption.Right,
            CrownWidth = Positive(context.GetDouble(Names.CrownWidth, 4.0), 0.1),
            LeveeSideSlope = Positive(context.GetDouble(Names.LeveeSideSlope, 0.5), 0.01),
            MaxScanDistance = Positive(context.GetDouble(Names.MaxScanDistance, 250.0), 1.0),
            AnalysisSampleInterval = Positive(context.GetDouble(Names.AnalysisSampleInterval, 0.5), 0.05),
            BreakMarkerSize = Positive(context.GetDouble(Names.BreakMarkerSize, 0.5), 0.05),
            ShowConvexMarkers = context.GetLong(Names.ShowConvexMarkers, 1) != 0,
            ShowTrendLines = context.GetLong(Names.ShowTrendLines, 1) != 0,
            Segmentation = new TrendSegmentationSettings
            {
                MaxTrendResidual = Positive(context.GetDouble(Names.MaxTrendResidual, 0.15), 0.01),
                MinTrendLength = Positive(context.GetDouble(Names.MinTrendLength, 5.0), 0.5),
                SlopeChangeThreshold = Math.Max(0.0, context.GetDouble(Names.SlopeChangeThreshold, 0.05))
            }
        };
    }

    private static double Positive(double value, double minimum) =>
        double.IsFinite(value) ? Math.Max(minimum, value) : minimum;

    public static class Names
    {
        public const string Version = "Version";
        public const string Side = "Side";
        public const string CrownWidth = "CrownWidth";
        public const string LeveeSideSlope = "LeveeSideSlope";
        public const string MaxScanDistance = "MaxScanDistance";
        public const string AnalysisSampleInterval = "AnalysisSampleInterval";
        public const string MaxTrendResidual = "MaxTrendResidual";
        public const string MinTrendLength = "MinTrendLength";
        public const string SlopeChangeThreshold = "SlopeChangeThreshold";
        public const string BreakMarkerSize = "BreakMarkerSize";
        public const string ShowConvexMarkers = "ShowConvexMarkers";
        public const string ShowTrendLines = "ShowTrendLines";
        public const string ConcaveBreakCount = "ConcaveBreakCount";
        public const string ConvexBreakCount = "ConvexBreakCount";
        public const string TrendCount = "TrendCount";
    }

    // ----- Civil 3D registration -----

    private const int LogicalSurfaceTarget = 1;
    private const int LogicalOffsetTarget = 4;

    public static void RegisterLogicalNames(CorridorState state)
    {
        AddLogical(state, WadiTargets.ExistingGround, LogicalSurfaceTarget, "Existing Ground Surface");
        AddLogical(state, WadiTargets.ScanLimitOffset, LogicalOffsetTarget, "Scan Limit Offset (thalweg)");
    }

    public static void RegisterInputParameters(CorridorState state)
    {
        state.ParamsString.Add(Names.Version, VersionTag);
        state.ParamsLong.Add(Names.Side, 0);
        state.ParamsDouble.Add(Names.CrownWidth, 4.0);
        state.ParamsDouble.Add(Names.LeveeSideSlope, 0.5);
        state.ParamsDouble.Add(Names.MaxScanDistance, 250.0);
        state.ParamsDouble.Add(Names.AnalysisSampleInterval, 0.5);
        state.ParamsDouble.Add(Names.MaxTrendResidual, 0.15);
        state.ParamsDouble.Add(Names.MinTrendLength, 5.0);
        state.ParamsDouble.Add(Names.SlopeChangeThreshold, 0.05);
        state.ParamsDouble.Add(Names.BreakMarkerSize, 0.5);
        state.ParamsLong.Add(Names.ShowConvexMarkers, 1);
        state.ParamsLong.Add(Names.ShowTrendLines, 1);
    }

    public static void RegisterOutputParameters(CorridorState state)
    {
        state.ParamsLong.Add(Names.ConcaveBreakCount, 0);
        state.ParamsLong.Add(Names.ConvexBreakCount, 0);
        state.ParamsLong.Add(Names.TrendCount, 0);
    }

    private static void AddLogical(CorridorState state, string name, int typeCode, string displayName)
    {
        var parameter = state.ParamsLong.Add(name, typeCode);
        parameter.DisplayName = displayName;
    }
}
