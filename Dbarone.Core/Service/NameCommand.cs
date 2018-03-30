using Dbarone.Core;
using Dbarone.Documentation;
using Dbarone.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dbarone.Core.ExtensionMethods;

namespace Dbarone.Service.Commands
{
    [Documentation("Returns the name of the service currently connected to.")]
    public class NameCommand : AbstractServiceCommand
    {
        public override string Execute()
        {
            return string.Format("{0} service", Assembly.GetEntryAssembly().GetName().Name);
        }
    }
}
