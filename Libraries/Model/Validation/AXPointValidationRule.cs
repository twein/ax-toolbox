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
            try
            {
                var point = AXPoint.Parse((string)value);
                return new ValidationResult(true, null);
            }
            catch
            {
                return new ValidationResult(false, "Value is not a valid point");
            }
        }
    }
}
