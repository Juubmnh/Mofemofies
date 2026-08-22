namespace Mofemofies.Components;

public class CurvedValue<TSource, TValue>
    where TSource : class
{
    public TSource? Source { get; set; }
    public Func<TSource, TValue>? Curve { get; set; }
    public TValue Value
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Source);
            ArgumentNullException.ThrowIfNull(Curve);
            return Curve.Invoke(Source);
        }
    }

    public static implicit operator TValue(CurvedValue<TSource, TValue> curvedValue)
        => curvedValue.Value;
}

public delegate double Curve(double t);

public static class CurvedValue
{
    public static CurvedValue<MofiEvent, double> PackProgress(this MofiEvent associatedEvent,
        Curve curve) => new()
        {
            Source = associatedEvent,
            Curve = e => curve.Invoke(e.Progress!.Value)
        };

    /// <summary>
    /// 𝒇(𝒕)=𝒄.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static double Constant(double t, double c) => c;

    /// <summary>
    /// 𝒇(𝒕)=𝒌𝒕+𝒃.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="k"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double Linear(double t, double k, double b)
        => k * t + b;

    /// <summary>
    /// 𝒇(𝒕)=𝒂𝒕²+𝒃𝒕+𝒄.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static double Quadratic(double t, double a, double b, double c)
        => a * Math.Pow(t, 2) + b * t + c;

    /// <summary>
    /// 𝒇(𝒕)=𝒂(𝒕-𝒙₀)²+𝒚₀.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="a"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <returns></returns>
    public static double QuadraticVertexForm(double t, double a, double x0, double y0)
        => a * Math.Pow(t - x0, 2) + y0;

    /// <summary>
    /// 𝒇(𝒕)=𝒂𝒕³+𝒃𝒕²+𝒄𝒕+𝒅.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static double Cubic(double t, double a, double b, double c, double d)
        => a * Math.Pow(t, 3) + b * Math.Pow(t, 2) + c * t + d;

    /// <summary>
    /// 𝒇(𝒕)=𝒂₀+𝒂₁𝒕+𝒂₂𝒕²+𝒂₃𝒕³+𝒂₄𝒕⁴+⋯
    /// </summary>
    /// <param name="t"></param>
    /// <param name="ascendingFactors"></param>
    /// <returns></returns>
    public static double Polynomial(double t, params double[] ascendingFactors)
    {
        (_, var acc) = ascendingFactors.Aggregate((0, 0.0), (tuple, factor) =>
        {
            (var exp, var acc) = tuple;
            var term = factor * Math.Pow(t, exp);
            return (exp + 1, acc + term);
        });
        return acc;
    }

    /// <summary>
    /// 𝒇(𝒕)=(𝟏-𝒕)𝒂+𝒕𝒃.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double Lerp(double t, double a, double b)
        => a + t * (b - a);

    /// <summary>
    /// De Casteljau's algorithm.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="controlPoints"></param>
    /// <returns></returns>
    public static double Bezier(double t, params double[] controlPoints)
    {
        for (var i = 0; i < controlPoints.Length - 1; i++)
        {
            for (var j = 0; j < controlPoints.Length - i; j++)
            {
                controlPoints[j] = Lerp(t, controlPoints[j], controlPoints[j + 1]);
            }
        }

        return controlPoints[0];
    }

    /// <summary>
    /// 𝒇(𝒕)=𝒌/(𝒕-𝒙₀)+𝒚₀.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="k"></param>
    /// <param name="x0"></param>
    /// <returns></returns>
    public static double Inverse(double t, double k, double x0, double y0)
        => k / (t - x0) + y0;

    /// <summary>
    /// Mobius linear fractional transformation.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static double LinearFractional(double t, double a, double b, double c, double d)
        => (a * t + b) / (c * t + d);

    /// <summary>
    /// Find the last key of <paramref name="pairs"/> that satisfies
    /// <paramref name="t"/> is greater than or equal to the key
    /// and invoke the corresponding curve function.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public static double PiecewiseGreater(double t, params KeyValuePair<double, Curve>[] pairs)
    {
        var curve = pairs.Last(pair => t >= pair.Key).Value;
        return curve.Invoke(t);
    }

    /// <summary>
    /// Find the first key of <paramref name="pairs"/> that satisfies
    /// <paramref name="t"/> is less than or equal to the key
    /// and invoke the corresponding curve function.
    /// </summary>
    /// <param name="t"></param>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public static double PiecewiseLess(double t, params KeyValuePair<double, Curve>[] pairs)
    {
        var curve = pairs.First(pair => t <= pair.Key).Value;
        return curve.Invoke(t);
    }
}
