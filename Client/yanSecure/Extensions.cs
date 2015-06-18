using System;

namespace Rabbit
{
	public static class Extensions
	{
		public static byte[] ToBytes(this String str)
		{
			return System.Text.Encoding.UTF8.GetBytes (str);
		}

		public static String FromBytes(this String str, byte[] bytes)
		{
			return System.Text.Encoding.UTF8.GetString (bytes);
		}


	}
}

