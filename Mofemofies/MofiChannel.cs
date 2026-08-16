using Mofemofies.Data;
using System.Diagnostics.CodeAnalysis;

namespace Mofemofies;

public sealed class MofiChannel(string? name = null) : IResettable
{
    private readonly SortedSet<MofiEventFactory> _eventFactories
        = new(Comparer<MofiEventFactory>.Create((left, right) => left.StartFrame.CompareTo(right.StartFrame)));
    private MofiEventFactory? _lastQueriedFactory;
    private MofiEvent? _currentEvent;

    public string? Name { get; set; } = name;
    public bool IsFlexibleLength { get; private set; } = true;
    public ExLong? Length { get; private set; }

    public MofiChannel(string name, long length) : this(name)
    {
        SetLength(length);
    }

    public MofiChannel(long length) : this(null)
    {
        SetLength(length);
    }

    void IResettable.Reset()
    {
        _lastQueriedFactory = null;

        _currentEvent?.Dispose();
        _currentEvent = null;
    }

    public void Subscribe(MofiEventFactory factory)
    {
        ArgumentException exception = new($"New {nameof(MofiEventFactory)} overlapping with existing item.", nameof(factory));

        var left = _eventFactories.GetViewBetween(MofiEventFactory.MinValue, factory).Max;
        if (left is not null && left.EndFrame > factory.StartFrame)
        {
            throw exception;
        }

        var right = _eventFactories.GetViewBetween(factory, MofiEventFactory.MaxValue).Min;
        if (right is not null && right.StartFrame < factory.EndFrame)
        {
            throw exception;
        }

        _ = _eventFactories.Add(factory);
    }

    /// <summary>
    /// Gets a view in which the <see cref="MofiEventFactory.StartFrame"/>s of the selected factories
    /// are between <paramref name="lower"/> and <paramref name="upper"/>.
    /// </summary>
    /// <param name="lower">Null for the minimum boundary.</param>
    /// <param name="upper">Null for the maximum boundary.</param>
    /// <returns></returns>
    public SortedSet<MofiEventFactory> SelectBetween(long? lower, long? upper)
    {
        var lowerValue = lower is null ? MofiEventFactory.MinValue : new(lower.Value);
        var upperValue = upper is null ? MofiEventFactory.MaxValue : new(upper.Value);
        return _eventFactories.GetViewBetween(lowerValue, upperValue);
    }

    /// <summary>
    /// Returns true when querying a new event.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="queriedEvent"></param>
    /// <returns></returns>
    public bool QueryEvent(long frame, [NotNullWhen(true)] out MofiEvent? queriedEvent)
    {
        if (_lastQueriedFactory is not null
            && _lastQueriedFactory.StartFrame <= frame && frame < _lastQueriedFactory.EndFrame)
        {
            queriedEvent = _currentEvent;
            return false;
        }

        var closest = _eventFactories.GetViewBetween(MofiEventFactory.MinValue, new(frame)).Max;
        var factory = closest is not null && frame < closest.EndFrame ? closest : null;

        var result = factory is not null && !factory.Equals(_lastQueriedFactory);
        _lastQueriedFactory = factory;
        _currentEvent?.Dispose();
        _currentEvent = factory?.Create();
        queriedEvent = _currentEvent;
        return result;
    }

    public void SetFlexibleLength()
    {
        IsFlexibleLength = true;
        Length = null;
    }

    [MemberNotNull(nameof(Length))]
    public void SetLength(long length)
    {
        IsFlexibleLength = false;
        Length = length;
    }

    public void CheckLength()
    {
        if (IsFlexibleLength)
        {
            Length = _eventFactories.Max is null ? 0 : _eventFactories.Max.EndFrame;
        }
    }
}
