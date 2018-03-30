using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Logging
{
    /// <summary>
    /// A default logger implementation that allows custom
    /// log event to be specified through a HandleLog event.
    /// This decouples the act of raising a log with the 
    /// actual logging implementation. This class enables
    /// a pluggable custom client logging implementation
    /// to be defined.
    /// </summary>
    public class Logger : MarshalByRefObject, ILogger
    {
        public event LogEventHandler HandleLog;

        public void Log(object sender, Exception exception, object state=null)
        {
            StackTrace trc = new StackTrace();
            LogEventArgs args = new LogEventArgs()
            {
                Caller = trc.GetFrame(1).GetMethod(),
                Message = exception.Message,
                Exception = exception,
                LogType = LogType.ERROR,
                State = state
            };
            Log(sender, args);
        }

        public void Log(object sender, LogType logType, string message, object state=null)
        {
            StackTrace trc = new StackTrace();
            LogEventArgs args = new LogEventArgs()
            {
                Caller = trc.GetFrame(1).GetMethod(),
                LogType = logType,
                Message = message,
                State = state
            };
            Log(sender, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void Log(object sender, LogEventArgs args)
        {
            OnHandleLogEvent(sender, args);
        }

        protected virtual void OnHandleLogEvent(object sender, LogEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            LogEventHandler handler = HandleLog;

            // Event will be null if there are no subscribers
            if (handler != null)
            {
                // Use the () operator to raise the event.
                handler(sender, e);
            }
        }
    }
}
