// Console test runner for the W2 trend segmentation core (no framework, no Civil 3D).
// Run: dotnet run --project tests/SegmentationTests
// Exit code 0 = all pass.

using WadiTrend.Core;

// Debug mode: dotnet run -- <terrain.csv> [residual] [minLen] [threshold]
// CSV rows: runX,elevation. Prints the segmentation instead of running tests.
if (args.Length > 0)
{
    var csv = File.ReadAllLines(args[0])
        .Select(l => l.Split(','))
        .Where(p => p.Length == 2)
        .Select(p => new TerrainPoint(double.Parse(p[0]), double.Parse(p[1])))
        .ToList();
    var defaults = new TrendSegmentationSettings();
    var debugSettings = new TrendSegmentationSettings
    {
        MaxTrendResidual = args.Length > 1 ? double.Parse(args[1]) : defaults.MaxTrendResidual,
        MinTrendLength = args.Length > 2 ? double.Parse(args[2]) : defaults.MinTrendLength,
        SlopeChangeThreshold = args.Length > 3 ? double.Parse(args[3]) : defaults.SlopeChangeThreshold
    };
    var debugChain = new TrendSegmenter().Segment(csv, debugSettings);
    Console.WriteLine($"points={csv.Count} span={csv[0].X:F1}..{csv[^1].X:F1}");
    foreach (var t in debugChain.Trends)
    {
        Console.WriteLine($"TREND [{t.StartX,6:F2} .. {t.EndX,6:F2}] slope={t.Slope,7:F4} rmse={t.Rmse:F3}");
    }

    foreach (var b in debugChain.Breaks)
    {
        Console.WriteLine($"BREAK {b.Kind} @ {b.X:F2} / {b.Y:F2}");
    }

    return 0;
}

var runner = new Runner();

// The user's reference case (like section 2+200): mild bench -> steep drop -> long mild
// bottom with small undulations. Expected: exactly 3 trend lines, convex at the bench
// edge, concave at the channel bottom, markers at the trend-line intersections.
runner.Test("ReferenceSection: bench/steep/bottom = 3 trends, 1 convex + 1 concave", () =>
{
    var ground = Terrain.Build(0.0,
        (18.0, -0.03, 0.00),   // bench
        (30.0, -0.35, 0.00),   // steep drop
        (65.0, 0.02, 0.10));   // long bottom with +-0.10 undulation
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 3, $"3 trends (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex", DumpBreaks(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 1, "one concave", DumpBreaks(chain));
    var convex = First(chain, BreakKind.Convex);
    var concave = First(chain, BreakKind.Concave);
    runner.Check(Math.Abs(convex.X - 18.0) < 1.5, $"convex near 18 (got {convex.X:F2})");
    runner.Check(Math.Abs(concave.X - 30.0) < 1.5, $"concave near 30 (got {concave.X:F2})");
    runner.Check(Math.Abs(chain.Trends[1].Slope - (-0.35)) < 0.04, $"steep trend slope ~-0.35 (got {chain.Trends[1].Slope:F3})");
});

runner.Test("SmallSlopeChanges: gentle kinks under threshold never split a trend", () =>
{
    // One long descending reach whose slope wobbles -0.02/-0.06/-0.03 (all deltas < 0.10).
    var ground = Terrain.Build(5.0, (15.0, -0.02, 0.0), (30.0, -0.06, 0.0), (50.0, -0.03, 0.0));
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 1, $"single trend (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(chain.Breaks.Count == 0, "no breaks", DumpBreaks(chain));
});

runner.Test("RoundedConcave: 6 m curve blend still gives one concave at the line intersection", () =>
{
    var ground = Terrain.Rounded(10.0, -0.5, -0.02, cornerX: 25.0, blendHalfWidth: 3.0, endX: 55.0);
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 2, $"2 trends (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 1, "one concave", DumpBreaks(chain));
    var b = First(chain, BreakKind.Concave);
    runner.Check(Math.Abs(b.X - 25.0) < 1.5, $"X near true intersection 25 (got {b.X:F2})");
    var surfaceY = SurfaceMath.ElevationAt(ground, b.X);
    runner.Check(b.Y < surfaceY - 0.05, $"marker below rounded surface ({b.Y:F2} vs {surfaceY:F2})");
});

