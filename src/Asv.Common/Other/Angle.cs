using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    /// <summary>
    /// Provides utility methods for working with angles.
    /// </summary>
    public static partial class Angle
    {
        [GeneratedRegex(
            @"^(-?((0|[1-9][0-9]?|1[0-7][0-9]|180|360)(\.\d{1,6})?)|360(\.0{1,6})?)$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleInDegrees();

        private static readonly Regex AngleInDegrees = GetAngleInDegrees();

        [GeneratedRegex(
            @"((?<sign>(\-|\+))?(?<deg>\d+)(°|˚|º|\^|~|\*|\s|\-|_)*(((?<min>[0-5]?\d|\d)?)?)('|′|\s|\-|_)*(?<sec>(([0-5]?\d|\d)([.]\d*)?))?(""|¨|˝|\s|\-|_)*)[\s]*$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleRegex();

        private static readonly Regex AngleRegex = GetAngleRegex();

        [GeneratedRegex(@"^0+(?=\d+$)")]
        private static partial Regex GetCutOffZeroRegex();

        private static readonly Regex CutOffZeroRegex = GetCutOffZeroRegex();

        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }

        public static string? GetErrorMessage(string value)
        {
            return !IsValid(value) ? RS.Angle_ErrorMessage : null;
        }

        public static bool TryParse(string value, out double angle)
        {
            // Initialize a new angle value with NaN
            angle = double.NaN;

            // Checks whether value is null or whitespace
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Replace coma by dot
            value = value.Replace(',', '.');

            if (AngleInDegrees.IsMatch(value))
            {
                if (
                    double.TryParse(
                        value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out angle
                    )
                )
                {
                    return true;
                }
            }

            // Checking value on specified potential regex matches
            var match = AngleRegex.Match(value);

            // Trying to parse value in angle as double
            if (match.Success == false)
            {
                var sign1 = 1.0;
                value = value.Replace("+", string.Empty);

                if (value.Contains('-'))
                {
                    sign1 = -1.0;
                    value = value.Replace("-", string.Empty);
                }

                value = CutOffZeroRegex.Replace(value, string.Empty);
                var result = double.TryParse(
                    value,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out angle
                );
                if (result)
                {
                    angle *= sign1;
                }

                return result;
            }

            // Getting all matching groups
            var signGroup = match.Groups["sign"];
            var degGroup = match.Groups["deg"];
            var minGroup = match.Groups["min"];
            var secGroup = match.Groups["sec"];

            if (!degGroup.Success)
            {
                return false;
            }

            if (
                !int.TryParse(
                    degGroup.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var deg
                )
            )
            {
                return false;
            }

            // if only seconds without minutes => error
            if (secGroup.Success && minGroup.Success == false)
            {
                return false;
            }

            // Initialize a new minutes and seconds values with zeros
            var min = 0;
            var sec = 0.0;

            if (minGroup.Success)
            {
                if (
                    !int.TryParse(
                        minGroup.Value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out min
                    )
                )
                {
                    return false;
                }
            }

            if (secGroup.Success)
            {
                if (
                    !double.TryParse(
                        secGroup.Value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out sec
                    )
                )
                {
                    return false;
                }
            }

            var sign = 1;
            if (signGroup.Success)
            {
                sign = signGroup.Value.Equals("-") ? -1 : 1;
            }

            angle = sign * (deg + (min / 60.0) + (sec / 3600.0));

            return !double.IsNaN(angle);
        }

        public static string PrintDms(double decimalDegrees)
        {
            var degrees = (int)Math.Abs(decimalDegrees);
            var remainingDegrees = Math.Abs(decimalDegrees) - degrees;
            var minutes = (int)(remainingDegrees * 60);
            var remainingMinutes = (remainingDegrees * 60) - minutes;
            var seconds = Math.Round(remainingMinutes * 60, 2);
            while (seconds >= 60d)
            {
                minutes++;
                seconds -= 60;
            }

            return $"{Math.Sign(decimalDegrees) * degrees:00}°{minutes:00}′{seconds:00.00}˝";
        }
    }
}
