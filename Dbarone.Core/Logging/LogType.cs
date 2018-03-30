using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Logging
{
    public enum LogType
    {
        /// <summary>
        /// Log type begin invoked at beginning of method execution.
        /// </summary>
        BEGIN,

        /// <summary>
        /// Log type begin invoked at end of method execution.
        /// </summary>
        END,

        /// <summary>
        /// Debugging information.
        /// </summary>
        DEBUG,

        /// <summary>
        /// An error.
        /// </summary>
        ERROR,

        /// <summary>
        /// Information only.
        /// </summary>
        INFORMATION,

        /// <summary>
        /// Progress.
        /// </summary>
        PROGRESS,

        /// <summary>
        /// Warning.
        /// </summary>
        WARNING,

        /// <summary>
        /// Success log type.
        /// </summary>
        SUCCESS,

        /// <summary>
        /// Failure log type.
        /// </summary>
        FAILURE,

        /// <summary>
        /// Completion log type.
        /// </summary>
        COMPLETE,

        /// <summary>
        /// Aborted log type.
        /// </summary>
        ABORT
    }
}
