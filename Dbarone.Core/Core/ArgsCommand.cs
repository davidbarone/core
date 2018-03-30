using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Core
{
    /// <summary>
    /// Represents a command that can be invoked or hydrated from command argument string.
    /// </summary>
    public abstract class ArgsCommand : MarshalByRefObject
    {
        public static List<Type> commands = new List<Type>();

        /// <summary>
        /// Allows a container to be injected into the command.
        /// </summary>
        public IContainer Container { get; set; }

        public abstract string Execute();

        /// <summary>
        /// Factory method which instantiates correct ArgsCommand object, and passes through arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static ArgsCommand Create(string[] args, bool throwOnError=false)
        {
            if (args.Count() < 1)
                throw new Exception("Must provide at least 1 argument.");

            var command = args[0];
            args = args.Splice(1);

            // Look for ArgCommand matching args[0]
            Type type = commands.FirstOrDefault(t => t.Name.Equals(string.Format("{0}Command", command), StringComparison.OrdinalIgnoreCase));

            if (type == null)
            {
                if (throwOnError)
                    throw new Exception(string.Format("Command {0} does not exist.", command));
                else
                    return null;
            }

            // If invalid command specified in command parameters, default to help.
            ArgsCommand concrete = (ArgsCommand)Activator.CreateInstance(type);
            if (concrete != null)
                OptionHydrator.Hydrate(args, concrete);
            return concrete;
        }

        static ArgsCommand()
        {
            // Cache all ArgsCommand objects
            foreach (var type in Assembly.GetEntryAssembly().GetTypes().Where(t => typeof(ArgsCommand).IsAssignableFrom(t)))
            {
                commands.Add(type);
            }
        }
    }
}
