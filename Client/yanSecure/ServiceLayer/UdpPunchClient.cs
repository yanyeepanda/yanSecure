using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace UdpPunchClient
{
	class ServerInterface
	{
		string host;
		int    port;
		string pool;

		public delegate void ReceivedConnectionDelegate(IPAddress ipAddress, int port, Socket socket);
		public event ReceivedConnectionDelegate ReceivedConnection;

		public ServerInterface(string host, int port, string pool)
		{
			this.host = host;
			this.port = port;
			this.pool = pool;
		}

		private String bytesToString(byte[] bytes) {
			return System.Text.Encoding.UTF8.GetString (bytes);
		}

		private byte[] stringToBytes(String str) {
			return System.Text.Encoding.UTF8.GetBytes(str);
		}

		public void Connect() {
			byte[] toSend;
			byte[] buffer;
			String received;

			IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), port);
			Socket sendingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			// SEND request to connect to pool
			toSend = stringToBytes (pool);
			sendingSocket.SendTo(toSend, endPoint);

			Console.WriteLine ("send over");

			// RECEIVE acknowledgement
			buffer = new byte[pool.Length + 3];
			sendingSocket.Receive (buffer);
			received = bytesToString (buffer);
			Console.WriteLine (received);

			// SEND acknowledgement
			toSend = stringToBytes ("ok");
			sendingSocket.SendTo(toSend, endPoint);

			// RECEIVE target connection address
			buffer = new byte[32];
			sendingSocket.Receive (buffer);
			received = bytesToString (buffer);
			var splitIp = received.Split ('|');

			IPAddress targetAddress = IPAddress.Parse (splitIp [0]);
			int targetPort = int.Parse (splitIp[1]);

			ReceivedConnection (targetAddress, targetPort, sendingSocket);
		}

	}
}
