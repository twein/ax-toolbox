using System;
using System.Globalization;
using System.Windows.Controls;

namespace AXToolbox.Model.Validation
{
    public class DateValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            DateTime date;
            if (DateTime.TryParse((string)value, out date))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Value is not a valid date");
            }
        }
    }
}
