using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Core
{
    /// <summary>
    /// Attribute to define a property to accept a command line argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public OptionAttribute(string shortName, string longName)
        {
            if (shortName!=null && shortName.Length != 1)
                throw new Exception("Short name length must be 1 character.");
            this.ShortName = shortName;
            if (longName!=null && longName.Length <= 1)
                throw new Exception("Long name length must be greater than 1 character.");
            this.LongName = longName;
        }

        /// <summary>
        /// The name of the parameter. Must be prefixed by '-' character.
        /// </summary>
        public string ShortName { get; set; }

        /// <summary>
        /// The long name of the parameter. Must be prefixed by '--' characters.
        /// </summary>
        public string LongName { get; set; }

        /// <summary>
        /// Set to true if the parameter is required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// The help text of the parameter.
        /// </summary>
        public string Help { get; set; }

        /// <summary>
        /// Optional default value.
        /// </summary>
        public object Default { get; set; }

    }
}
