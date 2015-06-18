using System;
using System.Security.Cryptography;

namespace yanSecure
{
	public abstract class Cryptographist
	{
		private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

		public Cryptographist ()
		{
		}

		public abstract byte[] Encrypt (byte[] plainText);

		public abstract byte[] Decrypt (byte[] cipherText);

		protected byte[] getIV (int size)
		{
			var iv = new byte[size];

			rng.GetNonZeroBytes (iv);
			// Generate a random iv to use in this turn.
//			while (iv.Length < size) 
//			{
//				Console.WriteLine ("SIZE SMALL");
//				rng.GetBytes (iv);
//			}

			return iv;
		}
	}
}

