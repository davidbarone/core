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

namespace Dbarone.Service
{
    /// <summary>
    /// Base class with service logic. A Program class should inherit from this class.
    /// </summary>
    public class Service : ServiceBase
    {
        private IContainer Container { get; set; }

        private int? TcpPort { get; set; }

        List<Type> backgroundCommands = new List<Type>();

        // Set up a timer to trigger every minute.  
        System.Timers.Timer timer = new System.Timers.Timer();

        #region ServiceBase methods

        public Service(string name, int? tcpPort)
        {
            base.ServiceName = name;
            this.TcpPort = tcpPort;
            this.Container = new Container();

            // Cache the background commands available
            foreach (var type in AppDomain.CurrentDomain.GetTypesImplementing(typeof(AbstractServiceBackgroundCommand)).Values)
                backgroundCommands.Add(type);

            if (Environment.UserInteractive)
                Console.WriteLine("Service running in UserInteractive mode. Press a key to exit.");
        }

        public void Run(string[] args)
        {
            // initialise container
            OnInit(Container);

            if (!Environment.UserInteractive)
            {
                // running as service
                Run(this);

                // If Tcp port set, start Tcp server
                if (TcpPort.HasValue)
                {
                    TcpServer srv = new TcpServer(base.ServiceName, TcpPort.Value, Container);
                }

            }
            else
            {
                // Run as a console app.
                OnStart(args);

                // If Tcp port set, start Tcp server
                if (TcpPort.HasValue)
                {
                    TcpServer srv = new TcpServer(base.ServiceName, TcpPort.Value, Container);
                }

                Console.ReadKey();
                OnStop();
            }
        }

        protected override void OnStart(string[] args)
        {
            timer.Interval = 30000; // 30 seconds  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimer);
            timer.AutoReset = false; // one shot timer
            timer.Start();
        }

        protected override void OnStop()
        {
            if (!Environment.UserInteractive)
                Environment.Exit(0);
        }

        #endregion

        /// <summary>
        /// Enables client to configure the container
        /// </summary>
        /// <param name="container"></param>
        public Action<IContainer> OnInit;

        /// <summary>
        /// Timer event that runs the background service work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            foreach (var backgroundCommand in backgroundCommands)
            {
                AbstractServiceBackgroundCommand instance = (AbstractServiceBackgroundCommand)Activator.CreateInstance(backgroundCommand);
                instance.Container = this.Container;
                instance.Execute();
            }
            timer.Start();  // re-enable the timer.
        }
    }
}


