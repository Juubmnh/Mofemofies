namespace Mofemofies.Data;

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
    /// <summary>
    /// 𝒇(𝒕)=𝒄𝒖𝒓𝒗𝒆(𝒐𝒕𝒉𝒆𝒓(𝒕)).
    /// </summary>
    /// <param name="curve"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Curve Combine(this Curve curve, Curve other)
        => t => curve.Invoke(other.Invoke(t));

    /// <summary>
    /// 𝒇(𝒕)=𝒄.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Curve Constant(double c)
        => t => c;

    public static Curve Pow(double exp)
        => t => Math.Pow(t, exp);

    /// <summary>
    /// 𝒇(𝒕)=𝒌𝒕+𝒃.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Curve Linear(double k, double b)
        => t => k * t + b;

    /// <summary>
    /// 𝒇(𝒕)=𝒂𝒕²+𝒃𝒕+𝒄.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Curve Quadratic(double a, double b, double c)
        => t => a * Math.Pow(t, 2) + b * t + c;

    /// <summary>
    /// 𝒇(𝒕)=𝒂(𝒕-𝒙₀)²+𝒚₀.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <returns></returns>
    public static Curve QuadraticVertexForm(double a, double x0, double y0)
        => t => a * Math.Pow(t - x0, 2) + y0;

    /// <summary>
    /// 𝒇(𝒕)=𝒂𝒕³+𝒃𝒕²+𝒄𝒕+𝒅.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Curve Cubic(double a, double b, double c, double d)
        => t => a * Math.Pow(t, 3) + b * Math.Pow(t, 2) + c * t + d;

    /// <summary>
    /// 𝒇(𝒕)=𝒂₀+𝒂₁𝒕+𝒂₂𝒕²+𝒂₃𝒕³+𝒂₄𝒕⁴+⋯.
    /// </summary>
    /// <param name="ascendingFactors"></param>
    /// <returns></returns>
    public static Curve Polynomial(params double[] ascendingFactors)
        => t =>
        {
            (_, var acc) = ascendingFactors.Aggregate((0, 0.0), (tuple, factor) =>
            {
                (var exp, var acc) = tuple;
                var term = factor * Math.Pow(t, exp);
                return (exp + 1, acc + term);
            });
            return acc;
        };

    /// <summary>
    /// 𝒇(𝒕)=(𝟏-𝒕)𝒂+𝒕𝒃.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Curve Lerp(double a, double b)
        => t => a + t * (b - a);

    /// <summary>
    /// De Casteljau's algorithm.
    /// </summary>
    /// <param name="controlPoints"></param>
    /// <returns></returns>
    public static Curve Bezier(params double[] controlPoints)
        => t =>
        {
            for (var i = 0; i < controlPoints.Length - 1; i++)
            {
                for (var j = 0; j < controlPoints.Length - i; j++)
                {
                    controlPoints[j] = Lerp(controlPoints[j], controlPoints[j + 1])(t);
                }
            }

            return controlPoints[0];
        };

    /// <summary>
    /// 𝒇(𝒕)=𝒌/(𝒕-𝒙₀)+𝒚₀.
    /// </summary>
    /// <param name="k"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <returns></returns>
    public static Curve Inverse(double k, double x0, double y0)
        => t => k / (t - x0) + y0;

    /// <summary>
    /// Mobius linear fractional transformation.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public static Curve LinearFractional(double a, double b, double c, double d)
        => t => (a * t + b) / (c * t + d);

    /// <summary>
    /// 𝒇(𝒕)=𝒂 𝒔𝒊𝒏(𝜔𝒕+𝜑)+𝒌.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="omega"></param>
    /// <param name="phi"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static Curve Sin(double a, double omega, double phi, double k)
        => t => a * Math.Sin(omega * t + phi) + k;

    /// <summary>
    /// 𝒇(𝒕)=𝒂 𝒄𝒐𝒔(𝜔𝒕+𝜑)+𝒌.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="omega"></param>
    /// <param name="phi"></param>
    /// <param name="k"></param>
    /// <returns></returns>
    public static Curve Cos(double a, double omega, double phi, double k)
        => t => a * Math.Cos(omega * t + phi) + k;

    public static Curve Exp(double baseNum = double.E)
        => t => Math.Pow(baseNum, t);

    public static Curve Log(double baseNum = double.E)
        => t => Math.Log(t, baseNum);

    /// <summary>
    /// Find the last key of <paramref name="pairs"/> that satisfies
    /// the parameter is greater than or equal to the key
    /// and invoke the corresponding curve function.
    /// </summary>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public static Curve PiecewiseGreater(params KeyValuePair<double, Curve>[] pairs)
        => t =>
        {
            var curve = pairs.Last(pair => t >= pair.Key).Value;
            return curve.Invoke(t);
        };

    /// <summary>
    /// Find the first key of <paramref name="pairs"/> that satisfies
    /// the parameter is less than or equal to the key
    /// and invoke the corresponding curve function.
    /// </summary>
    /// <param name="pairs"></param>
    /// <returns></returns>
    public static Curve PiecewiseLess(params KeyValuePair<double, Curve>[] pairs)
        => t =>
        {
            var curve = pairs.First(pair => t <= pair.Key).Value;
            return curve.Invoke(t);
        };

    public static CurvedValue<MofiEvent, double> PackProgress(this MofiEvent associatedEvent,
        Curve curve) => new()
        {
            Source = associatedEvent,
            Curve = e => curve.Invoke(e.Progress!.Value)
        };
}
