using Dbarone.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbarone.Validation
{
    public class NumericRangeValidatorAttribute : ValidatorAttribute
    {
        public double Min {get; set;}
        public double Max {get; set;}

        private string ErrorMessage
        {
            get
            {
                return "value of {0} must be between {1} and {2}";
            }
        }

        public override void DoValidate(object value, object target, string key, IList<ValidationResult> results)
        {
            // note that numeri range ONLY runs if value not null.
            // to test for nulls, use NotNull validator as well.
            if (value != null)
            {
                if (!value.IsNumeric())
                    results.Add(new ValidationResult { Key = key, Target = target, Message = "NumericRangeValidator must operate on a numeric type." });

                if (Max < Min)
                    results.Add(new ValidationResult { Key = key, Target = target, Message = "MaxLength is less than MinLength" });

                double v = Convert.ToDouble(value);
                if (v < Min || v > Max)
                    results.Add(new ValidationResult { Key = key, Target = target, Message = string.Format(ErrorMessage, key, Min, Max) });
            }
        }
    }
}
