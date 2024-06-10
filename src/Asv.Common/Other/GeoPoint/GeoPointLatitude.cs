using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static class GeoPointLatitude
    {
        private const double Min = -90;
        private const double Max = 90;
        private const string MinusChars = "-Ss";
        
        private static readonly Regex LatitudeDegreeRegex = new(@"^(-?[1-8]?\d(?:\.\d{1,6})?|90(?:\.0{1,6})?)$", RegexOptions.Compiled);
        
        private static readonly Regex LatitudeRegex = new(
            """^(?<s1>[NSns+-]?\s*)?(?<deg>\d{1,2})\s*([:°˚º^~*°\.\s_-]*)\s*(?<min>\d{1,2}(?:\.\d+)?|\d{1,2})\s*([′':;^\s_-]*)\s*(?<sec>\d{1,2}(?:\.\d+)?\s*)?(["”˝¨^\s_-]*)\s*(?<s2>[NSns+-]?\s*)?$""",
        RegexOptions.Compiled);
        public static bool IsValid(string? value)
        {
            return TryParse(value, out _);
        }
        public static string? GetErrorMessage(string? value)
        {
            return IsValid(value) == false ? RS.GeoPointLatitude_ErrorMessage : null;
        }

        public static bool TryParse(string? value, out double latitude)
        {
            latitude = Double.NaN;
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Replace(',', '.');
            if (LatitudeDegreeRegex.IsMatch(value))
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out latitude))
                {
                    return true;
                }
            }
            
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
            
            if (degGroup.Success && minGroup.Success == false && secGroup.Success == false) return false;
            
            if (int.TryParse(degGroup.Value,NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) == false) return false;
            // if only seconds without minutes => error
            if (secGroup.Success && minGroup.Success == false) return false;
            var min = 0.0;
            var sec = 0.0;
            if (minGroup.Success)
            {
                if (double.TryParse(minGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out min) == false) return false;
            }
            
            if (secGroup.Success)
            {
                double.TryParse(secGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out sec);
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
            
            latitude = sign1 * sign2 * (deg + min / 60 + sec / 3600);
            return latitude is >= Min and <= Max;
        }
        
        public static string PrintDms(double latitude)
        {
            var degrees = (int)Math.Abs(latitude);
            var remainingDegrees = Math.Abs(latitude) - degrees;
            var minutes = (int)(remainingDegrees * 60);
            var remainingMinutes = (remainingDegrees * 60) - minutes;
            var seconds = Math.Round(remainingMinutes * 60, 2);
            while (seconds >= 60d)
            {
                minutes++;
                seconds -= 60;
            }
            return $"{degrees:00}°{minutes:00}′{seconds:00.00}˝ {(latitude < 0 ? "S" : "N")}";  
        }
        
    }
}