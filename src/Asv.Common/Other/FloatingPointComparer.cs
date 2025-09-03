using System.Numerics;

namespace Asv.Common;

public static class FloatingPointComparer
{
    public const double Epsilon = 1e-8;

    /// <summary>
    ///     Determines whether two numbers are approximately equal when |a − b| &lt; epsilon.
    /// </summary>
    public static bool ApproximatelyEquals<T>(this T first, T second, T epsilon) where T : IFloatingPoint<T>
    {
        if (T.IsNegativeInfinity(first) && T.IsNegativeInfinity(second) 
            || T.IsPositiveInfinity(first) && T.IsPositiveInfinity(second) 
            || T.IsNaN(first) && T.IsNaN(second))
        {
            return true;
        }
        
        return T.Abs(first - second) < epsilon;
    }

    /// <summary>
    ///     Determines whether two numbers are approximately equal when |a − b| &lt; <see cref="Epsilon" />.
    /// </summary>
    public static bool ApproximatelyEquals<T>(this T first, T second) where T : IFloatingPoint<T>
    {
        return first.ApproximatelyEquals(second, T.CreateChecked(Epsilon));
    }

    /// <summary>
    ///     Logical negation of <see cref="ApproximatelyEquals{T}(T, T, T)" />.
    /// </summary>
    public static bool ApproximatelyNotEquals<T>(this T first, T second, T epsilon) where T : IFloatingPoint<T>
    {
        return !first.ApproximatelyEquals(second, epsilon);
    }

    /// <summary>
    ///     Logical negation of <see cref="ApproximatelyEquals{T}(T, T)" />.
    /// </summary>
    public static bool ApproximatelyNotEquals<T>(this T first, T second) where T : IFloatingPoint<T>
    {
        return !first.ApproximatelyEquals(second);
    }
}