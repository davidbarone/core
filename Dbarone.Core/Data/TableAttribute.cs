using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class TableAttribute : Attribute
    {
        public string Name { get; private set; }

        public TableAttribute(string name)
        {
            Name = name;
            Operation =
                CrudOperation.CREATE |
                CrudOperation.READ |
                CrudOperation.UPDATE |
                CrudOperation.DELETE;
        }

        public CrudOperation Operation { get; set; }
    }
}
