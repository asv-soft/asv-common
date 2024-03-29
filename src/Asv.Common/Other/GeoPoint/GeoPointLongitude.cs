using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static class GeoPointLongitude
    {
        private const double Min = -180;
        private const double Max = 180;
        private const string MinusChars = "-Ww";
        //private static readonly Regex LongitudeDegreeRegex = new(@"^(-?[1]?[0-7]?[0-9](?:\.\d{1,6})?|180(?:\.0{1,6})?)$", RegexOptions.Compiled);
        private static readonly Regex LongitudeDegreeRegex = new(@"^[\+-]?((1[0-7]\d|[1-9]?\d)(\.\d{1,})?|180)\D*[EWew]?$", RegexOptions.Compiled);
        
        private static readonly Regex LongitudeRegex = new(
            @"((?<s1>(\+|\-|E|e|W|w))?(?<deg>[0-9]{0,2}\d|180)(°|˚|º|\^|~|\*|\s|\-|_)*((?<min>[0-5]?\d|\d)?)('|′|\s|\-|_)*(?<sec>(([0-5]?\d|\d)([.]\d*)?))?(""|¨|˝|\s|\-|_)*(?<s2>(\+|\-|E|e|W|w))?)[\s]*$", 
            RegexOptions.Compiled);
        
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
            
            var match = LongitudeRegex.Match(value);
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
            
            if (degGroup.Success && minGroup.Success == false && secGroup.Success == false) return false;
            
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
            longitude = (s1Group.Success ? sign1 : sign2) * (deg + (double)min / 60 + sec / 3600);
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