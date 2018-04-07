using Dbarone.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dbarone.Server
{
    /// <summary>
    /// Defines a service strategy.
    /// </summary>
    public interface IServiceStrategy
    {
        /// <summary>
        /// The number of seconds between each iteration of the timer.
        /// </summary>
        int TimerInterval { get; }

        /// <summary>
        /// The name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Initialisation code to run when the service starts.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>An ICommand object that performs background processing.</returns>
        ICommand Start(string[] args);

        /// <summary>
        /// The code to run when the service stops.
        /// </summary>
        void Stop();
    }
}
