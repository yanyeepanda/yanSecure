using System;
using System.Linq;

namespace yanSecure
{
	public class BytesHelper
	{
		public BytesHelper ()
		{
		}

		public static byte[] ConcatBytes(byte[] srcl, byte[] srcr)
		{
			var dst = new byte[srcl.Length + srcr.Length];

			System.Buffer.BlockCopy (srcl, 0, dst, 0, srcl.Length);
			System.Buffer.BlockCopy (srcr, 0, dst, srcl.Length, srcr.Length);


			return dst;
		}

		public static byte[] ConcatBytesWithSeperator(byte[] srcl, byte[] srcr, string sep)
		{
			// Get the bytes
			var sepb = System.Text.Encoding.UTF8.GetBytes (sep);

			return ConcatBytesWithSeperator (srcl, srcr, sepb);
		}

		public static byte[] ConcatBytesWithSeperator(byte[] srcl, byte[] srcr, byte[] sepb)
		{
			return ConcatBytes (ConcatBytes (srcl, sepb), srcr);
		}

		public static Tuple<byte[], byte[]> SeperateBytesbySep (byte[] src, byte[] sepb)
		{
			int i = 0;
			for (; i <= src.Length - sepb.Length; i++) {
				if (sepb.SequenceEqual(src.Skip(i).Take(sepb.Length).ToArray()))
					break;
			}

			var dstl = new byte[i];
			var dstr = new byte[src.Length - i - 1];

			System.Buffer.BlockCopy (src, 0, dstl, 0, dstl.Length);
			System.Buffer.BlockCopy (src, dstl.Length + 1, dstr, 0, dstr.Length);

			return new Tuple<byte[], byte[]>(dstl, dstr);
		}

		public static Tuple<byte[], byte[]> SeperateBytesbySep (byte[] src, string sep)
		{
			// Get the bytes
			var sepb = System.Text.Encoding.UTF8.GetBytes (sep);

			return SeperateBytesbySep (src, sepb);
		}

		public static byte[] RemoveHeader (byte[] src, byte[] sepb)
		{
			var tuple = SeperateBytesbySep (src, sepb);
			var hearderSize = int.Parse(System.Text.Encoding.UTF8.GetString(tuple.Item1));
			var headlessData = new byte[hearderSize];

			System.Buffer.BlockCopy (tuple.Item2, 0, headlessData, 0, headlessData.Length);

			return headlessData;
		}
	}
}

