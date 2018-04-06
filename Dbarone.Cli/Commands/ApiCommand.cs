using Dbarone.Core;
using Dbarone.Documentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Cli.Commands
{
    [Documentation("Sets the current API endpoint. If no parameters specified, then displays the current API endpoint.")]
    public class ApiCommand : Dbarone.Service.Commands.ApiCommand
    {
        public override string Execute()
        {
            if (string.IsNullOrEmpty(Host) && string.IsNullOrEmpty(IPAddress) && !Port.HasValue)
            {
                //return current API
                return string.Format(@"Current api endpoint:
Host: {0}
IP Address: {1}
Port: {2}", Properties.Settings.Default["host"], Properties.Settings.Default["ip"], Properties.Settings.Default["port"]);
            } else 
            {
                if (!string.IsNullOrEmpty(IPAddress))
                    Host = null;

                if (!string.IsNullOrEmpty(Host))
                    IPAddress = null;

                // Set host / port
                if (!string.IsNullOrEmpty(Host) || !string.IsNullOrEmpty(IPAddress))
                {
                    Properties.Settings.Default["host"] = Host;
                    Properties.Settings.Default["ip"] = IPAddress;
                }
                if (Port.HasValue)
                    Properties.Settings.Default["port"] = Port;
                Properties.Settings.Default.Save();

                return "API endpoint set.";
            }
        }
    }
}
