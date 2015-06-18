using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Collections.Concurrent;

namespace yanSecure
{
    public class UdpSender
    {
		private Socket socket;
        private IPEndPoint remoteEndPoint;//ip address and port
		public static BlockingCollection<byte[]> sentDataQueue = new BlockingCollection<byte[]>();

		public UdpSender(Socket socket, IPEndPoint remoteEndPoint)
        {
			this.socket = socket;
			this.remoteEndPoint = remoteEndPoint;
        }

        public void sendData(BlockingCollection<byte[]> encryptedDataQueue)
        {
            try
            {
                var nextEncryptedData = encryptedDataQueue.Take();

				var header = System.Text.Encoding.UTF8.GetBytes(
					String.Format("{0} ", nextEncryptedData.Length));

				var headedData = new byte[nextEncryptedData.Length + header.Length];

				System.Buffer.BlockCopy (header, 0, headedData, 0, header.Length);
				System.Buffer.BlockCopy (nextEncryptedData, 0, headedData, header.Length, nextEncryptedData.Length);

//                Console.WriteLine("Sending data...");

                // Since we use udp, the sending process should be short.
//				socket.SendTo(headedData, remoteEndPoint);

				// Add it to a local queue for testing purpose
				sentDataQueue.Add (headedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}