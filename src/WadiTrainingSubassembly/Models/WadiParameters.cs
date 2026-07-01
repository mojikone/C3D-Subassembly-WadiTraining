using WadiTraining.Runtime;

namespace WadiTraining.Models;

internal sealed class WadiParameters
{
    public string Version { get; init; } = "W2.3";
    public BankMode BankMode { get; init; } = BankMode.Right;
    public double CrownWidth { get; init; } = 4.0;
    public double LeveeSideSlope { get; init; } = 0.5;
    public double MaxScanDistance { get; init; } = 250.0;
    public double AnalysisSampleInterval { get; init; } = 0.5;
    public double TrendWindowLength { get; init; } = 5.0;
    public double MinMildTrendLength { get; init; } = 5.0;
    public double MinSteepTrendLength { get; init; } = 0.6;
    public double SlopeChangeThreshold { get; init; } = 0.10;
    public double MaxTrendResidual { get; init; } = 0.25;
    public double MinBreakSpacing { get; init; } = 5.0;
    public double MildProtectionLength { get; init; } = 2.0;
    public double MaxSteepProtectionLength { get; init; } = 3.0;
    public double MergeDistance { get; init; } = 5.0;
    public double ToeScourLength { get; init; } = 2.0;
    public double ToeApronLength { get; init; } = 2.0;
    public double BreakMarkerSize { get; init; } = 0.5;
    public bool ShowConvexMarkers { get; init; } = true;

    public static WadiParameters From(CivilRuntime runtime, BankMode? forcedBankMode = null)
    {
        return new WadiParameters
        {
            Version = runtime.GetString(ParameterNames.Version, "W2.3"),
            BankMode = forcedBankMode ?? ResolveBankMode(runtime),
            CrownWidth = ClampPositive(runtime.GetDouble(ParameterNames.CrownWidth, 4.0), 0.1),
            LeveeSideSlope = ClampPositive(runtime.GetDouble(ParameterNames.LeveeSideSlope, 0.5), 0.01),
            MaxScanDistance = ClampPositive(runtime.GetDouble(ParameterNames.MaxScanDistance, 250.0), 1.0),
            AnalysisSampleInterval = ClampPositive(runtime.GetDouble(ParameterNames.AnalysisSampleInterval, 0.5), 0.05),
            TrendWindowLength = ClampPositive(runtime.GetDouble(ParameterNames.TrendWindowLength, 5.0), 0.5),
            MinMildTrendLength = ClampPositive(runtime.GetDouble(ParameterNames.MinMildTrendLength, 5.0), 0.5),
            MinSteepTrendLength = ClampPositive(runtime.GetDouble(ParameterNames.MinSteepTrendLength, 0.6), 0.1),
            SlopeChangeThreshold = Math.Max(0.0, runtime.GetDouble(ParameterNames.SlopeChangeThreshold, 0.10)),
            MaxTrendResidual = ClampPositive(runtime.GetDouble(ParameterNames.MaxTrendResidual, 0.25), 0.01),
            MinBreakSpacing = ClampPositive(runtime.GetDouble(ParameterNames.MinBreakSpacing, 5.0), 0.1),
            MildProtectionLength = ClampPositive(runtime.GetDouble(ParameterNames.MildProtectionLength, 2.0), 0.1),
            MaxSteepProtectionLength = ClampPositive(runtime.GetDouble(ParameterNames.MaxSteepProtectionLength, 3.0), 0.1),
            MergeDistance = Math.Max(0.0, runtime.GetDouble(ParameterNames.MergeDistance, 5.0)),
            ToeScourLength = Math.Max(0.0, runtime.GetDouble(ParameterNames.ToeScourLength, 2.0)),
            ToeApronLength = Math.Max(0.0, runtime.GetDouble(ParameterNames.ToeApronLength, 2.0)),
            BreakMarkerSize = ClampPositive(runtime.GetDouble(ParameterNames.BreakMarkerSize, 0.5), 0.05),
            ShowConvexMarkers = runtime.GetLong(ParameterNames.ShowConvexMarkers, 1) != 0
        };
    }

    private static double ClampPositive(double value, double minimum)
    {
        return double.IsFinite(value) ? Math.Max(minimum, value) : minimum;
    }

    private static BankMode ResolveBankMode(CivilRuntime runtime)
    {
        var bankMode = (BankMode)Math.Clamp(runtime.GetLong(ParameterNames.BankMode, (long)BankMode.Right), 0, 2);
        if (bankMode == BankMode.Both)
        {
            return BankMode.Both;
        }

        return runtime.GetLong(ParameterNames.Side, -1) switch
        {
            0 => BankMode.Right,
            1 => BankMode.Left,
            _ => bankMode
        };
    }
}
