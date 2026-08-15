using Mofemofies.Data;

namespace Mofemofies.Components;

public class CWhenSuddenly : IMofiDisplay, IResettable
{
    protected bool _lastResult;

    public Func<bool>? Condition { get; set; }
    public Action? Execute { get; set; }

    void IResettable.Reset()
    {
        _lastResult = false;

        Condition = null;
        Execute = null;
    }

    public virtual void Update(DisplayCallStack sender, long frame)
    {
        if (Condition is null || Execute is null)
        {
            return;
        }

        var result = Condition();
        if (result && !_lastResult)
        {
            Execute();
        }

        _lastResult = result;
    }
}
