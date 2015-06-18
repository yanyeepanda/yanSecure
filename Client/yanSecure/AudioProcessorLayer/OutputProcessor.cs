using System;
using System.Threading;

namespace yanSecure
{
	public class OutputProcessor
	{
		public OutputProcessor ()
		{
		}

		public byte[] processData (byte[] data) {
//			Console.WriteLine("Output - Processing Data...");

			// Processing data should be relatively fast.
			Thread.Sleep (10);

			return data;
		}
	}
}

