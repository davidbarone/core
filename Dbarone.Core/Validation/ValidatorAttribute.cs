using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbarone.Validation
{
    public abstract class ValidatorAttribute : Attribute
    {
        /// <summary>
        /// Abstract base (attribute) class for all validators.
        /// </summary>
        /// <param name="value">The property value being validated.</param>
        /// <param name="target">The object that the value belongs to.</param>
        /// <param name="key">The name of the property.</param>
        /// <param name="results">The list of current results.</param>
        public abstract void DoValidate(object value, object target, string key, IList<ValidationResult> results);
    }
}
