using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Asv.Common
{
    public class AngleDm
    {
        private static readonly Regex AngleInDegrees =
            new(@"^(-?((0|[1-9][0-9]?|1[0-7][0-9]|180|360)(\.\d{1,6})?)|360(\.0{1,6})?)$", RegexOptions.Compiled);
        
        private static readonly Regex AngleDmRegex = new(
            @"((?<sign>(\-|\+))?(?<deg>\d+)(°|˚|º|\^|~|\*|\s|\-|_)*(((?<min>(([0-5]?\d|\d)([.]\d*)?))?)?)('|′|\s|\-|_)*)[\s]*$",
            RegexOptions.Compiled);
            //                                                              (((?<min>[0-5]?\d|\d)?)?)
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
            //Initialize a new angle value with NaN
            angle = Double.NaN;
            
            //Checks whether value is null or whitespace 
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            //Replace coma by dot
            value = value.Replace(',', '.');

            if (AngleInDegrees.IsMatch(value))
            {
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out angle))
                {
                    return true;
                }
            }
            
            //Checking value on specified potential regex matches
            var match = AngleDmRegex.Match(value);
            
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

            if (degGroup.Success == false) return false;
            
            if (int.TryParse(degGroup.Value,NumberStyles.Integer, CultureInfo.InvariantCulture, out var deg) == false) return false;
            
            //Initialize a new minutes and seconds values with zeros
            var min = 0.0;
            
            if (minGroup.Success)
            {
                if (double.TryParse(minGroup.Value,NumberStyles.Any, CultureInfo.InvariantCulture, out min) == false) return false;
            }
            
            var sign = 1;
            if (signGroup.Success)
            {
                sign = signGroup.Value.Equals("-") ? -1 : 1;
            }
            
            angle = sign * (deg + min / 60.0);
            
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
            return $"{(Math.Sign(decimalDegrees) > 0 ? "" : "-")}{degrees:00}°{minutes:00.00}′";  
        }
    }
}