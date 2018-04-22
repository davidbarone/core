using Dbarone.Command;
using Dbarone.Core;
using Dbarone.Documentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dbarone.Core.ExtensionMethods;

namespace Dbarone.Server
{
    [Documentation("Runs a script of commands.")]
    public class ScriptCommand : ArgsCommand
    {
        [Option("f", "file", Required = true, Help = "Full path file name.")]
        public string File { get; set; }

        public override string Execute()
        {
            // Opens local script file, and returns contents of script
            return System.IO.File.ReadAllText(File);
        }
    }
}
