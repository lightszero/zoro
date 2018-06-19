using System;

namespace cs_tcpnode
{
    class Program
    {
        static TcpNode serverNode = new TcpNode();
        static TcpNode clientNode = new TcpNode();
        static void InitThread()
        {
            System.Threading.Thread t = new System.Threading.Thread(() =>
             {
                 while (true)
                 {
                     System.Threading.Thread.Sleep(1000);
                     var _in = serverNode.inConnect.Count;
                     Console.Write("connect=" + _in);

                     var _out = clientNode.outConnect.Count;
                     Console.WriteLine(" linked=" + _out);

                     foreach(var c in clientNode.outConnect.Values)
                     {
                         
                     }
                 }
             });
            t.Start();

        }
        static void Main(string[] args)
        {
            InitThread();
            Console.WriteLine("Hello World!");
            while (true)
            {
                var cmd = Console.ReadLine().Replace(" ", "").ToLower();
                if (cmd == "s")
                {
                    Console.WriteLine("start server");
                    serverNode.Listen("127.0.0.1", 1234);
                }
                if (cmd == "c")
                {
                    Console.WriteLine("start link 10000");
                    for (var i = 0; i < 10000; i++)
                    {
                        clientNode.Connect("127.0.0.1", 1234);
                    }
                }

            }
        }
    }
}
