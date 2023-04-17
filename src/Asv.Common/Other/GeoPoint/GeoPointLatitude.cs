using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static class GeoPointLatitude
    {
        private const double Min = -90;
        private const double Max = 90;
        private const string MinusChars = "-Ss";
        
        private static readonly Regex LatitudeRegex = new(
            @"((?<s1>[\+\-NnSs])?(?<deg>[0-8]?\d|90)[°˚º^~*\s\-_]+((?<min>[0-5]?\d|\d)?)['′\s\-_]+(((?<sec>[0-5]?\d|\d)([.]\d*)?)?)[""¨˝\s\-_]*(?<s2>[\+\-NnSs])?)[\s]*$", RegexOptions.Compiled);
        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }
        public static string? GetErrorMessage(string value)
        {
            return IsValid(value) == false ? RS.GeoPointLatitude_ErrorMessage : null;
        }

        public static bool TryParse(string value, out double latitude)
        {
            latitude = Double.NaN;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Replace(',', '.');
            var match = LatitudeRegex.Match(value);
            if (match.Success == false)
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude) == false)
                    return false;
                return latitude is >= Min and <= Max;
            }

            var degGroup = match.Groups["deg"];
            var minGroup = match.Groups["min"];
            var secGroup = match.Groups["sec"];
            var s1Group = match.Groups["s1"];
            var s2Group = match.Groups["s2"];
            

            if (degGroup.Success == false) return false;
            
            if (int.TryParse(degGroup.Value,NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) == false) return false;
            // if only seconds without minutes => error
            if (secGroup.Success && minGroup.Success == false) return false;
            var min = 0;
            var sec = 0.0;
            if (minGroup.Success)
            {
                if (int.TryParse(minGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out min) == false) return false;
            }
            
            if (secGroup.Success)
            {
                if (double.TryParse(secGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out sec) == false) return false;
            }

            var sign1 = 1;
            if (s1Group.Success)
            {
                var s1 = s1Group.Value;
                sign1 = MinusChars.Contains(s1) ? -1 : 1;
            }

            var sign2 = 1;
            if (s2Group.Success)
            {
                var s2 = s2Group.Value;
                sign2 = MinusChars.Contains(s2) ? -1 : 1;
            }

            if (s1Group.Success && s2Group.Success && sign1 != sign2)
            {
                return false;
            }
            latitude = sign1 * (deg + (double)min / 60 + sec / 3600);
            return latitude is >= Min and <= Max;
        }
        public static string PrintDms(double latitude)
        {
            var minutes = (latitude - (int)latitude) * 60;
            return $"{latitude:F0}°{(int)minutes}′{(minutes - (int)minutes) * 60:F2}˝ {(latitude<0? "S" : "N")}";  
        }
    }
}