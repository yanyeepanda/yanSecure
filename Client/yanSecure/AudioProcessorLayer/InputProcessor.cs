using System;
using System.Threading;

namespace yanSecure
{
	public class InputProcessor
	{
		public InputProcessor ()
		{
		}

		public byte[] processData (byte[] data) {
//			Console.WriteLine("Input - Processing Data...");

			// Processing data should be relatively fast.
			Thread.Sleep (10);

			return data;
		}
	}
}

