using Mofemofies.Data;

namespace Mofemofies.Components;

public class CWhenChanged : IMofiDisplay, IResettable
{
    protected object? _lastValue;

    public Func<object>? Selector { get; set; }
    public Action<object>? Executor { get; set; }
    public EqualityComparer<object>? Comparer { get; set; }

    void IResettable.Reset()
    {
        _lastValue = null;

        Selector = null;
        Executor = null;
        Comparer = null;
    }

    public virtual void Update(DisplayCallStack sender, long frame)
    {
        if (Selector is null || Executor is null)
        {
            return;
        }

        var value = Selector();
        if (Comparer is null ? !value.Equals(_lastValue) : !Comparer.Equals(value, _lastValue))
        {
            Executor.Invoke(value);
            _lastValue = value;
        }
    }
}
