using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dbarone.Validation
{
    public class RegExValidatorAttribute : ValidatorAttribute
    {
        public string Pattern { get; set; }

        public override void DoValidate(object value, object target, string key, IList<ValidationResult> results)
        {
            Regex ex = new Regex(Pattern);

            if (value != null)
            {
                if (value.GetType() != typeof(string))
                    results.Add(new ValidationResult { Key = key, Target = target, Message = "RegExValidator must operate on a string type" });

                if (!ex.IsMatch(value.ToString()))
                    results.Add(new ValidationResult { Key = key, Target = target, Message = string.Format("{0} is invalid", key) });
            }
        }
    }
}
