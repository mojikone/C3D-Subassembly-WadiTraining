// Core data models. Pure C# — no Civil 3D dependency. Everything in Core/ is unit-testable
// via tests/AlgorithmTests without Civil 3D installed.
//
// Coordinate convention for the analysis run ("run coordinates"):
//   X = distance along the section from the wadi-side levee toe, increasing TOWARD the wadi (m).
//   Y = elevation relative to the subassembly origin (levee crown) (m). Ground below crown is negative.
// Descending ground toward the wadi therefore has NEGATIVE slope dY/dX.

namespace WadiTrend.Core;

/// <summary>A sampled ground point in run coordinates.</summary>
public readonly record struct TerrainPoint(double X, double Y)
{
    /// <summary>Linear interpolation between two points at station <paramref name="x"/>.</summary>
    public static TerrainPoint Lerp(TerrainPoint a, TerrainPoint b, double x)
    {
        if (Math.Abs(b.X - a.X) < 1e-12)
        {
            return new TerrainPoint(x, a.Y);
        }

        var t = (x - a.X) / (b.X - a.X);
        return new TerrainPoint(x, a.Y + t * (b.Y - a.Y));
    }
}

/// <summary>Kind of terrain trend break.</summary>
public enum BreakKind
{
    /// <summary>Steep dropping trend becomes milder (hollow). Scour risk — protected.</summary>
    Concave,

    /// <summary>Milder trend becomes steeper dropping (crest). Debug marker only.</summary>
    Convex
}

/// <summary>A least-squares fitted trend line over [StartX, EndX].</summary>
public readonly record struct TrendFit(double Slope, double Intercept, double StartX, double EndX, double Rmse)
{
    public double Length => EndX - StartX;

    public double YAt(double x) => Slope * x + Intercept;
}

/// <summary>
/// A detected trend break. X/Y is the intersection of the two fitted trend lines —
/// deliberately NOT projected onto the surface, so rounded transitions get one unambiguous
/// marker that may sit above (convex) or below (concave) the actual ground.
/// </summary>
public sealed record TerrainBreak(BreakKind Kind, double X, double Y, TrendFit Before, TrendFit After, double Score);

/// <summary>A protection interval along the terrain run, in run X coordinates.</summary>
public readonly record struct ProtectionInterval(double StartX, double EndX, string Code)
{
    public ProtectionInterval Normalize() =>
        StartX <= EndX ? this : new ProtectionInterval(EndX, StartX, Code);
}
