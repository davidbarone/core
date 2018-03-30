using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbarone.Validation
{
    public class NotNullValidatorAttribute : ValidatorAttribute
    {
        public override void DoValidate(object value, object target, string key, IList<ValidationResult> results)
        {
            if (value == null)
                results.Add(new ValidationResult { Key = key, Target = target, Message = string.Format("{0} cannot be null.", key) });
        }
    }
}
