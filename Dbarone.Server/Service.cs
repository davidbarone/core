using Dbarone.Command;
using Dbarone.Core;
using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbarone.Server
{
    /// <summary>
    /// Base service class that implements automatic timer and runs a background task.
    /// Funcionality provided by strategy class provided.
    /// </summary>
    public class Service : ServiceBase
    {
        ICommand command = null;
        private IServiceStrategy strategy { get; set; }
        System.Timers.Timer timer = new System.Timers.Timer();

        #region ServiceBase methods

        public Service(IServiceStrategy strategy)
        {
            this.strategy = strategy;
            base.ServiceName = strategy.ServiceName;
        }

        public void Run(string[] args)
        {
            if (Environment.UserInteractive)
            {
                // Run as a console app.
                OnStart(args);
                Console.ReadKey();
                OnStop();
            }
            else
            {
                // running as service
                ServiceBase.Run(this);
            }
        }

        protected override void OnStart(string[] args)
        {
            command = strategy.Start(args);
            timer.Interval = this.strategy.TimerInterval * 1000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
            timer.AutoReset = false; // one shot timer
            timer.Start();
        }

        /// <summary>
        /// Timer event that runs the background service work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            command.Execute();
            timer.Start();  // re-enable the timer.
        }

        protected override void OnStop()
        {
            timer.Stop();
            strategy.Stop();
        }

        #endregion
    }
}


