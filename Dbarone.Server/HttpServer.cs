﻿using Dbarone.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Dbarone.Server
{
    /// <summary>
    /// Simple multi-threaded http server.
    /// 
    /// https://stackoverflow.com/questions/4672010/multi-threading-with-net-httplistener
    /// </summary>
    public class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly Thread[] _workers;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;

        public HttpServer(int maxThreads)
        {
            _workers = new Thread[maxThreads];
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _listener = new HttpListener();
            _listenerThread = new Thread(HandleRequests);

            // Specify Negotiate as the authentication scheme.
            //_listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;
            _listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;
        }

        public void Start(int port)
        {
            _listener.Prefixes.Add(String.Format(@"http://*:{0}/", port));
            _listener.Start();
            _listenerThread.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        public void Dispose()
        { Stop(); }

        public void Stop()
        {
            _stop.Set();
            _listenerThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        private void HandleRequests()
        {
            while (_listener.IsListening)
            {
                var context = _listener.BeginGetContext(ContextReady, null);

                if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker()
        {
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                HttpListenerContext context;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }

                try {

                    // Process request using impersonation
                    //var identity = (System.Security.Principal.WindowsIdentity)context.User.Identity;
                    //using (System.Security.Principal.WindowsImpersonationContext wic = identity.Impersonate())
                    //{
                        ProcessRequest(context);
                    //}
                }
                catch (Exception e) { Console.Error.WriteLine(e); }
            }
        }

        public event Action<HttpListenerContext> ProcessRequest;
    }
}
