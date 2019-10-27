using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ScanlineRendering
{
    class TrianglesSizeValidator: ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string)
            {
                int size;
                if (int.TryParse((string)value, out size) == true)
                {
                    if (size < 1)
                    {
                        return new ValidationResult(false, "Triangle has to be at least 1x1 size!");
                    }
                }
                else
                    return new ValidationResult(false, "Value is not an integer!");
            }
            return new ValidationResult(true, "");
        }
    }
}
