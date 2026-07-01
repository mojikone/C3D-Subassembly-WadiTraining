using Autodesk.Civil;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.Runtime;

namespace WadiTraining.Runtime;

public abstract class CivilSubassemblyBase
{
    public void GetLogicalNames()
    {
        Run(state => RegisterLogicalNames(state));
    }

    public void GetInputParameters()
    {
        Run(state => RegisterInputParameters(state));
    }

    public void GetOutputParameters()
    {
        Run(state => RegisterOutputParameters(state));
    }

    public void Draw()
    {
        Run(Draw);
    }

    protected virtual void RegisterLogicalNames(CorridorState state)
    {
    }

    protected virtual void RegisterInputParameters(CorridorState state)
    {
    }

    protected virtual void RegisterOutputParameters(CorridorState state)
    {
    }

    protected abstract void Draw(CorridorState state);

    private static void Run(Action<CorridorState> action)
    {
        CorridorState? state = null;
        try
        {
            state = CivilApplication.ActiveDocument.CorridorState;
            action(state);
        }
        catch (Exception ex)
        {
            if (state == null)
            {
                throw;
            }

            state.RecordError((CorridorError)(-2147221503), (CorridorErrorLevel)3, ex.Message, ex.Source ?? "WadiTraining", true);
        }
    }
}
