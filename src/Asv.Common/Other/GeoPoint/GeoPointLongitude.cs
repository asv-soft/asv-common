using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static partial class GeoPointLongitude
    {
        private const double Min = -180;
        private const double Max = 180;
        private const string MinusChars = "-Ww";
        [GeneratedRegex(@"^[\+-]?((1[0-7]\d|[1-9]?\d)(\.\d{1,})?|180)\D*[EWew]?$", RegexOptions.Compiled)]
        private static partial Regex GetLongitudeDegreeRegex();
        private static readonly Regex LongitudeDegreeRegex = GetLongitudeDegreeRegex();
        [GeneratedRegex("""^((?<s1>[WwEe+-]?\s*)?(?<deg>[0-9]{0,2}\d|180)\s*([:°˚º^~*°\.\s_-]+)\s*((?<min>[0-5]?\d|\d)(?:\.\d+)?|\d{1,2})\s*([′':;^~*\s_-]*)\s*(?<sec>([0-5]?\d|\d)(?:\.\d+)?\s*)?([""”˝¨^\s_-]*)\s*(?<s2>[WwEe+-]?\s*)?)\s*$""", RegexOptions.Compiled)]
        private static partial Regex GetLongitudeStrongRegex();
        private static readonly Regex LongitudeStrongRegex = GetLongitudeStrongRegex();
        [GeneratedRegex("""^(?<s1>[WwEe+-]?\s*)?(?<deg>\d{1,3})\s*(?<min>\d{2})?\s*(?<sec>\d{2}(\.\d+)?)?\s*(?<s2>[WwEe+-])?$""", RegexOptions.Compiled)]
        private static partial Regex GetLongitudeEasyRegex();
        private static readonly Regex LongitudeEasyRegex = GetLongitudeEasyRegex();
        
        public static bool IsValid(string? value)
        {
            return TryParse(value, out _);
        }
        public static string? GetErrorMessage(string? value)
        {
            return IsValid(value) == false ? RS.GeoPointLongitude_GetErrorMessage : null;
        }

        public static bool TryParse(string? value, out double longitude)
        {
            longitude = Double.NaN;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Replace(',', '.');
            if (LongitudeDegreeRegex.IsMatch(value))
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude))
                {
                    return true;
                }
            }


            var match = LongitudeStrongRegex.Match(value);
            if (match.Success == false)
                match = LongitudeEasyRegex.Match(value);
            if (match.Success == false)
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out longitude) == false)
                    return false;
                return longitude is >= Min and <= Max;
            }

            var degGroup = match.Groups["deg"];
            var minGroup = match.Groups["min"];
            var secGroup = match.Groups["sec"];
            var s1Group = match.Groups["s1"];
            var s2Group = match.Groups["s2"];
            

            if (degGroup.Success == false) return false;
            
            if (degGroup.Success == false && minGroup.Success == false && secGroup.Success == false) return false;
            
            if (int.TryParse(degGroup.Value,NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) == false) return false;
            // if only seconds without minutes => error
            if (secGroup.Success && minGroup.Success == false) return false;
            var min = 0.0;
            var sec = 0.0;
            if (minGroup.Success)
            {
                double.TryParse(minGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out min);
            }
            
            if (secGroup.Success)
            {
                double.TryParse(secGroup.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out sec);
                var valuableDigitsCount = secGroup.Value
                    .Split('.')
                    .Last()
                    .Length;
                sec = Math.Round(sec, valuableDigitsCount);
            }

            var sign1 = 1;
            if (s1Group.Success)
            {
                var s1 = s1Group.Value;
                sign1 = MinusChars.Contains(s1) && s1 != "" ? -1 : 1;
            }

            var sign2 = 1;
            if (s2Group.Success)
            {
                var s2 = s2Group.Value;
                sign2 = MinusChars.Contains(s2) && s2 != "" ? -1 : 1;
            }

            if (s1Group.Value != "" && s2Group.Value != "" && sign1 != sign2)
            {
                return false;
            }
            longitude = sign1 * sign2 * (deg + min / 60 + sec / 3600);
            return longitude is >= Min and <= Max;
        }
        public static string PrintDms(double longitude)
        {
            var degrees = (int)Math.Abs(longitude);
            var remainingDegrees = Math.Abs(longitude) - degrees;
            var minutes = (int)(remainingDegrees * 60);
            var remainingMinutes = (remainingDegrees * 60) - minutes;
            var seconds = Math.Round(remainingMinutes * 60, 2);
            while (seconds >= 60d)
            {
                minutes++;
                seconds -= 60;
            }
            return $"{degrees:000}°{minutes:00}′{seconds:00.00}˝ {(longitude < 0 ? "W" : "E")}";  
        }

       
    }
}