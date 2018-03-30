using Dbarone.Core;
using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbarone.Service
{
    /// <summary>
    /// Provides functionality to run a TcpServer application.
    /// This server can be accessed by the standard Dbarone
    /// Client program.
    /// </summary>
    class TcpServer
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private List<Type> commands = new List<Type>();
        private IContainer Container = null;
        private string Name;

        public TcpServer(string name, int port, IContainer container = null)
        {
            this.Name = name;
            this.Container = container;

            // Cache the service commands available
            foreach (var type in AppDomain.CurrentDomain.GetTypesImplementing(typeof(AbstractServiceCommand)).Values)
                commands.Add(type);

            this.tcpListener = new TcpListener(IPAddress.Any, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        /// <summary>
        /// Processes the entire request / response communication using
        /// a NegotiateStream object which enables secure client/server
        /// communication.
        /// </summary>
        /// <param name="client"></param>
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            // Create the NegotiateStream.
            NegotiateStream authStream = new NegotiateStream(clientStream, false);
            authStream.AuthenticateAsServer();

            // Get properties of the authenticated client.
            // This will be used to provide authorisation
            // in a later version.
            IIdentity id = authStream.RemoteIdentity;

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = authStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                // Use BinaryFormatter to get exact copy of string[] variable from client
                MemoryStream ms = new MemoryStream(message);
                BinaryFormatter bf = new BinaryFormatter();
                var args = (string[])bf.Deserialize(ms);

                // Process message
                var result = Execute(args);

                // send back result
                ASCIIEncoding encoder = new ASCIIEncoding();
                byte[] buffer = encoder.GetBytes((string)result);
                authStream.Write(buffer, 0, buffer.Length);
                authStream.Flush();
            }
            tcpClient.Close();
        }

        private string Execute(string[] args)
        {
            try
            {
                // default to help if no args passed.
                if (args.Length == 0)
                    args = new string[] { "help" };

                // Command always the first argument.
                var commandStr = args[0];

                // Remaining elemenets are arguments for the command.
                var arguments = args.Splice(1);

                Type type = commands.FirstOrDefault(t => t.Name.Equals(string.Format("{0}Command", commandStr), StringComparison.OrdinalIgnoreCase));
                if (type == null)
                    throw new Exception(string.Format("Command {0} does not exist.", commandStr));

                AbstractServiceCommand command = (AbstractServiceCommand)Activator.CreateInstance(type);
                command.Container = this.Container;
                OptionHydrator.Hydrate(arguments, command);
                return command.Execute();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
