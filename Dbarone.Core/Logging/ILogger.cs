using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Logging
{
    public interface ILogger
    {
        event LogEventHandler HandleLog;
        void Log(object sender, LogEventArgs args);
        void Log(object sender, LogType logType, string message, object state=null);
        void Log(object sender, Exception exception, object state=null);
    }
}
