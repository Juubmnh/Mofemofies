namespace Mofemofies.Data;

public readonly struct ExLong : IComparable<ExLong>, IEquatable<ExLong>
{
    public static readonly ExLong PositiveInfinity = new(false);
    public static readonly ExLong NegativeInfinity = new(true);

    private readonly long? _value;
    private readonly bool _isNegative;

    public bool IsFinite => _value is not null;
    public bool IsInfinite => _value is null;
    public bool IsPositiveInfinity => _value is null && !_isNegative;
    public bool IsNegativeInfinity => _value is null && _isNegative;

    public ExLong() : this(0)
    {

    }

    public ExLong(long value)
    {
        _value = value;
    }

    private ExLong(bool isNegative)
    {
        _value = null;
        _isNegative = isNegative;
    }

    public static implicit operator ExLong(long obj) => new(obj);
    public static explicit operator long(ExLong obj) => obj._value ?? throw new InvalidCastException($"Can't convert an infinity into {typeof(long)}.");

    public int CompareTo(ExLong other)
    {
        if (_value is null && other._value is null)
        {
            return _isNegative == other._isNegative
                ? throw new InvalidOperationException("Infinities with the same sign can't be compared.")
                : _isNegative ? -1 : 1;
        }
        else if (_value is null)
        {
            return _isNegative ? -1 : 1;
        }
        else if (other._value is null)
        {
            return other._isNegative ? 1 : -1;
        }

        return _value.Value.CompareTo(other._value);
    }

    public bool Equals(ExLong other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is ExLong other && Equals(other);

    public override int GetHashCode() => _value is null ? _isNegative ? int.MinValue : int.MaxValue : _value.GetHashCode();

    public static bool operator ==(ExLong left, ExLong right) => left.Equals(right);

    public static bool operator !=(ExLong left, ExLong right) => !left.Equals(right);

    public static bool operator <(ExLong left, ExLong right) => left.CompareTo(right) < 0;

    public static bool operator <=(ExLong left, ExLong right) => left.CompareTo(right) <= 0;

    public static bool operator >(ExLong left, ExLong right) => left.CompareTo(right) > 0;

    public static bool operator >=(ExLong left, ExLong right) => left.CompareTo(right) >= 0;
}
