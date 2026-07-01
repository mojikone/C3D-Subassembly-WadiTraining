namespace WadiTraining.Models;

internal sealed record TerrainRun(
    int Direction,
    double ToeLocalOffset,
    double StopLocalOffset,
    IReadOnlyList<SectionPoint> Points)
{
    public double Length => Math.Abs(StopLocalOffset - ToeLocalOffset);

    public double ToLocalOffset(double x) => ToeLocalOffset + Direction * x;

    public SectionPoint ToLocalPoint(SectionPoint point) => new(ToLocalOffset(point.X), point.Y);
}
