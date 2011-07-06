using System;
using System.Globalization;
using System.Windows.Controls;

namespace AXToolbox.Model.Validation
{
    public class TimeSpanValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            TimeSpan ts;
            if (TimeSpan.TryParse((string)value, out ts))
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
