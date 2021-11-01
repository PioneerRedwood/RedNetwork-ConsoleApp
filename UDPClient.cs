using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RedNetwork
{
    class UdpConnection
    {
        static string GetString(byte[] bytes, int size)
        {
			return Encoding.ASCII.GetString(bytes, 0, size);
			//return Encoding.UTF8.GetString(bytes, 0, size);
		}

        static byte[] GetBytes(string str)
        {
			return Encoding.ASCII.GetBytes(str);
			//return Encoding.UTF8.GetBytes(str);
		}

        struct State
        {
            public Socket socket;
            public byte[] data;
        }

        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12190);
        byte[] buffer = new byte[1024];

        State state;

        public UdpConnection()
        {
            socket.Connect("127.0.0.1", 12190);
        }

        public void Send(string msg)
        {
            try
            {
                if (socket != null)
                {
                    state = new State { socket = socket, data = GetBytes(msg) };
                    socket.BeginSendTo(state.data, 0, state.data.Length, SocketFlags.None, endpoint, SendCallback, state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        void SendCallback(IAsyncResult ar)
        {
            State s = (State)(ar.AsyncState);
            //Socket sock = (Socket)(ar.AsyncState);
            Socket sock = s.socket;
            int sent = sock.EndSendTo(ar);

            Console.WriteLine("sent " + GetString(s.data, sent) + ": " + sent + "bytes");
        }

        public void Receive()
        {
            try
            {
                if (socket != null)
                {
                    socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback, socket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        void ReceiveCallback(IAsyncResult ar)
        {
            Socket sock = (Socket)(ar.AsyncState);
            int recv = sock.EndReceiveFrom(ar, ref endpoint);

            if (recv > 0)
            {
                Console.WriteLine("received " + GetString(buffer, recv) + ": " + recv + "bytes");
            }
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref endpoint, ReceiveCallback, socket);
        }

        // 유니티에서 쓰지 말것.
        public void Update(int delay)
        {
            Console.WriteLine("Update " + delay + " millisec");
            while (true)
            {
                Send("Hello world");
                Receive();

                Thread.Sleep(delay);
            }
        }
    }
}
