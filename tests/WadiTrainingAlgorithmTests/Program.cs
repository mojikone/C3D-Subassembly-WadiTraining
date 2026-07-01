using WadiTraining.Analysis;
using WadiTraining.Models;

var detector = new TrendDetector();
var parameters = new WadiParameters
{
    AnalysisSampleInterval = 0.5,
    TrendWindowLength = 5.0,
    MinMildTrendLength = 5.0,
    MinSteepTrendLength = 0.6,
    SlopeChangeThreshold = 0.20,
    MaxTrendResidual = 0.05,
    MinBreakSpacing = 5.0
};

AssertHas(BreakKind.Concave, BuildConcaveRun(), "concave steep-to-mild break");
AssertHas(BreakKind.Convex, BuildConvexRun(), "convex mild-to-steep break");

Console.WriteLine("Algorithm tests passed.");

void AssertHas(BreakKind kind, TerrainRun run, string name)
{
    var breaks = detector.Detect(run, parameters);
    if (!breaks.Any(b => b.Kind == kind && Math.Abs(b.Point.X - 10.0) <= 1.0))
    {
        var found = string.Join(", ", breaks.Select(b => $"{b.Kind}@{b.Point.X:0.00}"));
        throw new InvalidOperationException($"Expected {name} near x=10. Found: {found}");
    }
}

static TerrainRun BuildConcaveRun()
{
    return BuildRun(x => x <= 10.0
        ? -0.50 * x
        : -5.0 - 0.03 * (x - 10.0));
}

static TerrainRun BuildConvexRun()
{
    return BuildRun(x => x <= 10.0
        ? -0.03 * x
        : -0.3 - 0.50 * (x - 10.0));
}

static TerrainRun BuildRun(Func<double, double> y)
{
    var points = Enumerable.Range(0, 41)
        .Select(i => i * 0.5)
        .Select(x => new SectionPoint(x, y(x)))
        .ToArray();
    return new TerrainRun(1, 0.0, 20.0, points);
}
