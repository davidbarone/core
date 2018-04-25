using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Validation
{
    [Flags]
    public enum ValidationResultType
    {
        ERROR,
        WARNING,
        INFORMATION
    }
}
