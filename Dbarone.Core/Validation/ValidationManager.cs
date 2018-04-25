using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dbarone.Core;
using Dbarone.Ioc;

namespace Dbarone.Validation
{
    public static class ValidationManager
    {
        /// <summary>
        /// Tests the validity of an object. If not valid, throws an exception.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="resultType"></param>
        /// <param name="container"></param>
        public static void AssertValidity(object target, ValidationResultType resultType, IContainer container = null)
        {
            var results = Validate(target, resultType, container);
            if (results.Any())
                throw new System.Exception(results.First().Message);
        }

        /// <summary>
        /// Tests the validity of an object. If not valid, throws an exception.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="container"></param>
        public static void AssertValidity(object target, IContainer container = null)
        {
            var results = Validate(target, container);
            if (results.Any())
                throw new System.Exception(results.First().Message);
        }

        public static IEnumerable<ValidationResult> Validate(object target, ValidationResultType resultType, IContainer container = null)
        {
            return Validate(target, container).Where(r => (resultType & r.ResultType) == r.ResultType);
        }

        public static IEnumerable<ValidationResult> Validate(object target, IContainer container = null)
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
                    if (container != null)
                        attribute.Container = container;

                    attribute.DoValidate(null, target, key, results);
                }
            }
            
            return results;
        }
    }
}
