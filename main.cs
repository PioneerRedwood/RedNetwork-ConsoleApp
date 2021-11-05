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
        public enum ChattingMode : uint
        {
            ALL,
            LOBBY,
            SPECIFIC
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Entered ip address");
                //return;
                args = new string[1];
                args[0] = "127.0.0.1";
            }


            // -- Login --

            Dictionary<string, string> resultDict = new Dictionary<string, string>();
            resultDict.Add("id", "redwood");
            /*
            string id, pwd;

            bool isLoginSuccess = false;

            do
            {
                Console.Write("ID: ");
                id = Console.ReadLine();
                Console.Write("PWD: ");
                pwd = Console.ReadLine();

                isLoginSuccess = LoginClient.TryLogin(args[0], id, pwd, ref resultDict);
                if(!isLoginSuccess)
                {
                    Console.WriteLine("Failed to Login");
                }
            } while (!isLoginSuccess);
            */
            // -- end Login --

            // -- TCP connection --
            // -- Lobby --
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

                while (count > 0)
                {
                    if (queue.TryDequeue(out string result))
                    {
                        Console.WriteLine(result);
                        count--;
                    }

                    if (!client.Connected())
                    {
                        break;
                    }
                }
            });
            
            {
                // 2021-11-05 입력 스레드 만들어봤는데 원하는대로 작동하지 않아서 폐기
                /*Thread inputTread = new Thread(() =>
                {
                    uint modeIdx = 1;
                    int count = 5000;
                    while (count-- > 0)
                    {
                        if (!client.Connected())
                        {
                            break;
                        }
                        bool isinputActivated;
                        string msg;
                        while (true)
                        {
                            msg = "";
                            isinputActivated = true;
                            //Console.Write(mode.ToString().Trim() + "> ");
                            ConsoleKeyInfo keyinfo = Console.ReadKey();

                            switch (keyinfo.Key)
                            {
                                case ConsoleKey.Enter:
                                    {
                                        Console.WriteLine();
                                        isinputActivated = false;
                                        break;
                                    }
                                case ConsoleKey.Tab:
                                    {
                                        modeIdx++;
                                        mode = (ChattingMode)(modeIdx %= 3);

                                        ClearCurrentConsoleLine();
                                        Console.Write(mode.ToString().Trim() + "> ");
                                        break;
                                    }
                                case ConsoleKey.Backspace:
                                    {
                                        if ((Console.CursorLeft < 5 && mode == ChattingMode.ALL) ||
                                        (Console.CursorLeft < 7 && mode == ChattingMode.LOBBY) ||
                                        (Console.CursorLeft < 10 && mode == ChattingMode.SPECIFIC))
                                        {
                                            Console.Write(" ");
                                            continue;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        msg += keyinfo.KeyChar;
                                        break;
                                    }
                            }

                            if(!isinputActivated)
                            {
                                break;
                            }
                        }

                        if (msg.Contains("exit") || msg.Contains("quit"))
                        {
                            break;
                        }
                        else
                        {
                            // search all lobby
                            if (msg.Equals("sal"))
                            {
                                client.RequestAllLobbies();
                            }
                            // sl
                            else if (msg.Contains("sl"))
                            {
                                client.RequestLobbyByIndex((uint)int.Parse(msg.Split(' ')[1]));
                            }
                            // jl 1
                            else if (msg.Contains("jl"))
                            {
                                client.JoinLobby((uint)int.Parse(msg.Split(' ')[1]));
                            }
                            else
                            {
                                client.ChattingAll(msg);
                            }
                        }
                    }
                });
                inputTread.Start();

                inputTread.Join();
                messageHandleThread.Join();
                */
            }
            Thread thr1 = new Thread(() =>
            {
                bool isExitPressed = false;
                while (!isExitPressed)
                {
                    Console.WriteLine("Press Key\n[M: show user info], [F: search lobbies], [J: join lobby]\n" +
                    "[C: chatting all], [L: chatting lobby(you must be in specific lobby)], [W: whisper someone]\n" +
                    "[Q: exit program]");
                    ConsoleKeyInfo key = Console.ReadKey();
                    Console.WriteLine();
                    switch (key.Key)
                    {
                        case ConsoleKey.M:
                            {
                                Console.WriteLine($"Player info id: {resultDict["id"]}");
                                break;
                            }
                        case ConsoleKey.F:
                            {
                                client.RequestAllLobbies();
                                break;
                            }
                        case ConsoleKey.J:
                            {
                                if(!client.isLobbiesInit)
                                {
                                    Console.WriteLine("You must update lobby first");
                                    break;
                                }

                                foreach (Lobby lobby in client.GetUpdatedLobbies())
                                {
                                    Console.Write(lobby.idx + " ");
                                }

                                int num = -1;
                                Console.Write("\nnumber to join: ");
                                try
                                {
                                    num = int.Parse(Console.ReadLine());
                                }
                                catch
                                {
                                    Console.WriteLine($"please enter the range of lobbies");
                                    break;
                                }

                                //num = Console.ReadKey() - '0';
                                Console.WriteLine(num);

                                if (client.CanJoinLobby(num))
                                {
                                    client.JoinLobby((uint)num);
                                }
                                else
                                {
                                    Console.WriteLine($"can't join {num} lobby");
                                }
                                break;
                            }
                        case ConsoleKey.C:
                            {
                                Console.Write("> ");
                                client.ChattingAll(Console.ReadLine());

                                break;
                            }
                        case ConsoleKey.L:
                            {
                                if (client.isInLobby)
                                {
                                    Console.Write("> ");
                                    client.ChattingLobby(Console.ReadLine());
                                }
                                else
                                {
                                    Console.WriteLine("You must be in specific lobby");
                                }
                                break;
                            }
                        case ConsoleKey.W:
                            {

                                Console.WriteLine("can't do this yet");
                                // P
                                //Console.Write("select to send: ");


                                //Console.Write("> ");
                                //client.ChattingPrivate(Console.ReadLine());

                                break;
                            }
                        case ConsoleKey.Q:
                            {
                                isExitPressed = true;
                                client.Disconnect();
                                break;
                            }
                    }
                }


            });

            messageHandleThread.Start();
            thr1.Start();
            thr1.Join();

            messageHandleThread.Join();

            // -- end Lobby --

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
