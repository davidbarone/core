using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dbarone.Core;

namespace Dbarone.Validation
{
    public static class ValidationManager
    {
        public static IList<ValidationResult> Validate(object target)
        {
            var props = target.GetPropertiesDecoratedBy<ValidatorAttribute>();
            List<ValidationResult> results = new List<ValidationResult>();

            foreach (var prop in props)
            {
                // get the attribute for the property
                var attributes = (ValidatorAttribute[])prop.GetCustomAttributes(typeof(ValidatorAttribute), false);
                foreach (var attribute in attributes)
                {
                    string key = prop.Name;
                    attribute.DoValidate(target.Value(key), target, key, results);
                }
            }
            
            // method validators
            var methods = target.GetMethodsDecoratedBy<MethodValidatorAttribute>();
            foreach (var method in methods)
            {
                // get the attribute for the property
                var attributes = (MethodValidatorAttribute[])method.GetCustomAttributes(typeof(MethodValidatorAttribute), false);
                foreach (var attribute in attributes)
                {
                    string key = method.Name;
                    attribute.Method = method;
                    attribute.DoValidate(null, target, key, results);
                }
            }
            
            return results;
        }
    }
}
