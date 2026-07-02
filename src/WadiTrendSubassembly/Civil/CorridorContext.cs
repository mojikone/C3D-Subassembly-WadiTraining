// Thin wrapper around Civil 3D CorridorState. The ONLY place that touches raw corridor
// parameter/target collections, so API quirks stay out of the algorithm and drawing code.

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;

namespace WadiTrend.Civil;

public sealed class CorridorContext
{
    public CorridorContext(CorridorState state)
    {
        State = state;
    }

    public CorridorState State { get; }

    public bool IsLayoutMode => (int)State.Mode == 1;

    public double CurrentStation => State.CurrentStation;

    // Offset-assembly handling mirrors the stock subassemblies: when the assembly rides a fixed
    // offset on an offset alignment, work in main-baseline coordinates.
    public double OriginOffset => State.CurrentAlignmentIsOffsetAlignment && State.CurrentAssemblyOffsetIsFixed
        ? State.CurrentOffset + State.CurrentAssemblyFixedOffset
        : State.CurrentOffset;

    public double OriginElevation => State.CurrentAlignmentIsOffsetAlignment && State.CurrentAssemblyOffsetIsFixed
        ? State.CurrentElevation + State.CurrentAssemblyFixedElevation
        : State.CurrentElevation;

    public ObjectId AlignmentId => State.CurrentAlignmentIsOffsetAlignment && State.CurrentAssemblyOffsetIsFixed
        ? State.CurrentBaselineId
        : State.CurrentAlignmentId;

    public double GetDouble(string name, double fallback)
    {
        try
        {
            return State.ParamsDouble.Value(name);
        }
        catch
        {
            return fallback;
        }
    }

    public long GetLong(string name, long fallback)
    {
        try
        {
            return State.ParamsLong.Value(name);
        }
        catch
        {
            return fallback;
        }
    }

    public bool TryGetSurfaceTarget(string name, out ObjectId surfaceId)
    {
        try
        {
            surfaceId = State.ParamsSurface.Value(name);
            return !surfaceId.IsNull;
        }
        catch
        {
            surfaceId = ObjectId.Null;
            return false;
        }
    }

    public bool TryGetOffsetTarget(string name, out WidthOffsetTarget target)
    {
        try
        {
            target = State.ParamsOffsetTarget.Value(name);
            return target != null;
        }
        catch
        {
            target = null!;
            return false;
        }
    }

    public void TrySetLongOutput(string name, int value)
    {
        try
        {
            State.ParamsLong.Add(name, value);
        }
        catch
        {
            // Output params are informational; never fail the draw over them.
        }
    }
}
