using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RedNetwork
{
    class RedNetworkMain
    {
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Entered ip address");
                return;
            }

            // -- Login --
            Dictionary<string, string> resultDict = new Dictionary<string, string>();
            string id, pwd;
            Console.Write("ID: ");
            id = Console.ReadLine();
            Console.Write("PWD: ");
            pwd = Console.ReadLine();

            LoginClient.TryLogin(args[0], id, pwd, ref resultDict);
            Console.WriteLine($"id:{resultDict["id"]}");

            // -- TCP connection --
            // -- LobbyClient --
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            LobbyClient client = new LobbyClient(resultDict["id"], ref queue);

            client.Connect(args[0], 9000);
            Thread.Sleep(1000);

            if (!client.Connected())
            {
                Console.WriteLine("can't connect to server");
                return;
            }

            Thread messageHandleThread = new Thread(() =>
            {
                int count = 5000;

                while(count > 0)
                {
                    if (queue.TryDequeue(out string result))
                    {
                        Console.WriteLine(result);
                        count--;
                    }
                    
                    if(!client.Connected())
                    {
                        break;
                    }
                }
            });
            messageHandleThread.Start();

            int count = 5000;
            while (count-- > 0)
            {
                if (!client.Connected())
                {
                    break;
                }

                Console.Write("Press enter, activate input mode: ");
                int input = Console.Read();

                switch (input)
                {
                    case (int)ConsoleKey.Enter:
                        {
                            Console.Read();
                            Console.WriteLine("InputMode activated ..");
                            while (true)
                            {
                                Console.Write("> ");

                                string msg = Console.ReadLine();

                                if (msg.Contains("exit") || msg.Contains("quit"))
                                {
                                    break;
                                }
                                else
                                {
                                    if (msg.Contains("Lobby"))
                                    {
                                        client.RequestAllLobbies();
                                    }
                                    else if (msg.Length > 0)
                                    {
                                        client.ChattingAll(msg);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }

            messageHandleThread.Join();

            // -- UDP connection --
            //UdpConnection conn = new UdpConnection();
            //conn.Update(1000 / 24);
            //try
            //{
            //	UdpClient udpClient = new UdpClient("127.0.0.1", 12190);
            //	//udpClient.Connect();
            //	Socket socket = udpClient.Client;
            //	if (socket.Connected)
            //	{
            //		int sent = socket.Send(Encoding.ASCII.GetBytes("Hello this is C# client!"));
            //		Console.WriteLine("Message sent " + sent);
            //		byte[] recv = new byte[128];
            //		socket.Receive(recv);
            //		Console.WriteLine("Message recv " + Encoding.ASCII.GetString(recv));
            //	}
            //}
            //catch(Exception e)
            //{
            //	Console.WriteLine(e);
            //}

        }
    }

}
