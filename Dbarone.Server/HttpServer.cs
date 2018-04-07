using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbarone.Lake.Svc.Server
{
    /// <summary>
    /// The HttpServer class provides a general query interface
    /// to the data lake data. This is used primarily by end-users
    /// via BI tools like PowerQuery.
    /// 
    /// https://msdn.microsoft.com/en-us/library/system.net.httplistener(v=vs.110).aspx
    /// </summary>
    public class HttpServer
    {
        public void WorkerThread()
        {

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            /*
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");
            */

            // Create a listener.
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");

            // Add the prefixes.
            /*
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            */

            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                response.AddHeader("Content-Type", "application/json");
                // Construct a response.
                string responseString = @"[{""Customer"": ""Fred"", ""Sales"": 100}, {""Customer"": ""John"", ""Sales"": 400}, {""Customer"": ""Peter"", ""Sales"": 300}, {""Customer"": ""Simon"", ""Sales"": 200}]";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
        }

        public HttpServer()
        {
            Thread clientThread = new Thread(new ThreadStart(WorkerThread));
            clientThread.Start();
        }
    }
}