runner.Test("RoundedConvex: marker above the rounded crest", () =>
{
    var ground = Terrain.Rounded(5.0, -0.02, -0.5, cornerX: 25.0, blendHalfWidth: 3.0, endX: 45.0);
    var chain = Segment(ground);
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex", DumpBreaks(chain));
    var b = First(chain, BreakKind.Convex);
    runner.Check(Math.Abs(b.X - 25.0) < 1.5, $"X near 25 (got {b.X:F2})");
    var surfaceY = SurfaceMath.ElevationAt(ground, b.X);
    runner.Check(b.Y > surfaceY + 0.05, $"marker above rounded surface ({b.Y:F2} vs {surfaceY:F2})");
});

runner.Test("VChannel: steep down into rising bank = concave thalweg", () =>
{
    var ground = Terrain.Build(8.0, (15.0, -0.4, 0.0), (30.0, 0.3, 0.0));
    var chain = Segment(ground);
    runner.Check(Count(chain, BreakKind.Concave) == 1, "one concave", DumpBreaks(chain));
    runner.Check(Math.Abs(First(chain, BreakKind.Concave).X - 15.0) < 1.0, "at channel bottom");
});

runner.Test("Crest: rising into steep drop = convex only", () =>
{
    var ground = Terrain.Build(0.0, (15.0, 0.3, 0.0), (30.0, -0.4, 0.0));
    var chain = Segment(ground);
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex", DumpBreaks(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 0, "no concave", DumpBreaks(chain));
});

runner.Test("FlatNoise: noisy plain = one trend, no breaks", () =>
{
    var ground = new List<TerrainPoint>();
    for (var x = 0.0; x <= 60.0; x += 0.5)
    {
        ground.Add(new TerrainPoint(x, -0.01 * x + 0.08 * Math.Sin(x * Math.PI / 4.0)));
    }

    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 1, $"one trend (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(chain.Breaks.Count == 0, "no breaks", DumpBreaks(chain));
});

runner.Test("TwoTerraces: all real breaks found in order", () =>
{
    var ground = Terrain.Build(15.0, (10.0, -0.5, 0.0), (25.0, -0.02, 0.0), (35.0, -0.5, 0.0), (52.0, -0.02, 0.0));
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 4, $"4 trends (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 2, "two concave", DumpBreaks(chain));
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex", DumpBreaks(chain));
});

runner.Test("GentleSection: 3.5% bench vs 10.5% drop still splits (real 2+400 shape)", () =>
{
    var ground = Terrain.Build(-3.6, (27.0, -0.035, 0.03), (43.0, -0.105, 0.03), (58.0, 0.005, 0.03));
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 3, $"3 trends (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex", DumpBreaks(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 1, "one concave", DumpBreaks(chain));
    runner.Check(Math.Abs(First(chain, BreakKind.Convex).X - 27.0) < 2.0, $"convex near 27 (got {First(chain, BreakKind.Convex).X:F2})");
    runner.Check(Math.Abs(First(chain, BreakKind.Concave).X - 43.0) < 2.0, $"concave near 43 (got {First(chain, BreakKind.Concave).X:F2})");
});

runner.Test("CliffStep: short steep step between benches is kept, not bridged", () =>
{
    var ground = Terrain.Build(0.0, (20.0, -0.01, 0.0), (23.0, -0.60, 0.0), (43.0, -0.01, 0.0));
    var chain = Segment(ground);
    runner.Check(chain.Trends.Count == 3, $"3 trends incl. the short cliff (got {chain.Trends.Count})", DumpTrends(chain));
    runner.Check(Count(chain, BreakKind.Convex) == 1, "one convex at cliff top", DumpBreaks(chain));
    runner.Check(Count(chain, BreakKind.Concave) == 1, "one concave at cliff base", DumpBreaks(chain));
});

runner.Test("Joints: polyline is monotonic and spans the whole run", () =>
{
    var ground = Terrain.Build(0.0, (18.0, -0.03, 0.0), (30.0, -0.35, 0.0), (65.0, 0.02, 0.1));
    var chain = Segment(ground);
    runner.Check(Math.Abs(chain.Joints[0].X - 0.0) < 1e-6, "starts at run start");
    runner.Check(Math.Abs(chain.Joints[^1].X - 65.0) < 1e-6, "ends at run end");
    var monotonic = true;
    for (var i = 1; i < chain.Joints.Count; i++)
    {
        if (chain.Joints[i].X <= chain.Joints[i - 1].X)
        {
            monotonic = false;
        }
    }

    runner.Check(monotonic, "joint X strictly increasing");
});

