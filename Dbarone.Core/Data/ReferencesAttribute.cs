using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    /// <summary>
    /// For columns that act as foreign key fields and reference another object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ReferencesAttribute : Attribute
    {
        public ReferencesAttribute(Type referencedType)
        {
            this.ReferencedType = referencedType;
        }

        public Type ReferencedType { get; set; }
    }
}
