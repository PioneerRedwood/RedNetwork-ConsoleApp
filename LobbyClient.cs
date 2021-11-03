﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace RedNetwork
{
	class LobbyClient
	{
		uint networkId = uint.MaxValue;
		Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		ConcurrentQueue<string> queue;
		
		const int BufferSize = 2048;
		byte[] writeBuffer = new byte[BufferSize];
		byte[] readBuffer = new byte[BufferSize];
		bool isStarted = false;
		
		List<Lobby> lobbies = new List<Lobby>();
        //Stopwatch watch = new Stopwatch();
        bool isLobbiesInit = false;
		public bool isInLobby = false;

		string tempName = "Redwood";

        enum MsgType : uint
		{
			// | 4bytes| 4bytes|--  ContentSize --|
			// |MsgType|MsgSize|--- MsgContent ---|
			// 만약 메시지 내용이 없는 메시지라고 해도
			// 메시지의 크기를 전송해야 함!; bodyless msg

			// networking core msg ~ 1000
			HEARTBEAT,

			ACCEPT_CONNECT,
			SESSION_DISCONNECT,
			CONNECTION_USER_INFO,

			// lobby ~ 2000
			LOBBY_INFO,
			ALL_LOBBY_INFO,

			JOIN_LOBBY,
			JOIN_LOBBY_OK,
			JOIN_LOBBY_FAIL_REJECTED,
			JOIN_LOBBY_FAIL_NOSPACE,
			JOIN_LOBBY_FAIL_NOTEXSISTS,
			JOIN_LOBBY_FAIL_ALREADYIN,

			NEW_JOINED_LOBBY,

			// lobby chatting stuff ~ 3000
			CHAT_ALL,
			CHAT_GROUP,
			CHAT_SPECIFIC,
		}

		struct Lobby
        {
			public uint idx;
			public uint current;
			public uint max;

			public Lobby(uint idx, uint current, uint max)
            {
				this.idx = idx;
				this.current = current;
				this.max = max;
            }

			public void SetCurrent(uint current) { this.current = current; }
			public void SetMax(uint max) { this.max = max; }
        }
		
		public LobbyClient(ref ConcurrentQueue<string> queue)
		{
			this.queue = queue;
		}

		public bool Connected()
		{
			return client.Connected;
		}

		public bool Connect(string address, int port)
		{
			try
			{
				bool result = false;
				if (client != null)
				{
					client.BeginConnect(IPAddress.Parse(address), port, new AsyncCallback(
						(IAsyncResult ar) =>
						{
							Socket client = (Socket)ar.AsyncState;
							client.EndConnect(ar);
							if (client.Connected)
							{
                                //Debug.Log("Connected, start receiving");
                                Console.WriteLine("Connected, start receiving");
                                result = true;
								isStarted = true;

								ReceiveHeader();
							}
							else
							{
								//Debug.Log("Not connected .. exit");
								result = false;
							}
						}), client);
				}
				else
				{
					//Debug.Log("client is null");
				}

				return result;
			}
			catch (Exception)
			{
				//Debug.Log(e);
				return false;
			}
		}

		private void ReceiveHeader()
		{
			try
			{
				if (client != null && client.Connected)
				{
					// 메시지의 헤더를 읽는다. 헤더의 크기는 항상 8바이트 - 타입; 4바이트, 메시지 크기; 4바이트
					client.BeginReceive(readBuffer, 0, sizeof(uint), SocketFlags.None, ReceiveBody, client);
				}
				else
				{
					isStarted = false;
				}
			}
			catch (Exception)
			{
				isStarted = false;
				return;
			}
		}

		private void ReceiveBody(IAsyncResult ar)
		{
			try
			{
				if (client != null && client.Connected)
				{
					// 받은 패킷 처리, bytes가 의미가 있을까
					int bytes = client.EndReceive(ar);

					MsgType type = (MsgType)BitConverter.ToUInt32(readBuffer, 0);
					switch (type)
					{
						case MsgType.ACCEPT_CONNECT:
                            {
								//queue.Enqueue("CONNECTION ACCEPTED!");
								client.BeginReceive(readBuffer, 0, sizeof(uint), SocketFlags.None, new AsyncCallback(
									(IAsyncResult ar) =>
									{
										int bytes = client.EndReceive(ar);
										if (bytes > 0)
										{
											Console.WriteLine($"Session ID: {BitConverter.ToUInt32(readBuffer, 0)}");
											networkId = BitConverter.ToUInt32(readBuffer, 0);
										}
									}), client);
								// 로그인한 유저 정보 전송
								SendUserInfo();

								break;

							}
						case MsgType.HEARTBEAT:
							//queue.Enqueue("HEARTBEATING " + networkId);
							break;
						case MsgType.LOBBY_INFO:
							{
								client.BeginReceive(readBuffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(
								(IAsyncResult ar) =>
								{
									int bytes = client.EndReceive(ar);
									if (bytes > 0)
									{
										isInLobby = true;

										//Console.Write($"recv {bytes}");
										queue.Enqueue(ParsingLobbyFromString(Encoding.Default.GetString(readBuffer, 0, bytes)));
									}
								}), client);
								break;
							}
						case MsgType.ALL_LOBBY_INFO:
                            {
								client.BeginReceive(readBuffer, 0, BufferSize, SocketFlags.None, new AsyncCallback(
								(IAsyncResult ar) =>
								{
									int bytes = client.EndReceive(ar);
									if (bytes > 0)
									{
										//Console.Write($"recv {bytes}");
										queue.Enqueue(ParsingLobbyFromString(Encoding.Default.GetString(readBuffer, 0, bytes)));
									}
								}), client);
								break;
							}
						case MsgType.JOIN_LOBBY_OK:
                            {
								queue.Enqueue("Join lobby success..!");
                                client.BeginReceive(readBuffer, 0, sizeof(uint), SocketFlags.None, new AsyncCallback(
                                    (IAsyncResult ar) =>
                                    {
                                        int bytes = client.EndReceive(ar);
										RequestLobbyByIndex(BitConverter.ToUInt32(readBuffer));
                                    }), client);
                                break;
							}
						case MsgType.JOIN_LOBBY_FAIL_REJECTED:
                            {
								// 상세한 이유는 추후 추가할 예정
								queue.Enqueue("Join lobby failed.. [rejected]");
								break;

							}
						case MsgType.JOIN_LOBBY_FAIL_NOSPACE:
                            {
								// 상세한 이유는 추후 추가할 예정
								queue.Enqueue("Join lobby failed.. [no space]");
								break;

							}
						case MsgType.JOIN_LOBBY_FAIL_NOTEXSISTS:
                            {
								// 상세한 이유는 추후 추가할 예정
								queue.Enqueue("Join lobby failed.. [not exsists]");
								break;

							}
						case MsgType.JOIN_LOBBY_FAIL_ALREADYIN:
							{
								// 상세한 이유는 추후 추가할 예정
								queue.Enqueue("Join lobby failed.. [already in]");
								break;

							}

						default:
							break;
					}
					ReceiveHeader();
				}
				else
				{
					//Debug.Log("client is not connected or NetworkStream is not readable");
					isStarted = false;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		// 주어진 문자열로부터 로비 정보를 파싱
		private string ParsingLobbyFromString(string data)
        {
			StringBuilder sb = new StringBuilder();			
			const char lobbyDelim = '|';
			const char indexDelim = ':';
			const char countDelim = '/';

			// 로비당 접속 20이 가능한 15개 로비(전부 비어있는 상태)를 요청했을 때 응답으로 110바이트가 소모
			// 0:0/10|1:0/10|2:0/10|3:0/10|4:0/10|5:0/10|6:0/10|7:0/10|8:0/10|9:0/10|10:0/10|
			// 101:245/1500|
			string[] splitedData = data.Split(lobbyDelim);
			foreach (string lobby in splitedData)
            {
				if (lobby.Length >= 5)
                {
					string[] lobby2 = lobby.Split(indexDelim);

					uint idx = (uint)int.Parse(lobby2[0]);
					uint current = (uint)int.Parse(lobby2[1].Split(countDelim)[0]);
					uint max = (uint)int.Parse(lobby2[1].Split(countDelim)[1]);

					//Console.WriteLine($"{lobbies.Count} #{idx} lobby info current: {current} max: {max}");
                    //sb.AppendLine($"#{idx} lobby info current: {current} max: {max}");

					if(!isLobbiesInit)
                    {
						lobbies.Add(new Lobby(idx, current, max));
					}
                    else
                    {
						lobbies[(int)idx].SetCurrent(current);
						lobbies[(int)idx].SetMax(max);
					}
				}
			}

			isLobbiesInit = true;
			return sb.ToString();
		}

		// 문자열 서버에 전송 -- deprecated
		public void Send(string msg)
		{
			try
			{
				if (client != null && client.Connected)
				{
					Buffer.BlockCopy(Encoding.Default.GetBytes(msg), 0, writeBuffer, 0, msg.Length);
					Send(0, msg.Length);
				}
				else
				{
					isStarted = false;
				}
			}
			catch (Exception)
			{
				isStarted = false;
				return;
			}
		}

		// 시작 인덱스와 크기를 인자로 받아서 writeBuffer에 있는 데이터를 전송
		private void Send(int startIndex, int size)
        {
			client.BeginSend(writeBuffer, startIndex, size, SocketFlags.None, new AsyncCallback((IAsyncResult ar) => 
			{
				int bytes = client.EndSend(ar);
				if(bytes == size)
                {
					// 전송성공
					//Console.WriteLine($"sent {size}");
				}
                else
                {
					// 전송 실패
					Console.WriteLine("send failed");
				}
			}), client);
        }

		// 핑 전송 - HEARTBEAT - bodyless
		public void Ping()
		{
			int size = 0;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)MsgType.HEARTBEAT), 0, writeBuffer, 0, sizeof(uint));
			size += sizeof(uint);

			Buffer.BlockCopy(BitConverter.GetBytes(0), 0, writeBuffer, sizeof(uint), sizeof(uint));
			size += sizeof(uint);

			Send(0, size);
		}

		// 유저 정보 전송
		public void SendUserInfo()
        {
			int size = 0;
			// 메시지 헤더
			Buffer.BlockCopy(BitConverter.GetBytes((uint)MsgType.CONNECTION_USER_INFO), 0, writeBuffer, 0, sizeof(uint));
			size += sizeof(uint);

			Buffer.BlockCopy(BitConverter.GetBytes(sizeof(uint) + sizeof(int) + tempName.Length), 0, writeBuffer, size, sizeof(int));
			size += sizeof(int);

			// 메시지 바디
			// 아이디
			Buffer.BlockCopy(BitConverter.GetBytes(networkId), 0, writeBuffer, size, sizeof(uint));
			size += sizeof(uint);

			// 이름 크기
			Buffer.BlockCopy(BitConverter.GetBytes(tempName.Length), 0, writeBuffer, size, sizeof(int));
			size += sizeof(int);

			// 이름
			Buffer.BlockCopy(Encoding.Default.GetBytes(tempName), 0, writeBuffer, size, tempName.Length);
			size += tempName.Length;

			// 계정 정보 등등
			Send(0, size);
		}

		// 전체 로비 정보 서버에 요청, 로비 정보 문자열 수신
		public void RequestAllLobbies()
		{
			int size = 0;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)MsgType.ALL_LOBBY_INFO), 0, writeBuffer, 0, sizeof(uint));
			size += sizeof(uint);

			// 만약 헤더만 있어도 된다면 MsgSize는 0이 되고 이는 보내야 함
			Buffer.BlockCopy(BitConverter.GetBytes(0), 0, writeBuffer, sizeof(uint), sizeof(uint));
			size += sizeof(uint);

			Send(0, size);
		}

		// 로비 접속 요청
		public void EnterLobby(uint idx)
        {
			int size = 0;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)MsgType.JOIN_LOBBY), 0, writeBuffer, size, sizeof(MsgType));
			size += sizeof(uint);
			
			Buffer.BlockCopy(BitConverter.GetBytes(sizeof(int)), 0, writeBuffer, size, sizeof(int));
			size += sizeof(int);

			Buffer.BlockCopy(BitConverter.GetBytes(idx), 0, writeBuffer, size, sizeof(uint));
			size += sizeof(uint);

			Send(0, size);
		}

		// 접속한 로비 정보 가져오기
		public void RequestLobbyByIndex(uint idx)
        {
			// 헤더
			int size = 0;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)MsgType.LOBBY_INFO), 0, writeBuffer, size, sizeof(MsgType));
			size += sizeof(uint);

			// 메시지 크기
			Buffer.BlockCopy(BitConverter.GetBytes(sizeof(int)), 0, writeBuffer, size, sizeof(int));
			size += sizeof(int);

			// 로비 인덱스
			Buffer.BlockCopy(BitConverter.GetBytes(idx), 0, writeBuffer, size, sizeof(int));
			size += sizeof(int);

			Send(0, size);
		}

		// 전체 채팅
		public void ChattingAll(string content)
        {

        }

		// 그룹 채팅
		public void ChattingGroup(string content)
        {

        }

		// 개인 채팅
		public void ChattingPrivate(string content)
        {

        }
	}
}