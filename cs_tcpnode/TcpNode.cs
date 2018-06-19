using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cs_tcpnode
{

    class TcpNode
    {
        string tag = "";
        Guid guid = Guid.NewGuid();
        TcpListener listener;
        public System.Collections.Concurrent.ConcurrentDictionary<Int64, TcpClient> inConnect = new System.Collections.Concurrent.ConcurrentDictionary<Int64, TcpClient>();
        public System.Collections.Concurrent.ConcurrentDictionary<Int64, TcpClient> outConnect = new System.Collections.Concurrent.ConcurrentDictionary<Int64, TcpClient>();
        public TcpNode()
        {
            InitEventArgs();
        }
        public void Listen(string ip, int port)
        {
            tag = "listen";
            if (this.listener != null)
            {
                throw new Exception("already in listen");
            }
            IPAddress ipAddress = IPAddress.Parse(ip);
            var endPoint = new IPEndPoint(ipAddress, port);
            listener = new TcpListener(endPoint);
            listener.Start();
            listener.BeginAcceptTcpClient(this.onListen, this);

        }
        public void onListen(IAsyncResult ar)
        {
            var tcpclient = listener.EndAcceptTcpClient(ar);
            var hash = tcpclient.Client.Handle.ToInt64();
            inConnect[hash] = tcpclient;
            listener.BeginAcceptTcpClient(this.onListen, this);
        }
        public void onConnect(IAsyncResult ar)
        {
            var c = ar.AsyncState as TcpClient;
            c.EndConnect(ar);
            var hash = c.Client.Handle.ToInt64();

            outConnect[hash] = c;
        }
        public void Connect(string ip, int port)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);
            var client = new TcpClient();
            client.BeginConnect(ipAddress, port, onConnect, client);

        }
        void InitEventArgs()
        {
            //for (var i = 0; i < 1000; i++)
            //{
            //    eventArgs.Push(new SocketAsyncEventArgs());
            //}
        }

    }
}
