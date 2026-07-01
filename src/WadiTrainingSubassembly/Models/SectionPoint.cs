namespace WadiTraining.Models;

internal readonly record struct SectionPoint(double X, double Y)
{
    public static SectionPoint Lerp(SectionPoint a, SectionPoint b, double x)
    {
        var dx = b.X - a.X;
        if (Math.Abs(dx) < 1e-9)
        {
            return new SectionPoint(x, a.Y);
        }

        var t = (x - a.X) / dx;
        return new SectionPoint(x, a.Y + (b.Y - a.Y) * t);
    }
}
