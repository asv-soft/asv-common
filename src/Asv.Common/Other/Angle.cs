using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public static class Angle
    {
        private static readonly Regex AngleRegex = new(
            @"((?<sign>[\-,\+])?(?<deg>\d+)[°˚º^~*\s\-_]+(((?<min>[0-5]?\d|\d)?)?)[',′,\s,\-,_]*(((?<sec>[0-5]?\d|\d)([.]\d*)?)?)["",¨,˝,\s,\-,_]*)[\s]*$",
            RegexOptions.Compiled);
        
        public static bool IsValid(string value)
        {
            return TryParse(value, out _);
        }
        
        public static string? GetErrorMessage(string value)
        {
            return IsValid(value) == false ? RS.Angle_ErrorMessage : null;
        }
        
        public static bool TryParse(string value, out double angle)
        {
            //Initialize a new angle value with NaN
            angle = Double.NaN;
            
            //Checks whether value is null or whitespace 
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            //Replace coma by dot
            value = value.Replace(',', '.');
            
            //Checking value on specified potential regex matches
            var match = AngleRegex.Match(value);
            
            //Trying to parse value in angle as double
            if (match.Success == false)
            {
                var signStr = "";
                
                if (value.Contains("+"))
                {
                    value = value.Replace("+", "");
                }
                
                if (value.Contains("-"))
                {
                    signStr = "-";
                    value = value.Replace("-", "");
                }
                
                value = Regex.Replace(value, @"^0+(?=\d+$)", "");
                return double.TryParse(signStr + value, NumberStyles.Any, CultureInfo.InvariantCulture, out angle);
            }
            
            //Getting all matching groups
            var signGroup = match.Groups["sign"];
            var degGroup = match.Groups["deg"];
            var minGroup = match.Groups["min"];
            var secGroup = match.Groups["sec"];

            if (degGroup.Success == false) return false;
            
            if (int.TryParse(degGroup.Value,NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) == false) return false;
            
            // if only seconds without minutes => error
            if (secGroup.Success && minGroup.Success == false) return false;
            
            //Initialize a new minutes and seconds values with zeros
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
            
            var sign = 1;
            if (signGroup.Success)
            {
                sign = signGroup.Value.Equals("-") ? -1 : 1;
            }
            
            angle = sign * (deg + (double)min / 60.0 + sec / 3600.0);
            
            return !double.IsNaN(angle);
        }
        
        public static string PrintDms(double decimalDegrees)
        {
            int degrees = (int)Math.Abs(decimalDegrees);
            double remainingDegrees = Math.Abs(decimalDegrees) - degrees;
            int minutes = (int)(remainingDegrees * 60);
            double remainingMinutes = (remainingDegrees * 60) - minutes;
            double seconds = Math.Round(remainingMinutes * 60);
            while (seconds >= 60)
            {
                minutes++;
                seconds -= 60;
            }
            return $"{Math.Sign(decimalDegrees) * degrees}°{minutes}′{seconds:F2}˝";  
        }
    }
}