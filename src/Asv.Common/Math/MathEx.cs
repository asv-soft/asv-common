using System;
using System.Linq;

namespace Asv.Common
{
    public class MathEx
    {
        private const double Pi2 = 2 * Math.PI;

        /// <summary>
        /// Fast implementation of the square root function Math.Sqrt(x^2 + y^2).
        /// </summary>
        /// <param name="x">The number.</param>
        /// <param name="y">The number.</param>
        /// <returns>The absolute value of <paramref name="x"/> and <paramref name="y"/>.</returns>
        public static double Abs(double x, double y)
        {
            if (double.IsInfinity(x) || double.IsInfinity(y))
            {
                return double.PositiveInfinity;
            }

            var num1 = Math.Abs(x);
            var num2 = Math.Abs(y);
            if (num1 > num2)
            {
                var num3 = num2 / num1;
                return num1 * Math.Sqrt(1.0 + (num3 * num3));
            }

            if (num2 == 0.0)
            {
                return num1;
            }

            var num4 = num1 / num2;
            return num2 * Math.Sqrt(1.0 + (num4 * num4));
        }

        public static string AngleToDegreeString(
            double angleValue,
            string formatStringForMinute = "00.00",
            string nanString = "-"
        )
        {
            if (double.IsNaN(angleValue))
            {
                return string.Empty;
            }

            var deg = (int)angleValue;
            angleValue = Math.Abs(angleValue - deg);
            var min = angleValue * 60;
            return $"{deg:D}° {min.ToString(formatStringForMinute)}′";
        }

        /// <summary>
        ///   Interpolates data using a piece-wise linear function.
        /// </summary>
        /// <param name="value">The value to be calculated.</param>
        /// <param name="x">The input data points <c>x</c>. Those values need to be sorted.</param>
        /// <param name="y">The output data points <c>y</c>.</param>
        /// <param name="lower">
        /// The value to be returned for values before the first point in <paramref name="x" />.</param>
        /// <param name="upper">
        /// The value to be returned for values after the last point in <paramref name="x" />.</param>
        /// <returns>Computes the output for f(value) by using a piecewise linear
        /// interpolation of the data points <paramref name="x" /> and <paramref name="y" />.</returns>
        public static double Interpolate1D(
            double value,
            double[] x,
            double[] y,
            double lower,
            double upper
        )
        {
            for (var index1 = 0; index1 < x.Length; ++index1)
            {
                if (value < x[index1])
                {
                    if (index1 == 0)
                    {
                        return lower;
                    }

                    var index2 = index1 - 1;
                    var index3 = index1;
                    var num = (value - x[index2]) / (x[index3] - x[index2]);
                    return y[index2] + ((y[index3] - y[index2]) * num);
                }
            }

            return upper;
        }

        public static void LeastSquaresMethod(double[] x, double[] y, out double a, out double b)
        {
            ArgumentNullException.ThrowIfNull(x);

            ArgumentNullException.ThrowIfNull(y);

            var length = Math.Min(x.Length, y.Length);
            if (length == 0)
            {
                a = 0;
                b = 0;
                return;
            }

            double sumx = 0;
            double sumy = 0;
            double sumx2 = 0;
            double sumxy = 0;
            for (var i = 0; i < length; i++)
            {
                sumx += x[i];
                sumy += y[i];
                sumx2 += x[i] * x[i];
                sumxy += x[i] * y[i];
            }

            a = ((length * sumxy) - (sumx * sumy)) / ((length * sumx2) - (sumx * sumx));
            b = (sumy - (a * sumx)) / length;
        }

        public static void SingleLeastSquaresMethod(double[] x, double[] y, out double a)
        {
            ArgumentNullException.ThrowIfNull(x);

            ArgumentNullException.ThrowIfNull(y);

            var length = Math.Min(x.Length, y.Length);
            if (length == 0)
            {
                a = 0;
                return;
            }

            double sumx2 = 0;
            double sumxy = 0;

            for (var i = 0; i < length; i++)
            {
                sumx2 += x[i] * x[i];
                sumxy += x[i] * y[i];
            }

            a = sumxy / sumx2;
        }

        /// <summary>
        /// Calculate мВт в мкВ
        /// </summary>
        /// <param name="mW">.</param>
        /// <param name="ohm">.</param>
        /// <returns></returns>
        public static double Mw2Uv(double mW, double ohm)
        {
            return Math.Sqrt((mW / 1000) * ohm) * 1e6;
        }

        /// <summary>
        /// Calculate дБм в мВт
        /// </summary>
        /// <param name="mW">.</param>
        /// <returns></returns>
        public static double Mw2dBm(double mW)
        {
            return 10 * Math.Log10(mW);
        }

        /// <summary>
        /// Calculate мВт в дБм
        /// </summary>
        /// <param name="dBm">.</param>
        /// <returns></returns>
        public static double DBm2Mw(double dBm)
        {
            return Math.Pow(10, dBm / 10);
        }

        /// <summary>
        /// Calculate dBm в мкВ
        /// </summary>
        /// <param name="dBm">.</param>
        /// <param name="ohm">.</param>
        /// <returns></returns>
        public static double DBm2Uv(double dBm, double ohm)
        {
            return Mw2Uv(DBm2Mw(dBm), ohm);
        }

        public static double GetAvgAngleDeg(double[] angles)
        {
            // https://rosettacode.org/wiki/Averages/Mean_angle#C.23
            var x = angles.Sum(a => Math.Cos(a * Math.PI / 180)) / angles.Length;
            var y = angles.Sum(a => Math.Sin(a * Math.PI / 180)) / angles.Length;
            return Math.Atan2(y, x) * 180 / Math.PI;
        }

        public static double GetDistanceAngleDeg(double a, double b)
        {
            // https://en.wikipedia.org/wiki/Mean_of_circular_quantities
            var distance = (a - b) % 360;
            if (distance < -180)
            {
                distance += 360;
            }
            else if (distance > 179)
            {
                distance -= 360;
            }

            return distance;
        }

        public static double GetDistanceAngleRad(double a, double b)
        {
            var distance = a - b;

            while (distance >= Pi2)
            {
                distance -= Pi2;
            }

            while (distance <= -Pi2)
            {
                distance += Pi2;
            }

            if (distance < -Math.PI)
            {
                distance += Pi2;
            }
            else if (distance > Math.PI)
            {
                distance -= Pi2;
            }

            return distance;
        }
    }
}
