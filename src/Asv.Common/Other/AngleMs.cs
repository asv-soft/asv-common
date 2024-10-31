using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    /// <summary>
    /// Provides methods for working with angle values in degrees, minutes, and seconds format.
    /// </summary>
    public partial class AngleMs
    {
        [GeneratedRegex(
            @"^(-?((0|[1-9][0-9]?|1[0-7][0-9]|180|360)(\.\d{1,6})?)|360(\.0{1,6})?)$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleInDegrees();

        private static readonly Regex AngleInDegrees = GetAngleInDegrees();

        [GeneratedRegex(
            @"((?<sign>(\-|\+))?(?<min>\d+)('|′|\s|\-|_)*(?<sec>(([0-5]?\d|\d)([.]\d*)?))?(""|¨|˝|\s|\-|_)*)[\s]*$",
            RegexOptions.Compiled
        )]
        private static partial Regex GetAngleMsRegex();

        private static readonly Regex AngleMsRegex = GetAngleMsRegex();

        [GeneratedRegex(@"^0+(?=\d+$)")]
        private static partial Regex GetCutOffZeroRegex();

        private static readonly Regex CutOffZeroRegex = GetCutOffZeroRegex();

        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }

        public static string? GetErrorMessage(string value)
        {
            return IsValid(value) == false ? RS.AngleMs_ErrorMessage : null;
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
            var match = AngleMsRegex.Match(value);

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
            var minGroup = match.Groups["min"];
            var secGroup = match.Groups["sec"];

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

            angle = sign * ((min / 60.0d) + (sec / 3600.0d));

            return !double.IsNaN(angle);
        }

        public static string PrintMs(double decimalDegrees)
        {
            var degrees = (int)Math.Abs(decimalDegrees);
            var remainingDegrees = Math.Abs(decimalDegrees) - degrees;
            var minutes = (int)(decimalDegrees * 60);
            var remainingMinutes = (remainingDegrees * 60) - (int)(remainingDegrees * 60);
            var seconds = Math.Round(remainingMinutes * 60, 2);
            while (seconds >= 60d)
            {
                minutes++;
                seconds -= 60;
            }

            return $"{Math.Sign(decimalDegrees) * minutes:00}′{seconds:00.00}˝";
        }
    }
}
