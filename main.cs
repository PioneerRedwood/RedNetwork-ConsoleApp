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
			// -- Login --
			//Dictionary<string, string> resultDict = new Dictionary<string, string>();
			//LoginClient.TryLogin("0", "1234", ref resultDict);
			//Console.WriteLine($"id:{resultDict["id"]}");

			// -- TCP connection --
			
			// -- Lobby --
			ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
			LobbyClient client = new LobbyClient(ref queue);
			client.Connect("127.0.0.1", 9000);

			int count = 100;

			ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();

			Task.Run(() =>
			{
				Console.WriteLine("Chatting ..");
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
						chatQueue.Enqueue(msg);
					}
				}
			});

			while (count-- > 0)
            {
				if (client.Connected())
				{
                    // for TEST
                    client.Ping();
                    Thread.Sleep(500);

					if(true)
                    {
						client.RequestAllLobbies();
						Thread.Sleep(500);
						client.EnterLobby(0);
						Thread.Sleep(500);
					}

					if (chatQueue.TryDequeue(out string content))
                    {
						Console.WriteLine("Try to send " + content);
						client.ChattingAll(content);
                    }
                }

				Task.Run(() =>
				{
					// 수신 큐 처리
					while (!queue.IsEmpty)
					{
						if (queue.TryDequeue(out string result))
						{
							Console.WriteLine(result);
						}
					}
				});
			}

			/*
			List<Task> requestTasks = new List<Task>();

			for (int i = 0; i < 10; ++i)
            {
				requestTasks.Add(Task.Run(() =>
				{
					if (client.Connected())
					{
						client.Ping();
						Thread.Sleep(1000);
					}
				}));

				requestTasks.Add(Task.Run(() =>
				{
					if (client.Connected())
					{
						client.RequestLobbies();
						Thread.Sleep(1000);
					}
				}));

				requestTasks.Add(Task.Run(() =>
				{
					if (client.Connected())
					{
						client.RequestEnterLobby(0);
						Thread.Sleep(1000);
					}
				}));
			}

			Task.WaitAll(requestTasks.ToArray());

			requestTasks.Add(Task.Run(() =>
			{
				// 수신 큐 처리
				while (!queue.IsEmpty)
				{
					if (queue.TryDequeue(out string result))
					{
						Console.WriteLine(result);
					}
				}
			}));

			Task.WaitAll(requestTasks.ToArray());
			*/


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
