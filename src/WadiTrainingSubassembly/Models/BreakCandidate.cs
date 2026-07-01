namespace WadiTraining.Models;

internal sealed record BreakCandidate(
    BreakKind Kind,
    SectionPoint Point,
    TrendLine Before,
    TrendLine After,
    double Score);
