using System;
using System.Security.Cryptography;

namespace yanSecure
{
	public class VAes : Cryptographist
	{
		private byte[] key;
		private CipherMode mode;


		public VAes (byte[] key, CipherMode mode)
		{
			this.key = key;
			this.mode = mode;
		}

		override public byte[] Encrypt (byte[] plainText) 
		{
			var iv = getIV (AesLib.IVSize);

			var encryptedData = AesLib.EncryptToBytes (plainText, key, iv, mode);

			Console.WriteLine (iv.Length);

			return BytesHelper.ConcatBytesWithSeperator (iv, encryptedData, "\0");
		}
			
		override public byte[] Decrypt (byte[] cipherTextWithIV) 
		{
			// Get the iv out of the data first
			var tuple = BytesHelper.SeperateBytesbySep (cipherTextWithIV, "\0");

			// Decrypt the data with the iv
			var iv = tuple.Item1;

			return AesLib.DecryptFromBytes (tuple.Item2, key, iv, mode);
			// return tuple.Item2;
		}
	}
}

