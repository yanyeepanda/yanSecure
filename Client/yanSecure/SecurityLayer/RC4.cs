using System;

namespace yanSecure
{
	public class RC4 : Cryptographist
	{
		private byte[] key;

		public RC4 (byte[] key)
		{
			this.key = key;
		}

		override public byte[] Encrypt (byte[] plainText)
		{
			return RC4Lib.RC4 (plainText, key);
		}

		override public byte[] Decrypt(byte[] cipherText)
		{
			// return cipherText;
			return RC4Lib.RC4 (cipherText, key);
		}
	}
}

