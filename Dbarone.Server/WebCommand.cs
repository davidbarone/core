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
using System.IO;
using Dbarone.Template;
using Newtonsoft.Json;

namespace Dbarone.Server
{
    /// <summary>
    /// Base / abstract class for all web controllers.
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
                        Regex r = new Regex(cmd.Route, RegexOptions.IgnoreCase);
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

        #region Helper Methods

        /// <summary>
        /// Writes text content to response.
        /// </summary>
        /// <param name="content"></param>
        protected void WriteContent(string content)
        {
            using (var sw = new StreamWriter(Context.Response.OutputStream))
            {
                sw.Write(content);
                sw.Flush();
            }
        }

        protected void RenderView(string viewPath, object model)
        {
            var viewHtml = File.OpenText(viewPath).ReadToEnd();
            WriteContent(new RegexTemplater().Render(viewHtml, model));
        }

        protected void RenderView(string viewPath, string layoutPath, object model)
        {
            var viewHtml = File.OpenText(viewPath).ReadToEnd();
            var content = new RegexTemplater().Render(viewHtml, model);

            var layoutHtml = File.OpenText(layoutPath).ReadToEnd();
            content = new RegexTemplater().Render(layoutHtml, new { Content = content });
            WriteContent(content);
        }

        /// <summary>
        /// Gets the application directory. Useful when opening static files like views.
        /// </summary>
        public string AppDir
        {
            get
            {
                return new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).DirectoryName;
            }
        }

        /// <summary>
        /// Writes json object to output stream.
        /// </summary>
        /// <param name="obj"></param>
        public void Json(object obj)
        {
            Context.Response.AddHeader("Content-Type", "application/json");
            StreamWriter sw = new StreamWriter(Context.Response.OutputStream);
            JsonTextWriter writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented };
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(writer, obj);
            sw.Flush();
        }

        #endregion

    }
}
