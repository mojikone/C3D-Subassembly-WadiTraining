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
AssertCurvedDrop();
AssertProtectionIntervals();

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

void AssertCurvedDrop()
{
    var curvedParameters = new WadiParameters
    {
        AnalysisSampleInterval = parameters.AnalysisSampleInterval,
        TrendWindowLength = parameters.TrendWindowLength,
        MinMildTrendLength = parameters.MinMildTrendLength,
        MinSteepTrendLength = parameters.MinSteepTrendLength,
        SlopeChangeThreshold = 0.10,
        MaxTrendResidual = 0.25,
        MinBreakSpacing = 5.0
    };
    var breaks = detector.Detect(BuildCurvedDropRun(), curvedParameters);
    var concave = breaks.Where(b => b.Kind == BreakKind.Concave).ToArray();
    var convex = breaks.Where(b => b.Kind == BreakKind.Convex).ToArray();

    if (concave.Length != 1 || Math.Abs(concave[0].Point.X - 30.0) > 3.0)
    {
        throw new InvalidOperationException($"Expected one lower concave near x=30. Found: {Describe(breaks)}");
    }

    if (convex.Length > 1)
    {
        throw new InvalidOperationException($"Expected duplicate convex breaks to be suppressed. Found: {Describe(breaks)}");
    }
}

void AssertProtectionIntervals()
{
    var breaks = detector.Detect(BuildConcaveRun(), parameters);
    var intervals = new ProtectionPlanner().BuildIntervals(BuildConcaveRun(), breaks, parameters);

    if (!intervals.Any(i => i.StartX <= 0.1 && i.EndX >= 3.5))
    {
        throw new InvalidOperationException("Expected toe scour and apron protection from the wadi-side toe.");
    }

    if (!intervals.Any(i => i.StartX <= 10.0 && i.EndX >= 11.9))
    {
        var found = string.Join(", ", intervals.Select(i => $"{i.Code}[{i.StartX:0.00},{i.EndX:0.00}]"));
        throw new InvalidOperationException($"Expected protection around the detected concave break. Found: {found}");
    }
}

static TerrainRun BuildCurvedDropRun()
{
    return BuildRunWithLength(60.0, 1.0, x =>
    {
        if (x <= 18.0)
        {
            return -0.02 * x;
        }

        if (x <= 30.0)
        {
            var t = (x - 18.0) / 12.0;
            var smoothStep = t * t * (3.0 - 2.0 * t);
            return -0.36 - 7.0 * smoothStep;
        }

        return -7.36 + 0.02 * (x - 30.0);
    });
}

static TerrainRun BuildRun(Func<double, double> y)
{
    return BuildRunWithLength(20.0, 0.5, y);
}

static TerrainRun BuildRunWithLength(double length, double interval, Func<double, double> y)
{
    var points = Enumerable.Range(0, (int)Math.Round(length / interval) + 1)
        .Select(i => i * interval)
        .Select(x => new SectionPoint(x, y(x)))
        .ToArray();
    return new TerrainRun(1, 0.0, length, points);
}

static string Describe(IEnumerable<BreakCandidate> breaks)
{
    return string.Join(", ", breaks.Select(b => $"{b.Kind}@{b.Point.X:0.00}"));
}
