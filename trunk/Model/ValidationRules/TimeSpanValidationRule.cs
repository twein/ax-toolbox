using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace AXToolbox.Model.ValidationRules
{
    public class TimeSpanValidationRule : ValidationRule
    {
        private static Regex pattern = new Regex(@"^\b*-?\d{2}:\d{2}:\d{2}\b*$");
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (pattern.IsMatch(value.ToString()))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Value is not a valid time span");
            }
        }
    }
}
