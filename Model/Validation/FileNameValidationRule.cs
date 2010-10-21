using System.Globalization;
using System.IO;
using System.Windows.Controls;

namespace AXToolbox.Model.Validation
{
    public class FileNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (File.Exists((string)value))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Value is not a valid file");
            }
        }
    }
}
