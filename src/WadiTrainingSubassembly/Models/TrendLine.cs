namespace WadiTraining.Models;

internal readonly record struct TrendLine(double Slope, double Intercept, double StartX, double EndX, double Rmse)
{
    public double Length => Math.Max(0.0, EndX - StartX);

    public double YAt(double x) => Slope * x + Intercept;
}
