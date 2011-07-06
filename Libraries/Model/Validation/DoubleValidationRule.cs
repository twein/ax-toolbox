using System;
using System.Globalization;
using System.Windows.Controls;

namespace AXToolbox.Model.Validation
{
    public class DoubleValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double number;
            if (double.TryParse((string)value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out number))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Value is not a valid double precission number");
            }
        }
    }
}
