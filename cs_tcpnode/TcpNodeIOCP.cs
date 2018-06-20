using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Zoro
{
    public class SocketInfo
    {
        public SocketInfo(Socket socket, bool inConnect)
        {
            this.Socket = socket;
            this.InConnect = inConnect;

        }
        public long Handle
        {
            get
            {
                return Socket.Handle.ToInt64();
            }
        }
        public Socket Socket
        {
            get;
            private set;
        }
        public bool InConnect//被动连接
        {
            get;
            private set;
        }
    }
    public class TcpNodeIOCP
    {
        System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs> freeEventArgs = new System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs>();
        Socket listenSocket = null;
        public System.Collections.Concurrent.ConcurrentDictionary<Int64, SocketInfo> Connects = new System.Collections.Concurrent.ConcurrentDictionary<Int64, SocketInfo>();
        public TcpNodeIOCP()
        {
            InitEventArgs();
        }
        public void Listen(string ip, int port)
        {
            if (this.listenSocket != null)
            {
                throw new Exception("already in listen");
            }
            listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Parse(ip);
            var endPoint = new IPEndPoint(ipAddress, port);
            var arg = GetEventArgs();
            listenSocket.Bind(endPoint);
            listenSocket.Listen(1024);
            listenSocket.AcceptAsync(arg);
        }
        public void Connect(string ip, int port)
        {
            SocketAsyncEventArgs eventArgs = null;
            Socket socket = null;

            {
                eventArgs = GetEventArgs();
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
            for (var i = 0; i < 1000; i++)
            {
                freeEventArgs.Push(new SocketAsyncEventArgs());
            }
        }

        public event Action<long> onSocketIn;//有连接进来
        public event Action<long> onSocketLinked;//我连接成了
        public event Action<Socket> onSocketLinkedError;//我连接出错
        public event Action<long,byte[]> onSocketRecv;

        public void CloseConnect(long handle)
        {
            SocketAsyncEventArgs eventArgs = GetEventArgs();
            eventArgs.UserToken = Connects[handle];
            Connects[handle].Socket.DisconnectAsync(eventArgs);
        }
        public void Send(long handle, byte[] data)
        {
            var args = GetEventArgs();
            args.UserToken = Connects[handle];
            args.SetBuffer(data, 0, data.Length);
            Connects[handle].Socket.SendAsync(args);
        }
        SocketAsyncEventArgs GetEventArgs()
        {
            SocketAsyncEventArgs outea = null;
            freeEventArgs.TryPop(out outea);
            if (outea == null)
            {
                outea = new SocketAsyncEventArgs();
                outea.Completed += this.onCompleted;
            }
            return outea;
        }
        private void SetRecivce(SocketInfo info)
        {
            var recvargs = GetEventArgs();
            if (recvargs.Buffer == null || recvargs.Buffer.Length != 1024)
            {
                byte[] buffer = new byte[1024];
                recvargs.SetBuffer(buffer, 0, 1024);
            }
            recvargs.UserToken = info;
            info.Socket.ReceiveAsync(recvargs);
        }
        private void onCompleted(object sender, SocketAsyncEventArgs args)
        {
            //try
            {
                switch (args.LastOperation)
                {
                    case SocketAsyncOperation.Accept:
                        {
                            var info = new SocketInfo(args.AcceptSocket, true);
                            Connects[info.Handle] = info;

                            //直接复用
                            args.AcceptSocket = null;
                            listenSocket.AcceptAsync(args);

                            onSocketIn?.Invoke(info.Handle);

                            SetRecivce(info);
                        }
                        break;
                    case SocketAsyncOperation.Connect:
                        {
                            if (args.SocketError != SocketError.Success)
                            {
                                onSocketLinkedError?.Invoke(args.UserToken as Socket);
                            }
                            else
                            {
                                var info = new SocketInfo(args.ConnectSocket, false);
                                Connects[info.Handle] = info;

                                onSocketLinked?.Invoke(info.Handle);

                                SetRecivce(info);
                            }
                            //connect 的这个args不能复用
                        }
                        break;
                    case SocketAsyncOperation.Disconnect:
                        {
                            var hash = (args.UserToken as SocketInfo).Handle;
                            SocketInfo socket = null;
                            Connects.TryRemove(hash, out socket);
                            socket.Socket.Dispose();

                            freeEventArgs.Push(args);//这个是可以复用的
                        }
                        break;
                    case SocketAsyncOperation.Receive:
                        {
                            var hash = (args.UserToken as SocketInfo).Handle;
                            byte[] recv = new byte[args.BytesTransferred];
                            Buffer.BlockCopy(args.Buffer, 0, recv, 0, args.BytesTransferred);

                            onSocketRecv?.Invoke(hash, recv);

                            freeEventArgs.Push(args);//这个是可以复用的
                        }
                        break;
                    default:
                        {

                        }
                        break;
                }
            }
            //catch (Exception err)
            //{

            //}
        }

    }
}
