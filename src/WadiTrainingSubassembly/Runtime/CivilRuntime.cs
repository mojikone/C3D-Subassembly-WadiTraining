using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using Autodesk.Civil.Runtime;

namespace WadiTraining.Runtime;

internal sealed class CivilRuntime
{
    private readonly CorridorState _state;

    public CivilRuntime(CorridorState state)
    {
        _state = state;
    }

    public CorridorState State => _state;

    public bool IsLayoutMode => (int)_state.Mode == 1;

    public double CurrentStation => _state.CurrentStation;

    public double OriginOffset => _state.CurrentAlignmentIsOffsetAlignment && _state.CurrentAssemblyOffsetIsFixed
        ? _state.CurrentOffset + _state.CurrentAssemblyFixedOffset
        : _state.CurrentOffset;

    public double OriginElevation => _state.CurrentAlignmentIsOffsetAlignment && _state.CurrentAssemblyOffsetIsFixed
        ? _state.CurrentElevation + _state.CurrentAssemblyFixedElevation
        : _state.CurrentElevation;

    public ObjectId AlignmentId => _state.CurrentAlignmentIsOffsetAlignment && _state.CurrentAssemblyOffsetIsFixed
        ? _state.CurrentBaselineId
        : _state.CurrentAlignmentId;

    public double GetDouble(string name, double fallback)
    {
        try
        {
            return _state.ParamsDouble.Value(name);
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
            return _state.ParamsLong.Value(name);
        }
        catch
        {
            return fallback;
        }
    }

    public string GetString(string name, string fallback)
    {
        try
        {
            return _state.ParamsString.Value(name);
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
            surfaceId = _state.ParamsSurface.Value(name);
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
            target = _state.ParamsOffsetTarget.Value(name);
            return target != null;
        }
        catch
        {
            target = null!;
            return false;
        }
    }
}
