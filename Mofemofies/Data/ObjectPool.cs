using System.Collections.Concurrent;

namespace Mofemofies.Data;

public interface IResettable
{
    void Reset();
}

public static class ObjectPool
{
    private static readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _typedPools = [];

    public static T Rent<T>() where T : class, new()
    {
        var bag = _typedPools.GetOrAdd(typeof(T), []);
        if (!bag.TryTake(out var obj))
        {
            obj = new T();
        }

        return (T)obj;
    }

    public static int Count<T>() where T : class, new()
        => _typedPools.TryGetValue(typeof(T), out var bag) ? bag.Count : 0;

    public static void Return<T>(T obj) where T : class, new()
    {
        if (obj is IResettable resettable)
        {
            resettable.Reset();
        }

        var bag = _typedPools.GetOrAdd(typeof(T), []);
        bag.Add(obj);
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
        }
    }

    public static void Clear()
    {
        foreach ((_, var bag) in _typedPools)
        {
            foreach (var obj in bag)
            {
                if (obj is IResettable resettable)
                {
                    resettable.Reset();
                }
            }
        }

        _typedPools.Clear();
    }
}
