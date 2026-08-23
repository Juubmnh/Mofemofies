namespace Mofemofies.Data;

public interface IYoneda<TSource>
{
    IEnumerable<TResult> Apply<TResult>(Func<TSource, TResult> mapper);
}

public static class YonedaExtensions
{
    private sealed class YonedaImpl<TSource>(IEnumerable<TSource> source) : IYoneda<TSource>
    {
        private readonly IEnumerable<TSource> _source = source;

        public IEnumerable<TResult> Apply<TResult>(Func<TSource, TResult> mapper)
            => _source.Select(mapper);
    }

    private sealed class MappedYoneda<TSource, TTarget>(IYoneda<TSource> prev, Func<TSource, TTarget> fn) : IYoneda<TTarget>
    {
        private readonly IYoneda<TSource> _prev = prev;
        private readonly Func<TSource, TTarget> _fn = fn;

        public IEnumerable<TResult> Apply<TResult>(Func<TTarget, TResult> mapper)
            => _prev.Apply(x => mapper(_fn(x)));
    }

    public static IYoneda<T> ToYoneda<T>(this IEnumerable<T> source)
        => new YonedaImpl<T>(source);

    public static IYoneda<T2> Map<T1, T2>(this IYoneda<T1> yoneda, Func<T1, T2> fn)
        => new MappedYoneda<T1, T2>(yoneda, fn);

    public static IEnumerable<T> FromYoneda<T>(this IYoneda<T> yoneda)
        => yoneda.Apply(x => x);
}
