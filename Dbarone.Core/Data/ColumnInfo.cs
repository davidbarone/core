using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;

namespace Dbarone.Data
{
    public class ColumnInfo
    {
        public string Name { get; set; }
        public Type DotNetType { get; set; }
        public PropertyInfo PropertyInfo { get; set; }
        public int Order { get; set; }
        public bool Key { get; set; }
        public bool DatabaseGenerated { get; set; }
        public EnumColumnBehaviourEnum? EnumColumnBehaviour { get; set; }
    }
}
