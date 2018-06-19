using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace cs_tcpnode
{

    class TcpNodeIOCP
    {
        string tag = "";
        Guid guid = Guid.NewGuid();
        //System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs> eventArgs = new System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs>();
        Socket listenSocket = null;
        public System.Collections.Concurrent.ConcurrentDictionary<Int64, Socket> inConnect = new System.Collections.Concurrent.ConcurrentDictionary<Int64, Socket>();
        public System.Collections.Concurrent.ConcurrentDictionary<Int64, Socket> outConnect = new System.Collections.Concurrent.ConcurrentDictionary<Int64, Socket>();
        public TcpNodeIOCP()
        {
            InitEventArgs();
        }
        public void Listen(string ip, int port)
        {
            tag = "listen";
            if (this.listenSocket != null)
            {
                throw new Exception("already in listen");
            }
            listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(ip);
            var endPoint = new IPEndPoint(ipAddress, port);
            var arg = GetListenEventArgs();
            listenSocket.Bind(endPoint);
            listenSocket.Listen(1024);
            listenSocket.AcceptAsync(arg);
        }
        public void Connect(string ip, int port)
        {
            SocketAsyncEventArgs eventArgs = null;
            Socket socket = null;

            {
                eventArgs = GetConnentEventArgs();
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                eventArgs.UserToken = socket;
            }
        

            IPAddress ipAddress = IPAddress.Parse(ip);
            var endPoint = new IPEndPoint(ipAddress, port);
            eventArgs.RemoteEndPoint = endPoint;
            socket.ConnectAsync(eventArgs);
        }
        void InitEventArgs()
        {
            //for (var i = 0; i < 1000; i++)
            //{
            //    eventArgs.Push(new SocketAsyncEventArgs());
            //}
        }

        SocketAsyncEventArgs GetListenEventArgs()
        {
            SocketAsyncEventArgs outea = null;

            outea = new SocketAsyncEventArgs();
            if (outea.Buffer == null)
            {
                byte[] buffer = new byte[1024];
                outea.SetBuffer(buffer, 0, buffer.Length);
                outea.Completed += this.onListenCompleted;
            }
            return outea;
        }
        SocketAsyncEventArgs GetConnentEventArgs()
        {
            SocketAsyncEventArgs outea = null;
            outea = new SocketAsyncEventArgs();
            if (outea.Buffer == null)
            {
                byte[] buffer = new byte[1024];
                outea.SetBuffer(buffer, 0, buffer.Length);
                outea.Completed += this.onConnectCompleted;
            }
            return outea;

        }
        private void onListenCompleted(object sender, SocketAsyncEventArgs args)
        {
            try
            {
                switch (args.LastOperation)
                {
                    case SocketAsyncOperation.Accept:
                        {
                            var hash = args.AcceptSocket.Handle.ToInt64();
                            inConnect[hash] = args.AcceptSocket;

                            args.AcceptSocket = null;
                            listenSocket.AcceptAsync(args);

                           

                        }
                        break;
                    case SocketAsyncOperation.Disconnect:
                        {
                            var hash = args.AcceptSocket.Handle.ToInt64();
                            Socket socket = null;
                            inConnect.TryRemove(hash, out socket);
                        }
                        break;
                    case SocketAsyncOperation.Receive:
                        {

                        }
                        break;
                    default:
                        {

                        }
                        break;
                }
            }
            catch(Exception err)
            {

            }
        }
        private void onConnectCompleted(object sender, SocketAsyncEventArgs args)
        {

            try
            {
                switch (args.LastOperation)
                {
                    case SocketAsyncOperation.Connect:
                        {
                            var hash = args.ConnectSocket.Handle.ToInt64();
                            outConnect[hash] = args.ConnectSocket;

                        }
                        break;
                    case SocketAsyncOperation.Disconnect:
                        {
                        }
                        break;
                    case SocketAsyncOperation.Receive:
                        {

                        }
                        break;
                    default:
                        {

                        }
                        break;
                }
            }
            catch(Exception err)
            {
                
            }
        }

    }
}
