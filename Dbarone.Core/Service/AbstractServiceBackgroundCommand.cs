using Dbarone.Core;
using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Service
{
    /// <summary>
    /// Base class for background tasks for service. When overridden, you implement the parameters
    /// as properties. Decorate the properties with an [Option] attribute to provide
    /// API documentation. Decorate the class with the [Documentation] attribute for
    /// documentation too.
    /// </summary>
    public abstract class AbstractServiceBackgroundCommand : ArgsCommand
    {
    }
}
