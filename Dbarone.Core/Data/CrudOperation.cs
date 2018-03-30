using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Data
{
    [Flags]
    public enum CrudOperation
    {
        NONE = 0,
        CREATE = 1,
        READ = 2,
        UPDATE = 4,
        DELETE = 8
    }
}
