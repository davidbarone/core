using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbarone.Validation
{
    public class ValidationResult
    {
        /// <summary>
        /// Name of the property / field
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// Validation message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// The parent object being validated
        /// </summary>
        public object Target { get; set; }
    }
}