return runner.Finish();

// ---------------------------------------------------------------- helpers

static TrendChain Segment(IReadOnlyList<TerrainPoint> ground) =>
    new TrendSegmenter().Segment(ground, new TrendSegmentationSettings());

static int Count(TrendChain chain, BreakKind kind) => chain.Breaks.Count(b => b.Kind == kind);

static TerrainBreak First(TrendChain chain, BreakKind kind) => chain.Breaks.First(b => b.Kind == kind);

static string DumpTrends(TrendChain chain) =>
    string.Join("; ", chain.Trends.Select(t => $"[{t.StartX:F1}..{t.EndX:F1}] s={t.Slope:F3}"));

static string DumpBreaks(TrendChain chain) =>
    chain.Breaks.Count == 0 ? "[none]" : string.Join("; ", chain.Breaks.Select(b => $"{b.Kind}@{b.X:F2}/{b.Y:F2}"));

internal static class Terrain
{
    /// <summary>
    /// Piecewise ground sampled at 0.5 m. Each segment runs to UntilX at Slope, with an
    /// optional sinusoidal undulation amplitude (wavelength 7 m) to mimic natural ground.
    /// </summary>
    public static IReadOnlyList<TerrainPoint> Build(double y0, params (double UntilX, double Slope, double Noise)[] segments)
    {
        var points = new List<TerrainPoint>();
        var y = y0;
        var previousX = 0.0;
        var index = 0;
        for (var x = 0.0; x <= segments[^1].UntilX + 1e-9; x += 0.5)
        {
            while (index < segments.Length - 1 && x > segments[index].UntilX + 1e-9)
            {
                index++;
            }

            y += segments[index].Slope * (x - previousX);
            previousX = x;
            var noise = segments[index].Noise * Math.Sin(x * 2.0 * Math.PI / 7.0);
            points.Add(new TerrainPoint(x, y + noise));
        }

        return points;
    }

    /// <summary>Two slopes blended by a tangent-continuous quadratic Bezier (rounded transition).</summary>
    public static IReadOnlyList<TerrainPoint> Rounded(double y0, double slope1, double slope2, double cornerX, double blendHalfWidth, double endX)
    {
        double Line1(double x) => y0 + slope1 * x;
        var cornerY = Line1(cornerX);
        double Line2(double x) => cornerY + slope2 * (x - cornerX);

        var x0 = cornerX - blendHalfWidth;
        var x2 = cornerX + blendHalfWidth;
        var points = new List<TerrainPoint>();
        for (var x = 0.0; x <= endX + 1e-9; x += 0.5)
        {
            double y;
            if (x <= x0)
            {
                y = Line1(x);
            }
            else if (x >= x2)
            {
                y = Line2(x);
            }
            else
            {
                var t = (x - x0) / (x2 - x0);
                y = (1 - t) * (1 - t) * Line1(x0) + 2 * (1 - t) * t * cornerY + t * t * Line2(x2);
            }

            points.Add(new TerrainPoint(x, y));
        }

        return points;
    }
}

internal sealed class Runner
{
    private int _checks;
    private int _failures;
    private string _current = "";

    public void Test(string name, Action body)
    {
        _current = name;
        Console.WriteLine($"--- {name}");
        try
        {
            body();
        }
        catch (Exception ex)
        {
            _failures++;
            Console.WriteLine($"FAIL {_current}: threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Check(bool condition, string what, string detail = "")
    {
        _checks++;
        if (condition)
        {
            Console.WriteLine($"PASS {what}");
        }
        else
        {
            _failures++;
            Console.WriteLine($"FAIL {what}{(detail.Length > 0 ? " | " + detail : "")}");
        }
    }

    public int Finish()
    {
        Console.WriteLine($"==== {_checks - _failures}/{_checks} checks passed, {_failures} failed");
        return _failures == 0 ? 0 : 1;
    }
}
