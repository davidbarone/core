using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ReadOnlyAttribute : Attribute
    {
    }
}
