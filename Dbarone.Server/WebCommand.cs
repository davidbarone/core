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

namespace Dbarone.Server
{
    public abstract class WebCommand : ICommand
    {
        public HttpListenerContext Context { get; set; }
        public IContainer Container { get; set; }
        public abstract string Execute();
    }
}
