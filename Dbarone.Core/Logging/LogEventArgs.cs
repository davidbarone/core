using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Logging
{
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Caller of the log event.
        /// </summary>
        public MethodBase Caller { get; set; }

        /// <summary>
        /// log event type.
        /// </summary>
        public LogType LogType { get; set; }

        /// <summary>
        /// Log event message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Any optional state relating to the logged event.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// Optional exception object raised by caller.
        /// </summary>
        public Exception Exception { get; set; }
    }
}
