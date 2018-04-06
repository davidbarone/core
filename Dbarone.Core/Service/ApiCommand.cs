using Dbarone.Core;
using Dbarone.Documentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Service.Commands
{
    /// <summary>
    /// This abstract class is here so that the help can construct the correct help text.
    /// You should implement this functionality in the client application.
    /// </summary>
    [Documentation("Sets the current API endpoint. If no parameters specified, then displays the current API endpoint.")]
    public class ApiCommand : AbstractServiceCommand
    {
        [Option("i", "host", Help = "The IP address of the API end point.", Required = false)]
        public string IPAddress { get; set; }

        [Option("h", "host", Help = "The host name of the API end point.", Required = false)]
        public string Host { get; set; }

        [Option("p", "port", Help = "The port number of the API end point.", Required = false)]
        public int? Port { get; set; }

        /// <summary>
        /// You must implement this in the client application.
        /// </summary>
        /// <returns></returns>
        public override string Execute()
        {
            throw new NotImplementedException();
        }
    }
}
