using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    /// <summary>
    /// Represents an angle in degrees and minutes.
    /// </summary>
    public partial class AngleDm
    {
        [GeneratedRegex(
            @"^(-?((0|[1-9][0-9]?|1[0-7][0-9]|180|360)(\.\d{1,6})?)|360(\.0{1,6})?)$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleInDegreesRegex();

        private static readonly Regex AngleInDegreesRegex = GetAngleInDegreesRegex();

        [GeneratedRegex(
            @"((?<sign>(\-|\+))?(?<deg>\d+)(°|˚|º|\^|~|\*|\s|\-|_)*(((?<min>(([0-5]?\d|\d)([.]\d*)?))?)?)('|′|\s|\-|_)*)[\s]*$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleDmRegex();

        private static readonly Regex AngleDmRegex = GetAngleDmRegex();

        [GeneratedRegex(@"^0+(?=\d+$)")]
        private static partial Regex GetCutOffZeroRegex();

        private static readonly Regex CutOffZeroRegex = GetCutOffZeroRegex();

        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }

        public static string? GetErrorMessage(string value)
        {
            return IsValid(value) == false ? RS.AngleDm_ErrorMessage : null;
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

            if (AngleInDegreesRegex.IsMatch(value))
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
            var match = AngleDmRegex.Match(value);

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

            // Initialize a new minutes and seconds values with zeros
            var min = 0.0;

            if (minGroup.Success)
            {
                if (
                    double.TryParse(
                        minGroup.Value,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out min
                    ) == false
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

            angle = sign * (deg + (min / 60.0));

            return !double.IsNaN(angle);
        }

        public static string PrintDm(double decimalDegrees)
        {
            var degrees = (int)Math.Abs(decimalDegrees);
            var remainingDegrees = Math.Abs(decimalDegrees) - degrees;
            var minutes = Math.Round(remainingDegrees * 60, 2);
            while (minutes >= 60d)
            {
                degrees++;
                minutes -= 60;
            }

            return $"{(Math.Sign(decimalDegrees) > 0 ? string.Empty : "-")}{degrees:00}°{minutes:00.00}′";
        }
    }
}
