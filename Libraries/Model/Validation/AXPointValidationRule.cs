using System;
using System.Globalization;
using System.Windows.Controls;
using AXToolbox.Common;
using System.Collections.Generic;

namespace AXToolbox.Model.Validation
{
    public class AXPointValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            AXPoint point;
            if (AXPoint.TryParse((string)value, out point))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Value is not a valid point");
            }
        }
    }
}
