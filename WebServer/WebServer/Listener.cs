using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;

namespace SilentOrbit.WebServer
{
    public class Listener
    {
        readonly HttpSessionManager manager;
        readonly IPAddress address;
        readonly int port;
        readonly TcpListener listener;

        public Listener(IPAddress address, int port, HttpSessionManager manager)
        {
            this.address = address;
            this.port = port;
            this.manager = manager;
            this.listener = new TcpListener(new IPEndPoint(address, port));
        }

        public void Run()
        {
            Console.WriteLine("HTTP listening on " + address + ", port " + port);

            listener.Start();

            while (true)
            {
                //Watchdog, TODO: move into separate thread
                /*foreach (HttpSession session in ToArray ()) {
                            if (session.WatchDog ()) {
                                Remove (session);
                            }
                        }*/

                var socket = listener.AcceptSocket();

                HttpSession ps = manager.GetSession();
                if (ps == null)
                {
                    Console.WriteLine("Session limit reached, denying");
                    socket.Close();
                    continue;
                }
                ps.StartAsync(manager, new WebStream(socket));
            }
        }

    }
}
