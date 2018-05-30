using Dbarone.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dbarone.Ioc;
using System.Web;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Dbarone.Server
{
    /// <summary>
    /// Base / abstract class for all web controllers
    /// </summary>
    public abstract class WebCommand : ICommand
    {
        public HttpListenerContext Context { get; set; }
        public IContainer Container { get; set; }
        public abstract string Execute();

        /// <summary>
        /// A regex expression that defines the routes that are supported.
        /// The route should not include the domain name. Only the local path
        /// that the command can handle
        /// </summary>
        public abstract string Route { get; }

        /// <summary>
        /// Returns the first WebCommand object that
        /// can handle the path of the http request.
        /// </summary>
        /// <param name="localPath"></param>
        /// <returns></returns>
        public static WebCommand Create(string localPath)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    foreach (var type in assembly.GetTypes().Where(t => typeof(WebCommand).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract))
                    {
                        WebCommand cmd = (WebCommand)Activator.CreateInstance(type);
                        Regex r = new Regex(cmd.Route,RegexOptions.IgnoreCase);
                        if (r.IsMatch(localPath))
                            return cmd;
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                }
            }
            throw new Exception("No WebCommand object found to handle http request path.");
        }
    }
}
