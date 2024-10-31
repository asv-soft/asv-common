using System;

namespace Asv.Common
{
    public static class ToleranceEqualExtension
    {
        public const double CDoubleEpsilon = 1e-9;
        public const float CFloatEpsilon = 1e-4f;

        public static bool EqualsWithTolerance(this int a, int b, int tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static bool EqualsWithTolerance(this long a, long b, long tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static bool EqualsWithTolerance(
            this double a,
            double b,
            double tolerance = CDoubleEpsilon
        )
        {
            return Math.Abs(a - b) < tolerance;
        }

        public static bool EqualsWithTolerance(
            this float a,
            float b,
            float tolerance = CFloatEpsilon
        )
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}
