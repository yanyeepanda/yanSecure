using System;
using System.Text;

namespace yanSecure
{
	class RC4Lib
	{
		public static byte[] RC4(byte[] input, byte[] key)
		{
			var result = new byte[input.Length];
			int x, y, j = 0;
			int[] box = new int[256];

			for (int i = 0; i < 256; i++)
			{
				box[i] = i;
			}

			for (int i = 0; i < 256; i++)
			{
				j = (key[i % key.Length] + box[i] + j) % 256;
				x = box[i];
				box[i] = box[j];
				box[j] = x;
			}

			for (int i = 0; i < input.Length; i++)
			{
				y = i % 256;
				j = (box[y] + j) % 256;
				x = box[y];
				box[y] = box[j];
				box[j] = x;

				result[i] = ((byte)((int)input[i] ^ box[(box[y] + box[j]) % 256]));
			}
			return result;
		}
	}
}
