using Dbarone.Command;
using Dbarone.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dbarone.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Write(ProcessMessage(args));
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        static string ProcessMessage(string[] args)
        {
            // If no arguments supplied, default to show help.
            if (args.Count()==0)
                args = new string[]{ "help"};

            // Look to see what local / library commands there are.
            var command = ArgsCommand.Create(args);

            // Generally, the only commands processed locally by the
            // cli are 'api' and 'script'. The API command sets the
            // service end point, and the SCRIPT command runs a
            // script of commands. All other commands are processed
            // on the server
            if (args[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                return command.Execute();
            }
            else if (args[0].Equals("script", StringComparison.OrdinalIgnoreCase))
            {
                var script = command.Execute();
                var lines = Regex.Split(script, "\r\n|\r|\n");
                string output = "";
                foreach (var line in lines)
                {
                    var l = line.Trim();
                    if (l.Length>0 && l.Substring(0, 1) != "#") {
                        output += ProcessMessage(line.ParseArgs()) + Environment.NewLine;
                    }
                }
                return output;
            }
            else
            {
                string result = string.Empty;
                string host = (string)Properties.Settings.Default["host"];
                int port = (int)Properties.Settings.Default["port"];

                // must be processed on server.
                TcpClient client = new TcpClient();
                //client.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                // Parse the host value. Can be host name or IPv4 address.
                IPAddress addr = null;
                if (!IPAddress.TryParse(host, out addr))
                {
                    // if failed, try dns lookup
                    var hostEntry = Dns.GetHostEntry(host);
                    foreach (var item in hostEntry.AddressList)
                    {
                        // Get the first IPv4 address.
                        if (item.AddressFamily == AddressFamily.InterNetwork)
                        {
                            addr = item;
                            break;
                        }
                    }
                    if (addr == null)
                        throw new Exception("Invalid host.");
                }

                IPEndPoint serverEndPoint = new IPEndPoint(addr, port);
                client.Connect(serverEndPoint);

                // Ensure the client does not close when there is 
                // still data to be sent to the server.
                client.LingerState = (new LingerOption(true, 0));

                // Request authentication
                NetworkStream clientStream = client.GetStream();
                NegotiateStream authStream = new NegotiateStream(clientStream, false);
                // Pass the NegotiateStream as the AsyncState object 
                // so that it is available to the callback delegate.
                authStream.AuthenticateAsClient();

                // Convert client arguments to a byte array
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream ms = new MemoryStream();
                bf.Serialize(ms, args);
                byte[] buffer = new byte[ms.Length];
                buffer = ms.ToArray();

                // Send a message to the server.
                // Encode the test data into a byte array.
                authStream.Write(buffer, 0, buffer.Length);

                // get the response
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

                    ASCIIEncoding encoder = new ASCIIEncoding();
                    result += encoder.GetString(message, 0, bytesRead);
                    if (bytesRead < 4096)
                        break;
                }

                // Close the client connection.
                authStream.Close();

                return result;
            }
        }
    }
}
