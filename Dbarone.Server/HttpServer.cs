using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Dbarone.Server
{
    /// <summary>
    /// The HttpServer class provides a general query interface
    /// to the data lake data. This is used primarily by end-users
    /// via BI tools like PowerQuery.
    /// 
    /// https://msdn.microsoft.com/en-us/library/system.net.httplistener(v=vs.110).aspx
    /// </summary>
    public class HttpServer
    {
        HttpListener Listener = new HttpListener();
        IContainer Container = null;
        public WebCommand Command = null;

        public HttpServer(int port, WebCommand command, IContainer container = null)
        {
            this.Container = container;
            this.Command = command;

            // Create a listener.
            Listener.Prefixes.Add(string.Format("http://*:{0}/", port));

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            // start worker thread

        }

        public void Start()
        {
            Thread clientThread = new Thread(new ThreadStart(delegate()
            {
                Listener.Start();

                while (true)
                {
                    HttpListenerContext context = Listener.GetContext();

                    Command.Context = context;
                    Command.Container = Container;
                    Command.Execute();
                }
            }));
            clientThread.Start();
        }
    }
}
