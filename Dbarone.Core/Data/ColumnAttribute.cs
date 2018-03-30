using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    /// <summary>
    /// DJB 8/6/2017
    /// This not currently used. Use in future if
    /// we want to allow writing of INT to database.
    /// At the moment, the database write will just
    /// do ToString().
    /// </summary>
    public enum EnumColumnBehaviourEnum
    {
        INT,
        STRING
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; private set; }
        public int Order { get; set; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }

        public EnumColumnBehaviourEnum EnumColumnBehaviour { get; set; }
    }
}
