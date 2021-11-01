using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace RedNetwork
{
    class RedNetworkMain
    {
        static void Main(string[] args)
        {
			// -- Login --
			//Dictionary<string, string> resultDict = new Dictionary<string, string>();
			//LoginClient.TryLogin("0", "1234", ref resultDict);
			//Console.WriteLine($"id:{resultDict["id"]}");

			// -- TCP connection --
			ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
			LobbyClient client = new LobbyClient(ref queue);
			client.Connect("127.0.0.1", 9000);

			while (true)
			{
				if(client.Connected())
				{
					client.Ping();
					Thread.Sleep(50);
					client.RequestLobbies();
					Thread.Sleep(50);

				}
				else
				{
					client.Connect("127.0.0.1", 9000);
					Thread.Sleep(2000);
				}

				if (queue.TryDequeue(out string result))
				{
					Console.WriteLine(result);
				}
			}

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
