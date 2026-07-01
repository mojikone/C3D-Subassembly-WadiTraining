namespace WadiTraining.Models;

internal sealed record ProtectionInterval(double StartX, double EndX, string Code)
{
    public ProtectionInterval Normalize()
    {
        return StartX <= EndX ? this : this with { StartX = EndX, EndX = StartX };
    }
}
