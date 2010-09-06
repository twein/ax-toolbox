using System;
using System.Globalization;
using System.Windows.Controls;
using AXToolbox.Common;
using System.Collections.Generic;

namespace AXToolbox.Model.Validation
{
    public class DatumValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            try
            {
                if (Datum.GetInstance((string)value) != null)
                    return new ValidationResult(true, null);
            }
            catch (KeyNotFoundException) { }

            return new ValidationResult(false, "Value is not a valid datum name");
        }
    }
}
