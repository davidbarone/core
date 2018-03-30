using System;
using System.Collections.Generic;
using System.Linq;

namespace Dbarone.Validation
{
    /// <summary>
    /// Validates a child object in an object graph.
    /// </summary>
    public class ObjectValidator : ValidatorAttribute
    {
        public override void DoValidate(object value, object target, string key, IList<ValidationResult> results)
        {
            var childResults = ValidationManager.Validate(value);
            foreach (var childResult in childResults)
            {
                childResult.Key = key + "." + childResult.Key;
                childResult.Target = value;
                results.Add(childResult);
            }
        }
    }
}
