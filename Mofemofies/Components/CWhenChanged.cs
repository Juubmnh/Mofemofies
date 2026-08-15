using Mofemofies.Data;

namespace Mofemofies.Components;

public class CWhenChanged : IMofiDisplay, IResettable
{
    protected object? _lastValue;

    public Func<object>? Selector { get; set; }
    public Action<object>? Execute { get; set; }
    public EqualityComparer<object>? Comparer { get; set; }

    void IResettable.Reset()
    {
        _lastValue = null;

        Selector = null;
        Execute = null;
        Comparer = null;
    }

    public virtual void Update(DisplayCallStack sender, long frame)
    {
        if (Selector is null || Execute is null)
        {
            return;
        }

        var value = Selector();
        if (Comparer is null ? !value.Equals(_lastValue) : !Comparer.Equals(value, _lastValue))
        {
            Execute.Invoke(value);
            _lastValue = value;
        }
    }
}
