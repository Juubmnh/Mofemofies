using Mofemofies.Data;

namespace Mofemofies.Components;

public class CWhenSuddenly : IMofiDisplay, IResettable
{
    protected bool _lastResult;

    public Func<bool>? Condition { get; set; }
    public Action? Executor { get; set; }

    void IResettable.Reset()
    {
        _lastResult = false;

        Condition = null;
        Executor = null;
    }

    public virtual void Update(DisplayCallStack sender, long frame)
    {
        if (Condition is null || Executor is null)
        {
            return;
        }

        var result = Condition();
        if (result && !_lastResult)
        {
            Executor();
        }

        _lastResult = result;
    }
}
