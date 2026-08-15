using System.Collections.Concurrent;
using System.Diagnostics;

namespace Mofemofies.Data;

/// <summary>
/// Instances of class implementing this interface can be automatically reset
/// when returned to the <see cref="ObjectPool"/>.
/// </summary>
public interface IResettable
{
    void Reset();
}

public static class ObjectPool
{
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _typedPools = [];

    public static bool ShowDebugInfo { get; set; }

    public static T Rent<T>() where T : class, new()
    {
        var bag = _typedPools.GetOrAdd(typeof(T), []);
        if (!bag.TryTake(out var obj))
        {
            obj = new T();
            Debug.WriteLineIf(ShowDebugInfo, $"[new] {typeof(T)}");
        }

        Debug.WriteLineIf(ShowDebugInfo, $"[{nameof(Rent)}] {typeof(T)}");
        return (T)obj;
    }

    public static int Count<T>() where T : class, new()
        => _typedPools.TryGetValue(typeof(T), out var bag) ? bag.Count : 0;

    public static void Return<T>(T obj) where T : class, new()
    {
        if (obj is IResettable resettable)
        {
            resettable.Reset();
            Debug.WriteLineIf(ShowDebugInfo, $"[{nameof(IResettable.Reset)}] {obj.GetType()}");
        }

        var bag = _typedPools.GetOrAdd(typeof(T), []);
        bag.Add(obj);
        Debug.WriteLineIf(ShowDebugInfo, $"[{nameof(Return)}] {typeof(T)}");
    }

    public static void Retain<T>(int count) where T : class, new()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0, nameof(count));
        if (_typedPools.TryGetValue(typeof(T), out var bag))
        {
            var removeCount = bag.Count - count;
            for (var i = 0; i < removeCount; i++)
            {
                if (!bag.TryTake(out _))
                {
                    break;
                }
            }

            Debug.WriteLineIf(ShowDebugInfo, $"[{nameof(Retain)}] {typeof(T)} (remove: {removeCount})");
        }
    }

    public static void Clear()
    {
        _typedPools.Clear();
        Debug.WriteLineIf(ShowDebugInfo, $"[{nameof(Clear)}] {nameof(ObjectPool)}");
    }
}
