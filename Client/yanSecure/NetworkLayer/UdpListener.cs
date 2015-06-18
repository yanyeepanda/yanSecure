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
    public class UdpListener
    {
		private Socket socket;

		public UdpListener(Socket socket)
        {
			this.socket = socket;
        }
        public void receiveData(BlockingCollection<byte[]> receivedDataQueue)
        {
			// Get the data from udp socket. Use maximum buffer to
			// store the data and abstract the real data by using the
			// length of it.
			/*
			byte[] nextReceivedData = new byte[64 * 1024];

			socket.Receive(nextReceivedData);
			*/

			// Get the data directly from the local queue for test purpose
			var nextReceivedData = UdpSender.sentDataQueue.Take ();

			int i = 0;

			for (; i < nextReceivedData.Length; i++) {
				if (nextReceivedData [i] == ' ')
					break;
			}

			var header = new byte[i];

			System.Buffer.BlockCopy (nextReceivedData, 0, header, 0, header.Length);

			var bufferSize = int.Parse(System.Text.Encoding.UTF8.GetString (header));

			var headlessData = new byte[bufferSize];

			System.Buffer.BlockCopy (nextReceivedData, i + 1, headlessData, 0, headlessData.Length);

//            Console.WriteLine("Receiving data...");

			receivedDataQueue.Add(headlessData);
        }
    }
}