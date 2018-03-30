using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;

namespace Dbarone.Proxy
{
    public class InterceptorEventArgs : EventArgs
    {
        public BoundaryType Boundary { get; set; }
        public IMessage Message { get; set; }
        public Exception Exception { get; set; }
        public bool Cancel { get; set; }
    }
}
