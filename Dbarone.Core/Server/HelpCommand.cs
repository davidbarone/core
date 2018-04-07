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
    [Documentation("Provides help for the service application.")]
    public class HelpCommand : ArgsCommand
    {
        private IEnumerable<Type> types;

        [Option("c", "command", Required = false, Help = "The command to provide extended help for.")]
        public string Command { get; set; }

        public HelpCommand()
        {
            // Get list of available commands
            types = AppDomain.CurrentDomain.GetSubclassTypesOf<ArgsCommand>();
        }

        public override string Execute()
        {
            if (string.IsNullOrEmpty(Command))
                return Help();
            else
                return HelpForCommand();
        }

        private string Help()
        {
            var model = types
                .OrderBy(t => t.Name)
                .Select(t => new {
                    Name = t.Name.Replace("Command",""),
                    Description = ((DocumentationAttribute)t.GetCustomAttribute(typeof(DocumentationAttribute))).Description
                });

            return string.Format(@"Usage: cli COMMAND [ARGUMENTS]

A generic client for the {0} service.

", Assembly.GetEntryAssembly().GetName().Name) + model.PrettyPrint(new List<PrettyPrintColumn>()
            {
                new PrettyPrintColumn { PropertyName = "Name", Title = "Commands", Width=30 },
                new PrettyPrintColumn { PropertyName = "Description", Title = "Description", Width=101 }

            }) + @"

Run 'cli help -c <command>' for more help on a specific command. For bug requests contact dbarone123@gmail.com.";
        }


        /// <summary>
        /// Returns a formatted string to display what kind of values can be entered based on the type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string GetDomain(Type type)
        {
            var propertyType = type;

            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                propertyType = Nullable.GetUnderlyingType(propertyType);
            }

            if (propertyType.IsEnum)
                return string.Format(@"Enum
{0}", string.Join(Environment.NewLine, Enum.GetNames(propertyType).Select(n => string.Format(" - {0}", n))));
            else
                return propertyType.Name;
        }

        private string HelpForCommand()
        {
            var command = Command.ToLower();
            var type = types.FirstOrDefault(t => t.Name.Replace("Command", "").Equals(command, StringComparison.OrdinalIgnoreCase));

            if (type==null)
                throw new Exception(string.Format("No help exists for [{0}].", command));

            var description = ((DocumentationAttribute)type.GetCustomAttribute(typeof(DocumentationAttribute))).Description;

            // Get options for the type
            var options = type
                .GetPropertiesDecoratedBy<OptionAttribute>()
                .Select(p => new
                {
                    Property = p,
                    Attribute = p.GetCustomAttribute<OptionAttribute>()
                })
                .OrderBy(p => p.Attribute.ShortName);

            var model = options
                .Select(o => new
                {
                    ShortName = o.Attribute.ShortName != null ? "-" + o.Attribute.ShortName : "",
                    LongName = o.Attribute.LongName != null ? "--" + o.Attribute.LongName : "",
                    Required = o.Attribute.Required ? "Yes" : "No",
                    Help = o.Attribute.Help,
                    DataType = GetDomain(o.Property.PropertyType)
                });

            var help = string.Format(@"
Usage: cli {0} [OPTIONS]

{1}

Options:

{2}
"
, command
, description
, model.PrettyPrint(new List<PrettyPrintColumn>()
            {
                      new PrettyPrintColumn{ PropertyName="ShortName", Title="Short Name", Width =10},
                      new PrettyPrintColumn{ PropertyName="LongName", Title="Long Name", Width =30},
                      new PrettyPrintColumn{ PropertyName="Required", Title="Required?", Width =8},
                      new PrettyPrintColumn{ PropertyName="Help", Title="Help", Width =60},
                      new PrettyPrintColumn{ PropertyName="DataType", Title="Type / Values", Width=20}
            }));
            return help;

        }
    }
}
