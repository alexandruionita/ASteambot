﻿using ASteambot.Networking.Webinterface.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ASteambot.Networking.Webinterface
{
    public class HTTPServer
    {
        private int port;
        private string ip;
        private TcpListener listener;
        private HttpProcessor processor;
        private bool isactive = true;

        public HTTPServer(int port)
        {
            this.ip = new System.Net.WebClient().DownloadString("https://api.ipify.org").Replace("\n", "");
            this.port = port;
            this.processor = new HttpProcessor(ip, port);
        }

        public void Listen()
        {
            this.listener = new TcpListener(IPAddress.Any, this.port);
            this.listener.Start();

            Thread thread = new Thread(() =>
            {
                while (this.isactive)
                {
                    try
                    {
                        TcpClient s = this.listener.AcceptTcpClient();
                        Thread t = new Thread(() =>
                        {
                            this.processor.HandleClient(s);
                        });
                        t.Start();
                        Thread.Sleep(1);
                    }
                    catch (SocketException e)
                    {
                        if ((e.SocketErrorCode != SocketError.Interrupted))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                }
            });
            thread.Start();
        }

        public void Stop()
        {
            isactive = false;
            listener.Stop();
        }
        
        public bool AddToRedirectTable(string id, string target, out string key)
        {
            return processor.AddRedirectRoute(id, target, out key);
        }
    }
}
