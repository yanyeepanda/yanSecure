using System;
using System.Security.Cryptography;

namespace yanSecure
{
	class Rabbit : Cryptographist
	{
		private byte[] key;

		public Rabbit (byte[] key)
		{
			this.key = key;
		}

		override public byte[] Encrypt (byte[] plainText)
		{
			var rabbit = new RabbitLib ();
			rabbit.Reset ();
			rabbit.setupKey (key);
			var iv = getIV (RabbitLib.IVSize);
			Console.WriteLine ("Encryption IV: {0}", String.Join(",", iv));
			rabbit.setupIV (iv);
			var encryptedData = rabbit.crypt (plainText);
			return BytesHelper.ConcatBytesWithSeperator (iv, encryptedData, "\0");
		}

		override public byte[] Decrypt(byte[] cipherText)
		{
			var rabbit = new RabbitLib ();
			rabbit.Reset ();
			rabbit.setupKey (key);
			var tuple = BytesHelper.SeperateBytesbySep (cipherText, "\0");
			var iv = tuple.Item1;
			Console.WriteLine ("Decryption IV: {0}", String.Join(",", iv));
			rabbit.setupIV (iv);
			// return tuple.Item2;
			return rabbit.crypt (tuple.Item2);
		}

		private void WriteHex(String str)
		{
			WriteHex(System.Text.Encoding.UTF8.GetBytes(str));
		}

		private void WriteHex(byte[] bytes)
		{
			foreach (var b in bytes)
				Console.Write ("{0:X2} ", b);
			Console.WriteLine ();
		}
	}
}
