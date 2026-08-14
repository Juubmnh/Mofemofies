using Mofemofies.Data;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Mofemofies;

public abstract class MofiObject<TComponent> where TComponent : class
{
    protected readonly Dictionary<Type, TComponent> _components = [];
    private static readonly ConcurrentDictionary<Type, Action<object>> _delegateCache = [];

    public void QueryComponent<T>(Action<T>? modifier = null) where T : class, TComponent, new()
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            modifier?.Invoke((T)component);
            return;
        }

        var newComponent = ObjectPool.Rent<T>();
        _components.Add(typeof(T), newComponent);
        modifier?.Invoke(newComponent);
        return;
    }

    public bool HasComponent<T>() where T : class, TComponent, new()
        => _components.ContainsKey(typeof(T));

    public bool RemoveComponent<T>() where T : class, TComponent, new()
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            ObjectPool.Return((T)component);
            _ = _components.Remove(typeof(T));
            return true;
        }

        return false;
    }

    private static Action<object> BuildReturnDelegate(Type type)
    {
        var method = typeof(ObjectPool).GetMethod(nameof(ObjectPool.Return),
                BindingFlags.Public | BindingFlags.Static)!;
        var constructedMethod = method.MakeGenericMethod(type);

        var param = Expression.Parameter(typeof(object), "obj");
        var typedParam = Expression.Convert(param, type);
        var body = Expression.Call(constructedMethod, typedParam);

        return Expression.Lambda<Action<object>>(body, param).Compile();
    }

    public void ClearComponents()
    {
        foreach ((var key, var value) in _components)
        {
            var remove = _delegateCache.GetOrAdd(key, BuildReturnDelegate);
            remove.Invoke(value);
        }

        _components.Clear();
    }
}
