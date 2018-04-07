using Dbarone.Core;
using Dbarone.Documentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Client.Commands
{
    [Documentation("Sets the current API endpoint. If no parameters specified, then displays the current API endpoint.")]
    public class ApiCommand : Dbarone.Server.ApiCommand
    {
        public override string Execute()
        {
            if (string.IsNullOrEmpty(Host) && !Port.HasValue)
            {
                //return current API
                return string.Format(@"Current api endpoint:
Host: {0}
Port: {1}", Properties.Settings.Default["host"], Properties.Settings.Default["port"]);
            } else 
            {
                // Set host / port
                if (!string.IsNullOrEmpty(Host))
                {
                    Properties.Settings.Default["host"] = Host;
                }
                if (Port.HasValue)
                    Properties.Settings.Default["port"] = Port;
                Properties.Settings.Default.Save();

                return "API endpoint set.";
            }
        }
    }
}
