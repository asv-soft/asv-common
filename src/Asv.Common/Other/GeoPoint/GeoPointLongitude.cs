using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static class GeoPointLongitude
    {
        public static readonly Regex LatitudeRegex = new(
            @"^((?<s1>[\+\-EeWw])?(?<deg>[0]?\d?\d|1[0-7]\d|180)[°˚º^~*\s\-_]+(?<min>[0-5]\d|\d)['′\s\-_]+(?<sec>[0-5]?\d|\d)([.]\d*)?[""¨˝\s_]*(?<s2>[\+\-EeWw])?)",RegexOptions.Compiled);
        
        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Replace(',', '.');
            if (LatitudeRegex.IsMatch(value)) return true;
            if (double.TryParse(value, NumberStyles.Any,CultureInfo.InvariantCulture, out var realValue) == false) return false;
            return realValue is >= -90 and <= 90;
        }
        public static string? GetErrorMessage(string value)
        {
            return IsValid(value) == false ? RS.GeoPointLongitude_GetErrorMessage : null;
        }

        public static bool TryParse(string value, out double latitude)
        {
            latitude = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Replace(',', '.');
            if (LatitudeRegex.IsMatch(value))
            {
                var match = LatitudeRegex.Match(value);
                var deg = double.Parse(match.Groups["deg"].Value, CultureInfo.InvariantCulture);
                var min = double.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture);
                var sec = double.Parse(match.Groups["sec"].Value, CultureInfo.InvariantCulture);
                var sign = "W".Equals(match.Groups["s1"].Value, StringComparison.CurrentCultureIgnoreCase) 
                           || "W".Equals(match.Groups["s2"].Value, StringComparison.CurrentCultureIgnoreCase) ? -1 : 1;
                latitude = sign * (deg + min / 60 + sec / 3600);
                return true;
            }
            return double.TryParse(value, NumberStyles.Any,CultureInfo.InvariantCulture, out latitude);
        }
        public static string PrintDms(double latitude)
        {
            var minutes = (latitude - (int)latitude) * 60;
            return $"{latitude:F0}°{(int)minutes}′{(minutes - (int)minutes) * 60:F2}˝ {(latitude<0? "W" : "E")}";  
        }
    }
}