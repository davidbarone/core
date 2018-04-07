using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dbarone.Core;

namespace Dbarone.Command
{
    /// <summary>
    /// Takes a set of command line arguments, and
    /// hydrates properties of a class that implement
    /// properties decorated with the [CommandParameter] attribute.
    /// </summary>
    public static class OptionHydrator
    {
        public static void Hydrate(string[] args, object target)
        {
            var sd = GetCommandParameters(args);

            // get target properties
            var properties = target
                .GetPropertiesDecoratedBy<OptionAttribute>()
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<OptionAttribute>()
                });

            var missingProperties = properties.Where(p => p.Attribute.Required && !sd.ContainsKey(p.Attribute.ShortName ?? "") && !sd.ContainsKey(p.Attribute.LongName ?? ""));
            if (missingProperties.Any())
                throw new Exception(string.Format("Argument -{0} is mandatory.", missingProperties.First().Attribute.ShortName));

            foreach (var key in sd.Keys)
            {
                if (!properties.Select(p => p.Attribute.ShortName).Contains(key, StringComparer.InvariantCultureIgnoreCase) &&
                    !properties.Select(p => p.Attribute.LongName).Contains(key, StringComparer.InvariantCultureIgnoreCase))
                    throw new Exception(string.Format("Invalid argument -{0}.", key));
            }

            // If got here, hydrate options.
            foreach (var property in properties)
            {
                // Has property got a default value?
                var def = property.Attribute.Default;
                if (def!=null)
                    property.Property.SetValue(target, Convert.ChangeType(def, property.Property.PropertyType));

                var key = property.Attribute.ShortName ?? property.Attribute.LongName;
                if (sd.ContainsKey(key))
                {
                    var value = sd[key];
                    var propertyType = property.Property.PropertyType;
                    if (Nullable.GetUnderlyingType(propertyType) != null)
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType);
                    }

                    if (value != null)
                    {
                        if (propertyType.IsEnum)
                            // Enum type
                            property.Property.SetValue(target, Enum.Parse(propertyType, (string)value));
                        else if (propertyType == typeof(Guid))
                            property.Property.SetValue(target, Guid.Parse(value));
                        else
                            // normal type
                            property.Property.SetValue(target, Convert.ChangeType(value, propertyType));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the additional arguments.
        /// Arguments start with '-' or '--' charcaters and
        /// some arguments have an optional value.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>A dictionary containing the parameters. Note that the keys are case insensitive.</returns>
        private static Dictionary<string, string> GetCommandParameters(string[] args)
        {
            Dictionary<string, string> sd = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            int c = 0;
            while (c < args.Length)
            {
                GetOption(args, sd, ref c);
            }
            return sd;
        }

        /// <summary>
        /// If the next arg is an argument, returns it, and advances
        /// the ArgCounter counter by 1.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static string GetArgument(string[] args, ref int c)
        {
            if (c < args.Length)
            {
                var v = args[c];
                if (v.Substring(0, 1) != "-" && (v.Length < 2 || v.Substring(0, 2) != "--"))
                {
                    c++;
                    return v;
                }
                else
                    return null;
            }
            return null;
        }

        /// <summary>
        /// Gets the next option. If the next argument is NOT
        /// an option, then returns false.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static bool GetOption(string[] args, Dictionary<string, string> sd, ref int c)
        {
            var s = args[c];
            c++;
            if (s.Substring(0, 2) == "--")
            {
                var k = s.Substring(2);
                // If GetArgument does not find an argument (but
                // instead the next option), it will return a null
                // value. We assume this is a boolean option and hence
                // set to 'True'.
                sd.Add(k, GetArgument(args, ref c) ?? "True");
                return true;
            }
            else if(s.Substring(0, 1) == "-")
            {
                var k = s.Substring(1);
                // If GetArgument does not find an argument (but
                // instead the next option), it will return a null
                // value. We assume this is a boolean option and hence
                // set to 'True'.
                sd.Add(k, GetArgument(args, ref c) ?? "True");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
